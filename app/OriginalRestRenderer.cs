// Part 5c (rule A9, Hatt 2026-11-08 "okay lets push this one to release"): compiled in production too; the production exe draws the promoted pack.
// Odin-only registration of original1024 directional heads onto original bodies.
// Every source PNG remains unchanged; only in-memory review/runtime composites.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace OdinV2
{
    internal static class OriginalRestRenderer
    {
        static readonly Rectangle[] SitEyes={new Rectangle(472,587,52,43),new Rectangle(532,595,43,35)};
        static readonly Rectangle[] LoafEyes={new Rectangle(562,709,71,55),new Rectangle(643,713,54,46)};
        static readonly Rectangle[] LayEyes={new Rectangle(569,697,64,52),new Rectangle(639,712,52,42)};
        // Source-minus-neutral translations registered on the original foreleg/
        // shoulder core, excluding the tail and head. Original look_sit bodies
        // are15-22px left of the neutral body; sampling without this registration
        // leaves the moving head/collar disconnected from the fixed shoulders.
        static readonly int[] SitSourceX={-19,-18,-22,-19,-22,-22,-22,-19,-19,-20,-21,-20,-18,-19,-19,-18,-15,-17,-15};
        static readonly int[] SitSourceY={-5,-6,-4,-5,-5,-5,-5,-5,-4,-4,-3,-4,-4,-4,-3,-4,-3,-3,-3};
        static readonly int[] LoafSourceX={0,-1,2,1,0,1,1,-2,-1,-2,0,0};
        static readonly int[] LoafSourceY={-3,-3,-2,-2,-1,-1,0,2,1,1,0,0};
        static readonly int[] LaySourceX={-1,-1,2,1,-1,-1,1,0,-1,-1,1,1,-1,-1};
        static readonly int[] LaySourceY={-2,-2,-2,-1,-2,-2,-2,-2,-2,-2,-2,-3,-2,-2};
        public static Point SourceOffset(int pose,string clip,int frame)
        {
            if(!clip.StartsWith("look_"))return Point.Empty;
            int[] xx=pose==1?SitSourceX:pose==2?LoafSourceX:pose==3?LaySourceX:null;
            int[] yy=pose==1?SitSourceY:pose==2?LoafSourceY:pose==3?LaySourceY:null;
            if(xx==null||frame<0||frame>=xx.Length)throw new ArgumentOutOfRangeException("frame");
            return new Point(xx[frame],yy[frame]);
        }
        static double Clamp(double value){return Math.Max(0,Math.Min(1,value));}
        static double Smooth(double value){double t=Clamp(value);return t*t*(3-2*t);}
        public static int ProtectedBodyTop(int pose)
        {
            switch(pose){case 1:return 752;case 2:return 878;case 3:return 862;case 4:return 920;default:throw new ArgumentOutOfRangeException("pose");}
        }
        public static int FixedSilhouetteTop(int pose)
        {
            switch(pose){case 1:return 735;case 2:return 865;case 3:return 850;case 4:return 920;default:throw new ArgumentOutOfRangeException("pose");}
        }
        // These original-native masks cover ears/face/jaw only. The lower seam
        // tapers into the original neck attachment; chest, paws and floor below
        // ProtectedBodyTop stay exact. They are unrelated to1254 draft masks.
        static double Head(int pose,int x,int y)
        {
            double left,right,top,bottom,neckLeft,neckRight;
            switch(pose)
            {
                case 1:left=345;right=710;top=480;bottom=752;neckLeft=410;neckRight=650;break;
                case 2:left=405;right=840;top=515;bottom=878;neckLeft=525;neckRight=745;break;
                case 3:left=440;right=845;top=535;bottom=862;neckLeft=550;neckRight=755;break;
                default:return 0;
            }
            if(y<top||y>=bottom)return 0;
            double lower=Smooth((y-(bottom-52))/52.0);
            left+=(neckLeft-left)*lower;right+=(neckRight-right)*lower;
            // Sitting's fixed right flank begins below the neck. Do not let a
            // directional source's ruff paint over that independently fixed rim.
            if(pose==1)right=Math.Min(right,710-95*Smooth((y-685)/50.0));
            double horizontal=Math.Min(Smooth((x-left)/12.0),Smooth((right-x)/12.0));
            return Math.Min(horizontal,Smooth((bottom-y)/24.0));
        }
        static Rectangle[] EyeRegions(int pose)
        {
            switch(pose)
            {
                case 1:return SitEyes;
                case 2:return LoafEyes;
                case 3:return LayEyes;
                default:throw new ArgumentOutOfRangeException("pose");
            }
        }
        static double Eyes(int pose,int x,int y)
        {
            double weight=0;
            foreach(Rectangle r in EyeRegions(pose))
            {
                double dx=(x+.5-r.Left-r.Width/2.0)/(r.Width/2.0),dy=(y+.5-r.Top-r.Height/2.0)/(r.Height/2.0);
                weight=Math.Max(weight,Smooth((1-Math.Sqrt(dx*dx+dy*dy))/.25));
            }
            return weight;
        }
        static double Tail(int pose,int x,int y)
        {
            double top,right,support;
            switch(pose)
            {
                case 1:top=780;right=360+Math.Max(0,y-850)*.20;support=944;break;
                case 3:top=745;right=350+Math.Max(0,y-790)*.22;support=927;break;
                case 4:top=790;right=360+Math.Max(0,y-835)*.18;support=936;break;
                default:return 0;
            }
            // Freeze the support band and keep the tail mask left of the torso.
            return Math.Min(Smooth((y-top)/25.0),Math.Min(Smooth((right-x)/20.0),Smooth((support-16-y)/16.0)));
        }
        static double Weight(int pose,string clip,int x,int y)
        {
            if(clip.StartsWith("look_"))return Head(pose,x,y);
            if(clip.EndsWith("_eyes"))return Eyes(pose,x,y);
            if(clip.EndsWith("_tail"))return Tail(pose,x,y);
            return 0;
        }
        static Rectangle WorkArea(int pose,string clip)
        {
            if(clip.StartsWith("look_"))
            {
                switch(pose){case 1:return Rectangle.FromLTRB(345,480,711,752);case 2:return Rectangle.FromLTRB(405,515,841,878);case 3:return Rectangle.FromLTRB(440,535,846,862);default:return Rectangle.Empty;}
            }
            if(clip.EndsWith("_eyes")){Rectangle[] eyes=EyeRegions(pose);return Rectangle.Union(eyes[0],eyes[1]);}
            if(clip.EndsWith("_tail"))
            {
                switch(pose){case 1:return Rectangle.FromLTRB(0,780,390,929);case 3:return Rectangle.FromLTRB(0,745,385,912);case 4:return Rectangle.FromLTRB(0,790,380,921);default:return Rectangle.Empty;}
            }
            return Rectangle.Empty;
        }
        static byte[] Pixels(Bitmap image)
        {
            BitmapData data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            try{if(data.Stride!=image.Width*4)throw new Exception("Unexpected original sprite stride.");byte[] pixels=new byte[data.Stride*image.Height];Marshal.Copy(data.Scan0,pixels,0,pixels.Length);return pixels;}
            finally{image.UnlockBits(data);}
        }
        static void Write(Bitmap image,byte[] pixels)
        {
            BitmapData data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try{Marshal.Copy(pixels,0,data.Scan0,pixels.Length);}finally{image.UnlockBits(data);}
        }
        public static Bitmap Compose(Bitmap basis,Bitmap draft,int pose,string clip,int frame)
        {
            if(basis.Size!=new Size(1024,1024)||draft.Size!=basis.Size)throw new ArgumentException("Original rest registration requires native1024 canvases.");
            if(!clip.StartsWith("look_")&&!clip.EndsWith("_eyes")&&!clip.EndsWith("_tail"))return (Bitmap)basis.Clone();
            byte[] a=Pixels(basis),b=Pixels(draft),result=(byte[])a.Clone();
            bool eyes=clip.EndsWith("_eyes"),gaze=clip.StartsWith("look_");
            Rectangle area=WorkArea(pose,clip);
            Point sourceOffset=SourceOffset(pose,clip,frame);
            for(int y=area.Top;y<area.Bottom;y++)for(int x=area.Left;x<area.Right;x++)
            {
                double w=Weight(pose,clip,x,y);if(w<=0)continue;
                int i=(y*1024+x)*4,j=((y+sourceOffset.Y)*1024+x+sourceOffset.X)*4;
                if(eyes)
                {
                    for(int c=0;c<3;c++)result[i+c]=(byte)Math.Round(a[i+c]+(b[j+c]-a[i+c])*w);
                    continue;
                }
                // Original directional fullframes move the ruff silhouette as
                // well. Never punch a transparent notch into the neutral neck:
                // below the jaw use only source color on the fixed neutral alpha.
                if(gaze&&y>=FixedSilhouetteTop(pose)-65)
                {
                    // Fade back to the fixed silhouette over a broad neck seam;
                    // switching alpha on one horizontal row makes a visible step.
                    double silhouetteWeight=w*Smooth((FixedSilhouetteTop(pose)-y)/65.0);
                    result[i+3]=(byte)Math.Round(a[i+3]*(1-silhouetteWeight)+b[j+3]*silhouetteWeight);
                    double aa=a[i+3]*(1-w*b[j+3]/255.0),bb=b[j+3]*w,total=aa+bb;
                    for(int c=0;c<3;c++)result[i+c]=total<=0?a[i+c]:(byte)Math.Round((a[i+c]*aa+b[j+c]*bb)/total);
                    continue;
                }
                if(w>=1){for(int c=0;c<4;c++)result[i+c]=b[j+c];continue;}
                double alpha=a[i+3]*(1-w)+b[j+3]*w;result[i+3]=(byte)Math.Round(alpha);
                for(int c=0;c<3;c++)result[i+c]=alpha<=0?(byte)0:(byte)Math.Round((a[i+c]*a[i+3]*(1-w)+b[j+c]*b[j+3]*w)/alpha);
            }
            Bitmap rendered=(Bitmap)basis.Clone();Write(rendered,result);return rendered;
        }
        public static int Verify(Bitmap basis,Bitmap rendered,int pose,string clip,int frame)
        {
            if(basis.Size!=rendered.Size)throw new Exception("Original rest canvas changed.");
            byte[] a=Pixels(basis),b=Pixels(rendered);int changed=0;bool eyes=clip.EndsWith("_eyes"),gaze=clip.StartsWith("look_");
            for(int y=0;y<basis.Height;y++)for(int x=0;x<basis.Width;x++)
            {
                int i=(y*basis.Width+x)*4;
                if(eyes&&a[i+3]!=b[i+3])throw new Exception("Original eyelid changed silhouette alpha.");
                if(gaze&&y>=FixedSilhouetteTop(pose)&&a[i+3]!=b[i+3])throw new Exception("Gaze changed the neutral lower-neck/body silhouette.");
                if(a[i]==b[i]&&a[i+1]==b[i+1]&&a[i+2]==b[i+2]&&a[i+3]==b[i+3])continue;
                changed++;
                if(Weight(pose,clip,x,y)<=0)throw new Exception("Original rest motion escaped its anatomy mask.");
                if(clip.StartsWith("look_")&&y>=ProtectedBodyTop(pose))throw new Exception("Gaze moved the fixed chest/body/paw/floor band.");
                if(y>=928&&clip.StartsWith("look_"))throw new Exception("Gaze changed floor pixels.");
            }
            return changed;
        }
    }
}
