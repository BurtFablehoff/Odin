using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.IO;

// Authoring only. Drawn tail keys; no animation warp, stretch or crossfade.
internal static class TailDrawnFrames
{
    static Bitmap Read(string path){using(Bitmap b=new Bitmap(path))return b.Clone(new Rectangle(Point.Empty,b.Size),PixelFormat.Format32bppArgb);}
    static int Bottom(Bitmap b,int left,int right){int bottom=0;for(int x=left;x<right;x++)for(int y=0;y<b.Height;y++)if(b.GetPixel(x,y).A>128)bottom=Math.Max(bottom,y);return bottom;}
    static Color Blend(Color a,Color b,double t){double alpha=a.A*(1-t)+b.A*t;if(alpha<.5)return Color.Transparent;return Color.FromArgb((int)Math.Round(alpha),(int)Math.Round((a.R*a.A*(1-t)+b.R*b.A*t)/alpha),(int)Math.Round((a.G*a.A*(1-t)+b.G*b.A*t)/alpha),(int)Math.Round((a.B*a.A*(1-t)+b.B*b.A*t)/alpha));}
    static int Main(string[] args)
    {
        string root=args[0],family=args[1],name=args[2],folder=Path.Combine(root,"draft-assets/tail-v5");Directory.CreateDirectory(Path.Combine(folder,"frames"));
        using(Bitmap original=Read(Path.Combine(root,"art/frames/"+family+"/000.png")))using(Bitmap raw=Read(Path.Combine(folder,"raw/"+family+"-"+name+".png")))using(Bitmap registered=new Bitmap(1024,1024,PixelFormat.Format32bppArgb))
        {
            // Canvas units stay fixed; the grounded front paws provide the vertical anchor.
            double scale=1024.0/raw.Width;int l=family=="sit"?535:family=="lay"?630:590,r=family=="sit"?610:730;
            double dy=Bottom(original,l,r)-Bottom(raw,(int)(l/scale),(int)(r/scale))*scale;
            using(Graphics g=Graphics.FromImage(registered)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(raw,new RectangleF(0,(float)dy,1024,(float)(raw.Height*scale)));}
            using(Bitmap result=(Bitmap)original.Clone())
            {
                for(int y=650;y<980;y++)for(int x=160;x<(family=="sleep"?520:440);x++)
                {
                    // Only the free tail is admitted. Haunch/forelegs stay original.
                    double edge=family=="sleep"?520:family=="lay"?395:y<840?390:y<900?400:420;
                    double t=Math.Max(0,Math.Min(1,(edge-x)/18.0));
                    if(family=="sleep")t*=Math.Max(0,Math.Min(1,(y-800)/25.0))*Math.Max(0,Math.Min(1,(x-320)/18.0));
                    if(family=="lay"&&x>=345&&y>=875)t=0; // resting rear paw
                    result.SetPixel(x,y,Blend(original.GetPixel(x,y),registered.GetPixel(x,y),t));
                }
                result.Save(Path.Combine(folder,"frames/"+family+"-"+name+".png"),ImageFormat.Png);
                using(Bitmap preview=new Bitmap(560,380))using(Graphics g=Graphics.FromImage(preview))
                {
                    g.Clear(Color.FromArgb(27,29,34));g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(result,new Rectangle(20,-130,480,480));preview.Save(Path.Combine(folder,"frames/"+family+"-"+name+"-preview.png"),ImageFormat.Png);
                }
            }
        }
        return 0;
    }
}
