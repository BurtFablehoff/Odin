using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

namespace OdinV2
{
    internal static class Tests
    {
        private sealed class PerchFixture : Form
        {
            protected override bool ShowWithoutActivation {get{return true;}}
            protected override CreateParams CreateParams {get{CreateParams p=base.CreateParams;p.ExStyle|=0x08000000;return p;}}
        }
        private static IEnumerable<string> behaviorClips;
        private static CatMind CreateMind(Rectangle area,int seed){CatMind c=new CatMind(area,seed);c.ConfigureArt(behaviorClips);return c;}
        private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
        private static void Advance(CatMind cat,double seconds){for(double t=0;t<seconds;t+=.05)cat.Step(.05,Point.Empty);}
        public static object Behavior(IEnumerable<string> clips=null)
        {
            behaviorClips=clips;
            ClickGesture input=new ClickGesture();Point clickAt=new Point(500,600);
            Check(!input.Observe(false,clickAt,0),"A button-up state became a click.");
            input.Observe(true,clickAt,.1);Check(input.Observe(false,clickAt,.25),"A short click was lost.");
            input.Observe(true,clickAt,1);input.Observe(true,new Point(700,600),1.2);Check(!input.Observe(false,new Point(700,600),1.3),"A window drag became a pounce trigger.");
            input.Observe(true,clickAt,2);Check(!input.Observe(false,clickAt,3),"A long button hold became a pounce trigger.");
            double totalRest=0,total=0,totalDistance=0;int zooms=0;Dictionary<string,int> counts=new Dictionary<string,int>();
            for(int seed=0;seed<12;seed++)
            {
                CatMind cat=CreateMind(seed%2==0?new Rectangle(-1920,-120,1920,1080):new Rectangle(0,0,1366,728),24029+seed);
                double prior=cat.X;
                for(int i=0;i<288000;i++)
                {
                    cat.Step(.1,new Point(cat.Area.Left+500,cat.Area.Bottom-300));
                    Check(cat.Supports(cat.Clip),"Preview requested missing art: "+cat.Clip);
                    Check(!Double.IsNaN(cat.X) && !Double.IsNaN(cat.FootY),"Invalid position.");
                    Check(cat.X>=cat.MinX-.01 && cat.X<=cat.MaxX+.01,"Odin escaped the monitor horizontally.");
                    Check(cat.FootY>=cat.Area.Top && cat.FootY<=cat.Area.Bottom,"Odin escaped the monitor vertically.");
                    if(cat.PerchedWindow==0 && !cat.IsAirborne)Check(cat.FootY==cat.Area.Bottom,"Desktop floor must meet the taskbar top.");
                    totalDistance+=Math.Abs(cat.X-prior);prior=cat.X;
                }
                foreach(KeyValuePair<string,double> p in cat.ActivitySeconds){total+=p.Value;if(CatMind.IsRest(p.Key))totalRest+=p.Value;}
                foreach(KeyValuePair<string,int> p in cat.ActivityCounts){if(!counts.ContainsKey(p.Key))counts[p.Key]=0;counts[p.Key]+=p.Value;}
                zooms+=cat.CompletedZoomies;
                cat.Paused=true;double frozen=cat.Clock,x=cat.X,y=cat.FootY;Advance(cat,20);
                Check(cat.Clock==frozen && cat.X==x && cat.FootY==y,"Pause did not freeze the routine.");
            }
            double fraction=totalRest/total;
            Check(fraction>.80,"Odin is still too restless: "+fraction);
            Check(totalDistance>10000 && zooms>0,"Zoomies never occurred.");
            foreach(string name in new[]{"sit","loaf","lay","sleep","run_right","run_left","groom","stretch"})if(behaviorClips==null || new HashSet<string>(behaviorClips).Contains(name))Check(counts.ContainsKey(name),"Routine omitted "+name);
            foreach(string activity in counts.Keys)Check(!activity.StartsWith("walk_") && !activity.StartsWith("scratch_") && activity!="belly" && activity!="belly_sleep","Excluded action was scheduled: "+activity);
            CatMind c=CreateMind(new Rectangle(0,0,1920,1080),17);
            WindowSurface ledge=new WindowSurface(7,new Rectangle(700,500,900,500));
            Check(!c.EligiblePerch(new WindowSurface(8,new Rectangle(0,0,1920,1080))),"A top-of-screen window was eligible.");
            c.UpdateWindows(new List<WindowSurface>{ledge});Check(c.TryPerch(true),"Low window perch was rejected.");Advance(c,1.8);
            Check(c.PerchedWindow==7 && Math.Abs(c.FootY-500)<1,"Window landing failed.");double oldX=c.X;
            c.UpdateWindows(new List<WindowSurface>{new WindowSurface(7,new Rectangle(800,460,900,500))});
            Check(Math.Abs(c.X-(oldX+100))<1 && c.FootY==460,"Odin failed to follow a moving perch.");
            c.UpdateWindows(new List<WindowSurface>());Advance(c,1.8);Check(c.PerchedWindow==0 && Math.Abs(c.FootY-1080)<1,"Odin did not leave a closed window.");
            c.UpdateWindows(new List<WindowSurface>{ledge});Check(c.TryPerch(true),"Second perch failed.");Advance(c,.3);c.UpdateWindows(new List<WindowSurface>());Advance(c,1.3);Check(c.PerchedWindow==0 && c.FootY==1080,"A closed landing target stranded Odin.");
            c=CreateMind(new Rectangle(0,0,1920,1080),17);c.Rest("sit",90);c.Notice(new PointF(500,400),true);Check(!c.AttentionActive,"Attention reacted without a natural delay.");
            for(int i=0;i<15;i++){c.Notice(new PointF(500+i,400),true);c.Step(.05,Point.Empty);}Check(c.AttentionActive,"Continuous window motion postponed attention forever.");
            for(int i=0;i<520;i++){c.Notice(new PointF(600+i,420),true);c.Step(.05,Point.Empty);}
            Check(!c.AttentionActive,"Repeated window motion kept Odin from looking forward again.");
            c.LookAtYou();Check(c.Clip=="sit" && !c.AttentionActive,"Look at me did not select the forward resting pose.");
            c.Rest("sleep",150);Advance(c,12);Check(c.Clip=="sleep","Nap transition failed.");c.Notice(new PointF(500,400),true);Advance(c,1);Check(!c.AttentionActive,"Pointer movement woke a sleeping Odin.");
            foreach(Rectangle area in new[]{new Rectangle(0,0,1920,1080),new Rectangle(-1920,-80,1920,1080)})
            foreach(bool atRight in new[]{false,true})
            {
                CatMind walker=CreateMind(area,23);walker.X=atRight?walker.MaxX:walker.MinX;walker.Rest("sit",90);double initial=walker.X;walker.Wander();
                Check(walker.Clip=="sit","Disabled walking changed the resting pose.");Advance(walker,2);
                Check(walker.X==initial && walker.Clip=="sit","Disabled Walk to new spot moved Odin.");
            }
            CatMind clicks=CreateMind(new Rectangle(0,0,1920,1080),42);
            if(c.HasPounces)
            {
            c.MoveHome(c.Area,900,1075);Check(c.ClickReleased(new Point(1000,1050),true),"A reachable pounce target was rejected.");Advance(c,2);
            Check(c.AcceptedPounces==1 && !c.ClickReleased(new Point(1000,1050),false),"Pounce cooldown failed.");
            c.ClickPouncesEnabled=false;Check(!c.ClickReleased(new Point(1000,1050),true),"Disabled click pounces still ran.");
            for(int i=0;i<72000;i++){clicks.Step(.1,Point.Empty);clicks.ClickReleased(new Point((int)clicks.X,1030),false);}
            Check(clicks.AcceptedPounces>0 && clicks.AcceptedPounces<=15,"Click pounces were missing or too frequent.");
            }
            if(!c.HasPounces)Check(!c.ClickReleased(new Point(1000,1050),true),"Unavailable click pounces still ran.");
            return new {passed=true,clickPouncesAvailable=c.HasPounces,clickPouncesChecked=c.HasPounces,simulatedHours=96,restFraction=Math.Round(fraction,4),zoomieBursts=zooms,distancePixels=Math.Round(totalDistance),activities=counts,rarePouncesInTwoHours=clicks.AcceptedPounces,checks=new[]{"long relaxed routine","walking disabled; zoomies retained","negative-coordinate monitors","pause","low window eligibility","moving and closed perches","closing a target during a jump","attention delay","sleep ignores pointer","available actions only; excluded actions cannot trigger"}};
        }
        public static object HeldRestStates(ArtPack art)
        {
            Check(new Preferences().Size==480,"Small must be the default size.");
            Check(!art.Manifest.clips.ContainsKey("belly") && !art.Manifest.clips.ContainsKey("belly_sleep"),"Belly-up poses must be excluded.");
            Check(!art.Manifest.clips.ContainsKey("walk_left") && !art.Manifest.clips.ContainsKey("walk_right"),"Pre-dev walking poses must be excluded.");
            foreach(double age in new[]{0.0,1.0,8.0,32.0,180.0})Check(art.Frame("look_loaf",age)==0,"Loaf gaze must not cycle the old animation.");
            foreach(string name in new[]{"loaf","lay","sleep"})
            {
                Check(art.Manifest.clips[name].frames==1 && !art.Manifest.clips[name].loop,"Rest state must hold one frame: "+name);
                foreach(double age in new[]{0.0,1.0,8.0,32.0,180.0})Check(art.Frame(name,age)==0,"Rest state cycles artwork: "+name);
            }
            Check(art.Manifest.clips.ContainsKey("look_lay") && !art.Manifest.clips["look_lay"].loop,"Laying gaze must use selected expressions, not a loop.");
            CatMind awake=new CatMind(new Rectangle(0,0,1920,1080),19);awake.ConfigureArt(art.Manifest.clips.Keys);awake.Rest("lay",1800);
            Check(awake.Clip=="lay" && !CatMind.IsSleeping(awake.Clip),"Laying must be an awake state.");
            double x=awake.X,y=awake.FootY;awake.Notice(new PointF(500,400),true);Advance(awake,1);
            Check(awake.AttentionActive && awake.Clip=="lay" && awake.X==x && awake.FootY==y,"Laying attention must preserve the resting body state.");
            IdleLook gaze=new IdleLook(57);gaze.Step(.1,true,false);int held=gaze.Frame;
            for(int i=0;i<20;i++)gaze.Step(.1,true,false);
            Check(gaze.Frame==held,"Idle gaze must hold its chosen direction.");double remaining=gaze.HoldRemaining;
            for(int i=0;i<100;i++)gaze.Step(.1,true,true);
            Check(gaze.HoldRemaining==remaining,"Mouse attention must pause idle laying gaze.");
            awake.Rest("sleep",1800);Check(awake.Clip=="sleep","Sleeping must be a distinct state.");awake.Notice(new PointF(500,400),true);Advance(awake,1);
            Check(!awake.AttentionActive && awake.Clip=="sleep","Sleeping must ignore gaze tracking.");
            return new{passed=true,smallDefault=true,loafLayAndSleepHoldStill=true,awakeLayingGaze=true,mouseAttentionOverridesIdle=true,sleepIgnoresMouse=true};
        }
        // --routine-time (Hatt 2026-11-08: "tell me how much time he spends in each state and in each action"). The routine
        // (CatMind, the code both builds run every tick) is stepped at 0.1 s with nobody at the screen: the cursor far away, no
        // clicks, one 1100 px window to perch on, the build's own clips. Several seeds share the simulated hours.
        public static object RoutineTime(ArtPack art,double hours,int seeds)
        {
            Dictionary<string,double> seconds=new Dictionary<string,double>();Dictionary<string,int> visits=new Dictionary<string,int>();
            double perched=0,total=0;Rectangle area=new Rectangle(0,0,1920,1032);Point idle=new Point(-20000,-20000);
            List<WindowSurface> ledge=new List<WindowSurface>{new WindowSurface(9401,new Rectangle(400,520,1100,512))};
            for(int seed=1;seed<=seeds;seed++)
            {
                CatMind cat=new CatMind(area,2000+seed);cat.ConfigureArt(art.Manifest.clips.Keys);cat.UpdateWindows(ledge);string last=null;
                for(double t=0;t<hours*3600/seeds;t+=.1)
                {
                    cat.ObserveHover(false);cat.Step(.1,idle);string clip=cat.Clip;
                    if(clip!=last){visits[clip]=(visits.ContainsKey(clip)?visits[clip]:0)+1;last=clip;}
                    seconds[clip]=(seconds.ContainsKey(clip)?seconds[clip]:0)+.1;if(cat.PerchedWindow!=0)perched+=.1;total+=.1;
                }
            }
            List<object> clips=new List<object>();List<string> names=new List<string>(seconds.Keys);names.Sort((a,b)=>seconds[b].CompareTo(seconds[a]));
            foreach(string name in names)clips.Add(new{clip=name,percent=Math.Round(100*seconds[name]/total,2),minutesPerHour=Math.Round(60*seconds[name]/total,2),
                visits=visits[name],averageSecondsPerVisit=Math.Round(seconds[name]/visits[name],1)});
            return new{simulatedHours=hours,seeds=seeds,stepSeconds=.1,perchedPercent=Math.Round(100*perched/total,2),clips=clips};
        }
        public static object PerchStates(ArtPack art)
        {
            // V12 3bP (Hatt, 2026-11-08): "perch and zoomies are animations for any state that isn't sleeping".
            List<string> allowed=new List<string>(),blocked=new List<string>();
            foreach(string state in new[]{"sit","loaf","lay","sleep"})
            {
                CatMind cat=new CatMind(new Rectangle(0,0,1920,1080),33);cat.ConfigureArt(art.Manifest.clips.Keys);cat.Rest(state,1800);
                cat.UpdateWindows(new List<WindowSurface>{new WindowSurface(1,new Rectangle(400,500,1000,400))});
                bool accepted=cat.TryPerch(true),expected=!CatMind.IsSleeping(state);
                Check(accepted==expected,"Perching allowed from the wrong state: "+state);
                Check(!accepted || !CatMind.IsRest(cat.Clip),"An accepted perch did not start: "+state+" ("+cat.Clip+")");
                CatMind dash=new CatMind(new Rectangle(0,0,1920,1080),34);dash.ConfigureArt(art.Manifest.clips.Keys);dash.Rest(state,1800);
                int bursts=dash.CompletedZoomies;dash.Zoomies();
                Check((dash.CompletedZoomies>bursts)==expected,"Zoomies allowed from the wrong state: "+state);
                (accepted?allowed:blocked).Add(state);
            }
            return new{passed=true,allowed=allowed,blocked=blocked,zoomiesFollowPerching=true};
        }
        public static object IdleLooking()
        {
            IdleLook look=new IdleLook(815);System.Collections.Generic.HashSet<int> frames=new System.Collections.Generic.HashSet<int>();System.Collections.Generic.HashSet<int> durations=new System.Collections.Generic.HashSet<int>();double front=0,total=0,held=0;int prior=look.Frame;
            for(int i=0;i<24000;i++)
            {
                look.Step(.05,true);frames.Add(look.Frame);if(look.Frame==16)front+=.05;total+=.05;
                if(look.Frame==prior)held+=.05;else{if(held>1)durations.Add((int)Math.Round(held*10));held=0;prior=look.Frame;}
                Check(look.Frame>=0 && look.Frame<=16,"Idle gaze requested an invalid frame.");
            }
            Check(frames.Contains(4)&&frames.Contains(12)&&frames.Contains(0)&&frames.Contains(8)&&frames.Contains(16),"Idle gaze omitted left, right, up, down or centre.");Check(front/total>.55 && durations.Count>8,"Idle gaze was too restless or repeated fixed timing.");
            look.Step(.05,false);Check(look.Frame==16,"Idle gaze did not yield to cursor/window attention.");
            return new{passed=true,variableIdleHoldDurations=durations.Count,forwardFraction=Math.Round(front/total,3),leftRightUpCentre=true,attentionTakesPriority=true};
        }
        public static object SittingPrototype()
        {
            Rectangle area=new Rectangle(0,0,1920,1080);
            double shortest=100,longest=0;int attentionChecks=0;
            for(int seed=0;seed<30;seed++)
            {
                CatMind cat=new CatMind(area,seed);cat.SittingPrototype=true;cat.Rest("sit",240);
                cat.Notice(new PointF(700,400),true);double began=-1,deadline=0;
                for(int i=0;i<560;i++)
                {
                    cat.Notice(new PointF(700+i,400),true);cat.Step(.05,Point.Empty);
                    if(cat.AttentionActive && began<0){began=cat.Clock;deadline=cat.AttentionUntil;}
                    if(cat.AttentionActive)Check(cat.AttentionUntil==deadline,"Window motion extended a tracking deadline.");
                    if(began>=0 && !cat.AttentionActive){double duration=cat.Clock-began;shortest=Math.Min(shortest,duration);longest=Math.Max(longest,duration);attentionChecks++;break;}
                }
                Check(began>=0 && !cat.AttentionActive,"Tracking failed to finish.");
            }
            Check(shortest>=17.9 && longest<=25.1,"Tracking is outside the 18-25 second range.");
            CatMind hover=new CatMind(area,7);hover.Rest("sit",240);double root=hover.X,floor=hover.FootY;
            hover.Notice(new PointF(600,400),true);Advance(hover,1);Check(hover.AttentionActive,"Hover fixture was not tracking.");
            hover.ObserveHover(true);Check(hover.ViewerAcknowledgement && !hover.AttentionActive,"Hover did not override attention.");
            Advance(hover,3);Check(hover.X==root && hover.FootY==floor && hover.Clip=="sit","Hover moved the resting body.");
            hover.ObserveHover(false);Check(hover.ViewerAcknowledgement,"Hover exit lacked a brief hold.");Advance(hover,2);Check(!hover.ViewerAcknowledgement,"Hover exit held forever.");
            hover.ObserveHover(true);Check(hover.ViewerAcknowledgement,"A repeated mouse entry was lost.");
            hover.Zoomies();string action=hover.Clip;hover.ObserveHover(true);Check(!hover.ViewerAcknowledgement && hover.Clip==action,"Hover interrupted travel.");
            CatMind pending=new CatMind(area,18);pending.Rest("sit",240);pending.Notice(new PointF(500,400),true);pending.Stretch();Advance(pending,1);Check(!pending.AttentionActive,"Pending attention leaked into a major action.");
            pending.Rest("sit",240);pending.Notice(new PointF(500,400),true);pending.SetWatching(false);Advance(pending,1);Check(!pending.AttentionActive,"Disabled watching left pending attention active.");
            IdleLook paused=new IdleLook(19);paused.Step(4,true,false);double remaining=paused.HoldRemaining;
            for(int i=0;i<400;i++)paused.Step(.05,true,true);
            Check(paused.HoldRemaining==remaining,"Attention reset or consumed the idle deadline.");
            paused.Step(.05,true,false);Check(Math.Abs(paused.HoldRemaining-(remaining-.05))<.0001,"Idle deadline did not resume.");
            IdleLook repeated=new IdleLook(32);
            for(int i=0;i<24000;i++)repeated.Step(.05,true,i%900<400);
            Check(repeated.Changes>30,"Frequent attention starved spontaneous glances.");
            SittingBehavior eyes=new SittingBehavior(285,true);double minNap=100,maxNap=0;int prior=0;
            for(int i=0;i<144000;i++)
            {
                eyes.Step(.05,true,false,false);
                if(eyes.Naps!=prior){minNap=Math.Min(minNap,eyes.LastNapSeconds);maxNap=Math.Max(maxNap,eyes.LastNapSeconds);prior=eyes.Naps;}
                Check(eyes.EyeClosure>=0 && eyes.EyeClosure<=1,"Invalid eyelid state.");
                if(eyes.Napping && eyes.EyeClosure==1)
                {
                    for(int j=0;j<8;j++)eyes.Step(.05,true,false,true);
                    Check(!eyes.Napping && eyes.EyeClosure==0,"Hover failed to gently wake a short nap.");
                }
            }
            Check(eyes.Blinks>15 && eyes.Naps>2 && minNap>=3 && maxNap<=20,"Short naps/blinks were missing or outside duration limits.");
            SittingBehavior unavailable=new SittingBehavior(285,false);
            for(int i=0;i<12000;i++)unavailable.Step(.05,true,false,false);
            Check(unavailable.Blinks==0 && unavailable.Naps==0 && unavailable.EyeClosure==0,"Unapproved eyelid art was used.");
            CatMind seated=new CatMind(area,42);seated.SittingPrototype=true;double x=seated.X,y=seated.FootY;
            for(int i=0;i<12000;i++){seated.Step(.1,Point.Empty);Check(seated.Clip=="sit" && seated.X==x && seated.FootY==y,"Sitting prototype changed the resting body.");}
            return new{passed=true,attentionSeeds=attentionChecks,trackingSeconds=new[]{Math.Round(shortest,2),Math.Round(longest,2)},hoverOverridesAttention=true,hoverPreservesBody=true,majorActionsProtected=true,idleClockResumes=true,repeatedAttentionGlanceChanges=repeated.Changes,simulatedEyeHours=2,blinks=eyes.Blinks,naps=eyes.Naps,napSeconds=new[]{minNap,maxNap},unapprovedEyesExcluded=true,sittingMinutes=20};
        }
        public static object Assets(ArtPack art)
        {
            int frames=0,edgePixels=0;long pixels=0;Rectangle all=Rectangle.Empty;
            foreach(KeyValuePair<string,ClipInfo> pair in art.Manifest.clips)
            {
                Check(pair.Value.seconds>0 || pair.Key.StartsWith("look_"),"Missing clip timing.");
                for(int i=0;i<pair.Value.frames;i++)
                {
                    Bitmap image=art.Get(pair.Key,i);int visible=0,lowest=0;
                    for(int y=0;y<art.Manifest.height;y+=3)for(int x=0;x<art.Manifest.width;x+=3)if(image.GetPixel(x,y).A>24){visible++;lowest=Math.Max(lowest,y);}
                    Check(visible>250,"Empty animation frame: "+pair.Key+":"+i);
                    Check(art.AnchorY(pair.Key,i)>0 && art.AnchorY(pair.Key,i)<art.Manifest.height,"Invalid support contact: "+pair.Key+":"+i);
                    for(int p=0;p<art.Manifest.width;p++)if(image.GetPixel(0,p).A>32||image.GetPixel(art.Manifest.width-1,p).A>32||image.GetPixel(p,0).A>32||image.GetPixel(p,art.Manifest.height-1).A>32)edgePixels++;
                    frames++;pixels+=visible;
                }
            }
            string[] requiredClips=art.Manifest.preview?new[]{"sit","look_sit","loaf","look_loaf","lay","sleep","run_left","run_right","groom","stretch","jump_left","jump_right"}:new[]{"sit","look_sit","loaf","look_loaf","lay","look_lay","belly","sleep","walk_left","walk_right","run_left","run_right","crab_left","crab_right","groom","stretch","jump","pounce_left","pounce_right","sit_to_loaf","loaf_to_sit","sit_to_lay","lay_to_sit","lay_to_belly","belly_to_lay"};
            foreach(string required in requiredClips)Check(art.Manifest.clips.ContainsKey(required),"Required animation missing: "+required);
            Check(edgePixels==0,"Artwork touches the canvas edge in "+edgePixels+" places.");Check(art.CachedFrames<=56,"Unbounded image cache.");
            foreach(string rest in new[]{"sit","loaf","lay"})if(art.Manifest.clips.ContainsKey("look_"+rest))Check(art.AnchorY(rest,0)>0 && art.AnchorY("look_"+rest,0)>0,"Missing weight-bearing support contact: "+rest);
            return new{passed=true,clips=art.Manifest.clips.Count,frames=frames,canvas=new[]{art.Manifest.width,art.Manifest.height},cameraGroundAnchor=art.Manifest.foot_anchor,gazeSharesRestingBaseline=true,edgePixels=edgePixels,maximumCachedFrames=56,externalArtworkRequired=false};
        }
        public static unsafe object Eyelids(ArtPack art)
        {
            if(!art.Manifest.clips.ContainsKey("sit_eyes"))return new{passed=true,available=false};
            Check(art.Manifest.clips["sit_eyes"].frames==3,"B35 must contain open, half-closed and closed expressions.");
            Bitmap basis=art.Get("sit",0);int changed=0;
            for(int frame=0;frame<3;frame++)
            {
                Bitmap eye=art.Get("sit_eyes",frame);Check(art.AnchorY("sit_eyes",frame)==art.AnchorY("sit",0),"Eyelid changes moved the support contact.");
                BitmapData a=basis.LockBits(new Rectangle(0,0,basis.Width,basis.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);
                BitmapData b=eye.LockBits(new Rectangle(0,0,eye.Width,eye.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);
                int frameChanges=0;
                try
                {
                    for(int y=0;y<basis.Height;y++)
                    {
                        uint* before=(uint*)((byte*)a.Scan0+y*a.Stride);uint* after=(uint*)((byte*)b.Scan0+y*b.Stride);
                        for(int x=0;x<basis.Width;x++)
                        {
                            Check((before[x]>>24)==(after[x]>>24),"B35 changed the seated silhouette/alpha.");
                            if(before[x]==after[x])continue;
                            Check(frame>0 && x>=470 && x<576 && y>=594 && y<641,"B35 changed pixels outside the approved eyelids.");frameChanges++;
                        }
                    }
                }
                finally{basis.UnlockBits(a);eye.UnlockBits(b);}
                Check(frame==0?frameChanges==0:frameChanges>200,"B35 expression is empty or its open frame differs from the original.");changed+=frameChanges;
            }
            return new{passed=true,available=true,approvedBatch="35",expressions=3,changedPixels=changed,bodyAndAlphaIdentical=true,noseAndToothIdentical=true,supportContactIdentical=true};
        }
        public static object NativePerches(ArtManifest manifest=null)
        {
            behaviorClips=manifest==null?null:manifest.clips.Keys;
            Rectangle area=Screen.PrimaryScreen.WorkingArea;
            using(Form ledge=new PerchFixture())
            {
                ledge.Text="Odin's temporary perch verification";ledge.StartPosition=FormStartPosition.Manual;ledge.TopMost=true;
                ledge.Bounds=new Rectangle(area.Left+80,area.Top+400,Math.Min(700,area.Width-160),Math.Min(300,area.Height-420));
                ledge.Show();Native.ShowWindow(ledge.Handle,4);Application.DoEvents();Rectangle initial;
                Check(Native.Bounds(ledge.Handle,out initial),"Windows did not expose the test ledge geometry. Visible="+Native.IsWindowVisible(ledge.Handle)+"; minimized="+Native.IsIconic(ledge.Handle)+"; session="+Process.GetCurrentProcess().SessionId);
                List<WindowSurface> visible=Native.Surfaces(-1);
                WindowSurface found=visible.Find(w=>w.Id==ledge.Handle.ToInt64());Check(found!=null,"Native window enumeration missed an exposed ledge.");
                Check(Native.Surfaces(Process.GetCurrentProcess().Id).Find(w=>w.Id==ledge.Handle.ToInt64())==null,"Own companion windows were eligible as perches.");
                CatMind cat=CreateMind(area,73);cat.UpdateWindows(new List<WindowSurface>{found});Check(cat.TryPerch(true),"Native ledge was rejected.");Advance(cat,1.8);
                Check(cat.PerchedWindow==ledge.Handle.ToInt64() && cat.FootY==initial.Top,"Native ledge landing failed.");double prior=cat.X;
                ledge.Location=new Point(ledge.Left+35,ledge.Top+20);Application.DoEvents();Rectangle moved;Check(Native.Bounds(ledge.Handle,out moved),"Moved ledge bounds disappeared.");
                cat.UpdateWindows(new List<WindowSurface>{new WindowSurface(ledge.Handle.ToInt64(),moved)});
                Check(Math.Abs(cat.X-prior-35)<1 && cat.FootY==moved.Top,"Native moved-window tracking failed.");
                ledge.WindowState=FormWindowState.Minimized;Application.DoEvents();
                Check(Native.Surfaces(-1).Find(w=>w.Id==ledge.Handle.ToInt64())==null,"Minimized windows remained eligible.");
                cat.UpdateWindows(new List<WindowSurface>());Advance(cat,1.8);Check(cat.PerchedWindow==0 && cat.FootY==area.Bottom,"Minimizing a native window stranded Odin.");
                ledge.Close();Application.DoEvents();
                return new{passed=true,workingArea=new[]{area.Left,area.Top,area.Width,area.Height},checks=new[]{"real Win32/DWM window geometry","exposed edge enumeration","own windows excluded","native perch landing","native moved-window tracking","minimized window excluded","return to desktop floor"}};
            }
        }

        public static object Shadows(ArtPack art)
        {
            int poses=0;
            foreach(string clip in art.Manifest.clips.Keys)
            {
                RectangleF shape=GroundShadow.Bounds(art,clip,640,180,90,0);
                using(Bitmap ground=new Bitmap(360,150,PixelFormat.Format32bppPArgb))
                using(Bitmap airborne=new Bitmap(360,150,PixelFormat.Format32bppPArgb))
                {
                    using(Graphics g=Graphics.FromImage(ground)){g.Clear(Color.Transparent);GroundShadow.Draw(g,shape,0);}
                    using(Graphics g=Graphics.FromImage(airborne)){g.Clear(Color.Transparent);GroundShadow.Draw(g,shape,180);}
                    int max=0,air=0;for(int y=0;y<150;y++)for(int x=0;x<360;x++){max=Math.Max(max,ground.GetPixel(x,y).A);air=Math.Max(air,airborne.GetPixel(x,y).A);}
                    Check(max>0 && max<100,"Contact shadow missing or too dark: "+clip);Check(air<max,"Airborne shadow did not fade: "+clip);
                    Check(ground.GetPixel(0,0).A==0 && ground.GetPixel(359,149).A==0,"Shadow has an opaque rectangular background: "+clip);
                }
                Rectangle sprite=new Rectangle(0,-280,640,640);RectangleF lowerShadow=GroundShadow.Bounds(art,clip,640,320,680,200);
                Check(GroundShadow.SceneBounds(sprite,lowerShadow).Contains(Rectangle.Ceiling(lowerShadow)),"Airborne shadow clipped to sprite window: "+clip);poses++;
            }
            return new{passed=true,posesChecked=poses,softTransparentShadow=true,airborneFade=true,overhangAllowed=true};
        }
        public static void Preview(ArtPack art,string path)
        {
            List<string> available=new List<string>();foreach(string name in new[]{"sit","loaf","lay","sleep","belly","belly_sleep","run_right","run_left","groom","stretch","jump_right","jump_left"})if(art.Manifest.clips.ContainsKey(name))available.Add(name);string[] names=available.ToArray();
            using(Bitmap sheet=new Bitmap(1200,((names.Length+3)/4)*300))using(Graphics g=Graphics.FromImage(sheet))using(Font font=new Font("Segoe UI",13))
            {
                g.Clear(Color.FromArgb(245,245,245));
                for(int i=0;i<names.Length;i++)
                {
                    int x=i%4*300,y=i/4*300;string clip=names[i];int frame=clip.StartsWith("jump_")?art.Manifest.clips[clip].frames-1:art.Manifest.clips[clip].frames/3;
                    double floor=y+265,scale=300.0/art.Manifest.width;g.FillRectangle(Brushes.DimGray,x,(float)floor,300,3);
                    GroundShadow.Draw(g,GroundShadow.Bounds(art,clip,300,x+150,floor,0),0);
                    g.DrawImage(art.Get(clip,frame),new RectangleF(x,(float)(floor-art.AnchorY(clip,frame)*scale),300,300));
                    g.DrawString(clip.Replace('_',' '),font,Brushes.Black,x+10,y+278);
                }
                sheet.Save(path,ImageFormat.Png);
            }
        }
    }
}
