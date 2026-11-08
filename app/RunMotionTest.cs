#if ODIN_DEV
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web.Script.Serialization;

namespace OdinV2
{
    // V12 2b --test-run-motion <out.json> [<frames-dir>]. Headless: the window is constructed, never shown.
    // 1. The DEV pack's run clips are the head registration (RunningRegistration, from planning/run-registration-v12.json):
    //    same play order and tween flags, anchorX = foot x - dx, supportY = anchor_y - dy, timeline frames x 16 ms, looping.
    // 2. Real CatMind zoomies at the three sizes, starting from each edge, one 16 ms tick at a time through the app's
    //    per-tick path (mind.Step, PaintSprite's clamps, ChooseSprite, PlaceSprite). Every tick of a run clip draws the
    //    next frame of the DEV pack (one tick per frame, loop seam included). The drawn nose (the registration landmark
    //    mapped through the placed rectangle), relative to the cat's root, moves at most the head-step limits per tick
    //    (4 native px next to a tween, 8 between originals) and bobs at most 20 (1 screen px of rounding per axis allowed).
    // 3. One full cycle of each direction at the small size is rendered by DrawScene into the frames dir (default
    //    <report dir>/run-motion-frames) with the nose point, for tools/local/Build-RunStripV12.py.
    internal sealed partial class OdinWindow
    {
        private sealed class RunRegistrationTables
        {
            public string[] Names;public bool[] Tween;public double[] NoseX,NoseY,Dx,Dy;
            public static RunRegistrationTables For(string clip)
            {
                bool left=clip=="run_left";
                return new RunRegistrationTables{Names=left?RunningRegistration.DevLeftFrames:RunningRegistration.DevRightFrames,Tween=left?RunningRegistration.DevLeftTween:RunningRegistration.DevRightTween,
                    NoseX=left?RunningRegistration.DevLeftNoseX:RunningRegistration.DevRightNoseX,NoseY=left?RunningRegistration.DevLeftNoseY:RunningRegistration.DevRightNoseY,
                    Dx=left?RunningRegistration.DevLeftDx:RunningRegistration.DevRightDx,Dy=left?RunningRegistration.DevLeftDy:RunningRegistration.DevRightDy};
            }
        }
        private object CheckRunPack(string clip,List<string> failures)
        {
            RunRegistrationTables t=RunRegistrationTables.For(clip);DevActionClip timeline=latestArt.ActionClip(clip);
            if(timeline==null){failures.Add(clip+": the DEV pack has no run timeline");return new{clip=clip,passed=false};}
            int before=failures.Count;
            if(timeline.frames!=t.Names.Length)failures.Add(clip+": DEV pack has "+timeline.frames+" frames, the registration "+t.Names.Length);
            if(!timeline.loop || Math.Abs(timeline.seconds-t.Names.Length*RunningMotion.FrameSeconds)>1e-6)failures.Add(clip+": DEV timeline is not "+t.Names.Length+" x 16 ms looping ("+timeline.seconds+" s)");
            double footX=art.Manifest.foot_anchor[0],anchorY=art.AnchorY(clip,0);
            for(int i=0;i<t.Names.Length;i++)
            {
                DevArtFrame frame;
                if(!latestArt.TryGet(clip,i,out frame) || frame==null){failures.Add(clip+": DEV pack lacks frame "+i);continue;}
                DevArtEntry e=frame.Pose;
                if(e.clip!=clip || e.frame!=i || e.phase!=t.Names[i] || (e.kind=="tween")!=t.Tween[i] || !e.customAnchor)failures.Add(clip+": DEV frame "+i+" is "+e.kind+" "+e.phase+", registration says "+(t.Tween[i]?"tween ":"original ")+t.Names[i]);
                if(Math.Abs(e.anchorX-(footX-t.Dx[i]))>.006 || Math.Abs(e.supportY-(anchorY-t.Dy[i]))>.006)
                    failures.Add(clip+": DEV frame "+i+" anchors ("+e.anchorX+", "+e.supportY+") are not the head registration ("+(footX-t.Dx[i])+", "+(anchorY-t.Dy[i])+")");
            }
            DevArtFrame extra;if(latestArt.TryGet(clip,t.Names.Length,out extra))failures.Add(clip+": DEV pack has a frame past the registration's play order");
            int tweens=0;foreach(bool b in t.Tween)if(b)tweens++;
            return new{clip=clip,passed=failures.Count==before,frames=timeline.frames,tweens=tweens,cycleSeconds=Math.Round(timeline.seconds,3),playOrder=t.Names};
        }
        private sealed class RunTick
        {
            public string Clip,Name;public int Frame;public bool Tween;public double RelX,RelY,Scale,X,FootY;public SpriteChoice Choice;public SceneLayout Layout;public PointF Nose;
        }
        internal object CheckRunMotion(string framesDir)
        {
            List<string> failures=new List<string>();List<object> packs=new List<object>(),runs=new List<object>(),saved=new List<object>();
            double maxTweenStep=0,maxPlainStep=0,maxBob=0;int checkedTicks=0;
            try
            {
                foreach(string clip in new[]{"run_left","run_right"})packs.Add(CheckRunPack(clip,failures));
                if(framesDir!=null)Directory.CreateDirectory(framesDir);
                Rectangle area=new Rectangle(0,0,2400,1400);const double dt=RunningMotion.FrameSeconds;Point idle=new Point(-20000,-20000);
                HashSet<string> recorded=new HashSet<string>();
                foreach(int choiceSize in new[]{480,640,800})
                foreach(bool fromLeft in new[]{true,false})
                {
                    size=choiceSize;settings.Size=choiceSize;RecreateCanvas();
                    mind.MoveHome(area,area.Left+area.Width/2,area.Bottom);mind.X=fromLeft?mind.MinX:mind.MaxX;mind.Zoomies();
                    string label=choiceSize+(fromLeft?"-from-left":"-from-right");
                    List<RunTick> instance=new List<RunTick>();List<List<RunTick>> instances=new List<List<RunTick>>();double lastAge=-1;string lastClip=null;
                    for(int i=0;i<20000;i++)
                    {
                        if(i>0){mind.ObserveHover(false);mind.Step(dt,idle);}
                        if(!RunningMotion.IsRun(mind.Clip))break;
                        mind.ClampHorizontal();mind.Clamp();
                        SpriteChoice choice=ChooseSprite(idle);SpritePlacement placed=PlaceSprite(choice);
                        if(mind.Clip!=lastClip || mind.AnimationAge<lastAge){if(instance.Count>0)instances.Add(instance);instance=new List<RunTick>();}
                        lastClip=mind.Clip;lastAge=mind.AnimationAge;
                        string at=label+" "+mind.Clip+" tick "+instance.Count;
                        if(placed.Latest==null || placed.Latest.Pose.clip!=mind.Clip || placed.Latest.Pose.frame!=choice.DisplayFrame || choice.DisplayClip!=mind.Clip){failures.Add(at+": did not draw the DEV pack's run frame");continue;}
                        RunRegistrationTables t=RunRegistrationTables.For(mind.Clip);int f=choice.DisplayFrame;Rectangle r=placed.Rectangle;
                        double sx=r.Width/(double)placed.Latest.Image.Width,sy=r.Height/(double)placed.Latest.Image.Height;
                        RunTick tick=new RunTick{Clip=mind.Clip,Frame=f,Name=t.Names[f],Tween=t.Tween[f],Scale=sx,X=mind.X,FootY=mind.FootY,Choice=choice,
                            Nose=new PointF((float)(r.X+t.NoseX[f]*sx),(float)(r.Y+t.NoseY[f]*sy))};
                        tick.RelX=tick.Nose.X-mind.X;tick.RelY=tick.Nose.Y-mind.FootY;
                        int expected=instance.Count==0?0:(instance[instance.Count-1].Frame+1)%t.Names.Length;
                        if(f!=expected)failures.Add(at+": drew frame "+f+", one tick per frame expects "+expected);
                        if(instance.Count>0)
                        {
                            RunTick p=instance[instance.Count-1];double step=Math.Sqrt(Math.Pow(tick.RelX-p.RelX,2)+Math.Pow(tick.RelY-p.RelY,2));
                            bool nextToTween=tick.Tween || p.Tween;double limit=nextToTween?RunningMotion.HeadStepLimitWithTweens:RunningMotion.HeadStepLimit;
                            double native=Math.Max(0,step-Math.Sqrt(2))/sx;
                            if(nextToTween)maxTweenStep=Math.Max(maxTweenStep,native);else maxPlainStep=Math.Max(maxPlainStep,native);
                            if(native>limit)failures.Add(at+": nose moved "+Math.Round(step,2)+" screen px ("+Math.Round(native,2)+" native beyond rounding) from "+p.Name+" to "+tick.Name+", limit "+limit);
                        }
                        if(choiceSize==480 && framesDir!=null && !recorded.Contains(mind.Clip))tick.Layout=LayoutScene(choice,placed);
                        instance.Add(tick);checkedTicks++;
                    }
                    if(instance.Count>0)instances.Add(instance);
                    if(RunningMotion.IsRun(mind.Clip))failures.Add(label+": zoomies never left the run clips");
                    List<object> parts=new List<object>();
                    foreach(List<RunTick> run in instances)
                    {
                        int frames=RunRegistrationTables.For(run[0].Clip).Names.Length;double low=double.MaxValue,high=double.MinValue;
                        foreach(RunTick k in run){low=Math.Min(low,k.RelY);high=Math.Max(high,k.RelY);}
                        double bob=Math.Max(0,high-low-1)/run[0].Scale;maxBob=Math.Max(maxBob,bob);
                        if(run.Count>=frames && bob>RunningMotion.BobLimit)failures.Add(label+" "+run[0].Clip+": nose bobbed "+Math.Round(bob,2)+" native px, limit "+RunningMotion.BobLimit);
                        parts.Add(new{clip=run[0].Clip,ticks=run.Count,frames=frames,cycles=Math.Round(run.Count/(double)frames,2),bobNative=Math.Round(bob,2)});
                        if(run[0].Layout.Scene.Width>0 && !recorded.Contains(run[0].Clip) && run.Count>=frames)
                        {
                            recorded.Add(run[0].Clip);
                            for(int n=0;n<frames;n++)
                            {
                                RunTick k=run[n];string file=k.Clip+"-"+n.ToString("00")+".png";
                                using(Bitmap drawn=Render(k.Choice,k.Layout))drawn.Save(Path.Combine(framesDir,file),ImageFormat.Png);
                                saved.Add(new{file=file,clip=k.Clip,tick=n,frame=k.Frame,name=k.Name,tween=k.Tween,scene=new[]{k.Layout.Scene.Left,k.Layout.Scene.Top,k.Layout.Scene.Width,k.Layout.Scene.Height},
                                    nose=new[]{Math.Round(k.Nose.X-k.Layout.Scene.Left,2),Math.Round(k.Nose.Y-k.Layout.Scene.Top,2)},root=new[]{Math.Round(k.X-k.Layout.Scene.Left,2),Math.Round(k.FootY-k.Layout.Scene.Top,2)}});
                            }
                        }
                    }
                    runs.Add(new{zoomies=label,size=choiceSize,parts=parts});
                }
                foreach(string clip in new[]{"run_left","run_right"})if(framesDir!=null && !recorded.Contains(clip))failures.Add(clip+": no full cycle was recorded for the strip");
                if(framesDir!=null)File.WriteAllText(Path.Combine(framesDir,"run-motion-frames.json"),new JavaScriptSerializer().Serialize(new{note="One full cycle of each DEV run clip as the app draws it (CatMind zoomies, size 480, one 16 ms tick per frame); canvases from OdinWindow.DrawScene, nose = head registration landmark in canvas px.",size=480,tickSeconds=RunningMotion.FrameSeconds,frames=saved}));
            }
            catch(Exception error){failures.Add("exception: "+error);}
            List<string> shown=failures.Count>60?failures.GetRange(0,60):failures;
            return new{passed=failures.Count==0,failureCount=failures.Count,failures=shown,tickSeconds=RunningMotion.FrameSeconds,oneTickPerFrame=failures.Count==0,checkedTicks=checkedTicks,
                maxHeadStepNextToTweenNative=Math.Round(maxTweenStep,2),maxHeadStepBetweenOriginalsNative=Math.Round(maxPlainStep,2),maxBobNative=Math.Round(maxBob,2),
                limits=new{headStepWithTweens=RunningMotion.HeadStepLimitWithTweens,headStep=RunningMotion.HeadStepLimit,bob=RunningMotion.BobLimit,roundingAllowanceScreenPx=1},
                packs=packs,runs=runs,stripFrames=saved.Count};
        }
    }
}
#endif
