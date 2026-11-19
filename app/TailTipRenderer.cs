using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace OdinV2
{
    // A18: a few screen pixels at the original tip, no whole-tail redraw/uncurl.
    internal static class TailTipRenderer
    {
        internal static Rectangle Region(int id,bool wider){return id==4?new Rectangle(wider?430:455,865,wider?90:50,105):new Rectangle(220,id==3?750:790,wider?110:60,id==3?220:180);}
        internal static double MaximumLift(int id){return id==4?8:id==3?10:12;}
        static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
        internal static Bitmap Compose(Bitmap still,int id,int frame,bool wider)
        {
            Bitmap result=(Bitmap)still.Clone();if(frame==0||frame==16)return result;
            Rectangle r=Region(id,wider);double phase=Math.Pow(Math.Sin(Math.PI*frame/16.0),2);
            var src=still.LockBits(new Rectangle(Point.Empty,still.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            var dst=result.LockBits(new Rectangle(Point.Empty,result.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try
            {
                byte[] a=new byte[src.Stride*still.Height],b=new byte[dst.Stride*result.Height];Marshal.Copy(src.Scan0,a,0,a.Length);Array.Copy(a,b,a.Length);
                for(int x=r.Left;x<r.Right;x++)
                {
                    double weight=id==4?Smooth((x-(wider?430:455))/(wider?50.0:25.0))*(1-Smooth((x-480)/(wider?40.0:25.0))):1-Smooth((x-235)/(wider?95.0:45.0));
                    // Constant vertical translation within each column keeps strand thickness.
                    // No horizontal displacement, y-dependent squeeze, stretch or silhouette blend.
                    double lift=MaximumLift(id)*phase*weight;
                    if(lift<.000001)continue;
                    for(int y=r.Top;y<r.Bottom;y++)
                    {
                        double sy=y+lift;int y0=(int)Math.Floor(sy),y1=y0+1,i=y*dst.Stride+x*4;
                        double f=sy-y0;int p=y0*src.Stride+x*4,q=y1*src.Stride+x*4;
                        double alpha=a[p+3]*(1-f)+a[q+3]*f;b[i+3]=(byte)Math.Round(alpha);
                        for(int c=0;c<3;c++)b[i+c]=alpha<.5?(byte)0:(byte)Math.Round((a[p+c]*a[p+3]*(1-f)+a[q+c]*a[q+3]*f)/alpha);
                    }
                }
                Marshal.Copy(b,0,dst.Scan0,b.Length);
            }
            finally{still.UnlockBits(src);result.UnlockBits(dst);}
            return result;
        }
    }
}
