#if ODIN_DEV
using System;
using System.Collections.Generic;
using System.Drawing;

namespace OdinV2
{
    // V12 3bR-2 (DEV): seated views 6 rear, 7 look-back and 8 side follow the mouse with the 12-hour clock clips
    // (clock_rear / clock_lookback / clock_side, index 0 = 12 o'clock = head up, clockwise, as ClockGaze.ClockAngles), one
    // fixed body with only the head and neck drawn per hour. They are reached as Latest artwork stills (a paused sitting
    // body), so CatMind attention never runs for them: the tracker runs when Follow mouse is on, or when Notice my mouse is
    // on and the cursor is near (CatMind's 850 px notice reach). The head point is the still's headCrop
    // (DevArtwork.TryHeadPoint); with no hour tracked (Frame -1) the centre still itself is drawn.
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
            if(!headTurnMouse.TryGetValue(family,out tracker)){tracker=new ClockGaze();headTurnMouse.Add(family,tracker);}
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
        internal static string HeadTurnClip(string family){return "clock_"+family;}
        // The clock frame to draw for the still on screen (-1 = the centre still itself).
        private int HeadTurnFrame(string family)
        {
            if(family==null)return -1;
            if(devGaze>=0)return Math.Min(devGaze,11);
            return HeadTurnTracker(family).Frame;
        }
        private static readonly string[] HeadTurnDirections={"12 o'clock","1 o'clock","2 o'clock","3 o'clock","4 o'clock","5 o'clock","6 o'clock","7 o'clock","8 o'clock","9 o'clock","10 o'clock","11 o'clock"};
    }
}
#endif
