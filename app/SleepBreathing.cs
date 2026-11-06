using System;

namespace OdinV2
{
    // An occasional resting action, never a continuously running simulation.
    internal sealed class SleepBreathing
    {
        readonly Random random;
        double remaining, age;
        bool entered;
        public const double CycleSeconds=3.2;
        public bool Busy {get;private set;}
        public int Plays {get;private set;}
        public int Frame {get{return Busy?Math.Min(7,(int)(age/.4)):0;}}
        public double Amount {get{return Busy ? .5-.5*Math.Cos(age*Math.PI*2/CycleSeconds) : 0;}}
        public SleepBreathing(int seed){random=new Random(seed);Reset();}
        double QuietDelay(){return 120+random.NextDouble()*180;}
        public void Reset(){Busy=false;age=0;entered=false;remaining=QuietDelay();}
        public void PreviewOnce(){Busy=true;age=0;entered=true;Plays++;}
        public void Step(double dt,bool sleeping,bool neutral)
        {
            if(!sleeping){if(entered||Busy)Reset();return;}
            if(!entered){entered=true;remaining=QuietDelay();return;}
            if(!neutral){if(Busy){Busy=false;age=0;remaining=QuietDelay();}return;}
            // A delayed timer must not replay missed actions after a pause.
            dt=Math.Min(1,Math.Max(0,dt));
            if(Busy)
            {
                age+=dt;
                if(age>=CycleSeconds){Busy=false;age=0;remaining=QuietDelay();}
                return;
            }
            remaining-=dt;
            if(remaining<=0){Busy=true;age=0;Plays++;}
        }
    }
    internal static class SleepBreathingTest
    {
        public static object Run()
        {
            SleepBreathing breath=new SleepBreathing(91);breath.Step(0,true,true);
            for(int i=0;i<1190;i++)breath.Step(.1,true,true);
            if(breath.Plays!=0||breath.Busy)throw new Exception("Breathing started before the quiet interval.");
            double active=0,elapsed=119;int previous=0;double lastEnd=-1;bool wasBusy=false;
            for(int i=0;i<36000;i++)
            {
                breath.Step(.1,true,true);elapsed+=.1;
                if(breath.Plays!=previous){if(lastEnd>=0&&elapsed-lastEnd<119.9)throw new Exception("Breathing bursts were too close together.");previous=breath.Plays;}
                if(breath.Busy)active+=.1;
                if(wasBusy&&!breath.Busy)lastEnd=elapsed;
                wasBusy=breath.Busy;
            }
            if(breath.Plays<10||breath.Plays>31||active/elapsed>.02)throw new Exception("Breathing is missing or too frequent.");
            SleepBreathing blocked=new SleepBreathing(12);
            for(int i=0;i<10000;i++)blocked.Step(.1,true,false);
            if(blocked.Plays!=0)throw new Exception("Breathing overlapped another action.");
            blocked.PreviewOnce();blocked.Step(.1,false,true);
            if(blocked.Busy||blocked.Amount!=0||blocked.Frame!=0)throw new Exception("Breathing survived leaving sleep.");
            SleepBreathing paused=new SleepBreathing(15);paused.Step(0,true,true);paused.Step(3600,true,true);
            if(paused.Plays!=0)throw new Exception("A suspended timer replayed missed actions.");
            return new{passed=true,quietSecondsMin=120,quietSecondsMax=300,cycleSeconds=SleepBreathing.CycleSeconds,simulatedPlays=breath.Plays,stillFraction=1-active/elapsed,nonLooping=true,noOverlap=true,noCatchUpBursts=true};
        }
    }
}
