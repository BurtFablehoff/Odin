#if ODIN_DEV
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace OdinV2
{
    // V12 1c --test-follow-mouse. Headless: the window is never shown. It reaches each awake state through the real
    // DEV menu items (state menu, Transitions, Latest artwork stills), switches tracking on (Follow mouse, or Notice my
    // mouse attention), then runs the per-tick code the app runs (hover, mind.Step, StepGazeTrackers, StepRestDetails,
    // ChooseSprite, PlaceSprite) with a virtual cursor at 16 angles around the DEV head point, recording the frame drawn.
    internal sealed partial class OdinWindow
    {
        private sealed class FollowCase
        {
            public bool Passed=true;public string Reason="",FollowedAs="";public int Distinct,Descents;public double HeadError,AimError;
            public readonly List<object> ByAngle=new List<object>();public readonly List<int> Frames=new List<int>();
            public void Fail(string why){Reason=Passed?why:Reason+"; "+why;Passed=false;}
        }
        // name, tracking control, how the state is reached in the DEV menu
        private static readonly string[][] FollowCases={
            new[]{"followMouse","followMouse","stateMenu"},new[]{"noticeMyMouse","attention","stateMenu"},
            new[]{"viaTransitions","followMouse","transitions"},new[]{"viaStill","followMouse","still"},new[]{"viaStillThenTransition","followMouse","stillThenTransition"}};
        // V12 3R: seated views 6/7/8 are Latest artwork stills (not CatMind states), so they are reached only through that menu.
        private static readonly string[][] HeadTurnFollowCases={new[]{"viaStill","followMouse","still"},new[]{"viaStillNoticeNear","attention","still"}};
        private static double[] FollowTable(string family)
        {
            if(DevArtwork.HeadTurnId(family)>0)return ClockGaze.ClockAngles;
            return family=="sit"?SittingGaze.Angles:family=="loaf"?ClockGaze.ClockAngles:ClockGaze.LayingAngles;
        }
        private static double AngleGap(double a,double b){double d=Math.Abs(a-b)%360;return Math.Min(d,360-d);}
        private static double LargestGap(double[] table)
        {
            List<double> sorted=new List<double>(table);sorted.Sort();double gap=360-sorted[sorted.Count-1]+sorted[0];
            for(int i=1;i<sorted.Count;i++)gap=Math.Max(gap,sorted[i]-sorted[i-1]);return gap;
        }
        private static ToolStripMenuItem FindItem(ToolStripItemCollection items,string text)
        {
            foreach(ToolStripItem entry in items){ToolStripMenuItem item=entry as ToolStripMenuItem;if(item!=null && item.Text==text)return item;}
            return null;
        }
        private ToolStripMenuItem DevActionItem(string text){PopulateDevActions();return FindItem(devActions.DropDownItems,text);}
        private ToolStripMenuItem DevTransitionItem(string state){PopulateDevTransitions();return FindItem(devTransitions.DropDownItems,StateName(state));}
        private ToolStripMenuItem DevStillItem(int id)
        {
            string label=null;foreach(DevArtEntry pose in latestArt.Poses)if(pose.id==id)label=pose.label;
            foreach(ToolStripItem entry in menu.Items)
            {
                ToolStripMenuItem stills=entry as ToolStripMenuItem;
                if(stills!=null && stills.Text.StartsWith("Latest artwork stills"))return label==null?null:FindItem(stills.DropDownItems,label);
            }
            return null;
        }
        // Reach the family the way Hatt can in the DEV menu. Returns null, or why that menu path is unavailable.
        private string EnterFamily(string family,string entry)
        {
            ToolStripMenuItem item;
            switch(entry)
            {
                case "stateMenu":item=devStateMenus[family];break;
                case "transitions":devStateMenus[family=="sit"?"loaf":"sit"].PerformClick();item=DevTransitionItem(family);break;
                case "still":item=DevStillItem(DevArtwork.HeadTurnId(family)>0?DevArtwork.HeadTurnId(family):Array.IndexOf(DevStates,family)+1);break;
                default:
                {
                    ToolStripMenuItem start=DevStillItem(family=="sit"?2:1);if(start==null)return "Latest artwork stills has no starting still";
                    start.PerformClick();item=DevTransitionItem(family);break;
                }
            }
            if(item==null || !item.Enabled)return "the "+entry+" path has no enabled menu item for "+StateName(family)+" (Actions/Transitions treat the current state as "+CurrentDevState+")";
            item.PerformClick();
            if(DevArtwork.HeadTurnId(family)>0)return devStillPoseId==DevArtwork.HeadTurnId(family)?null:"the still menu did not show still "+DevArtwork.HeadTurnId(family);
            // Let any transition animation play out before choosing an action, as a person would.
            Point idle=new Point((int)mind.X,(int)mind.FootY-1500);
            for(int i=0;i<600 && mind.Clip!=family;i++){mind.ObserveHover(false);mind.Step(1/60.0,idle);}
            return null;
        }
        // Same test as IsOverCat (alpha >= 80), read from the frame the renderer would draw.
        private static bool CursorOverDrawnCat(Point cursor,SpritePlacement placed)
        {
            DevArtFrame frame=placed.Latest;Rectangle r=placed.Rectangle;
            if(frame==null || r.Width<=0 || r.Height<=0 || !r.Contains(cursor))return false;
            int x=(int)((cursor.X-r.X)*(double)frame.Image.Width/r.Width),y=(int)((cursor.Y-r.Y)*(double)frame.Image.Height/r.Height);
            return x>=0 && y>=0 && x<frame.Image.Width && y<frame.Image.Height && frame.Image.GetPixel(x,y).A>=80;
        }
        private static string Describe(SpriteChoice choice,SpritePlacement placed)
        {
            if(placed.Latest==null)return "production atlas "+choice.DisplayClip+"/"+choice.DisplayFrame;
            return (placed.Latest.Pose.clip??("still "+placed.Latest.Pose.id))+"/"+placed.Latest.Pose.frame;
        }
        private FollowCase RunFollowCase(string family,string control,string entry)
        {
            FollowCase result=new FollowCase();double[] table=FollowTable(family);
            bool headTurn=DevArtwork.HeadTurnId(family)>0;
            string actionText=control=="followMouse"?"Follow mouse":headTurn?"Watch the mouse when near":"Automatic held glances";
            string entered=EnterFamily(family,entry);if(entered!=null){result.Fail(entered);return result;}
            if(entry=="stateMenu" && (mind.Clip!=family || mind.DevHeldState!=family)){result.Fail("state menu did not hold "+family+" (clip "+mind.Clip+")");return result;}
            ToolStripMenuItem action=DevActionItem(actionText);
            if(action==null || !action.Enabled){result.Fail("Actions in current state has no enabled '"+actionText+"' (current state "+CurrentDevState+")");return result;}
            action.PerformClick();
            if(control=="followMouse" && !devFollowMouse){result.Fail("Follow mouse did not switch tracking on");return result;}
            if(control=="attention"){settings.Watch=true;mind.SetWatching(true);}
            result.FollowedAs=headTurn?"still "+devStillPoseId:mind.Clip;
            PointF start=ArtHeadOnScreen(family);
            const double radius=400,dt=1/60.0;double now=watch.Elapsed.TotalSeconds,aimLimit=LargestGap(table)/2+4;
            HashSet<int> distinct=new HashSet<int>();double previous=-1;int misses=0;string firstMiss=null;
            // V12 3bR-2: 6/7/8 are sampled at the 12 hour bearings (12 o'clock = up, clockwise), the others at 16 angles.
            int angles=headTurn?12:16;string drawnClip=headTurn?HeadTurnClip(family):"look_"+family;
            for(int k=0;k<angles;k++)
            {
                double degrees=k*360.0/angles,radians=degrees*Math.PI/180;
                Point cursor=new Point((int)Math.Round(start.X+radius*Math.Sin(radians)),(int)Math.Round(start.Y-radius*Math.Cos(radians)));
                if(control=="attention"){mind.AttentionActive=true;mind.AttentionUntil=mind.Clock+3600;mind.AttentionTarget=cursor;}
                SpriteChoice choice=new SpriteChoice();SpritePlacement placed=new SpritePlacement();int drawn=int.MinValue,stable=0,steps=0;
                for(;steps<900 && stable<45;steps++)
                {
                    // Tick order: hover, mind.Step, gaze trackers, rest details; then PaintSprite's clamp, choice and placement.
                    mind.ObserveHover(CursorOverDrawnCat(cursor,placed));
                    mind.Step(dt,cursor);now+=dt;
                    StepGazeTrackers(dt,cursor);
                    if(!mind.Paused && devPreviewClip==null)StepRestDetails(dt,now);
                    mind.ClampHorizontal();mind.Clamp();
                    choice=ChooseSprite(cursor);placed=PlaceSprite(choice);
                    int frame=placed.Latest!=null && placed.Latest.Pose.clip==drawnClip?placed.Latest.Pose.frame:-1;
                    if(frame==drawn)stable++;else{drawn=frame;stable=0;}
                }
                PointF tracked=ArtHeadOnScreen(family),drawnHead;
                bool hasHead=latestArt.TryDrawnHeadPoint(placed.Latest,placed.Rectangle,out drawnHead);
                double error=hasHead?Math.Sqrt(Math.Pow(drawnHead.X-tracked.X,2)+Math.Pow(drawnHead.Y-tracked.Y,2)):Double.NaN;
                double cursorAngle=(Math.Atan2(cursor.X-tracked.X,-(cursor.Y-tracked.Y))*180/Math.PI+360)%360,aim=Double.NaN;
                if(!hasHead)result.Fail("cursor "+degrees+" deg: drawn "+Describe(choice,placed)+" has no head landmark");
                else result.HeadError=Math.Max(result.HeadError,error);
                if(drawn<0){misses++;if(firstMiss==null)firstMiss="cursor "+degrees+" deg drew "+Describe(choice,placed)+" (state "+mind.Clip+")";}
                else
                {
                    distinct.Add(drawn);
                    if(!choice.Looking || choice.DisplayClip!=drawnClip || choice.DisplayFrame!=drawn)result.Fail("cursor "+degrees+" deg selected "+choice.DisplayClip+"/"+choice.DisplayFrame+" but drew "+drawnClip+"/"+drawn);
                    if(previous>=0 && table[drawn]<previous-1e-6)result.Descents++;previous=table[drawn];
                    aim=AngleGap(table[drawn],cursorAngle);result.AimError=Math.Max(result.AimError,aim);
                }
                if(stable<45)result.Fail("cursor "+degrees+" deg never settled on one frame");
                result.Frames.Add(drawn);
                result.ByAngle.Add(new{cursorDegrees=degrees,frame=drawn,frameDegrees=drawn>=0?(object)table[drawn]:null,aimErrorDeg=Double.IsNaN(aim)?null:(object)Math.Round(aim,2),
                    headPointErrorPx=hasHead?(object)Math.Round(error,2):null,trackedHead=new[]{Math.Round(tracked.X,1),Math.Round(tracked.Y,1)},drawnHead=hasHead?new[]{Math.Round(drawnHead.X,1),Math.Round(drawnHead.Y,1)}:null,stepsToSettle=steps-stable});
            }
            result.Distinct=distinct.Count;
            if(misses>0)result.Fail(misses+" of "+angles+" cursor angles drew no "+drawnClip+" frame; first: "+firstMiss);
            int needDistinct=headTurn?12:Math.Min(8,table.Length);
            if(result.Distinct<needDistinct)result.Fail(result.Distinct+" distinct drawn "+drawnClip+" frames (need >= "+needDistinct+")");
            if(result.Descents>1)result.Fail("drawn frames are not monotonic by angle ("+result.Descents+" descents; one wrap allowed)");
            if(result.HeadError>10)result.Fail("tracking head point is "+Math.Round(result.HeadError,2)+" px from the drawn head (limit 10)");
            if(result.AimError>aimLimit)result.Fail("drawn gaze is "+Math.Round(result.AimError,1)+" deg from the cursor (limit "+Math.Round(aimLimit,1)+")");
            return result;
        }
        internal object CheckFollowMouse()
        {
            Dictionary<string,object> families=new Dictionary<string,object>();List<string> failed=new List<string>(),skipped=new List<string>();bool passed=true;
            menuTesting=true;
            try
            {
                foreach(string family in new[]{"sit","loaf","lay","rear","lookback","side"})
                {
                    // V12 3c: a seated view whose clock clips are not in this pack is a plain still with no head turns (Hatt at G3b: "revert for now").
                    if(DevArtwork.HeadTurnId(family)>0 && !latestArt.HasHeadTurns(family)){skipped.Add(family);families.Add(family,new{skipped=true,reason="no clock_"+family+" frames in this pack: a plain still with no head turns"});continue;}
                    Dictionary<string,object> sizes=new Dictionary<string,object>();bool familyPassed=true;
                    foreach(int choice in new[]{480,640,800})
                    {
                        // Same steps as the "Odin's size" menu item (settings are ephemeral in this test).
                        size=choice;settings.Size=choice;RecreateCanvas();
                        Dictionary<string,object> paths=new Dictionary<string,object>();bool ok=true;int fewest=int.MaxValue;double head=0,aim=0;
                        List<string> reasons=new List<string>();List<object> byAngle=null;
                        foreach(string[] spec in DevArtwork.HeadTurnId(family)>0?HeadTurnFollowCases:FollowCases)
                        {
                            FollowCase run=RunFollowCase(family,spec[1],spec[2]);
                            ok&=run.Passed;fewest=Math.Min(fewest,run.Distinct);head=Math.Max(head,run.HeadError);aim=Math.Max(aim,run.AimError);
                            if(!run.Passed)reasons.Add(spec[0]+": "+run.Reason);if(byAngle==null)byAngle=run.ByAngle;
                            paths.Add(spec[0],new{passed=run.Passed,control=spec[1],menuPath=spec[2],followedAs=run.FollowedAs,distinctFrames=run.Distinct,frames=run.Frames,
                                headPointErrorPx=Math.Round(run.HeadError,2),maxAimErrorDeg=Math.Round(run.AimError,2),descents=run.Descents,reason=run.Reason});
                        }
                        familyPassed&=ok;
                        sizes.Add(choice.ToString(),new{passed=ok,distinctFrames=fewest,framesByAngle=byAngle,headPointErrorPx=Math.Round(head,2),maxAimErrorDeg=Math.Round(aim,2),
                            reason=String.Join(" | ",reasons.ToArray()),paths=paths});
                    }
                    if(!familyPassed){failed.Add(family);passed=false;}
                    families.Add(family,sizes);
                }
            }
            finally{menuTesting=false;devFollowMouse=false;}
            return new{passed=passed,failedFamilies=failed,clockFamiliesSkipped=skipped,cursorRadiusPx=400,anglesPerCase=16,headTurnAnglesPerCase=12,headLimitPx=10,minimumDistinctFrames=8,minimumDistinctHeadTurnFrames=12,
                headTurnClips=new{rear=HeadTurnClip("rear"),lookback=HeadTurnClip("lookback"),side=HeadTurnClip("side")},headTurnTable=ClockGaze.ClockAngles,
                headTurnPaths=new{viaStill="Latest artwork stills > 6/7/8 > Actions in current state > Follow mouse",
                    viaStillNoticeNear="Latest artwork stills > 6/7/8 > Actions in current state > Watch the mouse when near (Notice my mouse on, cursor within 850 px)"},
                paths=new{followMouse="state menu > Actions in current state > Follow mouse",noticeMyMouse="state menu > Automatic held glances, Notice my mouse attention",
                    viaTransitions="other state > Transitions > state > Follow mouse",viaStill="Latest artwork stills > same-numbered still > Follow mouse",
                    viaStillThenTransition="Latest artwork stills > other still > Transitions > state > Follow mouse"},families=families};
        }
    }
}
#endif
