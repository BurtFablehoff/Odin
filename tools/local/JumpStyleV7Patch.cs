// Isolated headless proof renderer. Not linked into Odin; original art stays intact.
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Web.Script.Serialization;
using System.Runtime.InteropServices;
public static class JumpStyleV7Patch
{
    static readonly PointF[] Area={new PointF(468,423),new PointF(562,402),new PointF(650,445),new PointF(726,516),new PointF(748,628),new PointF(720,659),new PointF(620,630),new PointF(510,570),new PointF(445,500)};
    static bool Inside(double x,double y)
    {
        bool inside=false;
        for(int i=0,j=Area.Length-1;i<Area.Length;j=i++)
            if((Area[i].Y>y)!=(Area[j].Y>y)&&x<(Area[j].X-Area[i].X)*(y-Area[i].Y)/(Area[j].Y-Area[i].Y)+Area[i].X)inside=!inside;
        return inside;
    }
    static double Weight(double x,double y)
    {
        // Explicitly retain original distal hindlegs/paws and the whole outertail.
        if((x>=712&&y>=610)||(x>=740&&y>=400))return 0;
        if(!Inside(x,y))return 0;
        double distance=Double.MaxValue;
        for(int i=0;i<Area.Length;i++)
        {
            PointF a=Area[i],b=Area[(i+1)%Area.Length];double dx=b.X-a.X,dy=b.Y-a.Y;
            double t=Math.Max(0,Math.Min(1,((x-a.X)*dx+(y-a.Y)*dy)/(dx*dx+dy*dy)));
            double ex=x-a.X-t*dx,ey=y-a.Y-t*dy;distance=Math.Min(distance,Math.Sqrt(ex*ex+ey*ey));
        }
        return Math.Min(1,distance/10.0);
    }
    static byte[] Read(Bitmap image)
    {
        BitmapData locked=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try{byte[] bytes=new byte[image.Width*image.Height*4];if(locked.Stride!=image.Width*4)throw new Exception("Unexpected stride");Marshal.Copy(locked.Scan0,bytes,0,bytes.Length);return bytes;}finally{image.UnlockBits(locked);}
    }
    static void Write(Bitmap image,byte[] bytes)
    {
        BitmapData locked=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
        try{Marshal.Copy(bytes,0,locked.Scan0,bytes.Length);}finally{image.UnlockBits(locked);}
    }
    public static int Main(string[] args)
    {
        if(args.Length!=4)throw new ArgumentException("Expected original, draft, render PNG, report JSON");
        using(Bitmap original=new Bitmap(args[0]))using(Bitmap draft=new Bitmap(args[1]))
        using(Bitmap fitted=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb))
        using(Bitmap result=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb))
        {
            if(original.Width!=1024||original.Height!=1024||draft.Width!=draft.Height)throw new Exception("Unexpected dimensions");
            using(Graphics g=Graphics.FromImage(fitted))
            using(ImageAttributes attrs=new ImageAttributes())
            {g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;attrs.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(draft,new Rectangle(0,0,1024,1024),0,0,draft.Width,draft.Height,GraphicsUnit.Pixel,attrs);}
            byte[] a=Read(original),b=Read(fitted),r=(byte[])a.Clone();int changed=0,reducedAlpha=0,outside=0;int minx=1024,miny=1024,maxx=0,maxy=0;
            for(int y=0;y<1024;y++)for(int x=0;x<1024;x++)
            {
                double weight=Weight(x+.5,y+.5);int n=(y*1024+x)*4;
                if(weight>0)
                {
                    // Blend premultiplied RGBA so alpha can remove the old tubular
                    // underside without retaining its dark/light RGB fringe.
                    double alpha=a[n+3]*(1-weight)+b[n+3]*weight;
                    r[n+3]=(byte)Math.Round(alpha);
                    for(int c=0;c<3;c++)r[n+c]=alpha>0?(byte)Math.Round((a[n+c]*a[n+3]*(1-weight)+b[n+c]*b[n+3]*weight)/alpha):(byte)0;
                }
                bool differs=false;for(int c=0;c<4;c++)if(a[n+c]!=r[n+c])differs=true;
                if(differs)
                {changed++;if(weight<=0)outside++;minx=Math.Min(minx,x);miny=Math.Min(miny,y);maxx=Math.Max(maxx,x);maxy=Math.Max(maxy,y);if(a[n+3]>128&&r[n+3]<32)reducedAlpha++;}
            }
            if(outside!=0)throw new Exception("Pixels changed outside anatomy mask");
            Write(result,r);result.Save(args[2],ImageFormat.Png);
            File.WriteAllText(args[3],new JavaScriptSerializer().Serialize(new{passed=true,headless=true,windowsCreated=0,candidateOnly=true,runtimeIntegrated=false,changedPixels=changed,outsideAnatomyMaskChanges=outside,removedOpaqueUndersidePixels=reducedAlpha,changedBounds=new[]{minx,miny,maxx+1,maxy+1},originalHeadEyeForelegsUpperChestPawsAndOuterTailExact=true,protectedRegions=new[]{"all pixels outside lower torso/hip polygon","y<402: head/eye/upperchest","x<445: forelegs","x>=712,y>=610: distal hindlegs/paws","x>=740,y>=400: outertail"},visualAnatomyApproval=false}));
        }
        return 0;
    }
}
