using System;
using System.Collections.Generic;
using System.Web.Script.Serialization;
namespace OdinV2
{
    // Per-frame placement of the running frames, native px of the 1024 canvas (dx positive = right, dy positive = down).
    // V12 2b (DEV): registered by the HEAD. The tracked nose tip sits at one x relative to the anchor and inside a 14 px
    // height band (planning/run-registration-v12.json; tables generated into app/RunningRegistration.cs by
    // tools/local/Measure-RunHeadsV12.py). Production draws it too since Part 5c; the v11 torso tables stay for the test's comparison.
    internal static class RunningMotion
    {
        private static readonly int[] TorsoLeft={16,14,15,-7,-19,3,16,19,23,-23,-68,-15,28,24,22,-9,-29,-10};
        private static readonly int[] TorsoRight={44,83,125,48,-54,-46,-10,-27,-60,-26,-4,-12,-21,-19,-28,8};
        // Part 5c: the head registration is production (G2 passed; promoted on Hatt's word 2026-11-08).
        public const bool HeadRegistered=true;
        public const double HeadStepLimit=8,HeadStepLimitWithTweens=4,BobLimit=20;
        // V12 2b (DEV): run clips show exactly one frame per render tick (Desktop.cs timer, 16 ms), so no frame is held
        // for two ticks while its neighbour gets one. Rounding (not flooring) the clip age keeps a few ms of timer jitter
        // from skipping or repeating a frame.
        public const double FrameSeconds=.016;
        public static int TickFrame(double age,int frames){return frames<=0?0:(int)((long)Math.Round(Math.Max(0,age)/FrameSeconds)%frames);}
        public static bool IsRun(string clip){return clip=="run_left" || clip=="run_right";}
        private static int Index(int frame,int count){return Math.Max(0,Math.Min(count-1,frame));}
        // Offsets of the atlas frames (art/frames/run_*, production order) as this build draws them.
        public static int Offset(string clip,int frame){return Offset(clip,frame,HeadRegistered);}
        public static int OffsetY(string clip,int frame){return OffsetY(clip,frame,HeadRegistered);}
        private static int Offset(string clip,int frame,bool head)
        {
            if(!IsRun(clip))return 0;
            if(head){double[] dx=clip=="run_left"?RunningRegistration.LeftDx:RunningRegistration.RightDx;return (int)Math.Round(dx[Index(frame,dx.Length)]);}
            int[] torso=clip=="run_left"?TorsoLeft:TorsoRight;return torso[Index(frame,torso.Length)];
        }
        private static int OffsetY(string clip,int frame,bool head)
        {
            if(!head || !IsRun(clip))return 0;
            double[] dy=clip=="run_left"?RunningRegistration.LeftDy:RunningRegistration.RightDy;return (int)Math.Round(dy[Index(frame,dy.Length)]);
        }
        // Largest frame-to-frame nose jump in placed space (euclidean, loop seam included) and the bob (max - min placed y).
        // With tween flags, steps touching a tween are held to the tween limit and the others to the plain limit.
        private static void Steadiness(double[] x,double[] y,bool[] tween,out double step,out double tweenStep,out double plainStep,out double bob,out int worst)
        {
            step=tweenStep=plainStep=0;worst=0;double low=double.MaxValue,high=double.MinValue;
            for(int i=0;i<x.Length;i++)
            {
                int next=(i+1)%x.Length;double jump=Math.Sqrt(Math.Pow(x[next]-x[i],2)+Math.Pow(y[next]-y[i],2));
                if(jump>step){step=jump;worst=i;}
                if(tween!=null && (tween[i] || tween[next]))tweenStep=Math.Max(tweenStep,jump);else plainStep=Math.Max(plainStep,jump);
                low=Math.Min(low,y[i]);high=Math.Max(high,y[i]);
            }
            bob=high-low;
        }
        public static object Test()
        {
            bool checkedHead=HeadRegistered;   // checks exactly the registration this build draws
            List<string> failures=new List<string>();Dictionary<string,object> clips=new Dictionary<string,object>();
            if(Offset("sit",0)!=0 || OffsetY("sit",0)!=0 || Offset("jump_left",4)!=0 || OffsetY("jump_left",4)!=0)failures.Add("running registration changed another animation");
            foreach(string clip in new[]{"run_left","run_right"})
            {
                bool left=clip=="run_left";
                double[] noseX=left?RunningRegistration.LeftNoseX:RunningRegistration.RightNoseX,noseY=left?RunningRegistration.LeftNoseY:RunningRegistration.RightNoseY;
                int n=noseX.Length,torsoCount=(left?TorsoLeft:TorsoRight).Length;
                if(n!=torsoCount)failures.Add(clip+": head tables cover "+n+" frames, the atlas has "+torsoCount);
                double[] px=new double[n],py=new double[n],tx=new double[n],ty=new double[n];
                for(int i=0;i<n;i++){px[i]=noseX[i]+Offset(clip,i,checkedHead);py[i]=noseY[i]+OffsetY(clip,i,checkedHead);tx[i]=noseX[i]+Offset(clip,i,false);ty[i]=noseY[i];}
                double step,bob,unusedTween,unusedPlain,torsoStep,torsoBob;int worst,torsoWorst;
                Steadiness(px,py,null,out step,out unusedTween,out unusedPlain,out bob,out worst);
                Steadiness(tx,ty,null,out torsoStep,out unusedTween,out unusedPlain,out torsoBob,out torsoWorst);
                if(step>HeadStepLimit)failures.Add(clip+": head step "+Math.Round(step,2)+" native px (frame "+worst+" to "+((worst+1)%n)+") exceeds "+HeadStepLimit);
                if(bob>BobLimit)failures.Add(clip+": head bob "+Math.Round(bob,2)+" native px exceeds "+BobLimit);
                Dictionary<string,object> result=new Dictionary<string,object>{
                    {"registration",checkedHead?"head (nose tip) v12":"torso v11"},{"frames",n},{"headStepNative",Math.Round(step,2)},{"worstStepFrom",worst},{"bobNative",Math.Round(bob,2)},
                    {"v11TorsoHeadStepNative",Math.Round(torsoStep,2)},{"v11TorsoBobNative",Math.Round(torsoBob,2)}};
#if ODIN_DEV
                // The DEV play order (originals plus the kept optical-flow tweens), placed by its own registration (the DEV
                // pack's anchors; --test-run-motion checks the pack matches). A step touching a tween is held to the tween
                // limit; two originals left adjacent by a dropped tween keep the plain limit.
                string[] names=left?RunningRegistration.DevLeftFrames:RunningRegistration.DevRightFrames;bool[] tween=left?RunningRegistration.DevLeftTween:RunningRegistration.DevRightTween;
                double[] devX=left?RunningRegistration.DevLeftNoseX:RunningRegistration.DevRightNoseX,devY=left?RunningRegistration.DevLeftNoseY:RunningRegistration.DevRightNoseY;
                double[] devDx=left?RunningRegistration.DevLeftDx:RunningRegistration.DevRightDx,devDy=left?RunningRegistration.DevLeftDy:RunningRegistration.DevRightDy;
                int m=names.Length,tweens=0,original=0;double[] qx=new double[m],qy=new double[m];
                for(int i=0;i<m;i++)
                {
                    qx[i]=devX[i]+devDx[i];qy[i]=devY[i]+devDy[i];
                    if(tween[i]){tweens++;continue;}
                    double[] dx=left?RunningRegistration.LeftDx:RunningRegistration.RightDx,dy=left?RunningRegistration.LeftDy:RunningRegistration.RightDy;
                    if(names[i]!=original.ToString("000") || devDx[i]!=dx[original] || devDy[i]!=dy[original])failures.Add(clip+": DEV play order frame "+i+" ("+names[i]+") is not original "+original.ToString("000")+" with its registration");
                    original++;
                }
                if(original!=n)failures.Add(clip+": DEV play order holds "+original+" originals, the atlas has "+n);
                double devStep,devTweenStep,devPlainStep,devBob;int devWorst;
                Steadiness(qx,qy,tween,out devStep,out devTweenStep,out devPlainStep,out devBob,out devWorst);
                if(devTweenStep>HeadStepLimitWithTweens)failures.Add(clip+": DEV head step next to a tween "+Math.Round(devTweenStep,2)+" native px exceeds "+HeadStepLimitWithTweens);
                if(devPlainStep>HeadStepLimit)failures.Add(clip+": DEV head step "+Math.Round(devPlainStep,2)+" native px exceeds "+HeadStepLimit);
                if(devBob>BobLimit)failures.Add(clip+": DEV head bob "+Math.Round(devBob,2)+" native px exceeds "+BobLimit);
                result.Add("dev",new{frames=m,tweens=tweens,dropped=n-tweens,cycleSeconds=Math.Round(m*FrameSeconds,3),headStepNextToTweenNative=Math.Round(devTweenStep,2),
                    headStepBetweenOriginalsNative=Math.Round(devPlainStep,2),bobNative=Math.Round(devBob,2),playOrder=names});
#endif
                clips.Add(clip,result);
            }
            // One frame per tick: clip age accumulated tick by tick (as CatMind.Step does) shows frame k on tick k, also with
            // +/-5 ms of timer jitter on every tick.
            int tickCases=0;Random jitter=new Random(12);
            foreach(int frames in new[]{16,17,18,21,32,36})
            {
                double age=0;
                for(int tick=0;tick<frames*3;tick++)
                {
                    double late=(jitter.NextDouble()-.5)*.010;
                    if(TickFrame(age,frames)!=tick%frames || TickFrame(age+late,frames)!=tick%frames){failures.Add("tick "+tick+" of a "+frames+"-frame run did not show frame "+(tick%frames));break;}
                    age+=FrameSeconds;tickCases++;
                }
            }
            Dictionary<string,object> report=new Dictionary<string,object>{
                {"passed",failures.Count==0},{"oneFramePerTick",new{frameSeconds=FrameSeconds,cases=tickCases,jitterMs=5}},{"drawnRegistration",HeadRegistered?"head (nose tip) v12":"torso v11"},{"checkedRegistration",checkedHead?"head (nose tip) v12":"torso v11"},
                {"limits",new{headStepNative=HeadStepLimit,headStepWithTweens=HeadStepLimitWithTweens,bob=BobLimit,units="native px of the 1024 canvas, placed space"}},
                {"loopSeamChecked",true},{"clips",clips},{"failures",failures}};
            if(failures.Count>0)throw new Exception("Running head registration failed: "+new JavaScriptSerializer().Serialize(report));
            return report;
        }
    }
}
