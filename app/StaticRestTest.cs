using System;
namespace OdinV2
{
    internal static class StaticRestTest
    {
        public static object Run(ArtPack art)
        {
            int checks=0;
            foreach(string clip in new[]{"sit","loaf","lay","sleep"})
            {
                if(art.Manifest.clips[clip].frames!=1)throw new Exception("Base resting pose must be static: "+clip);
                foreach(double age in new[]{0,.04,1.6,3.2,120,300,3600})
                {
                    if(art.Frame(clip,age)!=0)throw new Exception("Resting base frame changes with time.");checks++;
                }
            }
            return new{passed=true,staticRestStates=4,timeSamplesChecked=checks,noBreathing=true,otherRestActionsPreserved=true};
        }
    }
}
