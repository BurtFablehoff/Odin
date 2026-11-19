// A20: author smooth candidate playback from registered keys. Original PNGs are read only.
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
using System.Collections.Generic;

public sealed class MotionKey { public string file,key;public float anchorX,supportY,renderWidthAtSmall; }
public sealed class MotionSequence { public string name;public double seconds;public MotionKey[] keys; }
public sealed class MotionConfig { public MotionSequence[] sequences; }
internal static class TransitionMotionFrames
{
    static readonly Rectangle Canvas=new Rectangle(0,0,1024,1024);
    static byte[] Pixels(Bitmap b){var d=b.LockBits(Canvas,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{byte[] a=new byte[d.Stride*1024];Marshal.Copy(d.Scan0,a,0,a.Length);return a;}finally{b.UnlockBits(d);}}
    static Bitmap Image(byte[] a){Bitmap b=new Bitmap(1024,1024,PixelFormat.Format32bppArgb);var d=b.LockBits(Canvas,ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{Marshal.Copy(a,0,d.Scan0,a.Length);}finally{b.UnlockBits(d);}return b;}
    static string Placed(string folder,string name,int key){return Path.Combine(folder,"placed/"+name+"-"+key+".png");}
    static void Prepare(string folder,MotionConfig config)
    {
        Directory.CreateDirectory(Path.Combine(folder,"placed"));
        foreach(var seq in config.sequences)for(int i=0;i<seq.keys.Length;i++){
            MotionKey k=seq.keys[i];using(Bitmap raw=new Bitmap(k.file))using(Bitmap normalized=new Bitmap(1024,1024,PixelFormat.Format32bppArgb))using(Bitmap placed=new Bitmap(1024,1024,PixelFormat.Format32bppArgb)){
                using(Graphics g=Graphics.FromImage(normalized)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(raw,Canvas);}
                float scale=k.renderWidthAtSmall/480f;
                using(Graphics g=Graphics.FromImage(placed)){g.CompositingMode=CompositingMode.SourceCopy;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(normalized,new RectangleF(512-k.anchorX*scale,944-k.supportY*scale,1024*scale,1024*scale));}
                if(k.key.StartsWith("loaf-lay")){
                    byte[] pixels=Pixels(placed);object measured=Measure(pixels,"",0,0,0);var type=measured.GetType();double anchor=(double)type.GetProperty("anchorX").GetValue(measured,null),support=(double)type.GetProperty("supportY").GetValue(measured,null);
                    using(Bitmap fixedFrame=new Bitmap(1024,1024,PixelFormat.Format32bppArgb))using(Graphics g=Graphics.FromImage(fixedFrame)){g.CompositingMode=CompositingMode.SourceCopy;g.DrawImageUnscaled(placed,(int)Math.Round(512-anchor),(int)Math.Round(944-support));fixedFrame.Save(Placed(folder,seq.name,i),ImageFormat.Png);}
                }else placed.Save(Placed(folder,seq.name,i),ImageFormat.Png);
            }
        }
    }
    static float[] Flow(string path){using(BinaryReader r=new BinaryReader(File.OpenRead(path))){if(r.ReadInt32()!=1024||r.ReadInt32()!=1024)throw new Exception("Flow size mismatch");byte[] bytes=r.ReadBytes(1024*1024*8);float[] f=new float[1024*1024*2];Buffer.BlockCopy(bytes,0,f,0,bytes.Length);return f;}}
    static void Sample(byte[] a,double x,double y,double[] result)
    {
        for(int c=0;c<4;c++)result[c]=0;
        int x0=(int)Math.Floor(x),y0=(int)Math.Floor(y);double fx=x-x0,fy=y-y0;
        for(int dy=0;dy<2;dy++)for(int dx=0;dx<2;dx++){
            int sx=x0+dx,sy=y0+dy;if(sx<0||sx>=1024||sy<0||sy>=1024)continue;
            double w=(dx==0?1-fx:fx)*(dy==0?1-fy:fy);int p=(sy*1024+sx)*4;double alpha=a[p+3];result[3]+=alpha*w;
            for(int c=0;c<3;c++)result[c]+=a[p+c]*alpha/255.0*w;
        }
    }
    static byte[] Between(byte[] a,byte[] b,float[] ab,float[] ba,double t)
    {
        byte[] output=new byte[a.Length];double[] sa=new double[4],sb=new double[4];
        for(int y=0;y<1024;y++)for(int x=0;x<1024;x++){
            int f=(y*1024+x)*2,p=(y*1024+x)*4;
            double ax=-t*(1-t)*ab[f]+t*t*ba[f],ay=-t*(1-t)*ab[f+1]+t*t*ba[f+1];
            double bx=(1-t)*(1-t)*ab[f]-t*(1-t)*ba[f],by=(1-t)*(1-t)*ab[f+1]-t*(1-t)*ba[f+1];
            Sample(a,x+ax,y+ay,sa);Sample(b,x+bx,y+by,sb);double alpha=sa[3]*(1-t)+sb[3]*t;
            output[p+3]=(byte)Math.Max(0,Math.Min(255,Math.Round(alpha)));
            for(int c=0;c<3;c++)output[p+c]=alpha<.5?(byte)0:(byte)Math.Max(0,Math.Min(255,Math.Round((sa[c]*(1-t)+sb[c]*t)*255/alpha)));
        }
        return output;
    }
    static object Measure(byte[] a,string file,int number,int segment,double t)
    {
        int left=1024,top=1024,right=0,bottom=0;var bottoms=new List<int>();
        for(int x=0;x<1024;x++){int low=-1;for(int y=0;y<1024;y++)if(a[(y*1024+x)*4+3]>128){left=Math.Min(left,x);top=Math.Min(top,y);right=Math.Max(right,x);bottom=Math.Max(bottom,y);low=y;}if(x>=430&&x<480&&low>=0)bottoms.Add(low);}
        bottoms.Sort();if(bottoms.Count==0)throw new Exception("No body support");double support=(bottoms[(bottoms.Count-1)/2]+bottoms[bottoms.Count/2])/2.0+1;
        return new{file=file,frame=number,segment=segment,t=t,bounds=new[]{left,top,right-left+1,bottom-top+1},supportY=support,anchorX=left+269.0,renderWidthAtSmall=480,contactColumns=new[]{430,480}};
    }
    static void Render(string folder,MotionConfig config)
    {
        Directory.CreateDirectory(Path.Combine(folder,"frames"));var all=new Dictionary<string,object>();
        foreach(var seq in config.sequences){
            var schedule=new JavaScriptSerializer().Deserialize<int[]>(File.ReadAllText(Path.Combine(folder,seq.name+"-steps.json")));
            var frames=new List<object>();int number=0;
            for(int k=0;k<seq.keys.Length-1;k++){
                byte[] a,b;using(Bitmap image=new Bitmap(Placed(folder,seq.name,k)))a=Pixels(image);using(Bitmap image=new Bitmap(Placed(folder,seq.name,k+1)))b=Pixels(image);
                float[] ab=Flow(Path.Combine(folder,seq.name+"-"+k+"-ab.flow")),ba=Flow(Path.Combine(folder,seq.name+"-"+k+"-ba.flow"));
                for(int step=0;step<schedule[k];step++){
                    double t=step/(double)schedule[k];byte[] pixels=step==0?a:Between(a,b,ab,ba,t);string file=Path.Combine(folder,"frames/"+seq.name+"-"+number.ToString("000")+".png");
                    using(Bitmap image=Image(pixels))image.Save(file,ImageFormat.Png);frames.Add(Measure(pixels,file,number++,k,t));
                }
            }
            byte[] last;using(Bitmap image=new Bitmap(Placed(folder,seq.name,seq.keys.Length-1)))last=Pixels(image);
            string final=Path.Combine(folder,"frames/"+seq.name+"-"+number.ToString("000")+".png");using(Bitmap image=Image(last))image.Save(final,ImageFormat.Png);frames.Add(Measure(last,final,number,seq.keys.Length-2,1));
            all.Add(seq.name,frames);
        }
        File.WriteAllText(Path.Combine(folder,"frames.json"),new JavaScriptSerializer{MaxJsonLength=20000000}.Serialize(all));
    }
    static int Main(string[] args){try{string folder=args[0];var config=new JavaScriptSerializer().Deserialize<MotionConfig>(File.ReadAllText(Path.Combine(folder,"keys.json")));if(args[1]=="prepare")Prepare(folder,config);else Render(folder,config);return 0;}catch(Exception ex){File.WriteAllText(Path.Combine(args[0],"author-error.txt"),ex.ToString());return 1;}}
}
