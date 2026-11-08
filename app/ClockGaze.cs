using System;
using System.Drawing;

namespace OdinV2
{
    internal sealed class ClockGaze
    {
        public int Frame=-1;
        private double untilStep;
        private const double Turn=Math.PI*2;
        internal static readonly double[] ClockAngles={0,30,60,90,120,150,180,210,240,270,300,330};
        internal static readonly double[] LayingAngles={0,30,60,90,120,150,180,210,240,270,300,330,340,350};
        // V12 3R head turns (DEV): five whole-body frames per seated view, far-left..far-right, as screen bearings from the
        // head (0 = up, clockwise). 6 rear and 7 look-back are seen from behind / over the shoulder: the far-left frame turns
        // his face to screen-left (cursor left, 270), centre faces up/away or at you (cursor above), far-right turns it to
        // screen-right (90). 8 side faces right: far-left is the head turned toward the viewer (cursor low left, 240), centre
        // the drawn three-quarter view (cursor above), right the profile (60), far-right turned away (cursor right, 100).
        // Not a clock: frames sit on a line, so turns step through the neighbours and never wrap far-left <-> far-right.
        internal static readonly double[] RearAngles={270,315,0,45,90};
        internal static readonly double[] LookBackAngles={270,315,0,45,90};
        internal static readonly double[] SideAngles={240,300,0,60,100};
        internal static double[] HeadTurnAngles(string family){return family=="rear"?RearAngles:family=="lookback"?LookBackAngles:family=="side"?SideAngles:null;}
        private readonly double[] angles;private readonly bool linear;
        public int PoseCount {get{return angles.Length;}}
        public ClockGaze(bool laying=false){angles=laying?LayingAngles:ClockAngles;}
        public ClockGaze(double[] table,bool linearTurns){angles=table;linear=linearTurns;}
        private static double Distance(double a,double b){double d=Math.Abs(a-b)%360;return Math.Min(d,360-d);}
        private int Nearest(double degrees){int best=0;for(int i=1;i<angles.Length;i++)if(Distance(degrees,angles[i])<Distance(degrees,angles[best]))best=i;return best;}

        public void Reset(){Frame=-1;untilStep=0;}
        public static int FromTarget(PointF head,PointF target,double deadZone)
        {
            double dx=target.X-head.X,dy=target.Y-head.Y;
            if(dx*dx+dy*dy<=deadZone*deadZone)return -1;
            double angle=(Math.Atan2(dx,-dy)+Turn)%Turn;
            return ((int)Math.Floor(angle/(Turn/12)+.5))%12;
        }
        public static PointF HeadOnScreen(double rootX,double floor,int size,int width,int anchorX,int anchorY,Rectangle head)
        {
            double scale=size/(double)width;
            return new PointF((float)(rootX+(head.Left+head.Width*.5-anchorX)*scale),(float)(floor+(head.Top+head.Height*.5-anchorY)*scale));
        }
        public void StepToFrame(double dt,int target)
        {
            if(target<0){Reset();return;}
            target%=angles.Length;
            if(Frame<0){Frame=target;untilStep=.065;return;}
            double distance=linear?target-Frame:(angles[target]-angles[Frame]+540)%360-180;
            if(distance==0){untilStep=0;return;}
            untilStep-=Math.Max(0,dt);if(untilStep>0)return;
            Frame=linear?Frame+(distance>0?1:-1):(Frame+(distance>0?1:angles.Length-1))%angles.Length;untilStep=.065;
        }
        public void Step(double dt,PointF head,PointF target,double deadZone)
        {
            double dx=target.X-head.X,dy=target.Y-head.Y;
            if(dx*dx+dy*dy<=deadZone*deadZone){Reset();return;}
            double angle=(Math.Atan2(dx,-dy)*180/Math.PI+360)%360;
            int desired=Nearest(angle);
            if(Frame>=0 && desired!=Frame && Distance(angle,angles[Frame])<=Distance(angle,angles[desired])+3)desired=Frame;
            StepToFrame(dt,desired);
        }
        public static object Test(ArtPack art)
        {
            if(art.Manifest.clips["look_loaf"].frames!=12 || art.Manifest.clips["look_lay"].frames!=14)throw new Exception("Expected 12 loaf and 14 laying poses.");
            PointF head=new PointF(-700,400);ClockGaze gaze=new ClockGaze();
            for(int hour=0;hour<12;hour++)
            {
                double angle=hour*Turn/12;PointF target=new PointF(head.X+(float)(300*Math.Sin(angle)),head.Y-(float)(300*Math.Cos(angle)));
                for(int i=0;i<120;i++)gaze.Step(1.0/60,head,target,15);
                if(gaze.Frame!=hour)throw new Exception("Clock direction mismatch: "+hour);
            }
            gaze.Frame=11;gaze.StepToFrame(.1,0);if(gaze.Frame!=0)throw new Exception("11-to-12 transition took the long path.");
            gaze.Frame=0;
            for(int i=0;i<240;i++)
            {
                double angle=(i%2==0?14:16)*Math.PI/180;
                gaze.Step(.016,head,new PointF(head.X+(float)(300*Math.Sin(angle)),head.Y-(float)(300*Math.Cos(angle))),15);
                if(gaze.Frame!=0)throw new Exception("Gaze jittered at a clock-sector boundary.");
            }
            gaze.Step(.016,head,head,15);if(gaze.Frame!=-1)throw new Exception("Pointer on the head must use the resting gaze.");
            PointF screenHead=HeadOnScreen(-1000,900,480,1024,512,927,new Rectangle(490,580,270,240));
            if(screenHead.X<=-1000 || screenHead.Y>=900)throw new Exception("Clock gaze used the body root instead of the head.");
            ClockIdleLook idle=new ClockIdleLook(71);for(int i=0;i<100;i++)idle.Step(.016,true,false);
            if(idle.Frame!=-1)throw new Exception("Idle gaze did not hold the resting pose.");
            double remaining=idle.HoldRemaining;for(int i=0;i<100;i++)idle.Step(.1,true,true);
            if(idle.HoldRemaining!=remaining)throw new Exception("Mouse tracking consumed the idle gaze clock.");
            ClockGaze laying=new ClockGaze(true);laying.Frame=11;
            foreach(int expected in new[]{12,13,0}){laying.StepToFrame(.1,0);if(laying.Frame!=expected)throw new Exception("Laying turn skipped a new pose.");}
            laying.Frame=0;foreach(int expected in new[]{13,12,11}){laying.StepToFrame(.1,11);if(laying.Frame!=expected)throw new Exception("Reverse laying turn skipped a new pose.");}
            for(int frame=0;frame<14;frame++)
            {
                double a=LayingAngles[frame]*Math.PI/180;
                PointF target=new PointF(head.X+(float)(300*Math.Sin(a)),head.Y-(float)(300*Math.Cos(a)));
                for(int i=0;i<120;i++)laying.Step(1.0/60,head,target,15);
                if(laying.Frame!=frame)throw new Exception("Laying direction mismatch.");
            }
            foreach(string family in new[]{"rear","lookback","side"})
            {
                double[] table=HeadTurnAngles(family);ClockGaze turn=new ClockGaze(table,true);
                for(int frame=0;frame<table.Length;frame++)
                {
                    double a=table[frame]*Math.PI/180;PointF target=new PointF(head.X+(float)(300*Math.Sin(a)),head.Y-(float)(300*Math.Cos(a)));
                    for(int i=0;i<120;i++)turn.Step(1.0/60,head,target,15);
                    if(turn.Frame!=frame)throw new Exception("Head-turn direction mismatch: "+family+" "+frame);
                }
                turn.Frame=0;foreach(int expected in new[]{1,2,3,4}){turn.StepToFrame(.1,4);if(turn.Frame!=expected)throw new Exception("Head turn wrapped instead of stepping through the centre: "+family);}
            }
            return new{passed=true,loafPoses=12,layingPoses=14,headTurnPoses=5,headTurnsStepThroughCentre=true,addedDegrees=new[]{340,350},clockwiseScreenMapping=true,negativeMonitorCoordinates=true,headCentered=true,shortestTurnPath=true,boundaryHysteresis=true,idleGazeHolds=true,trackingPausesIdle=true};
        }
    }
    internal sealed class ClockIdleLook
    {
        private readonly Random random;
        private readonly ClockGaze gaze;
        private int target=-1;private bool active;
        public double HoldRemaining;
        public int Frame {get{return gaze.Frame;}}
        public ClockIdleLook(int seed,bool laying=false){random=new Random(seed);gaze=new ClockGaze(laying);HoldRemaining=Range(8,20);}
        private double Range(double low,double high){return low+random.NextDouble()*(high-low);}
        public void Step(double dt,bool resting,bool suppressed)
        {
            if(!resting){if(active){active=false;target=-1;gaze.Reset();HoldRemaining=Range(8,20);}return;}
            active=true;if(suppressed)return;
            if(gaze.Frame!=target){gaze.StepToFrame(dt,target);return;}
            HoldRemaining-=Math.Max(0,dt);if(HoldRemaining>0)return;
            target=random.NextDouble()<.60?-1:random.Next(gaze.PoseCount);
            HoldRemaining=target<0?Range(10,30):Range(3,7);gaze.StepToFrame(dt,target);
        }
    }
}
