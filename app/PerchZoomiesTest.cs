#if ODIN_DEV
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace OdinV2
{
    // V12 3bP --test-perch-zoomies. Owner rule (Hatt, 2026-11-08): "perch and zoomies are animations for any state that isn't
    // sleeping". Headless: the window is constructed but never shown.
    // 1. CatMind with every clip (the rest transitions exist) and with this build's pack clips (no transition art: sitting up is a
    //    cut): from sit, loaf, lay and belly with one eligible window TryPerch(true) is accepted and reaches jump_* (loaf/lay/belly
    //    through *_to_sit first when that art exists) and Zoomies() queues run_*; sleep and belly_sleep refuse both.
    // 2. Held states: CatMind.DevSelectState for all six, and the DEV menu (state items; Latest artwork stills 1-4 and 6/7/8) on the
    //    app's per-tick path (StepMind). An awake hold's tick starts zoomies (run_*) or a perch (jump_*), nothing else, and comes
    //    back to the held state or still; a sleeping hold never acts.
    // 3. Actions in current state: Zoomies and Try a window perch for every awake state and for 6/7/8 (from the still, back to it);
    //    both disabled asleep.
    internal sealed partial class OdinWindow
    {
        private static readonly Point PerchIdle=new Point(-20000,-20000);
        private static readonly Rectangle PerchArea=new Rectangle(0,0,1920,1080);
        private const long PerchLedgeId=9301;
        private static List<WindowSurface> PerchLedge(){return new List<WindowSurface>{new WindowSurface(PerchLedgeId,new Rectangle(300,620,1200,320))};}
        private static bool PerchAllowedClip(string clip,string state)
        {
            return clip==state || clip=="sit" || clip.StartsWith("run_") || clip.StartsWith("jump") || clip.StartsWith("crab_") || clip.EndsWith("_to_sit") || clip=="belly_to_lay";
        }
        private static string GetUpClip(string state){return state=="sit"?null:state=="belly"?"belly_to_lay":state+"_to_sit";}
        private static string[] Head(string[] clips,int count){string[] head=new string[Math.Min(count,clips.Length)];Array.Copy(clips,head,head.Length);return head;}
        private static object MindPerchZoomies(IEnumerable<string> clips,string label,List<string> failures)
        {
            Dictionary<string,object> cases=new Dictionary<string,object>();
            foreach(string state in new[]{"sit","loaf","lay","belly","sleep","belly_sleep"})
            {
                CatMind cat=new CatMind(PerchArea,31);cat.ConfigureArt(clips);
                if(!cat.Supports(state)){cases.Add(state,new{inThisArt=false});continue;}
                bool awake=!CatMind.IsSleeping(state);string at=label+" "+state+": ",getUp=GetUpClip(state);
                bool transitionArt=awake && getUp!=null && cat.Supports(getUp);
                cat.DevSelectState(state);cat.DevHeldState=null;cat.UpdateWindows(PerchLedge());
                bool accepted=cat.TryPerch(true);string[] perchPlan=Head(cat.DevPlannedClips,4);string reached=null;double takeoff=-1;bool landed=false;
                for(int i=0;i<400 && accepted;i++)
                {
                    if(reached==null && cat.IsAirborne){reached=cat.Clip;takeoff=Math.Round(i*.05,2);}
                    if(reached!=null && !cat.IsAirborne){landed=cat.PerchedWindow==PerchLedgeId;break;}
                    cat.Step(.05,PerchIdle);
                }
                CatMind dash=new CatMind(PerchArea,32);dash.ConfigureArt(clips);dash.DevSelectState(state);dash.DevHeldState=null;
                int bursts=dash.CompletedZoomies;dash.Zoomies();string[] dashPlan=Head(dash.DevPlannedClips,5);
                bool started=dash.CompletedZoomies>bursts,queuedRun=Array.Exists(dash.DevPlannedClips,c=>c.StartsWith("run_"));
                if(awake)
                {
                    if(!accepted)failures.Add(at+"TryPerch(true) refused with one eligible window (clip "+cat.Clip+")");
                    else
                    {
                        if(transitionArt?perchPlan[0]!=getUp:!perchPlan[0].StartsWith("jump"))failures.Add(at+"the perch began with "+perchPlan[0]+" (expected "+(transitionArt?getUp:"jump_*")+")");
                        if(reached==null || !reached.StartsWith("jump"))failures.Add(at+"the perch never reached jump_*");
                        else if(!landed)failures.Add(at+"the perch did not land on the window");
                    }
                    if(!started || !queuedRun)failures.Add(at+"Zoomies() did not queue run_* ("+String.Join(",",dashPlan)+")");
                    else if(transitionArt?dashPlan[0]!=getUp:!dashPlan[0].StartsWith("run_"))failures.Add(at+"zoomies began with "+dashPlan[0]+" (expected "+(transitionArt?getUp:"run_*")+")");
                }
                else
                {
                    if(accepted || cat.Clip!=state)failures.Add(at+"asleep, but TryPerch(true) "+(accepted?"was accepted":"left the state")+" (clip "+cat.Clip+")");
                    if(started || queuedRun || dash.Clip!=state)failures.Add(at+"asleep, but Zoomies() "+(started?"started":"changed the plan")+" ("+String.Join(",",dashPlan)+")");
                }
                cases.Add(state,new{inThisArt=true,awake=awake,transitionArt=transitionArt,perchAccepted=accepted,perchPlan=perchPlan,perchReached=reached,secondsToTakeoff=takeoff,landedOnWindow=landed,
                    zoomiesStarted=started,zoomiesPlan=dashPlan});
            }
            return cases;
        }
        private sealed class HeldRun
        {
            public int Zoomies,Perches,Leaves,Episodes;public double Seconds,FirstActionAt=-1;
            public readonly List<string> Clips=new List<string>(),Problems=new List<string>();
            public readonly List<object> Actions=new List<object>();public readonly List<double> RestSteps=new List<double>();
            public void Problem(string why){if(Problems.Count<6)Problems.Add(why);}
            public object Report(bool awake)
            {
                return new{passed=Problems.Count==0,awake=awake,simulatedHours=Math.Round(Seconds/3600,3),firstActionAtSeconds=FirstActionAt<0?null:(object)FirstActionAt,
                    zoomies=Zoomies,perches=Perches,leftPerch=Leaves,actions=Actions,clipsSeen=Clips,restStepSecondsBetweenActions=RestSteps,problems=Problems};
            }
        }
        // Ticks a held state (0.1 s) until it has done zoomies and a perch, each ending back in the held state, or until maxSeconds.
        // step: one tick of the app's code; held: back in the held state (or still); drawn: what the renderer draws (window only).
        private static HeldRun RunHeld(CatMind cat,string state,bool awake,double maxSeconds,Action<double> step,Func<bool> held,Func<string> drawn)
        {
            HeldRun run=new HeldRun();List<WindowSurface> ledge=PerchLedge();cat.UpdateWindows(ledge);
            bool inAction=false;double began=0,age=cat.Age;string kind=null;List<string> sequence=null;
            for(int i=0;run.Seconds<maxSeconds;i++)
            {
                bool wasPerched=cat.PerchedWindow!=0;string before=cat.Clip;
                step(.1);run.Seconds+=.1;if(i%3==2)cat.UpdateWindows(ledge);
                if(!run.Clips.Contains(cat.Clip))run.Clips.Add(cat.Clip);
                if(!PerchAllowedClip(cat.Clip,state))run.Problem("held "+state+" played "+cat.Clip+" (only zoomies or a perch)");
                if(!inAction)
                {
                    if(CatMind.IsRest(cat.Clip)){if(cat.Age<age-1e-9 && run.RestSteps.Count<12)run.RestSteps.Add(Math.Round(age,1));age=cat.Age;continue;}
                    inAction=true;began=run.Seconds;run.Episodes++;sequence=new List<string>{cat.Clip};if(run.FirstActionAt<0)run.FirstActionAt=Math.Round(run.Seconds,1);
                    kind=cat.Clip.StartsWith("run_")?"zoomies":cat.Clip.StartsWith("jump")?(wasPerched?"leavePerch":"perch"):"getUp";
                    if(!CatMind.IsRest(before))run.Problem("an action began from "+before);
                    if(!awake)run.Problem("asleep, but the hold started "+cat.Clip+" at "+Math.Round(run.Seconds)+" s");
                    if(kind=="getUp" && !cat.Clip.EndsWith("_to_sit") && cat.Clip!="belly_to_lay")run.Problem("the held tick started "+cat.Clip);
                    if(drawn!=null)
                    {
                        string seen=drawn();
                        if(seen.StartsWith("still "))run.Problem("the still stayed drawn over "+cat.Clip+" ("+seen+")");
                        else if(seen.IndexOf(cat.Clip,StringComparison.Ordinal)<0)run.Problem("drew "+seen+" for "+cat.Clip);
                    }
                    continue;
                }
                if(sequence[sequence.Count-1]!=cat.Clip)sequence.Add(cat.Clip);
                if(kind=="getUp" && (cat.Clip.StartsWith("run_") || cat.Clip.StartsWith("jump")))kind=cat.Clip.StartsWith("run_")?"zoomies":"perch";
                if(drawn!=null && !CatMind.IsRest(cat.Clip) && drawn().StartsWith("still "))run.Problem("the still was drawn during "+cat.Clip);
                if(held())
                {
                    inAction=false;age=cat.Age;
                    if(kind=="zoomies")run.Zoomies++;else if(kind=="perch")run.Perches++;else if(kind=="leavePerch")run.Leaves++;
                    else run.Problem("an action ended without run_*/jump_*: "+String.Join(",",sequence.ToArray()));
                    if(run.Actions.Count<10)run.Actions.Add(new{kind=kind,atSeconds=Math.Round(began,1),secondsToReturn=Math.Round(run.Seconds-began,1),clips=sequence,
                        returnedTo=drawn!=null?drawn():cat.Clip,perched=cat.PerchedWindow!=0});
                    if(awake && run.Zoomies>0 && run.Perches>0)break;
                }
                else if(run.Seconds-began>400){run.Problem(kind+" begun at "+Math.Round(began)+" s did not return to the held state within 400 s ("+String.Join(",",sequence.ToArray())+")");inAction=false;age=cat.Age;}
            }
            if(awake && run.Zoomies==0)run.Problem("no zoomies from the held "+state+" in "+Math.Round(run.Seconds/3600,2)+" h"+(run.Episodes==0?" (the hold never acted)":""));
            if(awake && run.Perches==0)run.Problem("no perch from the held "+state+" in "+Math.Round(run.Seconds/3600,2)+" h"+(run.Episodes==0?" (the hold never acted)":""));
            return run;
        }
        private static object HeldMindCases(List<string> failures)
        {
            Dictionary<string,object> cases=new Dictionary<string,object>();int seed=40;
            foreach(string state in new[]{"sit","loaf","lay","belly","sleep","belly_sleep"})
            {
                CatMind cat=new CatMind(PerchArea,seed++);cat.ConfigureArt(null);cat.DevSelectState(state);bool awake=!CatMind.IsSleeping(state);
                HeldRun run=RunHeld(cat,state,awake,awake?6*3600:2*3600,delegate(double dt){cat.Step(dt,PerchIdle);},delegate{return cat.Clip==state && cat.DevHeldState==state;},null);
                foreach(string problem in run.Problems)failures.Add("held CatMind "+state+": "+problem);
                cases.Add(state,run.Report(awake));
            }
            return cases;
        }
        private string DrawnNow(){SpriteChoice choice=ChooseSprite(PerchIdle);return Describe(choice,PlaceSprite(choice));}
        private void ResetForPerchTest()
        {
            foreach(ToolStripItem entry in menu.Items)if(entry.Text=="Resume autonomous routine")entry.PerformClick();
            mind.MoveHome(PerchArea,PerchArea.Left+PerchArea.Width*.6,PerchArea.Bottom);mind.PerchesEnabled=true;mind.UpdateWindows(PerchLedge());
        }
        private bool HeldStill(int id){return mind.Paused && devStillPoseId==id && CatMind.IsRest(mind.Clip) && DrawnNow().StartsWith("still "+id+"/");}
        private object HeldWindowCase(string state,int stillId,List<string> failures)
        {
            string name=stillId>0?"still "+stillId:state;ResetForPerchTest();
            if(stillId>0)
            {
                ToolStripMenuItem item=DevStillItem(stillId);
                if(item==null){failures.Add("held "+name+": not in Latest artwork stills");return new{passed=false};}
                item.PerformClick();
            }
            else devStateMenus[state].PerformClick();
            string shown=stillId>0?DevStillState(stillId):state;bool awake=!CatMind.IsSleeping(shown);string under=mind.Clip;
            HeldRun run=RunHeld(mind,under,awake,awake?6*3600:2*3600,delegate(double dt){mind.ObserveHover(false);StepMind(dt,PerchIdle);},
                delegate{return stillId>0?HeldStill(stillId):devStillPoseId==0 && mind.Clip==state && mind.DevHeldState==state;},DrawnNow);
            foreach(string problem in run.Problems)failures.Add("held "+name+": "+problem);
            return new{shows=shown,heldBody=under,run=run.Report(awake)};
        }
        // A 6/7/8 Actions item from the still: the action starts at once over the suspended still and ends back on the still.
        private object StillMenuAction(int id,string text,string expect,List<string> failures)
        {
            string at="still "+id+" Actions > "+text+": ";ToolStripMenuItem item=DevActionItem(text);
            if(item==null || !item.Enabled){failures.Add(at+(item==null?"missing":"disabled"));return new{offered=item!=null,enabled=false};}
            item.PerformClick();string clip=mind.Clip,drawn=DrawnNow();double back=-1;
            for(int i=0;i<4000;i++){mind.ObserveHover(false);StepMind(.1,PerchIdle);if(i%3==2)mind.UpdateWindows(PerchLedge());if(HeldStill(id)){back=Math.Round((i+1)*.1,1);break;}}
            if(!clip.StartsWith(expect))failures.Add(at+"started "+clip+" (expected "+expect+"*)");
            if(drawn.StartsWith("still "))failures.Add(at+"the still stayed drawn over "+clip);
            if(back<0)failures.Add(at+"did not return to the still within 400 s (clip "+mind.Clip+")");
            return new{offered=true,enabled=true,started=clip,drawnAtStart=drawn,secondsBackToStill=back,perchedAfter=mind.PerchedWindow!=0};
        }
        private object PerchZoomiesMenus(List<string> failures)
        {
            Dictionary<string,object> states=new Dictionary<string,object>(),stills=new Dictionary<string,object>();
            foreach(string state in DevStates)
            {
                ResetForPerchTest();devStateMenus[state].PerformClick();bool awake=!CatMind.IsSleeping(state);
                ToolStripMenuItem zoom=DevActionItem("Zoomies"),perch=DevActionItem("Try a window perch");
                string zoomState=zoom==null?"missing":zoom.Enabled?"enabled":"disabled",perchState=perch==null?"missing":perch.Enabled?"enabled":"disabled",want=awake?"enabled":"disabled";
                if(zoomState!=want)failures.Add("Actions while "+state+": Zoomies is "+zoomState+" (expected "+want+")");
                if(perchState!=want)failures.Add("Actions while "+state+": Try a window perch is "+perchState+" (expected "+want+")");
                states.Add(state,new{zoomies=zoomState,perch=perchState});
            }
            foreach(int id in new[]{6,7,8})
            {
                // V12 3d: a seated view parked out of this version has no still to test.
                if(latestArt.IsParked(id)){stills.Add(id.ToString(),new{parked=true});continue;}
                ResetForPerchTest();ToolStripMenuItem item=DevStillItem(id);if(item==null){failures.Add("still "+id+": not in Latest artwork stills");continue;}
                item.PerformClick();
                object zoomies=StillMenuAction(id,"Zoomies","run_",failures);
                object perch=StillMenuAction(id,"Try a window perch","jump",failures);
                stills.Add(id.ToString(),new{zoomies=zoomies,perch=perch});
            }
            return new{states=states,stills=stills};
        }
        internal object CheckPerchZoomies()
        {
            List<string> failures=new List<string>();object perchStates;
            try{perchStates=Tests.PerchStates(art);}catch(Exception error){perchStates=new{passed=false,error=error.Message};failures.Add("Tests.PerchStates: "+error.Message);}
            object allClips=MindPerchZoomies(null,"all clips",failures),packClips=MindPerchZoomies(art.Manifest.clips.Keys,"pack clips",failures);
            object heldMind=HeldMindCases(failures);
            Dictionary<string,object> heldWindow=new Dictionary<string,object>();object menus;
            menuTesting=true;devSurfaceSource=PerchLedge;
            try
            {
                foreach(string state in DevStates)heldWindow.Add(state,HeldWindowCase(state,0,failures));
                foreach(int id in new[]{1,2,3,4,6,7,8})heldWindow.Add("still"+id,latestArt.IsParked(id)?new{parked=true}:HeldWindowCase(null,id,failures));
                menus=PerchZoomiesMenus(failures);
            }
            finally{menuTesting=false;devSurfaceSource=null;}
            return new{passed=failures.Count==0,rule="perch and zoomies are animations for any state that isn't sleeping (Hatt, 2026-11-08)",failures=failures,
                ledge=new{area=new[]{PerchArea.Left,PerchArea.Top,PerchArea.Width,PerchArea.Height},window=new[]{300,620,1200,320}},
                perchStatesSelfTest=perchStates,catMindAllClips=allClips,catMindPackClips=packClips,heldCatMind=heldMind,heldWindow=heldWindow,actionsMenu=menus};
        }
    }
}
#endif
