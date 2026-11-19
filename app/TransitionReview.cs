#if ODIN_DEV
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace OdinV2
{
    internal static class TransitionReview
    {
        internal static readonly string[] Clips={"sit_to_loaf","loaf_to_sit","sit_to_lay","lay_to_sit","lay_to_sleep","sleep_to_lay","sit_to_groom","groom_to_sit"};
        internal static string[] ClipsFor(DevArtwork art){var clips=new List<string>(Clips);if(art.HasActionFamily("loaf_to_lay")){clips.Add("loaf_to_lay");clips.Add("lay_to_loaf");}return clips.ToArray();}
        static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        static int FrameDelay(DevActionClip clip,int frame){return clip.frameSeconds!=null?(int)Math.Round(clip.frameSeconds[frame]*100):(int)Math.Round((frame+1)*clip.seconds*100/clip.frames)-(int)Math.Round(frame*clip.seconds*100/clip.frames);}
        static CatMind Mind(ArtPack art){CatMind mind=new CatMind(new Rectangle(0,0,1920,1080),72);mind.ConfigureArt(art.Manifest.clips.Keys);mind.ConfigureTimings(art.Manifest);mind.PerchesEnabled=false;mind.ZoomiesEnabled=false;return mind;}
        static List<string> Until(CatMind mind,string destination,double timeout)
        {
            List<string> seen=new List<string>();
            for(double t=0;t<timeout;t+=.02)
            {
                if(seen.Count==0||seen[seen.Count-1]!=mind.Clip)seen.Add(mind.Clip);
                if(mind.Clip==destination)return seen;
                mind.Step(.02,Point.Empty);
            }
            throw new Exception("Transition did not reach "+destination+": "+String.Join(",",seen));
        }
        static double[] VisibleRegistration(DevArtFrame frame)
        {
            Bitmap image=frame.Image;int left=image.Width;
            var data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            byte[] pixels=new byte[data.Stride*image.Height];System.Runtime.InteropServices.Marshal.Copy(data.Scan0,pixels,0,pixels.Length);image.UnlockBits(data);
            for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(pixels[y*data.Stride+x*4+3]>128){left=Math.Min(left,x);break;}
            int[] columns=frame.Pose.contactColumns;Check(columns!=null&&columns.Length==2,"Missing body-contact review region.");
            List<int> bottoms=new List<int>();
            for(int x=columns[0];x<columns[1];x++)for(int y=image.Height-1;y>=0;y--)if(pixels[y*data.Stride+x*4+3]>128){bottoms.Add(y);break;}
            Check(bottoms.Count>0,"No visible body pixels in the support region.");bottoms.Sort();
            double body=(bottoms[(bottoms.Count-1)/2]+bottoms[bottoms.Count/2])/2.0;
            Rectangle drawn=frame.Destination(480,0,900,0);double scale=drawn.Width/(double)image.Width;
            return new[]{drawn.X+left*scale,900-(drawn.Y+body*drawn.Height/image.Height)};
        }
        internal static object Run(ArtPack art,DevArtwork latest,string directory)
        {
            Directory.CreateDirectory(directory);int frames=0,routes=0;double maxRearDrift=0,maxBodyGap=0;
            double rear=VisibleRegistration(latest.Still(1))[0];
            foreach(string clip in ClipsFor(latest))
            {
                DevActionClip timeline=latest.ActionClip(clip);Check(timeline!=null&&timeline.frames>=5&&!timeline.loop,"Missing transition timeline: "+clip);
                if(timeline.frameSeconds!=null){double elapsed=0;Check(timeline.frameSeconds.Length==timeline.frames,"Incomplete frame timings.");for(int f=0;f<timeline.frames;f++){Check(art.Frame(clip,elapsed+.001)==f,"Companion timing selected the wrong drawing.");elapsed+=timeline.frameSeconds[f];}Check(Math.Abs(elapsed-timeline.seconds)<.000001,"Frame durations differ from total clip duration.");}
                string[] names=clip.Split(new[]{"_to_"},StringSplitOptions.None);
                for(int i=0;i<timeline.frames;i++)
                {
                    DevArtFrame image;Check(latest.TryGet(clip,i,out image),"Missing transition frame.");
                    double[] visible=VisibleRegistration(image);double rearDrift=Math.Abs(visible[0]-rear);
                    Check(rearDrift<1.5,"Visible rear silhouette slides during "+clip+"/"+i+": "+rearDrift+" px");
                    double gap=visible[1];Check(Math.Abs(gap)<2.5,"Visible body misses the floor during "+clip+"/"+i+": "+gap+" px");
                    maxRearDrift=Math.Max(maxRearDrift,rearDrift);maxBodyGap=Math.Max(maxBodyGap,Math.Abs(gap));
                    // V11 cells have different native canvas widths and individually registered
                    // anatomical units. A common raster scale is not a size invariant there.
                    // Report anatomical review pending; do not infer a visual scale pass.
                    if(i>=1&&i<timeline.frames-1&&!latest.UsesCoherentRest){DevArtFrame first;latest.TryGet(clip,1,out first);Check(Math.Abs(first.Scale(480)-image.Scale(480))<.00001,"Transition resizes the whole body between keys.");}
                    foreach(int size in new[]{480,640,800})foreach(int x in new[]{-1300,600})
                    {
                        Rectangle rect=image.Destination(size,x,900,0);
                        Check(Math.Abs(rect.Y+image.Pose.supportY*rect.Height/image.Image.Height-900)<2,"Transition support moved.");
                    }
                    if(i==0||i==timeline.frames-1)
                    {
                        DevArtFrame endpoint;string name=i==0?names[0]:names[1];int endpointIndex=name=="groom"&&i==0?15:0;
                        Check(latest.TryGet(name,endpointIndex,out endpoint),"Missing endpoint.");
                        Check(image.Pose.sha256==endpoint.Pose.sha256&&image.Pose.anchorX==endpoint.Pose.anchorX&&image.Pose.supportY==endpoint.Pose.supportY&&image.Pose.renderWidthAtSmall==endpoint.Pose.renderWidthAtSmall,"Transition endpoint differs from its corrected held pose.");
                    }
                    frames++;
                }
            }
            DevArtFrame groomBase;latest.TryGet("groom",0,out groomBase);double groomScale=groomBase.Scale(480);
            for(int i=0;i<16;i++){
                DevArtFrame image;latest.TryGet("groom",i,out image);double[] visible=VisibleRegistration(image);
                Check(Math.Abs(visible[0]-rear)<1.5,"Grooming slides sideways.");Check(Math.Abs(visible[1])<1.5,"Grooming body lifts from the floor.");
                Check(Math.Abs(image.Scale(480)-groomScale)<.00001,"Grooming changes whole-body scale.");
                maxRearDrift=Math.Max(maxRearDrift,Math.Abs(visible[0]-rear));maxBodyGap=Math.Max(maxBodyGap,Math.Abs(visible[1]));
            }
            foreach(string from in new[]{"sit","loaf","lay","sleep"})foreach(string to in new[]{"sit","loaf","lay","sleep"})
            {
                CatMind mind=Mind(art);mind.Rest(from,600);Until(mind,from,12);double x=mind.X,y=mind.FootY;
                double began= mind.Clock;mind.Rest(to,600);List<string> seen=Until(mind,to,12);
                double expected=0;foreach(string route in seen){DevActionClip timing=latest.ActionClip(route);if(route.Contains("_to_")&&timing!=null)expected+=timing.seconds;}
                Check(Math.Abs((mind.Clock-began)-expected)<.1,"Behavior held a transition longer than its authored frames.");
                if(from!=to)Check(seen.Exists(c=>c.Contains("_to_")),"Rest route snapped: "+from+" to "+to);
                if(latest.HasActionFamily("loaf_to_lay")&&from!="sit"&&to!="sit")foreach(string route in seen)Check(!route.Contains("sit"),"Low rest change detoured through sitting: "+from+" to "+to);
                Check(mind.X==x&&mind.FootY==y,"Rest route moved the root.");routes++;
            }
            CatMind groom=Mind(art);groom.Groom();List<string> grooming=Until(groom,"sit",40);
            Check(String.Join(",",grooming)=="sit_to_groom,groom,groom_to_sit,sit","Grooming entry/exit route changed.");
            CatMind paused=Mind(art);paused.Rest("lay",600);paused.Step(.02,Point.Empty);double age=paused.Age;paused.Paused=true;paused.Step(.1,Point.Empty);Check(paused.Age==age,"Pause advanced a transition.");
            CatMind retarget=Mind(art);retarget.Rest("lay",600);retarget.Step(.1,Point.Empty);double before=retarget.Age;
            retarget.Rest("loaf",600);Check(retarget.Clip=="sit_to_lay"&&retarget.Age==before,"Retargeting restarted the drawing mid-transition.");Until(retarget,"loaf",12);
            CatMind stopGroom=Mind(art);stopGroom.Groom();Until(stopGroom,"groom",5);stopGroom.Step(.1,Point.Empty);stopGroom.Rest("loaf",600);
            Check(stopGroom.Clip=="groom","Changing pose cut a lick cycle in half.");List<string> stopRoute=Until(stopGroom,"loaf",12);Check(stopRoute.Contains("groom_to_sit"),"Changing pose skipped paw lowering.");
            CatMind held=Mind(art);held.DevSelectState("loaf");Until(held,"loaf",10);held.Groom();List<string> heldRoute=Until(held,"loaf",45);Check(heldRoute.Contains("groom_to_sit")&&heldRoute.Contains("sit_to_loaf"),"Held state bypassed grooming return.");
            // Export through the same native frame selection and Destination used by the companion.
            var exports=new List<string>{"sit_to_loaf","sit_to_lay","lay_to_sleep","sit_to_groom"};if(latest.HasActionFamily("loaf_to_lay"))exports.Add("loaf_to_lay");
            foreach(string clip in exports)
            {
                string back=clip.Substring(clip.IndexOf("_to_",StringComparison.Ordinal)+4)+"_to_"+clip.Substring(0,clip.IndexOf("_to_",StringComparison.Ordinal));
                foreach(bool dark in new[]{false,true})
                {
                    string folder=Path.Combine(directory,clip+(dark?"-dark":"-light"));Directory.CreateDirectory(folder);
                    List<int> delays=new List<int>();int number=0;
                    int forward=latest.ActionClip(clip).frames,backward=latest.ActionClip(back).frames;
                    for(int i=0;i<forward;i++){Save(latest,clip,i,folder,number++,dark);delays.Add(i==0?70:FrameDelay(latest.ActionClip(clip),i));}
                    if(clip=="sit_to_groom")for(int i=0;i<16;i++){Save(latest,"groom",i,folder,number++,dark);delays.Add((int)Math.Round((i+1)*350.0/16)-(int)Math.Round(i*350.0/16));}
                    else {Save(latest,clip,forward-1,folder,number++,dark);delays.Add(90);}
                    for(int i=0;i<backward;i++){Save(latest,back,i,folder,number++,dark);delays.Add(i==backward-1?70:FrameDelay(latest.ActionClip(back),i));}
                    File.WriteAllText(Path.Combine(folder,"delays.txt"),String.Join(",",delays));
                }
            }
            return new{passed=true,status="candidate complete motion; owner visual review pending",transitionFrames=frames,restRoutes=routes,groomingRoute=grooming,heldGroomingRoute=heldRoute,pauseChecked=true,retargetKeepsCurrentMotion=true,poseChangeFinishesLickCycle=true,endpointPixelsPreserved=true,endpointsMatchCorrectedHeldRegistration=true,constantScaleAcrossIntermediateKeys=!latest.UsesCoherentRest,anatomicalScaleReviewPending=latest.UsesCoherentRest,all16GroomFramesRegistered=true,maxVisibleRearDriftPx=Math.Round(maxRearDrift,3),maxVisibleBodyFloorGapPx=Math.Round(maxBodyGap,3),placementCases=frames*6,exportDirectory=directory};
        }
        static void Save(DevArtwork latest,string clip,int index,string directory,int number,bool dark)
        {
            DevArtFrame frame;Check(latest.TryGet(clip,index,out frame),"Preview frame unavailable.");
            using(Bitmap canvas=new Bitmap(560,330,PixelFormat.Format32bppArgb))using(Graphics g=Graphics.FromImage(canvas))
            {
                g.Clear(dark?Color.FromArgb(27,29,34):Color.FromArgb(245,245,242));
                using(Brush band=new SolidBrush(dark?Color.FromArgb(50,53,60):Color.FromArgb(205,210,216)))g.FillRectangle(band,0,280,560,50);
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(frame.Image,frame.Destination(480,260,280,0));
                using(Font font=new Font("Segoe UI",10))using(Brush text=new SolidBrush(dark?Color.White:Color.Black))g.DrawString(clip+"   "+index+"   candidate",font,text,10,8);
                canvas.Save(Path.Combine(directory,number.ToString("00")+".png"),ImageFormat.Png);
            }
        }
    }
    internal sealed partial class OdinWindow
    {
        internal object CheckTransitionRendering()
        {
            int drawn=0;
            foreach(string clip in TransitionReview.ClipsFor(latestArt))
            {
                string[] route=clip.Split(new[]{"_to_"},StringSplitOptions.None);
                mind.DevHeldState=null;mind.Paused=false;
                mind.Rest("sit",600);for(int i=0;i<400&&mind.Clip!="sit";i++)mind.Step(.02,Point.Empty);
                if(route[0]=="groom")mind.Groom();else mind.Rest(route[0],600);
                for(int i=0;i<400&&mind.Clip!=route[0];i++)mind.Step(.02,Point.Empty);
                if(route[1]=="groom")mind.Groom();else mind.Rest(route[1],600);
                for(int i=0;i<400&&mind.Clip!=clip;i++)mind.Step(.02,Point.Empty);
                bool[] seen=new bool[latestArt.ActionClip(clip).frames];
                for(int i=0;i<400&&mind.Clip==clip;i++)
                {
                    SpriteChoice choice=ChooseSprite(new Point(-20000,-20000));SpritePlacement placed=PlaceSprite(choice);
                    if(placed.Latest==null||placed.Latest.Pose.clip!=clip||placed.Latest.Pose.frame!=choice.Frame)throw new Exception("Companion rendered the atlas fallback during "+clip);
                    SceneLayout layout=LayoutScene(choice,placed);
                    if(!seen[choice.Frame])using(Bitmap output=new Bitmap(layout.Scene.Width,layout.Scene.Height))using(Graphics graphics=Graphics.FromImage(output))
                    {
                        DrawScene(graphics,layout,placed.Latest.Image);seen[choice.Frame]=true;drawn++;
                    }
                    mind.Step(.02,Point.Empty);
                }
                foreach(bool frame in seen)if(!frame)throw new Exception("Companion skipped a transition frame: "+clip);
            }
            return new{passed=true,actualCompanionFramesRendered=drawn,noAtlasFallback=true,noVisibleWindowOpened=true};
        }
    }
}
#endif
