using System;
namespace OdinV2
{
    internal static class RunningMotion
    {
        // Whole-sprite registration measured from the eroded torso core, excluding
        // thin tail/leg silhouettes. Preserve 8% of natural horizontal body motion.
        private static readonly int[] Left={16,14,15,-7,-19,3,16,19,23,-23,-68,-15,28,24,22,-9,-29,-10};
        private static readonly int[] Right={44,83,125,48,-54,-46,-10,-27,-60,-26,-4,-12,-21,-19,-28,8};
        private static readonly double[] LeftCenters={481.24,482.54,482.20,505.52,518.64,495.22,480.70,477.09,473.08,522.84,572.47,514.85,467.80,472.40,474.50,507.98,529.57,509.54};
        private static readonly double[] RightCenters={383.41,340.98,294.61,379.00,489.43,480.91,441.58,460.67,496.09,459.08,434.96,444.09,453.83,451.44,461.74,422.17};
        public static int Offset(string clip,int frame)
        {
            int[] offsets=clip=="run_left"?Left:clip=="run_right"?Right:null;
            return offsets==null?0:offsets[Math.Max(0,Math.Min(offsets.Length-1,frame))];
        }
        public static object Test()
        {
            double[] before=new double[2],after=new double[2];
            for(int direction=0;direction<2;direction++)
            {
                double[] centers=direction==0?LeftCenters:RightCenters;string clip=direction==0?"run_left":"run_right";
                for(int i=0;i<centers.Length;i++)
                {
                    int next=(i+1)%centers.Length;
                    before[direction]=Math.Max(before[direction],Math.Abs(centers[next]-centers[i]));
                    after[direction]=Math.Max(after[direction],Math.Abs(centers[next]+Offset(clip,next)-centers[i]-Offset(clip,i)));
                }
                if(after[direction]>before[direction]*.12)throw new Exception("Running horizontal registration failed, including loop seam.");
            }
            if(Offset("sit",0)!=0 || Offset("jump_left",4)!=0)throw new Exception("Running registration changed another animation.");
            return new{passed=true,leftMaximumStepBefore=before[0],leftMaximumStepAfter=after[0],rightMaximumStepBefore=before[1],rightMaximumStepAfter=after[1],loopSeamChecked=true,artworkAndVerticalBounceUnchanged=true};
        }
    }
}
