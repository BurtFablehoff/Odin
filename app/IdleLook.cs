using System;
using System.Collections.Generic;
namespace OdinV2
{
    internal sealed class IdleLook
    {
        // 16 is the unchanged forward-facing sitting pose, not an atlas direction.
        public int Frame=16;public int Changes;public double HoldRemaining;
        private readonly Random random;private readonly Queue<int> route=new Queue<int>();private double turnRemaining;private bool active;
        public IdleLook(int seed){random=new Random(seed);HoldRemaining=Range(8,20);}
        private double Range(double a,double b){return a+random.NextDouble()*(b-a);}
        private int[] ToCentre(int value)
        {
            if(value==4)return new[]{3,2,1,16};if(value==12)return new[]{13,14,15,16};if(value==0 || value==8)return new[]{16};return new int[0];
        }
        private int[] FromCentre(int value)
        {
            if(value==4)return new[]{1,2,3,4};if(value==12)return new[]{15,14,13,12};if(value==0)return new[]{0};if(value==8)return new[]{8};return new int[0];
        }
        public void Step(double dt,bool sittingWithoutAttention)
        {
            Step(dt,sittingWithoutAttention,false);
        }
        public void Step(double dt,bool sitting,bool suppressed)
        {
            // Attention pauses the existing schedule. Only leaving this body pose
            // starts a fresh rest; repeated pointer motion must not postpone it.
            if(!sitting){if(active){active=false;Frame=16;route.Clear();HoldRemaining=Range(8,20);}return;}
            active=true;if(suppressed)return;
            dt=Math.Max(0,dt);
            if(route.Count>0)
            {
                turnRemaining-=dt;if(turnRemaining>0)return;Frame=route.Dequeue();Changes++;turnRemaining=Range(.12,.23);
                if(route.Count==0)HoldRemaining=Frame==16?Range(10,30):Frame==0?Range(2,5):Range(3,7);return;
            }
            HoldRemaining-=dt;if(HoldRemaining>0)return;
            int next;if(Frame!=16 && random.NextDouble()<.78)next=16;
            else {int pick=random.Next(8);next=pick<4?16:pick==4?4:pick==5?12:pick==6?0:8;}
            if(next==Frame){HoldRemaining=Frame==16?Range(10,30):Range(2,6);return;}
            foreach(int item in ToCentre(Frame))route.Enqueue(item);foreach(int item in FromCentre(next))route.Enqueue(item);
            turnRemaining=Range(.12,.23);
        }
    }
}
