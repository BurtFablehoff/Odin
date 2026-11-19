// A14: author complete candidate keys. Approved source files are read only.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Web.Script.Serialization;

internal static class TextureRecoveryFrames
{
    static readonly Rectangle Paw=new Rectangle(440,700,112,254);
    static Bitmap Read(string path){using(Bitmap b=new Bitmap(path))return b.Clone(new Rectangle(Point.Empty,b.Size),PixelFormat.Format32bppArgb);}
    static double[] Sole(Bitmap b,int left,int right)
    {
        int bottom=0;for(int y=0;y<b.Height;y++)for(int x=left;x<right;x++)if(b.GetPixel(x,y).A>128)bottom=Math.Max(bottom,y);
        double sum=0,n=0;for(int y=bottom-5;y<=bottom;y++)for(int x=left;x<right;x++)if(b.GetPixel(x,y).A>128){sum+=x;n++;}
        if(n==0)throw new Exception("Planted paw landmark missing.");return new[]{sum/n,(double)bottom};
    }
    static Bitmap Register(Bitmap source,Bitmap target,bool legacyGroom)
    {
        double s=1024.0/source.Width;double[] a=Sole(target,550,610),b=legacyGroom?Sole(source,470,540):Sole(source,(int)(550/s),(int)(610/s));
        Bitmap registered=new Bitmap(1024,1024,PixelFormat.Format32bppArgb);
        using(Graphics g=Graphics.FromImage(registered)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(source,new RectangleF((float)(a[0]-b[0]*s),(float)(a[1]-b[1]*s),(float)(source.Width*s),(float)(source.Height*s)));}
        return registered;
    }
    static Color Blend(Color a,Color b,double w)
    {
        double alpha=a.A*(1-w)+b.A*w;if(alpha<=0)return Color.Transparent;
        return Color.FromArgb((int)Math.Round(alpha),(int)Math.Round((a.R*a.A*(1-w)+b.R*b.A*w)/alpha),(int)Math.Round((a.G*a.A*(1-w)+b.G*b.A*w)/alpha),(int)Math.Round((a.B*a.A*(1-w)+b.B*b.A*w)/alpha));
    }
    static Bitmap PawKey(Bitmap original,Bitmap raw,double progress,bool legacyGroom,out int protectedPixels)
    {
        using(Bitmap registered=Register(raw,original,legacyGroom))using(Bitmap paw=new Bitmap(1024,1024,PixelFormat.Format32bppArgb))
        {
            for(int y=0;y<1024;y++)for(int x=0;x<1024;x++)
            {
                Color c=original.GetPixel(x,y);
                if(Paw.Contains(x,y)){
                    double d=Math.Min(Math.Min(x-Paw.Left,Paw.Right-1-x),Math.Min(y-Paw.Top,Paw.Bottom-1-y));
                    c=Blend(c,registered.GetPixel(x,y),Math.Min(1,d/8));
                }
                paw.SetPixel(x,y,c);
            }
            // Reposition ORIGINAL head/neck pixels into the established grooming entry.
            // Inverse nearest-pixel sampling avoids face crossfades and preserves fine source strands.
            Bitmap result=new Bitmap(1024,1024,PixelFormat.Format32bppArgb);protectedPixels=0;
            int headDx=(int)Math.Round(24*progress),headDy=(int)Math.Round(17*progress);
            for(int y=0;y<1024;y++)for(int x=0;x<1024;x++)
            {
                double t=Math.Max(0,Math.Min(1,(y-680)/110.0));double weight=1-t*t*(3-2*t);
                int sx=(int)Math.Round(x+headDx*weight),sy=(int)Math.Round(y-headDy*weight);
                Color c=sx>=0&&sx<1024&&sy>=0&&sy<1024?paw.GetPixel(sx,sy):Color.Transparent;
                result.SetPixel(x,y,c);
                if(y>=790&&!Paw.Contains(x,y)){
                    protectedPixels++;if(c.ToArgb()!=original.GetPixel(x,y).ToArgb())throw new Exception("Original grounded body/tail/other leg changed.");
                }
            }
            for(int y=515;y<660;y++)for(int x=390;x<650;x++)if(original.GetPixel(x,y).A>128)
                if(result.GetPixel(x-headDx,y+headDy).ToArgb()!=original.GetPixel(x,y).ToArgb())throw new Exception("Head lead-in changed the original facial texture.");
            return result;
        }
    }
    static void Normalize(string input,string output,double physicalRatio)
    {
        using(Bitmap source=Read(input))using(Bitmap result=new Bitmap(1024,1024,PixelFormat.Format32bppArgb))using(Graphics g=Graphics.FromImage(result))
        {
            g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
            float s=(float)physicalRatio;g.DrawImage(source,new RectangleF(512-512*s,944-944*s,1024*s,1024*s));result.Save(output,ImageFormat.Png);
        }
    }
    static int Main(string[] args)
    {
        string root=args[0],folder=Path.Combine(root,"draft-assets/texture-recovery-v3"),frames=Path.Combine(folder,"frames");Directory.CreateDirectory(frames);
        try{
            var reports=new List<object>();
            using(Bitmap original=Read(Path.Combine(root,"art/frames/sit/000.png")))
            {
                // The generated high source over-raised the paw; retain it as an unused candidate.
                // Use the exact original grooming entry for the final paw bridge instead.
                string[] inputs={Path.Combine(folder,"raw/paw-low.png"),Path.Combine(root,"draft-assets/paw-texture-pilot-v1/raw-edit.png"),Path.Combine(root,"draft-assets/style-consistency-v7/groom/frames/000.png")};
                for(int i=0;i<3;i++)using(Bitmap raw=Read(inputs[i])){
                    int protectedPixels;using(Bitmap key=PawKey(original,raw,(i+1)/4.0,i==2,out protectedPixels))key.Save(Path.Combine(frames,"sit_groom-"+i+".png"),ImageFormat.Png);
                    reports.Add(new{frame=i,originalHeadMaterial=true,originalFacePixelsTranslatedExactly=true,protectedLowerRegionPixels=protectedPixels,changedProtectedPixels=0,headLeadInNativePixels=new[]{-(int)Math.Round(24*(i+1)/4.0),(int)Math.Round(17*(i+1)/4.0)},noFaceCrossfade=true});
                }
            }
            string[] names={"sit-loaf-25","sit-loaf-50","sit-loaf-late","sit-lay-25","sit-lay-75","sit-lay-late","lay-sleep-50","lay-sleep-25","lay-sleep-75"};
            for(int i=0;i<names.Length;i++){
                string family=i<3?"sit_loaf":i<6?"sit_lay":"lay_sleep";
                // Late keys are edits of their endpoint, so inherit that endpoint's approved units.
                double ratio=names[i]=="sit-loaf-late"?372.4272/480:names[i]=="sit-lay-late"?444.432/480:1;
                Normalize(Path.Combine(folder,"raw/"+names[i]+".png"),Path.Combine(frames,family+"-"+(i%3)+".png"),ratio);
            }
            File.WriteAllText(Path.Combine(folder,"authoring-checks.json"),new JavaScriptSerializer().Serialize(new{passed=true,status="complete candidate keys; visual review pending",groomKeys=reports,postureKeys=9,sourceFilesReadOnly=true}));return 0;
        }catch(Exception e){File.WriteAllText(Path.Combine(folder,"authoring-error.txt"),e.ToString());return 1;}
    }
}
