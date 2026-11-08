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
                if(Clock>=zoomCooldown && Energy>.55 && random.NextDouble()<.28){Zoomies();return true;}
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
        private ToolStripMenuItem devStatus,devRoot,devAnimations;
        private readonly Dictionary<string,ToolStripMenuItem> devStateMenus=new Dictionary<string,ToolStripMenuItem>();
        private static readonly string[] DevStates={"sit","loaf","lay","sleep"};
        private static string StateName(string state)
        {
            switch(state){case "sit":return "Sitting";case "loaf":return "Loaf";case "lay":return "Laying down (awake)";case "sleep":return "Sleeping";case "belly":return "Belly up (excluded from routine)";case "belly_sleep":return "Belly sleeping (excluded from routine)";default:return state;}
        }
        private ToolStripMenuItem DevItem(ToolStripMenuItem parent,string title,Action action,bool enabled=true)
        {
            ToolStripMenuItem item=new ToolStripMenuItem(title){Enabled=enabled};
            if(action!=null)item.Click+=delegate{action();PaintSprite(true);};parent.DropDownItems.Add(item);return item;
        }
        private void SelectDevState(string state)
        {
            devStillPoseId=0;devSuspendedStill=0;devPreviewClip=null;devPreviewPlaying=false;devBlink=false;devGaze=-2;devFollowMouse=false;devState=state;
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
        private ToolStripMenuItem devTransitions,devActions;
        private static bool IsDevAnimation(string clip)
        {
            return !CatMind.IsRest(clip) && !clip.StartsWith("look_") && !clip.EndsWith("_eyes") && !clip.EndsWith("_tail");
        }
        // A numbered still of a resting state (ids 1-4 = DevStates order, as DevArtwork.FamilyId) stands for that state in the
        // Actions and Transitions menus. Stills are drawn over a paused sitting body, so mind.Clip alone sent loaf/lay to sitting.
        private string CurrentDevState {get{return devStillPoseId>=1 && devStillPoseId<=DevStates.Length?DevStates[devStillPoseId-1]:CatMind.IsRest(mind.Clip)?mind.Clip:devState;}}
        private void PopulateDevTransitions()
        {
            devTransitions.DropDownItems.Clear();string from=CurrentDevState;
            foreach(string value in DevStates)
            {
                string target=value;if(target==from)continue;
                ToolStripMenuItem item=DevItem(devTransitions,StateName(target),delegate
                {
                    // Leaving a still: start from the state it shows and unpause, so the transition actually plays.
                    if(devStillPoseId>0){devStillPoseId=0;mind.Paused=false;mind.DevSelectState(from);}
                    devSuspendedStill=0;devPreviewClip=null;devFollowMouse=false;devGaze=-2;devState=target;
                    if(target=="belly" || target=="belly_sleep")SelectDevState(target);
                    else {mind.DevHeldState=target;mind.Rest(target,Double.MaxValue);}
                },mind.Supports(target));
                if(!art.Manifest.clips.ContainsKey(from+"_to_"+target))item.ToolTipText="No transition animation in this build; switches to the selected state.";
            }
        }
        private void PopulateDevActions()
        {
            devActions.DropDownItems.Clear();string state=CurrentDevState;
            string headTurn=HeadTurnStillFamily();
            if(headTurn!=null)
            {
                // V12 3R: a 6/7/8 still with head-turn frames. Keep the still on screen (never SelectDevState here).
                devActions.ToolTipText="Head turns of still "+devStillPoseId;
                DevItem(devActions,"Watch the mouse when near",delegate{devFollowMouse=false;devGaze=-2;headTurnWatch=true;ResetHeadTurnTrackers();});
                DevItem(devActions,"Follow mouse",delegate{devFollowMouse=true;devGaze=-2;headTurnWatch=true;ResetHeadTurnTrackers();});
                DevItem(devActions,"Hold the still (no head turns)",delegate{devFollowMouse=false;devGaze=-2;headTurnWatch=false;ResetHeadTurnTrackers();});
                ToolStripMenuItem turns=DevItem(devActions,"Head turn clock - "+HeadTurnDirections.Length+" hours",null);
                for(int i=0;i<HeadTurnDirections.Length;i++){int frame=i;DevItem(turns,HeadTurnDirections[i],delegate{devFollowMouse=false;devGaze=frame;});}
                // V12 3bP: seated views are awake, so they get zoomies and perches; the still comes back afterwards.
                devActions.DropDownItems.Add(new ToolStripSeparator());
                DevItem(devActions,"Zoomies",delegate{DevStillAction(mind.Zoomies);},mind.Supports("run_left") && mind.Supports("run_right"));
                DevItem(devActions,"Try a window perch",delegate{DevStillAction(DevTryPerch);},mind.HasJumps);
                DevItem(devActions,"Leave current perch",delegate{DevStillAction(mind.LeavePerch);},mind.PerchedWindow!=0);
                return;
            }
            devActions.ToolTipText="Actions while "+StateName(state).ToLowerInvariant();
            bool hasGaze=art.Manifest.clips.ContainsKey("look_"+state) && art.Manifest.clips["look_"+state].frames>1 && !CatMind.IsSleeping(state);
            if(hasGaze)
            {
                DevItem(devActions,"Automatic held glances",delegate{SelectDevState(state);});
                DevItem(devActions,"Follow mouse",delegate{SelectDevState(state);devFollowMouse=true;});
                ToolStripMenuItem directions=DevItem(devActions,IsClockRest(state)?"Clock gaze - "+art.Manifest.clips["look_"+state].frames+" poses":"Look in a direction",null);
                if(IsClockRest(state))
                {
                    for(int i=0;i<art.Manifest.clips["look_"+state].frames;i++){int frame=i;DevItem(directions,(i==12?"11:20":i==13?"11:40":(i==0?12:i)+" o'clock"),delegate{SelectDevState(state);devGaze=frame;});}
                }
                else foreach(KeyValuePair<string,int> direction in new Dictionary<string,int>{{"Forward",-1},{"Left",4},{"Right",12},{"Up",0},{"Down",8}})
                {
                    int frame=direction.Value;DevItem(directions,direction.Key,delegate{SelectDevState(state);devGaze=frame;});
                }
            }
            else DevItem(devActions,CatMind.IsSleeping(state)?"Sleeping - use Transitions to wake him":"No gaze actions for this state",null,false);
            if(state!="sleep" && art.Manifest.clips.ContainsKey(state+"_eyes"))
            {
                ToolStripMenuItem eyes=DevItem(devActions,"Eyes / blink (3 poses)",null);
                foreach(KeyValuePair<string,int> eye in new Dictionary<string,int>{{"Open",0},{"Half closed",1},{"Closed",2}})
                {
                    int frame=eye.Value;DevItem(eyes,eye.Key,delegate{SelectDevState(state);PreviewDevAnimation(state+"_eyes",false,false,frame);});
                }
                DevItem(eyes,"Blink",delegate{SelectDevState(state);PreviewDevAnimation(state+"_eyes",true,false,0);devBlink=true;});
            }
            if(art.Manifest.clips.ContainsKey(state+"_tail"))DevItem(devActions,"Tail flick (5 frames)",delegate{SelectDevState(state);PreviewDevAnimation(state+"_tail",true,false,0);});
            if(state=="sit")
            {
                ToolStripMenuItem all=DevItem(devActions,"All sitting gaze stages (19)",null);
                for(int i=0;i<SittingGaze.Angles.Length;i++)
                {
                    int frame=i;double time=SittingGaze.Angles[i]/30;int hour=(int)time,minute=(int)Math.Round((time-hour)*60);
                    DevItem(all,(hour==0?12:hour)+":"+minute.ToString("00"),delegate{SelectDevState("sit");devGaze=frame;});
                }
            }
            devActions.DropDownItems.Add(new ToolStripSeparator());
            // V12 3bP: zoomies and perches from any awake state, never asleep.
            DevItem(devActions,"Zoomies",delegate{DevAction(state,mind.Zoomies);},mind.Supports("run_left") && mind.Supports("run_right") && !CatMind.IsSleeping(state));
            DevItem(devActions,"Try a window perch",delegate{DevPerch(state);},mind.HasJumps && !CatMind.IsSleeping(state));
            DevItem(devActions,"Leave current perch",delegate{devPreviewClip=null;mind.LeavePerch();},mind.PerchedWindow!=0);
            DevItem(devActions,"Click pounce",delegate{DevPounce(state);},mind.HasPounces && !CatMind.IsSleeping(state));
            DevItem(devActions,"Crab bounce",delegate{DevAction(state,mind.CrabBounce);},mind.HasCrab && !CatMind.IsSleeping(state));
        }
        private void BuildDevMenu()
        {
            devStatus=new ToolStripMenuItem("States"){Enabled=false};menu.Items.Add(devStatus);
            ToolStripMenuItem latest=new ToolStripMenuItem("Latest artwork stills ("+latestArt.Poses.Length+")");menu.Items.Add(latest);
            foreach(DevArtEntry entry in latestArt.Poses)
            {
                int id=entry.id;DevItem(latest,entry.label,delegate{SelectDevState("sit");devStillPoseId=id;mind.Paused=true;mind.Clamp();});
            }
            menu.Items.Add(new ToolStripMenuItem(latestArt.UsesWholeFrameRestFamilies?"Gaze: original whole-body frames, body held still (v11)":latestArt.UsesOriginalRestFamilies?"Gaze: original matching head and body frames":latestArt.HasEightDirections?"Gaze: eight directions (diagonal art testing)":latestArt.HasFullCardinals?"Gaze: four directions; diagonals use nearest cardinal":"Gaze: horizontal only; vertical art pending"){Enabled=false});
            if(latestArt.HasFullCardinals)menu.Items.Add(new ToolStripMenuItem("Up/down gaze artwork is a test candidate"){Enabled=false});
            menu.Items.Add(new ToolStripMenuItem(latestArt.UsesOriginalRestFamilies?"Original style recovery; further artwork corrections pending":latestArt.HasUpdatedJumps?"Jump animations use latest fur; other action artwork unchanged":"Action loops retain original fur; latest jump stills are in the menu above"){Enabled=false});
            foreach(string value in DevStates)
            {
                string state=value;ToolStripMenuItem item=new ToolStripMenuItem(StateName(state)){Enabled=mind.Supports(state)};
                item.Click+=delegate{SelectDevState(state);PaintSprite(true);};menu.Items.Add(item);devStateMenus.Add(state,item);
            }
            menu.Opening+=delegate{foreach(KeyValuePair<string,ToolStripMenuItem> item in devStateMenus)item.Value.Checked=devPreviewClip==null && mind.Clip==item.Key;};
            menu.Items.Add(new ToolStripSeparator());
            devTransitions=new ToolStripMenuItem("Transitions to other states");menu.Items.Add(devTransitions);PopulateDevTransitions();devTransitions.DropDownOpening+=delegate{PopulateDevTransitions();};
            devActions=new ToolStripMenuItem("Actions in current state");menu.Items.Add(devActions);PopulateDevActions();devActions.DropDownOpening+=delegate{PopulateDevActions();};
            devAnimations=new ToolStripMenuItem("Animations");menu.Items.Add(devAnimations);
            List<string> clips=new List<string>(art.Manifest.clips.Keys);clips.Sort();
            foreach(string value in clips)
            {
                string clip=value;if(!IsDevAnimation(clip))continue;
                int frameCount=DevFrames(clip);ToolStripMenuItem group=DevItem(devAnimations,clip+" ("+frameCount+" frames)",null);group.Tag=clip;
                DevItem(group,"Play once, then hold last frame",delegate{PreviewDevAnimation(clip,true,false,0);});
                DevItem(group,"Loop preview only",delegate{PreviewDevAnimation(clip,true,true,0);});
                ToolStripMenuItem frames=DevItem(group,"Choose a frame",null);
                for(int i=0;i<frameCount;i++){int frame=i;DevItem(frames,"Frame "+frame,delegate{PreviewDevAnimation(clip,false,false,frame);});}
            }
            Item("Stop preview / hold current state",delegate{SelectDevState(CurrentDevState);});
            Item("Resume autonomous routine",delegate{devStillPoseId=0;devSuspendedStill=0;devPreviewClip=null;devFollowMouse=false;devGaze=-2;mind.DevResumeRoutine();});
            menu.Items.Add(new ToolStripSeparator());
        }
        internal object CheckDevControls()
        {
            int expectedAnimations=0;foreach(string clip in art.Manifest.clips.Keys)if(IsDevAnimation(clip))expectedAnimations++;
            if(devStateMenus.Count!=DevStates.Length || devAnimations.DropDownItems.Count!=expectedAnimations)throw new Exception("Dev menu omits states or animations.");
            foreach(ToolStripItem entry in devAnimations.DropDownItems)if(!IsDevAnimation((string)entry.Tag))throw new Exception("A state or gaze pose leaked into Animations.");
            SelectDevState("lay");PopulateDevTransitions();PopulateDevActions();
            if(devTransitions.DropDownItems.Count!=DevStates.Length-1)throw new Exception("Transitions must list only other states.");
            bool follows=false;foreach(ToolStripItem entry in devActions.DropDownItems)if(entry.Text=="Follow mouse")follows=true;
            if(!follows)throw new Exception("Laying actions omit mouse tracking.");
            foreach(string state in DevStates)
            {
                SelectDevState(state);double x=mind.X,y=mind.FootY;
                for(int i=0;i<100;i++)mind.Step(.1,Point.Empty);
                if(mind.Clip!=state || mind.X!=x || mind.FootY!=y)throw new Exception("Selected state was not held: "+state);
            }
            foreach(string clip in art.Manifest.clips.Keys)
            {
                PreviewDevAnimation(clip,true,false,0);AdvanceDevPreview(devPreviewBegan+1000);
                if(devPreviewPlaying || devPreviewFrame!=DevFrames(clip)-1)throw new Exception("Play-once preview did not hold its final frame: "+clip);
                PaintSprite(true);
            }
            foreach(string state in new[]{"loaf","lay"})
            {
                SelectDevState(state);PopulateDevActions();bool clockMenu=false;
                foreach(ToolStripItem entry in devActions.DropDownItems)
                {
                    ToolStripMenuItem item=entry as ToolStripMenuItem;
                    if(item!=null && item.Text=="Clock gaze - "+art.Manifest.clips["look_"+state].frames+" poses"){clockMenu=item.DropDownItems.Count==art.Manifest.clips["look_"+state].frames;}
                    if(item!=null && item.Text=="Try a window perch" && item.Enabled!=mind.HasJumps)throw new Exception("Perch menu eligibility is wrong.");
                }
                if(!clockMenu)throw new Exception("Clock gaze menu does not expose 12 poses: "+state);
                for(int hour=0;hour<art.Manifest.clips["look_"+state].frames;hour++){devGaze=hour;PaintSprite(true);if(drawnLook!=hour)throw new Exception("Clock gaze pose did not render: "+state+":"+hour);}
            }
            SelectDevState("loaf");devGaze=3;PaintSprite(true);
            Rectangle loafOriginal=RunningScale.VisibleBounds(art.Get("look_loaf",3)),loafVisible=RunningScale.VisibleBounds(canvas);
            double loafExpected=loafOriginal.Width*RunningScale.Pixels(size,"loaf")/(double)art.Manifest.width;
            if(Math.Abs(loafVisible.Width-loafExpected)>5)throw new Exception("Loaf gaze did not use the smaller state scale.");
            SelectDevState("sit");
            for(int i=0;i<19;i++){devGaze=i;PaintSprite(true);if(drawnDisplayClip!="look_sit" || drawnDisplayFrame!=i)throw new Exception("Sitting gaze in-between did not render.");}
            foreach(string state in new[]{"sit","loaf","lay","sleep"})
            {
                SelectDevState(state);PopulateDevActions();bool tailAction=false,eyesAction=false;
                foreach(ToolStripItem item in devActions.DropDownItems){if(item.Text=="Tail flick (5 frames)")tailAction=item.Enabled;if(item.Text=="Eyes / blink (3 poses)")eyesAction=true;}
                if(tailAction!=(state!="loaf") || eyesAction!=(state!="sleep"))throw new Exception("Missing contextual eye/tail action: "+state);
                if(state!="sleep")for(int eye=0;eye<3;eye++){PreviewDevAnimation(state+"_eyes",false,false,eye);PaintSprite(true);if(drawnDisplayFrame!=eye)throw new Exception("Eye expression did not render.");}
                if(state!="loaf")for(int tail=0;tail<5;tail++){PreviewDevAnimation(state+"_tail",false,false,tail);PaintSprite(true);if(drawnDisplayFrame!=tail)throw new Exception("Tail flick frame did not render.");}
            }
            SelectDevState("sit");PopulateDevActions();bool zoomies=false;foreach(ToolStripItem entry in devActions.DropDownItems)if(entry.Text=="Zoomies" && entry.Enabled)zoomies=true;
            if(!zoomies)throw new Exception("Dev actions omit Zoomies.");
            int bursts=mind.CompletedZoomies;mind.Zoomies();if(mind.CompletedZoomies!=bursts+1)throw new Exception("Zoomies did not start.");
            for(int i=0;i<500;i++)mind.Step(.1,Point.Empty);if(mind.Clip!="sit")throw new Exception("Zoomies did not return to held state.");
            SelectDevState("sit");mind.Groom();for(int i=0;i<500;i++)mind.Step(.1,Point.Empty);
            if(mind.Clip!="sit")throw new Exception("Action did not return to held state.");
            int runningFrames=0;
            foreach(string name in new[]{"run_left","run_right"})
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
            devPreviewClip=null;mind.DevResumeRoutine();if(mind.DevHeldState!=null)throw new Exception("Autonomous routine did not resume.");
            return new{passed=true,states=devStateMenus.Count,animationGroups=devAnimations.DropDownItems.Count,statesExcludedFromAnimations=true,flatStateMenu=true,contextualActions=true,allStatesHold=true,allClipsRender=true,enlargedRunningFramesChecked=runningFrames,playOnceStops=true,heldGaze=true,clockGazeStates=2,loafGazePoses=12,layingGazePoses=14,zoomiesVerified=true,sittingGazePoses=19,eyesAndTailsVerified=true,smallerLoafRenderingVerified=true,actionsReturnToState=true,resumeAutonomous=true};
        }
    }
}
#endif
