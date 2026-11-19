// A21: only normalize a complete drawn sprite and measure placement; no animation synthesis.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;
using System.Web.Script.Serialization;

internal static class DrawnMotionAssets
{
    static int Main(string[] args)
    {
        string root=args[0],folder=Path.Combine(root,args.Length>1?args[1]:"draft-assets/drawn-motion-v9");Directory.CreateDirectory(Path.Combine(folder,"frames"));var report=new Dictionary<string,object>();
        foreach(string file in Directory.GetFiles(Path.Combine(folder,"raw"),"*.png"))
        using(Bitmap raw=new Bitmap(file))using(Bitmap image=new Bitmap(1024,1024,PixelFormat.Format32bppArgb))
        {
            using(Graphics g=Graphics.FromImage(image)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(raw,new Rectangle(0,0,1024,1024));}
            string name=Path.GetFileNameWithoutExtension(file),output=Path.Combine(folder,"frames/"+name+".png");image.Save(output,ImageFormat.Png);
            int left=1024,top=1024,right=0,bottom=0;var lows=new List<int>();
            for(int x=0;x<1024;x++){
                int low=-1;for(int y=0;y<1024;y++)if(image.GetPixel(x,y).A>128){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);low=y;}
                if(x>=430&&x<480&&low>=0)lows.Add(low);
            }
            lows.Sort();if(lows.Count==0)throw new Exception("No body contact: "+name);
            double support=(lows[(lows.Count-1)/2]+lows[lows.Count/2])/2.0+1;
            report.Add(name,new{file=output,bounds=new[]{left,top,right-left+1,bottom-top+1},anchorX=left+269.0,supportY=support,renderWidthAtSmall=480,contactColumns=new[]{430,480},rawWidth=raw.Width,rawHeight=raw.Height});
        }
        File.WriteAllText(Path.Combine(folder,"registration.json"),new JavaScriptSerializer().Serialize(report));return 0;
    }
}
