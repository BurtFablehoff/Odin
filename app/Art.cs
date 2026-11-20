using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Web.Script.Serialization;

namespace OdinV2
{
    public sealed class FrameInfo { public int offset; public int length; public int anchor_y; }
    public sealed class ClipInfo { public double[] frameSeconds,gaze_angles; public int frames; public double seconds; public bool loop; public int[] head_rect; public double[] shadow_footprint; public FrameInfo[] images; public int anchor_y; }
    public sealed class ArtManifest
    {
        public int version; public int width; public int height; public int[] foot_anchor; public bool preview; public string[] deferred;
        public Dictionary<string,ClipInfo> clips;
    }
    internal sealed class ArtPack : IDisposable
    {
        private readonly byte[] bytes;
        private readonly int start;
        public readonly ArtManifest Manifest;
        private readonly Dictionary<string,Bitmap> cache=new Dictionary<string,Bitmap>();
        private readonly LinkedList<string> recent=new LinkedList<string>();
        public int CachedFrames { get { return cache.Count; } }
        public ArtPack()
        {
            using(Stream input=Assembly.GetExecutingAssembly().GetManifestResourceStream("Odin.Assets"))
            {
                if(input==null)throw new InvalidDataException("The embedded Odin artwork is missing. Download the complete Odin.exe.");
                using(MemoryStream data=new MemoryStream()){input.CopyTo(data);bytes=data.ToArray();}
            }
            using(BinaryReader reader=new BinaryReader(new MemoryStream(bytes)))
            {
                if(reader.ReadInt32()!=0x324E444F)throw new InvalidDataException("Invalid Odin artwork header.");
                int length=reader.ReadInt32();
                if(length<32 || length>1000000 || length>bytes.Length-8)throw new InvalidDataException("Invalid Odin artwork index.");
                string json=System.Text.Encoding.UTF8.GetString(reader.ReadBytes(length));
                Manifest=new JavaScriptSerializer().Deserialize<ArtManifest>(json);start=8+length;
            }
            if(Manifest.version!=2 || Manifest.width!=1024 || Manifest.height!=1024 || Manifest.clips==null)throw new InvalidDataException("Unsupported artwork format.");
            foreach(KeyValuePair<string,ClipInfo> pair in Manifest.clips)
            {
                ClipInfo c=pair.Value;
                if(c.images==null || c.frames!=c.images.Length || c.frames<1)throw new InvalidDataException("Incomplete animation: "+pair.Key);
                foreach(FrameInfo f in c.images)if(f.offset<0 || f.length<20 || (long)start+f.offset+f.length>bytes.Length)throw new InvalidDataException("Truncated animation: "+pair.Key);
            }
            // A25: production and DEV read the native pack's authored timelines.
            // Atlas placeholders remain seated; the companion draws validated native frames.
            using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Odin.LatestArt"))
            if(stream!=null)using(ZipArchive archive=new ZipArchive(stream,ZipArchiveMode.Read))
            using(StreamReader reader=new StreamReader(archive.GetEntry("manifest.json").Open()))
            {
                DevArtManifest latest=new JavaScriptSerializer().Deserialize<DevArtManifest>(reader.ReadToEnd());
                if(latest.subtleTailTipV6||latest.repairedTailFlipV4||latest.drawnTailFlipV5)foreach(string family in new[]{"sit","lay","sleep"})
                {
                    ClipInfo tail=Manifest.clips[family+"_tail"];FrameInfo resting=tail.images[0];
                    tail.frames=latest.drawnTailFlipV5?7:17;tail.images=new FrameInfo[tail.frames];for(int i=0;i<tail.frames;i++)tail.images[i]=resting;
                }
                foreach(DevActionClip clip in latest.actionClips)if(clip.clip.Contains("_to_")&&(!Manifest.clips.ContainsKey(clip.clip)||((latest.smoothRestV8||latest.drawnRestV9)&&(clip.clip=="sit_to_loaf"||clip.clip=="loaf_to_sit"||clip.clip=="sit_to_lay"||clip.clip=="lay_to_sit"||clip.clip=="loaf_to_lay"||clip.clip=="lay_to_loaf"))))
                {
                    FrameInfo[] fallback=new FrameInfo[clip.frames];for(int i=0;i<fallback.Length;i++)fallback[i]=Manifest.clips["sit"].images[0];
                    Manifest.clips[clip.clip]=new ClipInfo{frames=clip.frames,seconds=clip.seconds,frameSeconds=clip.frameSeconds,loop=false,images=fallback,anchor_y=944};
                }
                DevActionClip stretch=Array.Find(latest.actionClips,c=>c.clip=="stretch");
                if(stretch!=null){FrameInfo[] fallback=new FrameInfo[stretch.frames];for(int i=0;i<fallback.Length;i++)fallback[i]=Manifest.clips["sit"].images[0];Manifest.clips["stretch"]=new ClipInfo{frames=stretch.frames,seconds=stretch.seconds,frameSeconds=stretch.frameSeconds,loop=false,images=fallback,anchor_y=944};}
                DevActionClip grooming=Array.Find(latest.actionClips,c=>c.clip=="groom");
                if(grooming!=null){ClipInfo original=Manifest.clips["groom"];FrameInfo[] fallback=new FrameInfo[grooming.frames];for(int i=0;i<fallback.Length;i++)fallback[i]=Manifest.clips["sit"].images[0];Manifest.clips["groom"]=new ClipInfo{frames=grooming.frames,seconds=grooming.seconds,frameSeconds=grooming.frameSeconds,loop=grooming.loop,images=fallback,anchor_y=original.anchor_y,shadow_footprint=original.shadow_footprint};}
            }
        }
        public Bitmap Get(string name,int frame)
        {
            ClipInfo clip;
            if(!Manifest.clips.TryGetValue(name,out clip))throw new InvalidDataException("Missing animation: "+name);
            frame=Math.Max(0,Math.Min(clip.frames-1,frame));string key=name+":"+frame;
            Bitmap result;
            if(cache.TryGetValue(key,out result)){recent.Remove(key);recent.AddLast(key);return result;}
            FrameInfo f=clip.images[frame];
            using(MemoryStream stream=new MemoryStream(bytes,start+f.offset,f.length,false))
            using(Bitmap png=new Bitmap(stream))
            {
                if(png.Width!=Manifest.width || png.Height!=Manifest.height)throw new InvalidDataException("Wrong animation dimensions: "+key);
                result=png.Clone(new Rectangle(0,0,png.Width,png.Height),PixelFormat.Format32bppPArgb);
            }
            cache.Add(key,result);recent.AddLast(key);
            while(cache.Count>56){string old=recent.First.Value;recent.RemoveFirst();cache[old].Dispose();cache.Remove(old);}
            return result;
        }
        public Rectangle HeadRect(string name)
        {
            int[] h=Manifest.clips[name].head_rect;
            return h!=null && h.Length==4?new Rectangle(h[0],h[1],h[2],h[3]):new Rectangle(0,0,Manifest.width,Manifest.height);
        }
        public int Frame(string name,double age)
        {
            ClipInfo c=Manifest.clips[name];if(c.seconds<=0)return 0;
            if(c.frameSeconds!=null){double elapsed=c.loop?Math.Max(0,age)%c.seconds:Math.Max(0,age);for(int i=0;i<c.frameSeconds.Length;i++){if(elapsed<c.frameSeconds[i]-1e-9)return i;elapsed-=c.frameSeconds[i];}return c.frames-1;}
            double ratio=age/c.seconds;
            return c.loop ? (int)(Math.Max(0,ratio)*c.frames)%c.frames : Math.Min(c.frames-1,(int)(Math.Max(0,ratio)*c.frames));
        }
        public int AnchorY(string name,int frame)
        {
            ClipInfo c=Manifest.clips[name];FrameInfo f=c.images[Math.Max(0,Math.Min(c.frames-1,frame))];
            return f.anchor_y>0?f.anchor_y:c.anchor_y>0?c.anchor_y:Manifest.foot_anchor[1];
        }
        public void Dispose(){foreach(Bitmap b in cache.Values)b.Dispose();cache.Clear();recent.Clear();}
    }
}
