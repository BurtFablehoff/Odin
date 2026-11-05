using System;
using System.Drawing;

namespace OdinV2
{
    internal sealed class ClockGaze
    {
        public int Frame=-1;
        private double untilStep;
        private const double Turn=Math.PI*2;
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
            target%=12;
            if(Frame<0){Frame=target;untilStep=.065;return;}
            int distance=(target-Frame+18)%12-6;
            if(distance==0){untilStep=0;return;}
            untilStep-=Math.Max(0,dt);if(untilStep>0)return;
            Frame=(Frame+(distance>0?1:11))%12;untilStep=.065;
        }
        public void Step(double dt,PointF head,PointF target,double deadZone)
        {
            int desired=FromTarget(head,target,deadZone);
            if(desired>=0 && Frame>=0)
            {
                double angle=(Math.Atan2(target.X-head.X,-(target.Y-head.Y))+Turn)%Turn;
                double difference=Math.Abs(angle-Frame*Turn/12);difference=Math.Min(difference,Turn-difference);
                if(difference<=Turn/24+.08)desired=Frame;
            }
            StepToFrame(dt,desired);
        }
        public static object Test(ArtPack art)
        {
            foreach(string state in new[]{"loaf","lay"})if(art.Manifest.clips["look_"+state].frames!=12)throw new Exception("Each clock gaze set must contain 12 poses.");
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
            return new{passed=true,posesPerState=12,clockwiseScreenMapping=true,negativeMonitorCoordinates=true,headCentered=true,shortestTurnPath=true,boundaryHysteresis=true,idleGazeHolds=true,trackingPausesIdle=true};
        }
    }
    internal sealed class ClockIdleLook
    {
        private readonly Random random;
        private readonly ClockGaze gaze=new ClockGaze();
        private int target=-1;private bool active;
        public double HoldRemaining;
        public int Frame {get{return gaze.Frame;}}
        public ClockIdleLook(int seed){random=new Random(seed);HoldRemaining=Range(8,20);}
        private double Range(double low,double high){return low+random.NextDouble()*(high-low);}
        public void Step(double dt,bool resting,bool suppressed)
        {
            if(!resting){if(active){active=false;target=-1;gaze.Reset();HoldRemaining=Range(8,20);}return;}
            active=true;if(suppressed)return;
            if(gaze.Frame!=target){gaze.StepToFrame(dt,target);return;}
            HoldRemaining-=Math.Max(0,dt);if(HoldRemaining>0)return;
            target=random.NextDouble()<.60?-1:random.Next(12);
            HoldRemaining=target<0?Range(10,30):Range(3,7);gaze.StepToFrame(dt,target);
        }
    }
}
