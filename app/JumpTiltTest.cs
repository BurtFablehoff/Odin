#if ODIN_DEV
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;

namespace OdinV2
{
    // V12 2c --test-jump-tilt <out.json> [<frames-dir>]. Headless: the window is constructed, never shown.
    // Real CatMind flights in both directions at the three sizes: TryPerch from the floor up to a window top, then
    // LeavePerch down to the floor. Every tick runs the code the app runs (mind.Step, PaintSprite's clamps,
    // ChooseSprite, PlaceSprite, LayoutScene). At 10 points per flight (and the first/last crouch frame) the real
    // DrawScene renders the canvas, which is checked for clipping and for the drawn rotation.
    internal sealed partial class OdinWindow
    {
        private const double TiltApexBand=40;   // px/s of vertical motion treated as the apex (sign not asserted there)
        private const double TiltApexLimit=5,TiltStepLimit=8,TiltLimit=45;
        private sealed class TiltTick
        {
            public double Phase,T,X,FootY,Tilt,Vy,Overshoot;public float Rotation;public int Slot;public string Source="",Motion="";
            public SpriteChoice Choice;public SceneLayout Layout;
            public bool Crouch {get{return Source.Length>0 && Source.IndexOf("/run_",StringComparison.Ordinal)<0;}}
        }
        private static bool Has(int[] slots,int slot){return slots!=null && Array.IndexOf(slots,slot)>=0;}
        private static string Role(DevJumpFlight roles,int slot)
        {
            if(roles==null)return "unknown";
            if(Has(roles.preFlight,slot))return "preFlight";if(Has(roles.rising,slot))return "rising";if(Has(roles.apex,slot))return "apex";
            if(Has(roles.descending,slot))return "descending";if(Has(roles.landing,slot))return "landing";return "unknown";
        }
        // Alpha (> 64) principal-axis orientation in degrees (y down, so a clockwise screen rotation adds), and the
        // largest alpha on the canvas border (non-zero fur there means the drawing was clipped).
        private static double[] AlphaShape(Bitmap image)
        {
            BitmapData data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            try
            {
                byte[] pixels=new byte[data.Stride*image.Height];Marshal.Copy(data.Scan0,pixels,0,pixels.Length);
                double n=0,sx=0,sy=0,sxx=0,syy=0,sxy=0;int border=0;
                for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)
                {
                    int a=pixels[y*data.Stride+x*4+3];
                    if((x==0||y==0||x==image.Width-1||y==image.Height-1)&&a>border)border=a;
                    if(a>64){n++;sx+=x;sy+=y;sxx+=(double)x*x;syy+=(double)y*y;sxy+=(double)x*y;}
                }
                if(n==0)return new double[]{0,0,border};
                double cx=sx/n,cy=sy/n,mu20=sxx/n-cx*cx,mu02=syy/n-cy*cy,mu11=sxy/n-cx*cy;
                return new double[]{n,0.5*Math.Atan2(2*mu11,mu20-mu02)*180/Math.PI,border};
            }
            finally{image.UnlockBits(data);}
        }
        private Bitmap Render(SpriteChoice choice,SceneLayout layout)
        {
            DevArtFrame frame;
            Bitmap sprite=latestArt.TryGet(choice.DisplayClip,choice.DisplayFrame,out frame)&&frame!=null?frame.Image:art.Get(choice.DisplayClip,choice.DisplayFrame);
            Bitmap canvasCopy=new Bitmap(layout.Scene.Width,layout.Scene.Height,PixelFormat.Format32bppPArgb);
            using(Graphics g=Graphics.FromImage(canvasCopy))DrawScene(g,layout,sprite);
            return canvasCopy;
        }
        private object CheckTiltSample(TiltTick tick,string kind,List<string> failures,string flight,string framesDir,List<object> frames,int number)
        {
            double measured=Double.NaN;int border;
            using(Bitmap drawn=Render(tick.Choice,tick.Layout))
            {
                double[] shape=AlphaShape(drawn);border=(int)shape[2];
                if(tick.Rotation!=0 && Math.Abs(tick.Rotation)>=10)
                {
                    SceneLayout upright=tick.Layout;upright.Rotation=0;
                    using(Bitmap reference=Render(tick.Choice,upright))
                    {
                        double delta=shape[1]-AlphaShape(reference)[1];while(delta>90)delta-=180;while(delta<=-90)delta+=180;measured=delta;
                    }
                    if(Math.Abs(measured-tick.Rotation)>2.5)failures.Add(flight+" "+kind+" t="+Math.Round(tick.T,3)+": drawn fur rotated "+Math.Round(measured,2)+" deg, layout says "+Math.Round(tick.Rotation,2));
                }
                if(border>=16)failures.Add(flight+" "+kind+" t="+Math.Round(tick.T,3)+": the canvas border has alpha "+border+" (sprite clipped)");
                if(framesDir!=null)
                {
                    string file=flight+"-"+number.ToString("00")+".png";drawn.Save(Path.Combine(framesDir,file),ImageFormat.Png);
                    frames.Add(new{file=file,kind=kind,t=Math.Round(tick.T,4),phase=Math.Round(tick.Phase,4),tiltDeg=Math.Round(tick.Tilt,2),slot=tick.Slot,source=tick.Source,motion=tick.Motion,
                        scene=new[]{tick.Layout.Scene.Left,tick.Layout.Scene.Top,tick.Layout.Scene.Width,tick.Layout.Scene.Height},anchor=new[]{Math.Round(tick.X,1),Math.Round(tick.FootY,1)}});
                }
            }
            return new{kind=kind,t=Math.Round(tick.T,3),phase=Math.Round(tick.Phase,3),motion=tick.Motion,verticalPxPerSec=Math.Round(tick.Vy,1),tiltDeg=Math.Round(tick.Tilt,2),
                gdiRotationDeg=Math.Round(tick.Rotation,2),measuredFurRotationDeg=Double.IsNaN(measured)?null:(object)Math.Round(measured,2),slot=tick.Slot,role=Role(latestArt.JumpFlight,tick.Slot),
                source=tick.Source,canvasBorderAlpha=border,cornerOvershootPx=Math.Round(tick.Overshoot,2),scene=new[]{tick.Layout.Scene.Width,tick.Layout.Scene.Height}};
        }
        private object RunTiltFlight(string flight,string expectClip,List<WindowSurface> surfaces,List<string> failures,string framesDir,List<object> frames)
        {
            const double dt=1/60.0;Point idle=new Point(-20000,-20000);DevJumpFlight roles=latestArt.JumpFlight;
            if(mind.Clip!=expectClip){failures.Add(flight+": flight began as "+mind.Clip+", expected "+expectClip);return new{flight=flight,passed=false};}
            int before=failures.Count;List<TiltTick> ticks=new List<TiltTick>();
            for(int i=0;i<400;i++)
            {
                if(i>0){mind.ObserveHover(false);mind.Step(dt,idle);if(i%15==0)mind.UpdateWindows(surfaces);}
                if(!mind.IsAirborne)break;
                // PaintSprite's order: clamp, choose, place, lay out the scene.
                mind.ClampHorizontal();mind.Clamp();
                SpriteChoice choice=ChooseSprite(idle);SpritePlacement placed=PlaceSprite(choice);SceneLayout layout=LayoutScene(choice,placed);
                TiltTick tick=new TiltTick{Phase=mind.Age/mind.Duration,T=mind.FlightProgress,X=mind.X,FootY=mind.FootY,Tilt=layout.Tilt,Rotation=layout.Rotation,Choice=choice,Layout=layout,
                    Slot=placed.Latest!=null?placed.Latest.Pose.frame:choice.DisplayFrame,Source=placed.Latest!=null?placed.Latest.Pose.source??"":""};
                double overshoot=Double.MinValue;
                foreach(PointF p in SpriteCorners(layout,true))overshoot=Math.Max(overshoot,Math.Max(Math.Max(-p.X,p.X-layout.Scene.Width),Math.Max(-p.Y,p.Y-layout.Scene.Height)));
                tick.Overshoot=overshoot;ticks.Add(tick);
            }
            if(ticks.Count<10){failures.Add(flight+": only "+ticks.Count+" airborne ticks");return new{flight=flight,passed=false};}
            double maxAbs=0,maxStep=0,minRising=Double.MaxValue,maxFalling=Double.MinValue;int rising=0,falling=0,apex=0,crouchTicks=0;
            List<string> drawnRising=new List<string>(),drawnFalling=new List<string>();
            if(roles==null)failures.Add(flight+": the DEV pack has no jump flight roles (rising/apex/descending slots)");
            for(int i=0;i<ticks.Count;i++)
            {
                TiltTick k=ticks[i];
                double ahead=i+1<ticks.Count?ticks[i+1].FootY:k.FootY,behind=i>0?ticks[i-1].FootY:k.FootY;int span=(i+1<ticks.Count?1:0)+(i>0?1:0);
                k.Vy=(ahead-behind)/(Math.Max(1,span)*dt);
                k.Motion=k.T<=0?"preFlight":k.T>=1?"landed":k.Vy<-TiltApexBand?"rising":k.Vy>TiltApexBand?"falling":"apex";
                string at=flight+" t="+Math.Round(k.T,3)+" (slot "+k.Slot+", "+k.Motion+")";
                maxAbs=Math.Max(maxAbs,Math.Abs(k.Tilt));if(i>0)maxStep=Math.Max(maxStep,Math.Abs(k.Tilt-ticks[i-1].Tilt));
                if(Math.Abs(k.Tilt)>TiltLimit+1e-9)failures.Add(at+": tilt "+Math.Round(k.Tilt,2)+" exceeds 45 deg");
                if((k.Motion=="preFlight"||k.Motion=="landed")&&k.Tilt!=0)failures.Add(at+": on the ground but tilted "+Math.Round(k.Tilt,2));
                if(k.Crouch){crouchTicks++;if(k.Tilt!=0)failures.Add(at+": crouch frame "+k.Source+" tilted "+Math.Round(k.Tilt,2));}
                if(k.Motion=="rising"){rising++;minRising=Math.Min(minRising,k.Tilt);if(!(k.Tilt>0))failures.Add(at+": rising but tilt "+Math.Round(k.Tilt,3)+" (needs nose up, > 0)");}
                if(k.Motion=="falling"){falling++;maxFalling=Math.Max(maxFalling,k.Tilt);if(!(k.Tilt<0))failures.Add(at+": falling but tilt "+Math.Round(k.Tilt,3)+" (needs nose down, < 0)");}
                if(k.Motion=="apex"){apex++;if(Math.Abs(k.Tilt)>TiltApexLimit)failures.Add(at+": at the apex but tilt "+Math.Round(k.Tilt,2));}
                if(k.Overshoot>.5)failures.Add(at+": rotated sprite corner "+Math.Round(k.Overshoot,2)+" px outside the scene/canvas");
                string role=Role(roles,k.Slot);
                if(roles!=null)
                {
                    bool ok=k.Motion=="preFlight"?role=="preFlight":k.Motion=="landed"?role=="landing":k.Motion=="rising"?(role=="rising"||role=="apex"):k.Motion=="falling"?(role=="descending"||role=="apex"):(role!="preFlight"&&role!="landing");
                    if(!ok)failures.Add(at+": drew a "+role+" slot");
                }
                if(k.Motion=="rising"&&!drawnRising.Contains(k.Slot+" "+k.Source))drawnRising.Add(k.Slot+" "+k.Source);
                if(k.Motion=="falling"&&!drawnFalling.Contains(k.Slot+" "+k.Source))drawnFalling.Add(k.Slot+" "+k.Source);
            }
            if(maxStep>TiltStepLimit)failures.Add(flight+": tilt changed "+Math.Round(maxStep,2)+" deg in one 60 fps tick (snap; limit "+TiltStepLimit+")");
            if(!ticks[0].Crouch||!ticks[ticks.Count-1].Crouch)failures.Add(flight+": the flight does not start and end on the crouch frames");
            List<object> samples=new List<object>();List<double> sampledTilts=new List<double>();
            List<TiltTick> inFlight=ticks.FindAll(k=>k.T>0&&k.T<1);
            samples.Add(CheckTiltSample(ticks[0],"takeoffCrouch",failures,flight,framesDir,frames,0));
            for(int n=0;n<10;n++)
            {
                double target=(n+.5)/10;TiltTick best=inFlight[0];
                foreach(TiltTick k in inFlight)if(Math.Abs(k.T-target)<Math.Abs(best.T-target))best=k;
                samples.Add(CheckTiltSample(best,"flight",failures,flight,framesDir,frames,n+1));sampledTilts.Add(Math.Round(best.Tilt,1));
            }
            samples.Add(CheckTiltSample(ticks[ticks.Count-1],"landingCrouch",failures,flight,framesDir,frames,11));
            return new{flight=flight,clip=expectClip,passed=failures.Count==before,ticks=ticks.Count,risingTicks=rising,fallingTicks=falling,apexTicks=apex,crouchTicks=crouchTicks,
                sampledTiltsDeg=sampledTilts,maxAbsTiltDeg=Math.Round(maxAbs,2),maxTiltStepDeg=Math.Round(maxStep,2),
                minRisingTiltDeg=rising>0?(object)Math.Round(minRising,3):null,maxFallingTiltDeg=falling>0?(object)Math.Round(maxFalling,3):null,
                slotsDrawnRising=drawnRising,slotsDrawnFalling=drawnFalling,samples=samples};
        }
        internal object CheckJumpTilt(string framesDir)
        {
            List<string> failures=new List<string>();List<object> flights=new List<object>(),frameFlights=new List<object>();
            try
            {
                if(framesDir!=null)Directory.CreateDirectory(framesDir);
                Rectangle area=new Rectangle(0,0,2400,1400);
                foreach(int choice in new[]{480,640,800})
                {
                    size=choice;settings.Size=choice;RecreateCanvas();
                    foreach(bool right in new[]{true,false})
                    {
                        string clip=right?"jump_right":"jump_left";int s=size;long id=right?9101:9102;
                        // A ledge one cat-size above the floor, starting past the cat so the leap also travels forward.
                        Rectangle window=right?new Rectangle((int)(1.3*s),area.Bottom-s,(int)(1.6*s),300):new Rectangle(area.Right-(int)(2.9*s),area.Bottom-s,(int)(1.6*s),300);
                        List<WindowSurface> surfaces=new List<WindowSurface>{new WindowSurface(id,window)};
                        mind.MoveHome(area,right?.9*s:area.Right-.9*s,area.Bottom);mind.PerchesEnabled=true;mind.UpdateWindows(surfaces);
                        double startX=mind.X;
                        string up=clip+"-up-"+choice,down=clip+"-down-"+choice;
                        bool record=framesDir!=null&&choice==480;List<object> upFrames=new List<object>(),downFrames=new List<object>();
                        if(!mind.TryPerch(true)){failures.Add(up+": CatMind.TryPerch refused the ledge "+window);continue;}
                        flights.Add(RunTiltFlight(up,clip,surfaces,failures,record?framesDir:null,upFrames));
                        if(mind.PerchedWindow!=id||Math.Abs(mind.FootY-window.Top)>.01)failures.Add(up+": did not land on the ledge (perched "+mind.PerchedWindow+", foot "+mind.FootY+")");
                        double ledgeX=mind.X;
                        mind.LeavePerch();
                        flights.Add(RunTiltFlight(down,clip,surfaces,failures,record?framesDir:null,downFrames));
                        if(mind.PerchedWindow!=0||Math.Abs(mind.FootY-area.Bottom)>.01)failures.Add(down+": did not land on the floor (foot "+mind.FootY+")");
                        if(record)
                        {
                            frameFlights.Add(new{flight=up,clip=clip,direction="up",size=choice,area=new[]{area.Left,area.Top,area.Width,area.Height},floorY=area.Bottom,
                                ledge=new[]{window.Left,window.Top,window.Width,window.Height},fromX=Math.Round(startX,1),toX=Math.Round(ledgeX,1),frames=upFrames});
                            frameFlights.Add(new{flight=down,clip=clip,direction="down",size=choice,area=new[]{area.Left,area.Top,area.Width,area.Height},floorY=area.Bottom,
                                ledge=new[]{window.Left,window.Top,window.Width,window.Height},fromX=Math.Round(ledgeX,1),toX=Math.Round(mind.X,1),frames=downFrames});
                        }
                    }
                }
                if(framesDir!=null)File.WriteAllText(Path.Combine(framesDir,"jump-tilt-frames.json"),new JavaScriptSerializer().Serialize(new{note="Canvases rendered by OdinWindow.DrawScene (the app's render path) at real CatMind flight samples, size 480.",flights=frameFlights}));
            }
            catch(Exception error){failures.Add("exception: "+error);}
            List<string> shown=failures.Count>60?failures.GetRange(0,60):failures;
            return new{passed=failures.Count==0,failureCount=failures.Count,failures=shown,
                rules=new{positiveTilt="nose up for both directions",limitDeg=TiltLimit,apexBandPxPerSec=TiltApexBand,apexLimitDeg=TiltApexLimit,stepLimitDegPerTick=TiltStepLimit,tickSeconds=1/60.0,
                    samples="10 flight points at t=0.05..0.95 plus the first and last crouch frame; every tick is also checked"},
                flights=flights};
        }
    }
}
#endif
