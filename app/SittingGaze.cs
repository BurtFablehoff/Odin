using System;
using System.Collections.Generic;
using System.Drawing;

namespace OdinV2
{
    internal sealed class SittingGaze
    {
        public static readonly double[] Angles={0,337.5,315,292.5,270,247.5,225,202.5,180,157.5,135,112.5,90,67.5,45,22.5,15,100,110};
        private static readonly int[] Order=SortedOrder();
        public int Frame=-1;private double untilStep;
        private static int[] SortedOrder(){List<int> indices=new List<int>();for(int i=0;i<Angles.Length;i++)indices.Add(i);indices.Sort((a,b)=>Angles[a].CompareTo(Angles[b]));return indices.ToArray();}
        private static double Distance(double a,double b){double d=Math.Abs(a-b)%360;return Math.Min(d,360-d);}
        public void Reset(){Frame=-1;untilStep=0;}
        public void Step(double dt,PointF head,PointF target,double deadZone)
        {
            double dx=target.X-head.X,dy=target.Y-head.Y;if(dx*dx+dy*dy<deadZone*deadZone){Reset();return;}
            double angle=(Math.Atan2(dx,-dy)*180/Math.PI+360)%360;int nearest=0;double best=Double.MaxValue;
            for(int i=0;i<Angles.Length;i++){double d=Distance(angle,Angles[i]);if(d<best){best=d;nearest=i;}}
            if(Frame<0){Frame=nearest;return;}
            if(Distance(angle,Angles[Frame])<=best+1)return;
            untilStep-=Math.Max(0,dt);if(untilStep>0)return;
            double turn=(Angles[nearest]-Angles[Frame]+540)%360-180;int position=Array.IndexOf(Order,Frame);
            Frame=Order[(position+(turn>=0?1:Order.Length-1))%Order.Length];untilStep=.05;
        }
        public static object Test(ArtPack art)
        {
            if(art.Manifest.clips["look_sit"].frames!=19)throw new Exception("Sitting needs 19 gaze poses.");
            SittingGaze gaze=new SittingGaze();PointF head=new PointF(-300,400);
            foreach(int wanted in new[]{16,17,18})
            {
                double angle=Angles[wanted]*Math.PI/180;PointF target=new PointF(head.X+(float)(300*Math.Sin(angle)),head.Y-(float)(300*Math.Cos(angle)));
                for(int i=0;i<240;i++)gaze.Step(.016,head,target,12);
                if(gaze.Frame!=wanted)throw new Exception("Sitting in-between mapped to the wrong direction: "+wanted);
            }
            gaze.Frame=12;HashSet<int> visited=new HashSet<int>();double end=110*Math.PI/180;
            for(int i=0;i<120;i++){gaze.Step(.016,head,new PointF(head.X+(float)(300*Math.Sin(end)),head.Y-(float)(300*Math.Cos(end))),12);visited.Add(gaze.Frame);}
            if(!visited.Contains(17)||!visited.Contains(18))throw new Exception("Right/down-right gaze skipped its new in-betweens.");
            gaze.Reset();gaze.Frame=0;visited.Clear();end=22.5*Math.PI/180;
            for(int i=0;i<120;i++){gaze.Step(.016,head,new PointF(head.X+(float)(300*Math.Sin(end)),head.Y-(float)(300*Math.Cos(end))),12);visited.Add(gaze.Frame);}
            if(!visited.Contains(16))throw new Exception("Up/up-right gaze skipped its new in-between.");
            return new{passed=true,poses=19,addedDegrees=new[]{15,100,110},newPosesTraversed=true};
        }
    }
}
