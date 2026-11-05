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
        internal void DevSelectState(string state)
        {
            if(!IsRest(state) || !Supports(state))throw new InvalidOperationException("State artwork is unavailable: "+state);
            DevHeldState=state;plans.Clear();PerchedWindow=0;FootY=Area.Bottom;
            AttentionActive=false;attentionPending=false;hovering=false;hoverUntil=0;attentionRestUntil=0;lookCooldown=0;
            Begin(new StepPlan(state,Double.MaxValue),"Dev: holding "+state);Clamp();
        }
        internal void DevResumeRoutine()
        {
            DevHeldState=null;Paused=false;plans.Clear();PerchedWindow=0;FootY=Area.Bottom;
            Begin(new StepPlan("sit",1),"Returning to autonomous routine");Clamp();
        }
        private bool DevReturnToHeldState()
        {
            if(DevHeldState==null)return false;
            if(plans.Count>0 && !IsRest(plans.Peek().Clip))return false;
            plans.Clear();Begin(new StepPlan(DevHeldState,Double.MaxValue),"Dev: holding "+DevHeldState);return true;
        }
    }

    internal sealed partial class OdinWindow
    {
        private string devState="sit",devPreviewClip;
        private int devPreviewFrame,devGaze=-2;
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
            devPreviewClip=null;devPreviewPlaying=false;devBlink=false;devGaze=-2;devFollowMouse=false;devState=state;
            mind.DevSelectState(state);mind.Paused=false;layingMouse.Reset();loafMouse.Reset();sittingTracker.Reset();
        }
        private void DevAction(string state,Action action)
        {
            SelectDevState(state);action();
        }
        private void DevPerch(string state)
        {
            DevAction(state,delegate
            {
                bool enabled=mind.PerchesEnabled;mind.PerchesEnabled=true;mind.UpdateWindows(Native.Surfaces(ownProcess));
                bool found=mind.TryPerch(true);mind.PerchesEnabled=enabled;
                if(!found)tray.ShowBalloonTip(3500,"Dev: no available perch","Move a window low enough for Odin to perch, then try again.",ToolTipIcon.Info);
            });
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
            ClipInfo clip=art.Manifest.clips[devPreviewClip];
            double seconds=clip.seconds>0?clip.seconds:Math.Max(1,clip.frames*.15);
            double progress=Math.Max(0,(now-devPreviewBegan)/seconds);
            if(!devPreviewLoop && progress>=1){devPreviewFrame=clip.frames-1;devPreviewPlaying=false;return;}
            devPreviewFrame=Math.Min(clip.frames-1,(int)((devPreviewLoop?progress%1:progress)*clip.frames));
        }
        private ToolStripMenuItem devTransitions,devActions;
        private static bool IsDevAnimation(string clip)
        {
            return !CatMind.IsRest(clip) && !clip.StartsWith("look_") && !clip.EndsWith("_eyes") && !clip.EndsWith("_tail");
        }
        private string CurrentDevState {get{return CatMind.IsRest(mind.Clip)?mind.Clip:devState;}}
        private void PopulateDevTransitions()
        {
            devTransitions.DropDownItems.Clear();string from=CurrentDevState;
            foreach(string value in DevStates)
            {
                string target=value;if(target==from)continue;
                ToolStripMenuItem item=DevItem(devTransitions,StateName(target),delegate
                {
                    devPreviewClip=null;devFollowMouse=false;devGaze=-2;devState=target;
                    if(target=="belly" || target=="belly_sleep")SelectDevState(target);
                    else {mind.DevHeldState=target;mind.Rest(target,Double.MaxValue);}
                },mind.Supports(target));
                if(!art.Manifest.clips.ContainsKey(from+"_to_"+target))item.ToolTipText="No transition animation in this build; switches to the selected state.";
            }
        }
        private void PopulateDevActions()
        {
            devActions.DropDownItems.Clear();string state=CurrentDevState;
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
            DevItem(devActions,"Zoomies",delegate{DevAction(state,mind.Zoomies);},mind.Supports("run_left") && mind.Supports("run_right"));
            DevItem(devActions,"Try a window perch",delegate{DevPerch(state);},mind.HasJumps && (state=="sit" || state=="lay"));
            DevItem(devActions,"Leave current perch",delegate{devPreviewClip=null;mind.LeavePerch();},mind.PerchedWindow!=0);
            DevItem(devActions,"Click pounce",delegate{DevPounce(state);},mind.HasPounces && !CatMind.IsSleeping(state));
            DevItem(devActions,"Crab bounce",delegate{DevAction(state,mind.CrabBounce);},mind.HasCrab && !CatMind.IsSleeping(state));
        }
        private void BuildDevMenu()
        {
            devStatus=new ToolStripMenuItem("States"){Enabled=false};menu.Items.Add(devStatus);
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
                ClipInfo info=art.Manifest.clips[clip];ToolStripMenuItem group=DevItem(devAnimations,clip+" ("+info.frames+" frames)",null);group.Tag=clip;
                DevItem(group,"Play once, then hold last frame",delegate{PreviewDevAnimation(clip,true,false,0);});
                DevItem(group,"Loop preview only",delegate{PreviewDevAnimation(clip,true,true,0);});
                ToolStripMenuItem frames=DevItem(group,"Choose a frame",null);
                for(int i=0;i<info.frames;i++){int frame=i;DevItem(frames,"Frame "+frame,delegate{PreviewDevAnimation(clip,false,false,frame);});}
            }
            Item("Stop preview / hold current state",delegate{SelectDevState(CurrentDevState);});
            Item("Resume autonomous routine",delegate{devPreviewClip=null;devFollowMouse=false;devGaze=-2;mind.DevResumeRoutine();});
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
                if(devPreviewPlaying || devPreviewFrame!=art.Manifest.clips[clip].frames-1)throw new Exception("Play-once preview did not hold its final frame: "+clip);
                PaintSprite(true);
            }
            foreach(string state in new[]{"loaf","lay"})
            {
                SelectDevState(state);PopulateDevActions();bool clockMenu=false;
                foreach(ToolStripItem entry in devActions.DropDownItems)
                {
                    ToolStripMenuItem item=entry as ToolStripMenuItem;
                    if(item!=null && item.Text=="Clock gaze - "+art.Manifest.clips["look_"+state].frames+" poses"){clockMenu=item.DropDownItems.Count==art.Manifest.clips["look_"+state].frames;}
                    if(item!=null && item.Text=="Try a window perch" && item.Enabled!=(state=="lay"))throw new Exception("Perch menu eligibility is wrong.");
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
