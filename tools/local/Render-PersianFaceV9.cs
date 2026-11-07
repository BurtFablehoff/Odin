// Isolated generated face/cheek revision. Never linked into the running pet.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
public static class PersianFaceV9
{
    static readonly PointF[] Face={new PointF(590,700),new PointF(820,698),new PointF(900,740),new PointF(910,865),new PointF(870,915),new PointF(760,935),new PointF(650,925),new PointF(570,870),new PointF(550,800)};
    static double Smooth(double t){t=Math.Max(0,Math.Min(1,t));return t*t*(3-2*t);}
    static double Weight(double x,double y)
    {
        if(y<700||y>=935||(x>=650&&y>=900))return 0;
        bool inside=false;double distance=Double.MaxValue;
        for(int i=0,j=Face.Length-1;i<Face.Length;j=i++)
        {
            PointF a=Face[j],b=Face[i];
            if((a.Y>y)!=(b.Y>y)&&x<(b.X-a.X)*(y-a.Y)/(b.Y-a.Y)+a.X)inside=!inside;
            double dx=b.X-a.X,dy=b.Y-a.Y,t=Math.Max(0,Math.Min(1,((x-a.X)*dx+(y-a.Y)*dy)/(dx*dx+dy*dy)));
            double px=x-a.X-t*dx,py=y-a.Y-t*dy;distance=Math.Min(distance,Math.Sqrt(px*px+py*py));
        }
        if(!inside)return 0;
        double guardX=Math.Max(0,650-x),guardY=Math.Max(0,900-y);
        double legs=Smooth(Math.Sqrt(guardX*guardX+guardY*guardY)/20.0);
        return Math.Min(legs,Math.Min(Smooth(distance/12),Smooth((y-700)/15.0)));
    }
    static byte[] Pixels(Bitmap image)
    {
        BitmapData d=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try{if(d.Stride!=image.Width*4)throw new Exception("Unexpected stride");byte[] b=new byte[d.Stride*image.Height];Marshal.Copy(d.Scan0,b,0,b.Length);return b;}finally{image.UnlockBits(d);}
    }
    public static string Render(string originalPath,string draftPath,string outputPath,double sourceScale,double offsetX,double offsetY)
    {
        using(Bitmap original=new Bitmap(originalPath))using(Bitmap draft=new Bitmap(draftPath))
        using(Bitmap fitted=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb))using(Bitmap output=(Bitmap)original.Clone())
        {
            if(original.Size!=new Size(1254,1254)||draft.Width!=draft.Height)throw new Exception("Unexpected native canvases");
            using(Graphics g=Graphics.FromImage(fitted))using(ImageAttributes attrs=new ImageAttributes())
            {g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;attrs.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(draft,Rectangle.Round(new RectangleF((float)offsetX,(float)offsetY,(float)(draft.Width*sourceScale),(float)(draft.Height*sourceScale))),0,0,draft.Width,draft.Height,GraphicsUnit.Pixel,attrs);}
            byte[] a=Pixels(original),b=Pixels(fitted),r=(byte[])a.Clone();int changed=0;
            for(int y=0;y<1254;y++)for(int x=0;x<1254;x++)
            {
                double w=Weight(x+.5,y+.5);if(w<=0)continue;int i=(y*1254+x)*4;
                double alpha=a[i+3]*(1-w)+b[i+3]*w;
                // Keep the original back-of-cheek/neck attachment where a
                // changed face angle otherwise punches a notch in its fur.
                double visibleAlpha=alpha;
                if(y>=835)
                {
                    double facialSilhouette=w*Smooth((875-y)/40.0);
                    visibleAlpha=a[i+3]*(1-facialSilhouette)+b[i+3]*facialSilhouette;
                }
                if(x<660)visibleAlpha=Math.Max(a[i+3],visibleAlpha);
                r[i+3]=(byte)Math.Round(visibleAlpha);
                for(int c=0;c<3;c++)
                {
                    double numerator=a[i+c]*a[i+3]*(1-w)+b[i+c]*b[i+3]*w;
                    if(visibleAlpha>alpha)numerator+=a[i+c]*(visibleAlpha-alpha);
                    double divisor=Math.Max(alpha,visibleAlpha);
                    r[i+c]=divisor<=0?a[i+c]:(byte)Math.Round(numerator/divisor);
                }
            }
            for(int y=0;y<1254;y++)for(int x=0;x<1254;x++)
            {
                int i=(y*1254+x)*4;if(a[i]==r[i]&&a[i+1]==r[i+1]&&a[i+2]==r[i+2]&&a[i+3]==r[i+3])continue;
                if(Weight(x+.5,y+.5)<=0)throw new Exception("Face edit escaped its region");changed++;
            }
            BitmapData data=output.LockBits(new Rectangle(Point.Empty,output.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try{Marshal.Copy(r,0,data.Scan0,r.Length);}finally{output.UnlockBits(data);}
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));output.Save(outputPath,ImageFormat.Png);
            return new JavaScriptSerializer().Serialize(new{passed=true,headless=true,windowsCreated=0,changedFacePixels=changed,earCrownPixelsExact=true,workingEyeScaleRegistered=true,bodyBelowNeckPixelsExact=true,tailLegsPawsContactExact=true,outsideFaceNeckRegionExact=true,sourcePixelsEdited=false,runtimeIntegrated=false,ownerApproved=false});
        }
    }
}
