using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
namespace OdinV2
{
    internal static class SleepBellyProtectionQA
    {
        static string Hash(string path)
        {
            using(SHA256 hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();
        }
        static int Bottom(Bitmap image,int x)
        {
            for(int y=1159;y>=1080;y--)if(image.GetPixel(x,y).A>64)return y;
            return -1;
        }
        static int Main(string[] args)
        {
            string root=args[0],report=args[1];
            try
            {
                string basisPath=Path.Combine(root,"draft-assets/queued-review-v3/sleeping.png");
                string sourceHash=Hash(basisPath);Rectangle protectedBelly=new Rectangle(595,1125,160,20);
                List<object> cases=new List<object>();
                using(Bitmap basis=new Bitmap(basisPath))
                    foreach(string phase in new[]{"out","in"})
                    {
                        string tailPath=Path.Combine(root,"draft-assets/tail-reconciliation-v1/sleep-"+phase+".png");string tailHash=Hash(tailPath);
                        using(Bitmap tail=new Bitmap(tailPath))foreach(double amount in new[]{.25,.5,1.0})
                        using(Bitmap rendered=PoseVariantRenderer.Compose(basis,tail,4,"tail",amount))
                        {
                            int changed=PoseVariantRenderer.Verify(basis,rendered,4,"tail");
                            for(int y=protectedBelly.Top;y<protectedBelly.Bottom;y++)for(int x=protectedBelly.Left;x<protectedBelly.Right;x++)
                                if(basis.GetPixel(x,y)!=rendered.GetPixel(x,y))throw new Exception("Belly contact changed in sleep-tail "+phase+" at "+x+","+y);
                            foreach(int x in new[]{600,625,650,675})if(Bottom(rendered,x)!=1140)throw new Exception("Dense belly fur lost floor1140 at x"+x);
                            foreach(int x in new[]{700,725,750})if(Math.Abs(Bottom(rendered,x)-1140)>3)throw new Exception("Natural contact fringe exceeds3px floor tolerance.");
                            int movingTailPixels=0,tailOverhangPixels=0;
                            for(int y=930;y<rendered.Height;y++)for(int x=180;x<575;x++)
                            {
                                if(basis.GetPixel(x,y)!=rendered.GetPixel(x,y))movingTailPixels++;
                                if(y>1140&&rendered.GetPixel(x,y).A>64)tailOverhangPixels++;
                            }
                            if(movingTailPixels<100||tailOverhangPixels<100)throw new Exception("Tail movement or its permitted overhang was lost.");
                            Rectangle bounds=PoseVariantRenderer.VisibleBounds(rendered);
                            if(bounds.Left<=0||bounds.Top<=0||bounds.Right>=rendered.Width||bounds.Bottom>=rendered.Height)throw new Exception("Rendered tail margins clipped.");
                            cases.Add(new{phase=phase,amount=amount,changedPixels=changed,bellyRgbaIdentical=true,denseContactY=1140,movingTailPixels=movingTailPixels,allowedTailOverhangPixels=tailOverhangPixels,marginsClear=true});
                        }
                        if(Hash(tailPath)!=tailHash)throw new Exception("Tail source changed.");
                    }
                if(Hash(basisPath)!=sourceHash)throw new Exception("Sleeping source changed.");
                object result=new{passed=true,headless=true,caseCount=cases.Count,supportY=1140,protectedBelly=new[]{595,1125,160,20},pixelsOutsideLayerUnchanged=true,sourceHashesPreserved=true,cases=cases};
                string json=new JavaScriptSerializer().Serialize(result);File.WriteAllText(report,json);Console.WriteLine(json);return 0;
            }
            catch(Exception error){File.WriteAllText(report,error.ToString());Console.Error.WriteLine(error);return 1;}
        }
    }
}
