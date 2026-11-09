#if ODIN_DEV
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace OdinV2
{
    internal sealed partial class CatMind
    {
        internal string DevHeldState;
        // V12 3bP: a hold lasts Range(90,300) s, then DevHoldTick decides (it was Double.MaxValue, so nothing ever happened while held).
        private StepPlan devHold;
        private void DevHold(string reason){devHold=new StepPlan(DevHeldState,Range(90,300));Begin(devHold,reason);}
        internal void DevSelectState(string state)
        {
            if(!IsRest(state) || !Supports(state))throw new InvalidOperationException("State artwork is unavailable: "+state);
            DevHeldState=state;plans.Clear();PerchedWindow=0;FootY=Area.Bottom;
            AttentionActive=false;attentionPending=false;hovering=false;hoverUntil=0;attentionRestUntil=0;lookCooldown=0;
            DevHold("Dev: holding "+state);Clamp();
        }
        internal void DevResumeRoutine()
        {
            DevHeldState=null;devHold=null;Paused=false;plans.Clear();PerchedWindow=0;FootY=Area.Bottom;
            Begin(new StepPlan("sit",1),"Returning to autonomous routine");Clamp();
        }
        private bool DevReturnToHeldState()
        {
            if(DevHeldState==null)return false;
            if(plans.Count>0 && (!IsRest(plans.Peek().Clip) || plans.Peek().Perch))return false;
            plans.Clear();DevHold("Dev: holding "+DevHeldState);return true;
        }
        // V12 3bP: the held state's tick (Hatt, 2026-11-08: "perch and zoomies are animations for any state that isn't sleeping").
        // Zoomies or a perch under the routine's cooldowns and energy, or leaving a perch (20 %, as Decide), else hold again.
        // Never a groom, stretch or pose change while held; a sleeping hold only holds again.
        private bool DevHoldTick()
        {
            if(DevHeldState==null || devHold==null || current!=devHold || Age<Duration)return false;
            devHold=null;
            if(PerchedWindow!=0){if(random.NextDouble()<.20){LeavePerch();return true;}}
            else if(!IsSleeping(Clip))
            {
                if(ZoomiesEnabled && Clock>=zoomCooldown && Energy>.55 && random.NextDouble()<.28){Zoomies();return true;}
                if(Clock>=perchCooldown && random.NextDouble()<.18 && TryPerch(false))return true;
            }
            DevHold("Dev: holding "+DevHeldState);return true;
        }
        // A still is drawn over a paused body, so Step never runs: the window ticks its hold here, with a rest's clock and energy.
        // An action unpauses it; a rest step (the hold again, or the sitting step after a perch landing) stays paused.
        internal void DevStepPausedHold(double dt)
        {
            if(!Paused || DevHeldState==null || !IsRest(Clip))return;
            dt=Math.Min(.1,Math.Max(0,dt));Clock+=dt;Age+=dt;Energy=Math.Min(1,Energy+dt*.00035);
            if(Age<Duration)return;
            Paused=false;AdvancePlan("Following the relaxed routine");Paused=IsRest(Clip);
        }
        // The clip playing now, then the queued steps (read by --test-perch-zoomies).
        internal string[] DevPlannedClips {get{List<string> all=new List<string>{Clip};foreach(StepPlan step in plans)all.Add(step.Clip);return all.ToArray();}}
    }

    internal sealed partial class OdinWindow
    {
        private string devState="sit",devPreviewClip;
        private int devPreviewFrame,devGaze=-2,devStillPoseId;
        private bool devPreviewPlaying,devPreviewLoop,devFollowMouse,devBlink;
        private double devPreviewBegan;
        private readonly Dictionary<string,ToolStripMenuItem> devStateMenus=new Dictionary<string,ToolStripMenuItem>();
        private static readonly string[] DevStates={"sit","loaf","lay","sleep"};
        private static string StateName(string state)
        {
            switch(state){case "sit":return "Sitting forward";case "loaf":return "Loaf";case "lay":return "Laying";case "sleep":return "Sleeping";case "belly":return "Belly up (excluded from routine)";case "belly_sleep":return "Belly sleeping (excluded from routine)";default:return state;}
        }
        private void SelectDevState(string state)
        {
            devStillPoseId=0;devSuspendedStill=0;devPreviewClip=null;devPreviewPlaying=false;devBlink=false;devGaze=-2;devFollowMouse=false;devFollowWindow=false;devState=state;
            mind.DevSelectState(state);mind.Paused=false;layingMouse.Reset();loafMouse.Reset();sittingTracker.Reset();
            ResetHeadTurnTrackers();headTurnWatch=true;
        }
        private void DevAction(string state,Action action)
        {
            SelectDevState(state);action();
        }
        // Window tops the DEV perch item may use: the desktop's, or a fixed list in a headless test.
        private Func<List<WindowSurface>> devSurfaceSource;
        private List<WindowSurface> DevSurfaces(){return devSurfaceSource!=null?devSurfaceSource():Native.Surfaces(ownProcess);}
        private void DevPerch(string state){DevAction(state,DevTryPerch);}
        private void DevTryPerch()
        {
            bool enabled=mind.PerchesEnabled;mind.PerchesEnabled=true;mind.UpdateWindows(DevSurfaces());
            bool found=mind.TryPerch(true);mind.PerchesEnabled=enabled;
            if(!found && !menuTesting)tray.ShowBalloonTip(3500,"Dev: no available perch","Move a window low enough for Odin to perch, then try again.",ToolTipIcon.Info);
        }
        // V12 3bP: stills 1-3 and 6/7/8 are a paused sitting body whose hold still ticks (CatMind.DevStepPausedHold), so it may start
        // zoomies or a perch. The still is suspended while mind.Clip is not a rest clip (6/7/8 have no transition art: the still cuts
        // to the sitting body's run or jump, the Q2 assumption) and comes back, paused, once it is a rest clip again. Still 4
        // (sleeping) and the action stills never tick.
        private int devSuspendedStill;
        private static string DevStillState(int id){return id>=1 && id<=DevStates.Length?DevStates[id-1]:DevArtwork.HeadTurnFamily(id)!=null?"sit":null;}
        private void DevStillTick(double dt)
        {
            if(devStillPoseId>0)
            {
                string state=DevStillState(devStillPoseId);
                if(state!=null && !CatMind.IsSleeping(state))mind.DevStepPausedHold(dt);
                if(!CatMind.IsRest(mind.Clip)){devSuspendedStill=devStillPoseId;devStillPoseId=0;mind.Paused=false;}
            }
            else if(devSuspendedStill>0 && CatMind.IsRest(mind.Clip)){devStillPoseId=devSuspendedStill;devSuspendedStill=0;mind.Paused=true;}
        }
        // An Actions item on a 6/7/8 still: the action starts from the paused sitting body and the still returns afterwards.
        private void DevStillAction(Action action)
        {
            mind.Paused=false;action();
            if(CatMind.IsRest(mind.Clip))mind.Paused=true;else{devSuspendedStill=devStillPoseId;devStillPoseId=0;}
        }
        private void DevPounce(string state)
        {
            DevAction(state,delegate
            {
                // Sleeping is first woken for this action. Use a reachable floor target.
                if(CatMind.IsSleeping(mind.Clip)){mind.DevSelectState("sit");mind.DevHeldState=state;}
                bool enabled=mind.ClickPouncesEnabled;mind.ClickPouncesEnabled=true;
                Point point=new Point((int)Math.Max(mind.MinX,Math.Min(mind.MaxX,mind.X+120)),mind.Area.Bottom-1);
                mind.ClickReleased(point,true);mind.ClickPouncesEnabled=enabled;
            });
        }
        private void PreviewDevAnimation(string clip,bool play,bool loop,int frame)
        {
            SelectDevState(devState);devPreviewClip=clip;devPreviewFrame=frame;
            devPreviewPlaying=play;devPreviewLoop=loop;devPreviewBegan=watch.Elapsed.TotalSeconds;
        }
        private void AdvanceDevPreview(double now)
        {
            if(devPreviewClip==null || !devPreviewPlaying)return;
            if(devBlink)
            {
                int step=(int)(Math.Max(0,now-devPreviewBegan)/.12);
                if(step>=5){devPreviewPlaying=false;devBlink=false;devPreviewClip=null;return;}
                devPreviewFrame=new[]{0,1,2,1,0}[step];return;
            }
            int frames=DevFrames(devPreviewClip);double seconds=DevSeconds(devPreviewClip);
            double progress=Math.Max(0,(now-devPreviewBegan)/seconds);
            if(!devPreviewLoop && progress>=1){devPreviewFrame=frames-1;devPreviewPlaying=false;return;}
            devPreviewFrame=Math.Min(frames-1,(int)((devPreviewLoop?progress%1:progress)*frames));
        }
        // V12 2b: DEV run clips have their own timeline in the pack (originals plus kept tweens, one frame per tick).
        private DevActionClip DevRunTimeline(string clip){return RunningMotion.IsRun(clip)?latestArt.ActionClip(clip):null;}
        private int DevFrames(string clip){DevActionClip run=DevRunTimeline(clip);return run!=null?run.frames:art.Manifest.clips[clip].frames;}
        private double DevSeconds(string clip)
        {
            DevActionClip run=DevRunTimeline(clip);if(run!=null)return run.seconds;
            ClipInfo info=art.Manifest.clips[clip];return info.seconds>0?info.seconds:Math.Max(1,info.frames*.15);
        }
        // 3.2.0 testing menu (Hatt 2026-11-08, "simplify the menu too"): Auto; Awake states; Sleeping states; Actions (blink, follow
        // mouse, follow window, swish tail, Clock gazes submenu); Animations submenu (zoomies, grooming, window perch); Options submenu
        // (the release menu's three "Turn off" switches); Quit Odin. Latest artwork stills, transitions and the clip browser are gone.
        private ToolStripMenuItem devAuto,devBlinkItem,devFollowMouseItem,devFollowWindowItem,devTailItem,devClockGazes,devAnimationsMenu,devZoomiesItem,devGroomItem,devPerchItem,devOptions;
        private readonly List<ToolStripItem> devActionItems=new List<ToolStripItem>();
        private bool devFollowWindow;private PointF devWindowPoint;
        private static bool IsDevAnimation(string clip)
        {
            return !CatMind.IsRest(clip) && !clip.StartsWith("look_") && !clip.EndsWith("_eyes") && !clip.EndsWith("_tail");
        }
        // A numbered still of a resting state (ids 1-4 = DevStates order, as DevArtwork.FamilyId) stands for that state.
        private string CurrentDevState {get{return devStillPoseId>=1 && devStillPoseId<=DevStates.Length?DevStates[devStillPoseId-1]:CatMind.IsRest(mind.Clip)?mind.Clip:devState;}}
        private bool DevHasGaze(string state){return art.Manifest.clips.ContainsKey("look_"+state) && art.Manifest.clips["look_"+state].frames>1 && !CatMind.IsSleeping(state);}
        // Follow window: the foreground window's top centre (the point the release build's window attention uses), else the cursor.
        private PointF DevFollowPoint(Point cursor)
        {
            if(!devFollowWindow)return cursor;
            IntPtr window=Native.GetForegroundWindow();Rectangle rect;
            if(window!=Handle && window!=menu.Handle && Native.Bounds(window,out rect))devWindowPoint=new PointF(rect.Left+rect.Width*.5f,rect.Top+Math.Min(80,rect.Height*.25f));
            return devWindowPoint.IsEmpty?(PointF)cursor:devWindowPoint;
        }
        private void DevFollow(bool window)
        {
            string state=CurrentDevState;bool on=!(devFollowMouse && devFollowWindow==window);
            SelectDevState(state);devFollowMouse=on;devFollowWindow=on && window;devWindowPoint=PointF.Empty;
        }
        private static string ClockLabel(string state,int frame)
        {
            if(state=="sit"){double time=SittingGaze.Angles[frame]/30;int hour=(int)time,minute=(int)Math.Round((time-hour)*60);return (hour==0?12:hour)+":"+minute.ToString("00");}
            return frame==12?"11:20":frame==13?"11:40":(frame==0?12:frame)+" o'clock";
        }
        private void PopulateClockGazes()
        {
            devClockGazes.DropDownItems.Clear();string state=CurrentDevState;
            if(!DevHasGaze(state))return;
            int count=state=="sit"?SittingGaze.Angles.Length:art.Manifest.clips["look_"+state].frames;
            for(int i=0;i<count;i++)
            {
                int frame=i;ToolStripMenuItem item=new ToolStripMenuItem(ClockLabel(state,i)){Checked=devGaze==frame && mind.DevHeldState==state};
                item.Click+=delegate{SelectDevState(state);devGaze=frame;PaintSprite(true);};devClockGazes.DropDownItems.Add(item);
            }
        }
        private void RefreshDevMenu()
        {
            string state=CurrentDevState;bool awake=!CatMind.IsSleeping(state);
            devAuto.Checked=mind.DevHeldState==null && devStillPoseId==0 && devPreviewClip==null;
            foreach(KeyValuePair<string,ToolStripMenuItem> item in devStateMenus)item.Value.Checked=!devAuto.Checked && devPreviewClip==null && state==item.Key;
            devBlinkItem.Enabled=awake && art.Manifest.clips.ContainsKey(state+"_eyes");
            devFollowMouseItem.Enabled=devFollowWindowItem.Enabled=DevHasGaze(state);
            devFollowMouseItem.Checked=devFollowMouse && !devFollowWindow;devFollowWindowItem.Checked=devFollowMouse && devFollowWindow;
            devTailItem.Enabled=art.Manifest.clips.ContainsKey(state+"_tail");
            devClockGazes.Enabled=DevHasGaze(state);PopulateClockGazes();
            devZoomiesItem.Enabled=awake && mind.Supports("run_left") && mind.Supports("run_right");
            devGroomItem.Enabled=awake && mind.Supports("groom");
            devPerchItem.Enabled=awake && mind.HasJumps;
        }
        private ToolStripMenuItem DevMenuItem(ToolStripItemCollection into,string title,Action action)
        {
            ToolStripMenuItem item=new ToolStripMenuItem(title);if(action!=null)item.Click+=delegate{action();PaintSprite(true);};into.Add(item);return item;
        }
        private ToolStripMenuItem DevActionMenuItem(string title,Action action){ToolStripMenuItem item=DevMenuItem(menu.Items,title,action);devActionItems.Add(item);return item;}
        private void BuildDevMenu()
        {
            devAuto=DevMenuItem(menu.Items,"Auto",delegate{devStillPoseId=0;devSuspendedStill=0;devPreviewClip=null;devFollowMouse=false;devFollowWindow=false;devGaze=-2;mind.DevResumeRoutine();});
            menu.Items.Add(new ToolStripSeparator());Section("Awake states");
            foreach(string value in DevStates)
            {
                string state=value;if(value=="sleep"){menu.Items.Add(new ToolStripSeparator());Section("Sleeping states");}
                ToolStripMenuItem item=DevMenuItem(menu.Items,StateName(state),delegate{SelectDevState(state);});item.Enabled=mind.Supports(state);devStateMenus.Add(state,item);
            }
            menu.Items.Add(new ToolStripSeparator());Section("Actions");
            devBlinkItem=DevActionMenuItem("Blink",delegate{string state=CurrentDevState;SelectDevState(state);PreviewDevAnimation(state+"_eyes",true,false,0);devBlink=true;});
            devFollowMouseItem=DevActionMenuItem("Follow mouse",delegate{DevFollow(false);});
            devFollowWindowItem=DevActionMenuItem("Follow window",delegate{DevFollow(true);});
            devTailItem=DevActionMenuItem("Swish tail",delegate{string state=CurrentDevState;SelectDevState(state);PreviewDevAnimation(state+"_tail",true,false,0);});
            devClockGazes=DevActionMenuItem("Clock gazes",null);devClockGazes.DropDownOpening+=delegate{PopulateClockGazes();};
            menu.Items.Add(new ToolStripSeparator());
            devAnimationsMenu=DevMenuItem(menu.Items,"Animations",null);
            devZoomiesItem=DevMenuItem(devAnimationsMenu.DropDownItems,"Zoomies",delegate{DevAction(CurrentDevState,mind.Zoomies);});
            devGroomItem=DevMenuItem(devAnimationsMenu.DropDownItems,"Grooming",delegate{DevAction(CurrentDevState,mind.Groom);});
            devPerchItem=DevMenuItem(devAnimationsMenu.DropDownItems,"Window perch",delegate{DevPerch(CurrentDevState);});
            foreach(ToolStripItem item in devAnimationsMenu.DropDownItems)devActionItems.Add(item);
            menu.Items.Add(new ToolStripSeparator());
            devOptions=DevMenuItem(menu.Items,"Options",null);AddOptionItems(devOptions.DropDownItems);
            menu.Items.Add(new ToolStripSeparator());Item("Quit Odin",Close);
            RefreshDevMenu();menu.Opening+=delegate{RefreshDevMenu();};
        }
        internal static readonly string[] DevTopLevel={"Auto","|","Awake states","Sitting forward","Loaf","Laying","|","Sleeping states","Sleeping","|","Actions","Blink","Follow mouse","Follow window","Swish tail","Clock gazes","|","Animations","|","Options","|","Quit Odin"};
        internal static List<string> TopLevelTexts(ToolStripItemCollection items)
        {
            List<string> texts=new List<string>();foreach(ToolStripItem item in items)texts.Add(item is ToolStripSeparator?"|":item.Text);return texts;
        }
        private ToolStripMenuItem DevActionNamed(string text){RefreshDevMenu();foreach(ToolStripItem item in devActionItems)if(item.Text==text)return (ToolStripMenuItem)item;return null;}
        // What the next paint would draw. Headless (--test-menus) there is no drawing surface, so PaintSprite does nothing and the
        // check reads ChooseSprite, the code PaintSprite draws from; with a surface (--dev-controls-test) it reads the painted values.
        private SpriteChoice DevNextDrawn(){if(memoryDC!=IntPtr.Zero){PaintSprite(true);return new SpriteChoice{DisplayClip=drawnDisplayClip,DisplayFrame=drawnDisplayFrame};}mind.ClampHorizontal();mind.Clamp();return ChooseSprite(new Point(-20000,-20000));}
        internal object CheckDevControls()
        {
            bool painted=memoryDC!=IntPtr.Zero;
            string shape=String.Join(" / ",TopLevelTexts(menu.Items).ToArray());
            if(shape!=String.Join(" / ",DevTopLevel))throw new Exception("Testing menu is not Hatt's layout: "+shape);
            if(devStateMenus.Count!=DevStates.Length)throw new Exception("Testing menu omits states.");
            List<string> animationTexts=new List<string>(),optionTexts=new List<string>();
            foreach(ToolStripItem item in devAnimationsMenu.DropDownItems)animationTexts.Add(item.Text);
            foreach(ToolStripItem item in devOptions.DropDownItems)optionTexts.Add(item.Text);
            if(String.Join(",",animationTexts.ToArray())!="Zoomies,Grooming,Window perch")throw new Exception("Animations submenu: "+String.Join(",",animationTexts.ToArray()));
            if(String.Join(",",optionTexts.ToArray())!="Turn off zoomies,Turn off window perch,Turn off follow")throw new Exception("Options submenu: "+String.Join(",",optionTexts.ToArray()));
            SelectDevState("lay");
            if(!DevActionNamed("Follow mouse").Enabled || !DevActionNamed("Follow window").Enabled)throw new Exception("Laying actions omit following.");
            DevActionNamed("Follow window").PerformClick();if(!devFollowMouse || !devFollowWindow || mind.DevHeldState!="lay")throw new Exception("Follow window did not switch on while laying.");
            DevActionNamed("Follow mouse").PerformClick();if(!devFollowMouse || devFollowWindow)throw new Exception("Follow mouse did not replace follow window.");
            DevActionNamed("Follow mouse").PerformClick();if(devFollowMouse)throw new Exception("Follow mouse did not switch off on a second click.");
            foreach(string state in DevStates)
            {
                devStateMenus[state].PerformClick();double x=mind.X,y=mind.FootY;
                for(int i=0;i<100;i++)mind.Step(.1,Point.Empty);
                if(mind.Clip!=state || mind.X!=x || mind.FootY!=y)throw new Exception("Selected state was not held: "+state);
                bool awake=!CatMind.IsSleeping(state);
                foreach(string text in new[]{"Blink","Follow mouse","Follow window","Clock gazes","Zoomies","Grooming","Window perch"})
                {
                    bool expected=awake && (text!="Blink" || art.Manifest.clips.ContainsKey(state+"_eyes")) && (text!="Window perch" || mind.HasJumps);
                    if(DevActionNamed(text).Enabled!=expected)throw new Exception(text+" is "+(expected?"disabled":"enabled")+" while "+state+" (awake only)");
                }
                if(DevActionNamed("Swish tail").Enabled!=art.Manifest.clips.ContainsKey(state+"_tail"))throw new Exception("Swish tail eligibility is wrong while "+state);
            }
            foreach(string clip in art.Manifest.clips.Keys)
            {
                PreviewDevAnimation(clip,true,false,0);AdvanceDevPreview(devPreviewBegan+1000);
                if(devPreviewPlaying || devPreviewFrame!=DevFrames(clip)-1)throw new Exception("Play-once preview did not hold its final frame: "+clip);
                PaintSprite(true);
            }
            Dictionary<string,int> clockCounts=new Dictionary<string,int>();
            foreach(string state in new[]{"sit","loaf","lay"})
            {
                devStateMenus[state].PerformClick();RefreshDevMenu();int count=devClockGazes.DropDownItems.Count;clockCounts.Add(state,count);
                int expected=state=="sit"?SittingGaze.Angles.Length:art.Manifest.clips["look_"+state].frames;
                if(count!=expected)throw new Exception("Clock gazes lists "+count+" directions while "+state+" (expected "+expected+")");
                for(int i=0;i<count;i++)
                {
                    devClockGazes.DropDownItems[i].PerformClick();SpriteChoice drawn=DevNextDrawn();
                    if(drawn.DisplayClip!="look_"+state || drawn.DisplayFrame!=i)throw new Exception("Clock gaze did not draw: "+state+":"+i+" drew "+drawn.DisplayClip+"/"+drawn.DisplayFrame);
                }
            }
            if(painted)
            {
            SelectDevState("loaf");devGaze=3;PaintSprite(true);
            Rectangle loafOriginal=RunningScale.VisibleBounds(art.Get("look_loaf",3)),loafVisible=RunningScale.VisibleBounds(canvas);
            double loafExpected=loafOriginal.Width*RunningScale.Pixels(size,"loaf")/(double)art.Manifest.width;
            if(Math.Abs(loafVisible.Width-loafExpected)>5)throw new Exception("Loaf gaze did not use the smaller state scale.");
            }
            foreach(string state in DevStates)
            {
                if(state!="sleep")for(int eye=0;eye<3;eye++){SelectDevState(state);PreviewDevAnimation(state+"_eyes",false,false,eye);if(DevNextDrawn().DisplayFrame!=eye)throw new Exception("Eye expression did not draw: "+state+":"+eye);}
                if(art.Manifest.clips.ContainsKey(state+"_tail"))for(int tail=0;tail<5;tail++){SelectDevState(state);PreviewDevAnimation(state+"_tail",false,false,tail);if(DevNextDrawn().DisplayFrame!=tail)throw new Exception("Tail swish frame did not draw: "+state+":"+tail);}
            }
            devStateMenus["sit"].PerformClick();int bursts=mind.CompletedZoomies;DevActionNamed("Zoomies").PerformClick();if(mind.CompletedZoomies!=bursts+1)throw new Exception("Zoomies did not start.");
            for(int i=0;i<500;i++)mind.Step(.1,Point.Empty);if(mind.Clip!="sit")throw new Exception("Zoomies did not return to held state.");
            devStateMenus["sit"].PerformClick();DevActionNamed("Grooming").PerformClick();
            bool groomed=false;for(int i=0;i<500;i++){mind.Step(.1,Point.Empty);if(mind.Clip.StartsWith("groom"))groomed=true;}
            if(!groomed)throw new Exception("Grooming did not play.");if(mind.Clip!="sit")throw new Exception("Grooming did not return to held state.");
            // Options: each "Turn off" switch flips its setting and the mind (settings are ephemeral in this test).
            ToolStripMenuItem offZoomies=(ToolStripMenuItem)devOptions.DropDownItems[0],offPerch=(ToolStripMenuItem)devOptions.DropDownItems[1],offFollow=(ToolStripMenuItem)devOptions.DropDownItems[2];
            offZoomies.PerformClick();offPerch.PerformClick();offFollow.PerformClick();
            if(mind.ZoomiesEnabled || settings.Zoomies || mind.PerchesEnabled || settings.WindowPerches || settings.Watch)throw new Exception("Turn off options did not turn the features off.");
            offZoomies.PerformClick();offPerch.PerformClick();offFollow.PerformClick();
            if(!mind.ZoomiesEnabled || !settings.Zoomies || mind.PerchesEnabled!=mind.HasJumps || !settings.WindowPerches || !settings.Watch)throw new Exception("Turn off options did not turn the features back on.");
            int runningFrames=0;
            if(painted)foreach(string name in new[]{"run_left","run_right"})
            for(int i=0;i<art.Manifest.clips[name].frames;i++)
            {
                PreviewDevAnimation(name,false,false,i);PaintSprite(true);
                Rectangle visible=RunningScale.VisibleBounds(canvas);
                if(visible.IsEmpty || visible.Left<=0 || visible.Top<=0 || visible.Right>=canvas.Width || visible.Bottom>=canvas.Height)throw new Exception("Enlarged running pose was clipped: "+name+":"+i);
                Rectangle original=RunningScale.VisibleBounds(art.Get(name,i));
                double expected=original.Width*RunningScale.Pixels(size,name)/(double)art.Manifest.width;
                if(Math.Abs(visible.Width-expected)>5)throw new Exception("Running renderer used the wrong scale: "+name+":"+i);
                runningFrames++;
            }
            devAuto.PerformClick();if(mind.DevHeldState!=null)throw new Exception("Auto did not resume the routine.");
            return new{passed=true,layout=shape,states=devStateMenus.Count,animations=animationTexts,options=optionTexts,clockGazes=clockCounts,followWindow=true,awakeOnlyActions=true,
                allStatesHold=true,playOnceStops=true,eyesAndTailsVerified=true,canvasChecks=painted?"loaf scale and enlarged running frames checked on the painted canvas":"headless: no drawing surface; canvas checks run in --dev-controls-test",enlargedRunningFramesChecked=runningFrames,
                actionsReturnToState=true,turnOffOptions=true,autoResumes=true};
        }
    }
}
#endif
