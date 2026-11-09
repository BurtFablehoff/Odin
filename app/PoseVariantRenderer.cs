// Part 5c (rule A9, Hatt 2026-11-08 "okay lets push this one to release"): compiled in production too; the production exe draws the promoted pack.
// Odin's anatomy and layer boundaries belong to this pet, not the shared framework.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
namespace OdinV2
{
    internal static class PoseVariantRenderer
    {
        static double Clamp(double x){return Math.Max(0,Math.Min(1,x));}
        // Sitting's original PNG is1024square; its newer head/tail drafts use1254.
        // Keep the original base native and map only the draft and anatomy mask.
        static double AnatomyScale(int pose,Size size){return pose==1?1254.0/size.Width:1;}
        static double AnatomyCoordinate(int value,double scale){return (value+.5)*scale-.5;}
        public static Bitmap FitDraft(Bitmap basis,Bitmap draft,int pose)
        {
            if(basis.Size==draft.Size)return (Bitmap)draft.Clone();
            if(pose!=1||basis.Width!=basis.Height||draft.Width!=draft.Height)
                throw new ArgumentException("Variant canvas does not match its base.");
            Bitmap fitted=new Bitmap(basis.Width,basis.Height,PixelFormat.Format32bppArgb);
            using(Graphics graphics=Graphics.FromImage(fitted))
            using(ImageAttributes attributes=new ImageAttributes())
            {
                graphics.CompositingMode=CompositingMode.SourceCopy;
                graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
                graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
                attributes.SetWrapMode(WrapMode.TileFlipXY);
                graphics.DrawImage(draft,new Rectangle(Point.Empty,basis.Size),0,0,draft.Width,draft.Height,GraphicsUnit.Pixel,attributes);
            }
            return fitted;
        }
        static Rectangle Area(int pose,string kind,Size size)
        {
            Rectangle area;
            if(kind=="gaze")
            {
                switch(pose)
                {
                    case 1:area=new Rectangle(460,590,390,330);break;
                    case 2:area=new Rectangle(585,690,435,390);break;
                    case 3:area=new Rectangle(585,685,440,420);break;
                    default:throw new ArgumentOutOfRangeException("pose");
                }
            }
            else if(kind=="tail")
            {
                int top;double right;
                switch(pose)
                {
                    case 1:top=920;right=460+Math.Max(0,1254-1-980)*.60;break;
                    case 3:top=865;right=500;break;
                    case 4:top=890;right=350+Math.Max(0,size.Height-1-940)*1.48;break;
                    default:throw new ArgumentOutOfRangeException("pose");
                }
                area=Rectangle.FromLTRB(0,top,(int)Math.Ceiling(right),pose==1?1254:size.Height);
            }
            else throw new ArgumentException("Unknown motion kind.");
            double scale=AnatomyScale(pose,size);
            if(scale!=1)area=Rectangle.FromLTRB((int)Math.Floor(area.Left/scale),(int)Math.Floor(area.Top/scale),
                (int)Math.Ceiling(area.Right/scale),(int)Math.Ceiling(area.Bottom/scale));
            return Rectangle.Intersect(area,new Rectangle(Point.Empty,size));
        }
        static double Coverage(double x,double y,int pose,string kind)
        {
            if(kind=="gaze")
            {
                Rectangle r;
                switch(pose)
                {
                    case 1:r=new Rectangle(460,590,390,330);break;
                    case 2:r=new Rectangle(585,690,435,390);break;
                    case 3:r=new Rectangle(585,685,440,420);break;
                    default:throw new ArgumentOutOfRangeException("pose");
                }
                // Blend only the neck seam; the ears and full head retain their original alpha.
                return Math.Min(Clamp((x-r.Left)/20.0),Math.Min(Clamp((r.Right-x)/20.0),Math.Min(Clamp((y-r.Top)/20.0),Clamp((r.Bottom-y)/55.0))));
            }
            if(kind!="tail")throw new ArgumentException("Unknown motion kind.");
            double boundary,top;
            switch(pose)
            {
                case 1:boundary=460+Math.Max(0,y-980)*.60;top=Clamp((y-920)/60.0);break;
                case 3:boundary=500;top=Clamp((y-865)/65.0);break;
                case 4:boundary=350+Math.Max(0,y-940)*1.48;top=Clamp((y-890)/55.0);break;
                default:throw new ArgumentOutOfRangeException("pose");
            }
            double coverage=Math.Min(top,Clamp((boundary-x)/25.0));
            // Sleep's newly filled belly must stay on its fixed support plane.
            // Old tail drafts include a gap here; fade their layer out before the belly contact.
            if(pose==4)coverage*=1-Clamp((x-575)/20.0)*Clamp((y-1110)/15.0);
            return coverage;
        }
        static byte[] Pixels(Bitmap image)
        {
            BitmapData data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            try
            {
                if(data.Stride!=image.Width*4)throw new Exception("Unexpected pixel stride.");
                byte[] result=new byte[data.Stride*image.Height];Marshal.Copy(data.Scan0,result,0,result.Length);return result;
            }
            finally{image.UnlockBits(data);}
        }
        static void Write(Bitmap image,byte[] pixels)
        {
            BitmapData data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try{Marshal.Copy(pixels,0,data.Scan0,pixels.Length);}
            finally{image.UnlockBits(data);}
        }
        public static Bitmap Compose(Bitmap basis,Bitmap draft,int pose,string kind,double amount)
        {
            if(amount<0||amount>1)throw new ArgumentOutOfRangeException("amount");
            using(Bitmap fitted=FitDraft(basis,draft,pose))
            {
            byte[] a=Pixels(basis),b=Pixels(fitted),result=(byte[])a.Clone();
            double scale=AnatomyScale(pose,basis.Size);
            Rectangle area=Area(pose,kind,basis.Size);
            for(int y=area.Top;y<area.Bottom;y++)for(int x=area.Left;x<area.Right;x++)
            {
                double weight=Coverage(AnatomyCoordinate(x,scale),AnatomyCoordinate(y,scale),pose,kind)*amount;if(weight<=0)continue;
                int offset=(y*basis.Width+x)*4;
                double alpha=a[offset+3]*(1-weight)+b[offset+3]*weight;
                result[offset+3]=(byte)Math.Round(alpha);
                // Alpha-weighted blending avoids dark halos where the tail/head silhouette moves.
                for(int c=0;c<3;c++)result[offset+c]=alpha<=0?(byte)0:(byte)Math.Round((a[offset+c]*a[offset+3]*(1-weight)+b[offset+c]*b[offset+3]*weight)/alpha);
            }
            Bitmap rendered=(Bitmap)basis.Clone();Write(rendered,result);return rendered;
            }
        }
        public static int Verify(Bitmap basis,Bitmap rendered,int pose,string kind)
        {
            if(basis.Size!=rendered.Size)throw new ArgumentException("Registered variant changed its base canvas.");
            byte[] a=Pixels(basis),b=Pixels(rendered);int changed=0;
            double scale=AnatomyScale(pose,basis.Size);
            for(int y=0;y<basis.Height;y++)for(int x=0;x<basis.Width;x++)
            {
                int offset=(y*basis.Width+x)*4;
                if(a[offset]==b[offset]&&a[offset+1]==b[offset+1]&&a[offset+2]==b[offset+2]&&a[offset+3]==b[offset+3])continue;
                changed++;
                double nativeX=AnatomyCoordinate(x,scale),nativeY=AnatomyCoordinate(y,scale);
                if(Coverage(nativeX,nativeY,pose,kind)<=0)throw new Exception("Movement escaped its head/tail layer.");
                // Distal front paws are always below these bounds and outside a tail layer.
                if(nativeX>=700&&nativeY>=1110)throw new Exception("Movement touched a front paw.");
                if(kind=="tail"&&nativeX>=710&&nativeY<1090)throw new Exception("Tail movement touched the face or torso.");
            }
            if(changed<50)throw new Exception("Variant did not change its intended layer.");
            return changed;
        }
        public static Rectangle VisibleBounds(Bitmap image)
        {
            byte[] p=Pixels(image);int left=image.Width,top=image.Height,right=-1,bottom=-1;
            for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(p[(y*image.Width+x)*4+3]>8)
            {left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);}
            return right<0?Rectangle.Empty:Rectangle.FromLTRB(left,top,right+1,bottom+1);
        }
    }
}
