using System;

namespace OdinV2
{
    // Body pose, gaze, and eyelids have independent clocks. Eyelid artwork is
    // optional: unapproved or missing expressions never substitute another pose.
    internal sealed class SittingBehavior
    {
        public readonly IdleLook Gaze;
        public int Blinks, Naps;
        public double LastNapSeconds, ClosedSeconds;
        public double EyeClosure { get; private set; }
        public int EyeFrame { get { return EyeClosure<.18?0:EyeClosure<.76?1:2; } }
        public bool Napping { get { return nap && stage!=0; } }
        public bool EyesBusy { get { return stage!=0; } }
        private readonly Random random;
        private readonly bool hasEyes;
        private double nextChoice, phaseAge, closedFor, napCooldown;
        private int stage;private bool nap;

        public SittingBehavior(int seed,bool approvedEyeArtwork)
        {
            random=new Random(seed);Gaze=new IdleLook(seed^0x41C7);
            hasEyes=approvedEyeArtwork;nextChoice=Range(12,30);
        }
        private double Range(double low,double high){return low+random.NextDouble()*(high-low);}
        private void Open(){if(stage!=0 && stage!=4){stage=4;phaseAge=(1-EyeClosure)*.28;}}
        public void Step(double dt,bool sitting,bool attention,bool hover)
        {
            // Catch up across slow decodes using the same elapsed-time slices as
            // normal rendering. No lost seconds and no burst of frame chance rolls.
            double left=Math.Max(0,dt);
            if(left==0){StepSlice(0,sitting,attention,hover);return;}
            while(left>.000001){double part=Math.Min(.05,left);StepSlice(part,sitting,attention,hover);left-=part;}
        }
        private void StepSlice(double dt,bool sitting,bool attention,bool hover)
        {
            dt=Math.Max(0,dt);
            if(!sitting)
            {
                stage=0;nap=false;EyeClosure=0;Gaze.Step(dt,false,false);return;
            }
            napCooldown=Math.Max(0,napCooldown-dt);
            if(attention || (hover && nap))Open();
            Gaze.Step(dt,true,attention || hover || EyesBusy);
            if(stage!=0)
            {
                phaseAge+=dt;
                if(stage==1)
                {
                    EyeClosure=Math.Min(1,phaseAge/.22);
                    if(phaseAge>=.22){stage=nap?3:2;phaseAge=0;}
                }
                else if(stage==2 || stage==3)
                {
                    EyeClosure=1;ClosedSeconds+=dt;
                    if(phaseAge>=closedFor){stage=4;phaseAge=0;}
                }
                else
                {
                    EyeClosure=Math.Max(0,1-phaseAge/.28);
                    if(phaseAge>=.28){stage=0;nap=false;EyeClosure=0;nextChoice=Range(12,30);}
                }
                return;
            }
            // No probability rolls per frame. Choices occur at elapsed deadlines,
            // include quiet inactivity, and wait while incompatible actions run.
            if(attention || hover || !hasEyes)return;
            nextChoice-=dt;
            if(nextChoice>0 || Gaze.Frame!=16)return;
            nextChoice=Range(12,30);double choice=random.NextDouble();
            if(choice<.56)return;
            nap=choice>.92 && napCooldown<=0;
            stage=1;phaseAge=0;closedFor=nap?Range(3,20):Range(.09,.18);
            if(nap){Naps++;LastNapSeconds=closedFor;napCooldown=Range(120,300);}else Blinks++;
        }
    }
}
