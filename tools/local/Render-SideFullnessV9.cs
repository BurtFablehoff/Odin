// Local generated body edit only; no runtime integration or source modification.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
public static class SideFullnessV9
{
    static readonly PointF[] Body={new PointF(565,845),new PointF(635,860),new PointF(650,965),new PointF(660,1117),new PointF(620,1137),new PointF(440,1137),new PointF(422,1040),new PointF(447,950),new PointF(500,875)};
    static double Weight(double x,double y)
    {
        if(y>=1135||(x>=580&&y<875)||(x<420&&y>=1060)||(x>=650&&y>=900)||(x>=600&&y>=1110))return 0;
        // Feather before every protected region as well as the outer polygon;
        // a hard rectangular exclusion makes visible steps at the tail/paw seam.
        double guard=Math.Min(1,(1135-y)/20.0);
        double guardX=Math.Max(0,580-x),guardY=Math.Max(0,y-875);guard=Math.Min(guard,Math.Min(1,Math.Sqrt(guardX*guardX+guardY*guardY)/20.0));
        guardX=Math.Max(0,x-420);guardY=Math.Max(0,1060-y);guard=Math.Min(guard,Math.Min(1,Math.Sqrt(guardX*guardX+guardY*guardY)/20.0));
        guardX=Math.Max(0,650-x);guardY=Math.Max(0,900-y);guard=Math.Min(guard,Math.Min(1,Math.Sqrt(guardX*guardX+guardY*guardY)/20.0));
        guardX=Math.Max(0,600-x);guardY=Math.Max(0,1110-y);guard=Math.Min(guard,Math.Min(1,Math.Sqrt(guardX*guardX+guardY*guardY)/20.0));
        bool inside=false;double distance=Double.MaxValue;
        for(int i=0,j=Body.Length-1;i<Body.Length;j=i++)
        {
            PointF a=Body[j],b=Body[i];
            if((a.Y>y)!=(b.Y>y)&&x<(b.X-a.X)*(y-a.Y)/(b.Y-a.Y)+a.X)inside=!inside;
            double dx=b.X-a.X,dy=b.Y-a.Y,t=Math.Max(0,Math.Min(1,((x-a.X)*dx+(y-a.Y)*dy)/(dx*dx+dy*dy)));
            double ex=x-a.X-t*dx,ey=y-a.Y-t*dy;distance=Math.Min(distance,Math.Sqrt(ex*ex+ey*ey));
        }
        if(!inside)return 0;
        double edge=Math.Min(1,distance/12),foot=Math.Min(1,(1135-y)/20.0);
        return Math.Min(guard,Math.Min(edge,foot));
    }
    static byte[] Pixels(Bitmap image)
    {
        BitmapData d=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try{if(d.Stride!=image.Width*4)throw new Exception("Unexpected stride");byte[] b=new byte[d.Stride*image.Height];Marshal.Copy(d.Scan0,b,0,b.Length);return b;}finally{image.UnlockBits(d);}
    }
    public static string Render(string originalPath,string draftPath,string outputPath,double sourceScale,double offsetX,double offsetY)
    {
        using(Bitmap original=new Bitmap(originalPath))using(Bitmap draft=new Bitmap(draftPath))
        using(Bitmap fitted=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb))
        using(Bitmap output=(Bitmap)original.Clone())
        {
            if(original.Size!=new Size(1254,1254)||draft.Width!=draft.Height)throw new Exception("Unexpected native canvases");
            using(Graphics g=Graphics.FromImage(fitted))using(ImageAttributes attrs=new ImageAttributes())
            {g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;attrs.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(draft,Rectangle.Round(new RectangleF((float)offsetX,(float)offsetY,(float)(draft.Width*sourceScale),(float)(draft.Height*sourceScale))),0,0,draft.Width,draft.Height,GraphicsUnit.Pixel,attrs);}
            byte[] a=Pixels(original),b=Pixels(fitted),r=(byte[])a.Clone();int changed=0,added=0;
            for(int y=0;y<1254;y++)for(int x=0;x<1254;x++)
            {
                double w=Weight(x+.5,y+.5);if(w<=0)continue;int i=(y*1254+x)*4;
                double alpha=a[i+3]*(1-w)+b[i+3]*w;r[i+3]=(byte)Math.Round(alpha);
                for(int c=0;c<3;c++)r[i+c]=alpha<=0?(byte)0:(byte)Math.Round((a[i+c]*a[i+3]*(1-w)+b[i+c]*b[i+3]*w)/alpha);
            }
            for(int y=0;y<1254;y++)for(int x=0;x<1254;x++)
            {
                int i=(y*1254+x)*4;bool same=a[i]==r[i]&&a[i+1]==r[i+1]&&a[i+2]==r[i+2]&&a[i+3]==r[i+3];
                if(same)continue;
                if(Weight(x+.5,y+.5)<=0)throw new Exception("Body edit escaped protected mask");
                changed++;if(a[i+3]<=64&&r[i+3]>64)added++;
            }
            BitmapData data=output.LockBits(new Rectangle(Point.Empty,output.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try{Marshal.Copy(r,0,data.Scan0,r.Length);}finally{output.UnlockBits(data);}
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));output.Save(outputPath,ImageFormat.Png);
            return new JavaScriptSerializer().Serialize(new{passed=true,headless=true,windowsCreated=0,changedBodyPixels=changed,newDenseBodyPixels=added,headFacePixelsExact=true,outerTailPixelsExact=true,forelegsPawsExact=true,contactBandPixelsExact=true,outsideBodyMaskExact=true,sourcePixelsEdited=false,runtimeIntegrated=false,ownerApproved=false});
        }
    }
}
