// Original jump-profile muzzle registered onto the existing Persian-side draft.
// This reuses original artwork; no source PNG or silhouette is modified.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
public static class SideFaceV10
{
    static byte[] Read(Bitmap image){BitmapData d=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{byte[] b=new byte[d.Stride*image.Height];Marshal.Copy(d.Scan0,b,0,b.Length);return b;}finally{image.UnlockBits(d);}}
    public static string Render(string basisPath,string profilePath,string outputPath)
    {
        using(Bitmap basis=new Bitmap(basisPath))using(Bitmap profile=new Bitmap(profilePath))
        using(Bitmap fitted=new Bitmap(basis.Width,basis.Height,PixelFormat.Format32bppArgb))using(Bitmap result=(Bitmap)basis.Clone())
        {
            // Same anatomical features in the original jump013 and side draft:
            // working-eye center and nose center. One rigid similarity transform.
            double sx=522,sy=306,snx=554,sny=323,tx=754,ty=748,tnx=789,tny=762;
            double vx=snx-sx,vy=sny-sy,ux=tnx-tx,uy=tny-ty,den=vx*vx+vy*vy;
            double a=(ux*vx+uy*vy)/den,b=(uy*vx-ux*vy)/den,dx=tx-a*sx+b*sy,dy=ty-b*sx-a*sy;
            using(Graphics g=Graphics.FromImage(fitted))using(Matrix transform=new Matrix((float)a,(float)b,(float)-b,(float)a,(float)dx,(float)dy))
            using(ImageAttributes attrs=new ImageAttributes())
            {g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.Transform=transform;attrs.SetWrapMode(WrapMode.TileFlipXY);g.DrawImage(profile,new Rectangle(0,0,profile.Width,profile.Height),0,0,profile.Width,profile.Height,GraphicsUnit.Pixel,attrs);}
            byte[] original=Read(basis),source=Read(fitted),pixels=(byte[])original.Clone();int changed=0;
            for(int y=754;y<815;y++)for(int x=732;x<840;x++)
            {
                double ex=(x-786)/52.0,ey=(y-784)/30.0,r=Math.Sqrt(ex*ex+ey*ey);
                double w=Math.Max(0,Math.Min(1,(1-r)/.24))*Math.Max(0,Math.Min(1,(y-754)/8.0));
                int n=(y*basis.Width+x)*4;w*=source[n+3]/255.0;if(w<=0)continue;
                for(int c=0;c<3;c++)pixels[n+c]=(byte)Math.Round(original[n+c]*(1-w)+source[n+c]*w);
            }
            for(int n=0;n<pixels.Length;n+=4)
            {if(pixels[n+3]!=original[n+3])throw new Exception("Muzzle refinement changed silhouette");if(pixels[n]!=original[n]||pixels[n+1]!=original[n+1]||pixels[n+2]!=original[n+2])changed++;}
            BitmapData d=result.LockBits(new Rectangle(Point.Empty,result.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try{Marshal.Copy(pixels,0,d.Scan0,pixels.Length);}finally{result.UnlockBits(d);}
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));result.Save(outputPath,ImageFormat.Png);
            return new JavaScriptSerializer().Serialize(new{passed=true,headless=true,windowsCreated=0,changedCorePixels=changed,allAlphaExact=true,outsideMuzzleExact=true,bodyTailPawsFloorExact=true,sourceOriginalProfile="art/frames/jump_right/013.png",sourceLandmarks=new[]{new[]{522,306},new[]{554,323}},targetLandmarks=new[]{new[]{754,748},new[]{789,762}},uniformSourceScale=Math.Sqrt(a*a+b*b),rotationRadians=Math.Atan2(b,a),sourcePixelsEdited=false,ownerApproved=false});
        }
    }
}
