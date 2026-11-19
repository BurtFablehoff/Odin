// A13: one bounded paw edit. This is an isolated review, never an application artwork pack.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace OdinPawTexturePilot
{
    internal static class Pilot
    {
        static readonly Rectangle Region=new Rectangle(440,740,112,214);
        static Bitmap Read(string name){using(Stream s=Assembly.GetExecutingAssembly().GetManifestResourceStream(name))using(Bitmap image=new Bitmap(s))return image.Clone(new Rectangle(Point.Empty,image.Size),PixelFormat.Format32bppArgb);}
        static double[] Sole(Bitmap image,int left,int right)
        {
            int bottom=0;for(int y=0;y<image.Height;y++)for(int x=left;x<right;x++)if(image.GetPixel(x,y).A>128)bottom=Math.Max(bottom,y);
            double sum=0,count=0;for(int y=bottom-5;y<=bottom;y++)for(int x=left;x<right;x++)if(image.GetPixel(x,y).A>128){sum+=x;count++;}
            if(count==0)throw new Exception("No planted paw reference.");return new[]{sum/count,(double)bottom};
        }
        static Bitmap Compose(Bitmap original,Bitmap source,out object report)
        {
            double scale=original.Width/(double)source.Width;
            double[] target=Sole(original,550,610),input=Sole(source,(int)(550/scale),(int)(610/scale));
            float dx=(float)(target[0]-input[0]*scale),dy=(float)(target[1]-input[1]*scale);
            using(Bitmap registered=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb))
            {
                using(Graphics g=Graphics.FromImage(registered)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(source,new RectangleF(dx,dy,(float)(source.Width*scale),(float)(source.Height*scale)));}
                Bitmap result=new Bitmap(original.Width,original.Height,PixelFormat.Format32bppArgb);int protectedPixels=0,changed=0,leaks=0,alphaLeaks=0;
                for(int y=0;y<result.Height;y++)for(int x=0;x<result.Width;x++)
                {
                    Color a=original.GetPixel(x,y);
                    result.SetPixel(x,y,a);
                    if(Region.Contains(x,y))
                    {
                        double distance=Math.Min(Math.Min(x-Region.Left,Region.Right-1-x),Math.Min(y-Region.Top,Region.Bottom-1-y));
                        double w=Math.Min(1,distance/8);Color b=registered.GetPixel(x,y);double alpha=a.A*(1-w)+b.A*w;
                        int r=alpha==0?0:(int)Math.Round((a.R*a.A*(1-w)+b.R*b.A*w)/alpha);
                        int green=alpha==0?0:(int)Math.Round((a.G*a.A*(1-w)+b.G*b.A*w)/alpha);
                        int blue=alpha==0?0:(int)Math.Round((a.B*a.A*(1-w)+b.B*b.A*w)/alpha);
                        result.SetPixel(x,y,Color.FromArgb((int)Math.Round(alpha),r,green,blue));
                    }
                    Color c=result.GetPixel(x,y);if(a.ToArgb()!=c.ToArgb())changed++;
                    if(!Region.Contains(x,y)){protectedPixels++;if(a.ToArgb()!=c.ToArgb())leaks++;if(a.A!=c.A)alphaLeaks++;}
                }
                if(leaks!=0||alphaLeaks!=0)throw new Exception("Paw pilot changed protected original pixels.");
                report=new{passed=true,status="one texture pilot; no artwork or motion approval",mask=new[]{Region.X,Region.Y,Region.Width,Region.Height},registeredSourceScale=scale,registeredSourceTranslation=new[]{dx,dy},protectedPixels=protectedPixels,outsideMaskChangedPixels=leaks,outsideMaskAlphaChanges=alphaLeaks,changedPixelsInsideMask=changed,outsideForelegRegionOriginalPixelsExact=true};
                return result;
            }
        }
        static Bitmap Preview(Bitmap original,Bitmap candidate,bool dark)
        {
            Bitmap sheet=new Bitmap(960,620);using(Graphics g=Graphics.FromImage(sheet))
            {
                g.Clear(dark?Color.FromArgb(28,30,35):Color.FromArgb(246,246,243));g.InterpolationMode=InterpolationMode.HighQualityBicubic;
                using(Font font=new Font("Segoe UI",12))using(Brush text=new SolidBrush(dark?Color.White:Color.Black))
                {
                    g.DrawString("Original approved sit",font,text,20,12);g.DrawString("One paw edit - candidate",font,text,500,12);
                    g.DrawImage(original,new Rectangle(0,-180,480,480));g.DrawImage(candidate,new Rectangle(480,-180,480,480));
                    g.DrawString("Foreleg detail",font,text,160,315);g.DrawString("Foreleg detail",font,text,640,315);
                    Rectangle crop=new Rectangle(420,720,160,250);
                    g.DrawImage(original,new Rectangle(160,350,144,225),crop,GraphicsUnit.Pixel);g.DrawImage(candidate,new Rectangle(640,350,144,225),crop,GraphicsUnit.Pixel);
                    g.DrawString("Same display scale. Only the foreleg region may change.",font,text,20,590);
                }
            }
            return sheet;
        }
        [STAThread] static int Main(string[] args)
        {
            try
            {
                using(Bitmap original=Read("Odin.PilotBase"))using(Bitmap source=Read("Odin.PilotSource"))
                {
                    object report;using(Bitmap candidate=Compose(original,source,out report))
                    {
                        if(args.Length==2&&args[0]=="--export")
                        {
                            Directory.CreateDirectory(args[1]);candidate.Save(Path.Combine(args[1],"candidate.png"),ImageFormat.Png);
                            using(Bitmap light=Preview(original,candidate,false))light.Save(Path.Combine(args[1],"comparison-light.png"),ImageFormat.Png);
                            using(Bitmap dark=Preview(original,candidate,true))dark.Save(Path.Combine(args[1],"comparison-dark.png"),ImageFormat.Png);
                            File.WriteAllText(Path.Combine(args[1],"checks.json"),new JavaScriptSerializer().Serialize(report));return 0;
                        }
                        Application.EnableVisualStyles();using(Form window=new Form())using(Bitmap preview=Preview(original,candidate,true))
                        {
                            window.Text="Odin - original fur and one paw edit - candidate review";window.ClientSize=preview.Size;
                            window.BackgroundImage=preview;window.BackgroundImageLayout=ImageLayout.None;Application.Run(window);
                        }
                    }
                }
                return 0;
            }
            catch(Exception e){if(args.Length==2&&args[0]=="--export")File.WriteAllText(Path.Combine(args[1],"error.txt"),e.ToString());return 1;}
        }
    }
}
