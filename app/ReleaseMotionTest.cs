using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.IO;using System.IO.Compression;using System.Runtime.InteropServices;using System.Web.Script.Serialization;
namespace OdinV2 {
 internal static class ReleaseMotionTest {
  internal static readonly string[] Clips={"sit_to_loaf","loaf_to_sit","sit_to_lay","lay_to_sit","loaf_to_lay","lay_to_loaf"};
  static void Check(bool value,string message){if(!value)throw new Exception(message);}
  static byte[] Pixels(Bitmap image){BitmapData data=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{byte[] p=new byte[image.Width*image.Height*4];for(int y=0;y<image.Height;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),p,y*image.Width*4,image.Width*4);return p;}finally{image.UnlockBits(data);}}
  static Bitmap Render(DevArtFrame frame,int size,bool dark){Bitmap canvas=new Bitmap(1000,700,PixelFormat.Format32bppArgb);using(Graphics g=Graphics.FromImage(canvas)){g.Clear(dark?Color.FromArgb(27,29,34):Color.FromArgb(245,245,242));g.InterpolationMode=InterpolationMode.HighQualityBicubic;Rectangle destination=frame.Destination(size,450,650,0);RectangleF clip=frame.SourceClip(destination);if(!clip.IsEmpty)g.SetClip(clip);g.DrawImage(frame.Image,destination);}return canvas;}
  internal static object Run(ArtPack art,DevArtwork latest,string approvedPack){
   int frames=0,renders=0,routes=0;
   using(FileStream file=File.OpenRead(approvedPack))using(ZipArchive z=new ZipArchive(file,ZipArchiveMode.Read)){
    DevArtManifest approved;using(StreamReader reader=new StreamReader(z.GetEntry("manifest.json").Open()))approved=new JavaScriptSerializer().Deserialize<DevArtManifest>(reader.ReadToEnd());
    foreach(string clip in Clips){DevActionClip timing=latest.ActionClip(clip),reference=Array.Find(approved.actionClips,c=>c.clip==clip);Check(timing!=null&&timing.frames==reference.frames&&timing.seconds==reference.seconds,"Approved timeline changed: "+clip);
     for(int i=0;i<timing.frames;i++){
      Check(timing.frameSeconds[i]==reference.frameSeconds[i],"Approved frame duration changed.");DevArtFrame actual;Check(latest.TryGet(clip,i,out actual),"Missing production transition frame.");DevArtEntry e=Array.Find(approved.actions,p=>p.clip==clip&&p.frame==i);
      Check(actual.Pose.anchorX==e.anchorX&&actual.Pose.supportY==e.supportY&&actual.Pose.renderWidthAtSmall==e.renderWidthAtSmall,"Approved frame registration changed.");
      using(Stream input=z.GetEntry(e.key).Open())using(Bitmap decoded=new Bitmap(input))using(Bitmap original=new Bitmap(decoded)){
       DevArtFrame expected=new DevArtFrame{Image=original,Pose=e};
       foreach(int size in new[]{480,640,800})foreach(bool dark in new[]{false,true})using(Bitmap a=Render(actual,size,dark))using(Bitmap b=Render(expected,size,dark)){
        byte[] pa=Pixels(a),pb=Pixels(b);for(int j=0;j<pa.Length;j++)if(pa[j]!=pb[j])throw new Exception("Production differs from owner-passed rendering: "+clip+"/"+i+" size "+size+" pixel byte "+j);renders++;
       }
      }frames++;
     }
    }
   }
   foreach(string from in new[]{"sit","loaf","lay"})foreach(string to in new[]{"sit","loaf","lay"}){
    CatMind mind=new CatMind(new Rectangle(0,0,1920,1080),72);mind.ConfigureArt(art.Manifest.clips.Keys);mind.ConfigureTimings(art.Manifest);mind.PerchesEnabled=false;mind.ZoomiesEnabled=false;mind.Rest(from,600);for(int i=0;i<1000&&mind.Clip!=from;i++)mind.Step(.005,Point.Empty);Check(mind.Clip==from,"Could not settle source pose.");
    mind.Rest(to,600);double elapsed=0;string direct=from+"_to_"+to;while(mind.Clip!=to&&elapsed<5){Check(mind.Clip==direct,"Production rest route detoured: "+from+" to "+to+" via "+mind.Clip);mind.Step(.005,Point.Empty);elapsed+=.005;}
    Check(mind.Clip==to,"Production transition did not finish.");if(from!=to)Check(Math.Abs(elapsed-.315)<.011,"Production transition clock changed.");routes++;
   }
   return new{passed=true,approvedFrames=frames,pixelIdenticalRenderings=renders,restRoutes=routes,authoredSeconds=.315,storageMarginsDoNotAlterVisiblePixels=true};
  }
 }
 internal sealed partial class OdinWindow {
  internal object CheckReleaseMotionRendering(){int count=0;foreach(string clip in ReleaseMotionTest.Clips){string[] pair=clip.Split(new[]{"_to_"},StringSplitOptions.None);mind.Rest("sit",600);for(int i=0;i<1000&&mind.Clip!="sit";i++)mind.Step(.005,Point.Empty);mind.Rest(pair[0],600);for(int i=0;i<1000&&mind.Clip!=pair[0];i++)mind.Step(.005,Point.Empty);mind.Rest(pair[1],600);bool[] seen=new bool[latestArt.ActionClip(clip).frames];for(int i=0;i<1000&&mind.Clip==clip;i++){SpriteChoice choice=ChooseSprite(new Point(-20000,-20000));SpritePlacement placed=PlaceSprite(choice);if(placed.Latest==null||placed.Latest.Pose.clip!=clip||placed.Latest.Pose.frame!=choice.Frame)throw new Exception("Production renderer used a fallback: "+clip);if(!seen[choice.Frame]){SceneLayout scene=LayoutScene(choice,placed);using(Bitmap image=new Bitmap(scene.Scene.Width,scene.Scene.Height))using(Graphics g=Graphics.FromImage(image))DrawScene(g,scene,placed.Latest.Image);seen[choice.Frame]=true;count++;}mind.Step(.005,Point.Empty);}foreach(bool frame in seen)if(!frame)throw new Exception("Production skipped a drawn transition frame.");}return new{passed=true,actualProductionFramesRendered=count,noAtlasFallback=true,noVisibleWindowOpened=true};}
 }
}
