#if ODIN_DEV
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OdinV2
{
    // V12 3R (DEV): seated views 6 rear, 7 look-back and 8 side follow the mouse with whole-body Codex head-turn frames
    // (look_rear / look_lookback / look_side, five frames far-left..far-right, registered by the lower body). They are
    // reached as Latest artwork stills (a paused sitting body), so CatMind attention never runs for them: the tracker
    // runs when Follow mouse is on, or when Notice my mouse is on and the cursor is near (CatMind's 850 px notice reach).
    // The head point is the still's headCrop (DevArtwork.TryHeadPoint); the centre frame is the still itself.
    internal sealed partial class OdinWindow
    {
        private const double HeadTurnReach=850;
        private readonly Dictionary<string,ClockGaze> headTurnMouse=new Dictionary<string,ClockGaze>();
        private bool headTurnWatch=true;
        // The head-turn family of the still on screen, or null (not a 6/7/8 still, or its frames are not in the pack).
        private string HeadTurnStillFamily()
        {
            string family=DevArtwork.HeadTurnFamily(devStillPoseId);
            return family!=null && latestArt.HasHeadTurns(family)?family:null;
        }
        private ClockGaze HeadTurnTracker(string family)
        {
            ClockGaze tracker;
            if(!headTurnMouse.TryGetValue(family,out tracker)){tracker=new ClockGaze(ClockGaze.HeadTurnAngles(family),true);headTurnMouse.Add(family,tracker);}
            return tracker;
        }
        private void ResetHeadTurnTrackers(){foreach(ClockGaze tracker in headTurnMouse.Values)tracker.Reset();}
        private bool HeadTurnTracking(PointF head,Point cursor)
        {
            if(devFollowMouse)return true;
            return headTurnWatch && settings.Watch && Math.Abs(cursor.X-head.X)<HeadTurnReach && Math.Abs(cursor.Y-head.Y)<HeadTurnReach;
        }
        private void StepHeadTurnTracker(double dt,Point cursor)
        {
            string family=HeadTurnStillFamily();
            foreach(KeyValuePair<string,ClockGaze> entry in headTurnMouse)if(entry.Key!=family)entry.Value.Reset();
            if(family==null)return;
            ClockGaze tracker=HeadTurnTracker(family);
            PointF head=ArtHeadOnScreen(family);
            if(devGaze<0 && HeadTurnTracking(head,cursor))tracker.Step(dt,head,cursor,Math.Max(12,size*.03));
            else tracker.Reset();
        }
        // The head-turn frame to draw for the still on screen (-1 = the still itself, which is the centre frame).
        private int HeadTurnFrame(string family)
        {
            if(family==null)return -1;
            if(devGaze>=0)return Math.Min(devGaze,4);
            return HeadTurnTracker(family).Frame;
        }
        private static readonly string[] HeadTurnDirections={"Far left","Left","Centre","Right","Far right"};
    }
}
#endif
