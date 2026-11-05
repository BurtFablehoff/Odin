using System;

namespace OdinV2
{
    internal sealed class RestBlink
    {
        private readonly Random random;private double untilBlink,age;private int phase=-1;
        public int Blinks;
        public int Frame {get{return phase<0?0:new[]{1,2,1,0}[Math.Min(3,phase)];}}
        public bool Busy {get{return phase>=0;}}
        public RestBlink(int seed){random=new Random(seed);untilBlink=Range();}
        private double Range(){return 9+random.NextDouble()*17;}
        public void Step(double dt,bool active,bool suppressed)
        {
            if(!active){phase=-1;age=0;return;}
            if(suppressed){phase=-1;age=0;return;}
            if(phase<0){untilBlink-=Math.Max(0,dt);if(untilBlink<=0){phase=0;age=0;Blinks++;}return;}
            age+=Math.Max(0,dt);while(age>=.11 && phase>=0){age-=.11;phase++;if(phase>=4){phase=-1;untilBlink=Range();}}
        }
    }
    internal sealed class TailFlick
    {
        private readonly Random random;private readonly bool sleeping;private double untilFlick,age;private bool busy;
        public int Flicks;
        public bool Busy {get{return busy;}}
        public int Frame {get{return busy?Math.Min(4,(int)(age/.14)):0;}}
        public TailFlick(int seed,bool asleep=false){random=new Random(seed);sleeping=asleep;untilFlick=Range();}
        private double Range(){return sleeping?60+random.NextDouble()*90:25+random.NextDouble()*50;}
        public void Step(double dt,bool active,bool neutral)
        {
            if(!active){busy=false;age=0;return;}
            if(busy){age+=Math.Max(0,dt);if(age>=.70){busy=false;age=0;untilFlick=Range();}return;}
            if(!neutral)return;untilFlick-=Math.Max(0,dt);if(untilFlick<=0){busy=true;age=0;Flicks++;}
        }
    }
    internal static class RestEffectsTest
    {
        public static object Run(ArtPack art)
        {
            foreach(string state in new[]{"loaf","lay"})if(!art.Manifest.clips.ContainsKey(state+"_eyes") || art.Manifest.clips[state+"_eyes"].frames!=3)throw new Exception("Missing half/closed eye expressions: "+state);
            foreach(string state in new[]{"sit","lay","sleep"})
            {
                ClipInfo clip=art.Manifest.clips[state+"_tail"];
                if(clip.frames!=5 || clip.loop || clip.images[0].offset!=clip.images[4].offset)throw new Exception("Tail flick must return to its resting pose: "+state);
            }
            if(art.Manifest.clips.ContainsKey("loaf_tail"))throw new Exception("Broken loaf tail flick must be excluded.");
            RestBlink eyes=new RestBlink(23);TailFlick awake=new TailFlick(47),asleep=new TailFlick(53,true);
            for(int i=0;i<20000;i++){eyes.Step(.1,true,false);awake.Step(.1,true,true);asleep.Step(.1,true,true);}
            if(eyes.Blinks<20 || awake.Flicks<15 || asleep.Flicks<8 || asleep.Flicks>=awake.Flicks)throw new Exception("Eye/tail scheduling is missing or too frequent during sleep.");
            TailFlick blocked=new TailFlick(7);for(int i=0;i<10000;i++)blocked.Step(.1,true,false);
            if(blocked.Flicks!=0)throw new Exception("Tail flick interrupted another expression.");
            return new{passed=true,eyePosesPerAwakeState=3,tailFramesPerState=5,awakeBlinks=eyes.Blinks,awakeTailFlicks=awake.Flicks,sleepTailFlicks=asleep.Flicks,nonLooping=true};
        }
    }
}
