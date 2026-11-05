using System;
using System.Drawing;

namespace OdinV2
{
    internal static class RunningScale
    {
        // Whole-pose uniform corrections. The right-facing source was previously
        // reduced more than the left-facing source to fit its trailing tail.
        public static double Factor(string clip)
        {
            return clip=="loaf" || clip=="look_loaf" || clip=="loaf_eyes"?.90:clip=="run_left"?1.50:clip=="run_right"?1.88:1.0;
        }
        public static int Pixels(int size,string clip){return (int)Math.Round(size*Factor(clip));}
        public const double LeftMargin=.92;
        public const double RightMargin=.58;
        public const double Headroom=.62;
        public static unsafe Rectangle VisibleBounds(Bitmap image)
        {
            int left=image.Width,top=image.Height,right=-1,bottom=-1;
            System.Drawing.Imaging.BitmapData data=image.LockBits(new Rectangle(0,0,image.Width,image.Height),System.Drawing.Imaging.ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            try
            {
                for(int y=0;y<image.Height;y++)
                {
                    byte* row=(byte*)data.Scan0+y*data.Stride;
                    for(int x=0;x<image.Width;x++)if(row[x*4+3]>64){left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
                }
            }
            finally{image.UnlockBits(data);}
            return right<left?Rectangle.Empty:Rectangle.FromLTRB(left,top,right+1,bottom+1);
        }
        public static object Test(ArtPack art)
        {
            int frames=0;
            foreach(string name in new[]{"run_left","run_right"})
            {
                double factor=Factor(name);
                for(int index=0;index<art.Manifest.clips[name].frames;index++)
                {
                    Rectangle bounds=VisibleBounds(art.Get(name,index));bounds.Offset(RunningMotion.Offset(name,index),0);
                    double left=(art.Manifest.foot_anchor[0]-bounds.Left)*factor/art.Manifest.width;
                    double right=(bounds.Right-art.Manifest.foot_anchor[0])*factor/art.Manifest.width;
                    if(left>LeftMargin || right>RightMargin)throw new Exception("Running tail exceeds reserved monitor margin: "+name);
                    frames++;
                }
            }
            foreach(int size in new[]{480,640,800})
            {
                if(Pixels(size,"sit")!=size || Pixels(size,"lay")!=size)throw new Exception("Scale correction changed sitting or laying.");
                if(Pixels(size,"loaf")!=(int)Math.Round(size*.90) || Pixels(size,"look_loaf")!=Pixels(size,"loaf"))throw new Exception("Loaf state and gaze must share the smaller scale.");
                foreach(Rectangle area in new[]{new Rectangle(0,0,1366,768),new Rectangle(-1920,-120,1920,1080)})
                {
                    CatMind mind=new CatMind(area,5);mind.LeftMargin=size*LeftMargin;mind.RightMargin=size*RightMargin;
                    if(mind.MinX-size*.896<area.Left || mind.MaxX+size*.55>area.Right || mind.MinX>=mind.MaxX)throw new Exception("Running margin clips the tail or removes travel space.");
                }
            }
            return new{passed=true,framesChecked=frames,leftUniformScale=Factor("run_left"),rightUniformScale=Factor("run_right"),sittingAndLayingSizesUnchanged=true,loafScale=.90,tailMarginsVerified=true};
        }
    }
}
