using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace OdinV2
{
    // A16: move the held pose's own tail. Never crossfade independently drawn coats.
    internal static class TailFlipRenderer
    {
        internal static Rectangle Region(int id){return id==4?new Rectangle(320,830,200,150):id==3?new Rectangle(200,700,200,280):new Rectangle(200,680,235,300);}
        static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
        internal static Bitmap Compose(Bitmap still,int id,int frame)
        {
            Bitmap result=(Bitmap)still.Clone();if(frame==0||frame==16)return result;
            double phase=Math.Pow(Math.Sin(Math.PI*frame/16.0),2);Rectangle region=Region(id);
            var src=still.LockBits(new Rectangle(Point.Empty,still.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            var dst=result.LockBits(new Rectangle(Point.Empty,result.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try
            {
                byte[] a=new byte[src.Stride*still.Height],b=new byte[dst.Stride*result.Height];Marshal.Copy(src.Scan0,a,0,a.Length);Array.Copy(a,b,a.Length);
                for(int x=region.Left;x<region.Right;x++)for(int y=region.Top;y<region.Bottom;y++)
                {
                    double weight=id==4?Smooth((x-320)/140.0)*(1-Smooth((x-480)/40.0)):1-Smooth((x-240)/(id==3?160.0:195.0));
                    double lift=phase*(id==4?62:id==3?95:110)*weight;
                    // Fade within the existing tail root, with no pasted seam or moving torso.
                    double top=id==4?830+Math.Max(0,x-380)*.40:700;
                    double sy=y+lift*Smooth((y-top)/70.0);
                    int y0=(int)Math.Floor(sy),y1=y0+1,i=y*dst.Stride+x*4;
                    if(y0<0||y1>=still.Height){for(int c=0;c<4;c++)b[i+c]=0;continue;}
                    double f=sy-y0;int p=y0*src.Stride+x*4,q=y1*src.Stride+x*4;
                    double alpha=a[p+3]*(1-f)+a[q+3]*f;b[i+3]=(byte)Math.Round(alpha);
                    for(int c=0;c<3;c++)b[i+c]=alpha<.5?(byte)0:(byte)Math.Round((a[p+c]*a[p+3]*(1-f)+a[q+c]*a[q+3]*f)/alpha);
                }
                Marshal.Copy(b,0,dst.Scan0,b.Length);
            }
            finally{still.UnlockBits(src);result.UnlockBits(dst);}
            return result;
        }
    }
}
