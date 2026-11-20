using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.IO;using System.IO.Compression;using System.Reflection;using System.Runtime.InteropServices;using System.Security.Cryptography;using System.Web.Script.Serialization;
namespace OdinV2 {
 internal static class ApprovedActionsReview {
  internal static readonly string[] Clips={"sit_to_loaf","loaf_to_sit","sit_to_lay","lay_to_sit","loaf_to_lay","lay_to_loaf","stretch","sit_to_groom","groom","groom_to_sit"};
  static byte[] Read(ZipArchiveEntry entry){using(Stream s=entry.Open())using(MemoryStream m=new MemoryStream()){s.CopyTo(m);return m.ToArray();}}
  static bool Same(byte[] a,byte[] b){if(a.Length!=b.Length)return false;for(int i=0;i<a.Length;i++)if(a[i]!=b[i])return false;return true;}
  static byte[] Pixels(Bitmap image){BitmapData d=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{byte[] b=new byte[image.Width*image.Height*4];for(int y=0;y<image.Height;y++)Marshal.Copy(IntPtr.Add(d.Scan0,y*d.Stride),b,y*image.Width*4,image.Width*4);return b;}finally{image.UnlockBits(d);}}
  static Bitmap Source(ZipArchive z,DevArtEntry e){using(MemoryStream s=new MemoryStream(Read(z.GetEntry(e.key))))using(Bitmap b=new Bitmap(s)){if(e.contentRect==null)return new Bitmap(b);int[] r=e.contentRect;using(Bitmap crop=b.Clone(new Rectangle(r[0],r[1],r[2],r[3]),PixelFormat.Format32bppArgb))return new Bitmap(crop);}}
  static Bitmap Render(DevArtFrame frame,int size,bool dark){Bitmap b=new Bitmap(1000,700,PixelFormat.Format32bppArgb);using(Graphics g=Graphics.FromImage(b)){g.Clear(dark?Color.FromArgb(27,29,34):Color.FromArgb(245,245,242));g.InterpolationMode=InterpolationMode.HighQualityBicubic;Rectangle dest=frame.Destination(size,450,650,0);RectangleF clip=frame.SourceClip(dest);if(!clip.IsEmpty)g.SetClip(clip);g.DrawImage(frame.Image,dest);}return b;}
  internal static object Run(ArtPack art,DevArtwork latest,string approved){int resources=0,frames=0,renders=0;
   using(FileStream file=File.OpenRead(approved))using(ZipArchive expected=new ZipArchive(file,ZipArchiveMode.Read))using(Stream input=Assembly.GetExecutingAssembly().GetManifestResourceStream("Odin.LatestArt"))using(ZipArchive actual=new ZipArchive(input,ZipArchiveMode.Read)){
    if(expected.Entries.Count!=actual.Entries.Count)throw new Exception("Production resource count differs from approved V5.");
    foreach(ZipArchiveEntry entry in expected.Entries){ZipArchiveEntry current=actual.GetEntry(entry.FullName);if(current==null||!Same(Read(entry),Read(current)))throw new Exception("Production pack content differs from approved V5: "+entry.FullName);resources++;}
    DevArtManifest m=new JavaScriptSerializer().Deserialize<DevArtManifest>(System.Text.Encoding.UTF8.GetString(Read(expected.GetEntry("manifest.json"))));
    foreach(string name in Clips){DevActionClip c=Array.Find(m.actionClips,x=>x.clip==name);ClipInfo timeline=art.Manifest.clips[name];if(c.frames!=timeline.frames||c.seconds!=timeline.seconds||c.loop!=timeline.loop)throw new Exception("Production timeline differs from approved V5: "+name);if(c.frameSeconds!=null)for(int i=0;i<c.frames;i++)if(c.frameSeconds[i]!=timeline.frameSeconds[i])throw new Exception("Production authored hold changed.");
     for(int i=0;i<c.frames;i++){DevArtEntry e=Array.Find(m.actions,x=>x.clip==name&&x.frame==i);DevArtFrame current;if(!latest.TryGet(name,i,out current))throw new Exception("Missing approved production drawing.");using(Bitmap source=Source(expected,e)){DevArtFrame reference=new DevArtFrame{Image=source,Pose=e};foreach(int size in new[]{480,640,800})foreach(bool dark in new[]{false,true})using(Bitmap a=Render(current,size,dark))using(Bitmap b=Render(reference,size,dark)){if(!Same(Pixels(a),Pixels(b)))throw new Exception("Production rendering differs from V5: "+name+"/"+i);renders++;}}frames++;}
    }
   }
   return new{passed=true,identicalPackResources=resources,approvedActionFrames=frames,pixelIdenticalSizeBackgroundCases=renders,sourceViewportsPreserved=true};
  }
 }
 internal sealed partial class OdinWindow {
  internal object CheckApprovedActionRendering(){int rendered=0;foreach(string name in ApprovedActionsReview.Clips)for(int i=0;i<latestArt.ActionClip(name).frames;i++){
   SpriteChoice choice=new SpriteChoice{Clip=name,DisplayClip=name,Frame=i,DisplayFrame=i};SpritePlacement p=PlaceSprite(choice);if(p.Latest==null||p.Latest.Pose.clip!=name||p.Latest.Pose.frame!=i)throw new Exception("Production action uses a fallback.");SceneLayout scene=LayoutScene(choice,p);using(Bitmap b=new Bitmap(scene.Scene.Width,scene.Scene.Height))using(Graphics g=Graphics.FromImage(b))DrawScene(g,scene,p.Latest.Image);rendered++;
  }return new{passed=true,actualProductionActionFramesRendered=rendered,noFallback=true,noVisibleWindowOpened=true};}
 }
}
