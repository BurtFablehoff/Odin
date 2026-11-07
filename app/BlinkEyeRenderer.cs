// Odin-specific eye locations. Keep anatomy out of the shared pet framework.
#if ODIN_CONTACT_REVIEW || ODIN_DEV
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace OdinV2
{
    internal static class BlinkEyeRenderer
    {
        static Rectangle[] Regions(int pose)
        {
            switch(pose)
            {
                case 1:return new[]{new Rectangle(575,730,72,54),new Rectangle(645,739,57,45)};
                case 2:return new[]{new Rectangle(687,875,84,64),new Rectangle(788,881,70,58)};
                case 3:return new[]{new Rectangle(695,861,78,58),new Rectangle(774,881,70,50)};
                default:throw new ArgumentOutOfRangeException("pose");
            }
        }
        static double Weight(double x,double y,Rectangle region)
        {
            double dx=(x+.5-(region.Left+region.Width/2.0))/(region.Width/2.0);
            double dy=(y+.5-(region.Top+region.Height/2.0))/(region.Height/2.0);
            double radius=Math.Sqrt(dx*dx+dy*dy);
            return Math.Max(0,Math.Min(1,(1-radius)/.22));
        }
        public static Bitmap Compose(Bitmap basis,Bitmap draft,int pose)
        {
            using(Bitmap fitted=PoseVariantRenderer.FitDraft(basis,draft,pose))
            {
            double scale=pose==1?1254.0/basis.Width:1;
            Bitmap result=(Bitmap)basis.Clone();
            foreach(Rectangle region in Regions(pose))
                for(int y=(int)Math.Floor(region.Top/scale);y<(int)Math.Ceiling(region.Bottom/scale);y++)
                for(int x=(int)Math.Floor(region.Left/scale);x<(int)Math.Ceiling(region.Right/scale);x++)
                {
                    double weight=Weight((x+.5)*scale-.5,(y+.5)*scale-.5,region);if(weight<=0)continue;
                    Color a=basis.GetPixel(x,y),b=fitted.GetPixel(x,y);
                    result.SetPixel(x,y,Color.FromArgb(a.A,
                        (int)Math.Round(a.R+(b.R-a.R)*weight),
                        (int)Math.Round(a.G+(b.G-a.G)*weight),
                        (int)Math.Round(a.B+(b.B-a.B)*weight)));
                }
            return result;
            }
        }
        static byte[] Pixels(Bitmap image)
        {
            BitmapData data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            try
            {
                if(data.Stride!=image.Width*4)throw new Exception("Unexpected pixel stride.");
                byte[] pixels=new byte[data.Stride*image.Height];Marshal.Copy(data.Scan0,pixels,0,pixels.Length);return pixels;
            }
            finally{image.UnlockBits(data);}
        }
        public static int Verify(Bitmap basis,Bitmap rendered,int pose)
        {
            if(basis.Size!=rendered.Size)throw new ArgumentException("Blink changed its base canvas.");
            byte[] a=Pixels(basis),b=Pixels(rendered);Rectangle[] regions=Regions(pose);int changed=0;
            double scale=pose==1?1254.0/basis.Width:1;
            for(int y=0;y<basis.Height;y++)for(int x=0;x<basis.Width;x++)
            {
                int offset=(y*basis.Width+x)*4;
                if(a[offset+3]!=b[offset+3])throw new Exception("Blink changed silhouette alpha.");
                bool same=a[offset]==b[offset]&&a[offset+1]==b[offset+1]&&a[offset+2]==b[offset+2];
                if(same)continue;
                changed++;
                double nativeX=(x+.5)*scale-.5,nativeY=(y+.5)*scale-.5;
                if(Weight(nativeX,nativeY,regions[0])<=0&&Weight(nativeX,nativeY,regions[1])<=0)
                    throw new Exception("Blink moved pixels outside eyelids.");
            }
            if(changed<50)throw new Exception("Blink did not change the eyelids.");
            return changed;
        }
    }
}
#endif
