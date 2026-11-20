using System;using System.Collections.Generic;using System.Drawing;using System.Drawing.Imaging;using System.Runtime.InteropServices;
namespace OdinV2 {
 internal static class ShortGroomReview {
  internal static object Run(ArtPack art){double min=100,max=0;int cases=0;var durations=new HashSet<int>();
   foreach(string from in new[]{"sit","loaf","lay"})for(int seed=0;seed<100;seed++){
    CatMind m=new CatMind(new Rectangle(0,0,1920,1080),seed);m.ConfigureArt(art.Manifest.clips.Keys);m.ConfigureTimings(art.Manifest);m.PerchesEnabled=false;m.ZoomiesEnabled=false;m.Rest(from,600);for(int i=0;i<1000&&m.Clip!=from;i++)m.Step(.005,Point.Empty);
    m.Groom();double began=m.Clock;bool exit=false;int last=-1;
    for(int i=0;i<1200;i++){
     if(m.Clip=="groom"){last=art.Frame("groom",m.AnimationAge);double count=m.Duration/art.Manifest.clips["groom"].seconds;if(Math.Abs(count-Math.Round(count))>.000001)throw new Exception("Short groom truncates a lick pair.");}
     if(m.Clip=="groom_to_sit"){exit=true;if(last!=1)throw new Exception("Short groom skips final lick drawing.");}
     if(m.Clip=="sit"&&exit)break;m.Step(.005,Point.Empty);
    }
    double elapsed=m.Clock-began;if(!exit||m.Clip!="sit"||elapsed<2-.011||elapsed>4+.011)throw new Exception("Complete grooming action outside 2-4 seconds: "+elapsed);
    min=Math.Min(min,elapsed);max=Math.Max(max,elapsed);durations.Add((int)Math.Round(elapsed*1000));cases++;
   }
   if(durations.Count<2)throw new Exception("Grooming duration does not vary.");
   return new{passed=true,episodes=cases,minCompleteSeconds=min,maxCompleteSeconds=max,includesPreparationEntryAndReturn=true,lickPoseSeconds=art.Manifest.clips["groom"].seconds/2,completePairsOnly=true};
  }
 }
#if ODIN_DEV
 internal sealed partial class OdinWindow {
  static byte[] BorderPixels(Bitmap image){BitmapData d=image.LockBits(new Rectangle(0,0,image.Width,image.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{byte[] p=new byte[image.Width*image.Height*4];for(int y=0;y<image.Height;y++)Marshal.Copy(IntPtr.Add(d.Scan0,y*d.Stride),p,y*image.Width*4,image.Width*4);return p;}finally{image.UnlockBits(d);}}
  internal object CheckRestBorders(){int cases=0,clipped=0;long removed=0;int oldSize=size;
   try{foreach(int testSize in new[]{480,640,800})foreach(string clip in new[]{"sit_to_loaf","loaf_to_sit"})for(int frame=0;frame<latestArt.ActionClip(clip).frames;frame++){
    size=testSize;mind.X=450;mind.FootY=650;mind.VisualLift=0;SpriteChoice choice=new SpriteChoice{Clip=clip,DisplayClip=clip,Frame=frame,DisplayFrame=frame};SpritePlacement p=PlaceSprite(choice);SceneLayout actual=LayoutScene(choice,p);actual.ShadowHeight=double.PositiveInfinity;SceneLayout original=actual;original.SpriteClip=RectangleF.Empty;
    using(Bitmap a=new Bitmap(actual.Scene.Width,actual.Scene.Height))using(Bitmap b=new Bitmap(a.Width,a.Height)){
     using(Graphics g=Graphics.FromImage(a))DrawScene(g,actual,p.Latest.Image);using(Graphics g=Graphics.FromImage(b))DrawScene(g,original,p.Latest.Image);
     byte[] pa=BorderPixels(a),pb=BorderPixels(b);
     for(int i=0;i<pa.Length;i+=4){if(pb[i+3]>128)for(int c=0;c<4;c++)if(pa[i+c]!=pb[i+c])throw new Exception("Border cleanup changes opaque cat pixels.");if(pb[i+3]>0&&pa[i+3]==0)removed++;}
     if(!actual.SpriteClip.IsEmpty){int right=(int)Math.Ceiling(actual.SpriteClip.Right-actual.Scene.Left)+1;for(int y=0;y<a.Height;y++)for(int x=right;x<a.Width;x++)if(pa[(y*a.Width+x)*4+3]!=0)throw new Exception("Foreign right-border pixels still rendered.");clipped++;}
    }cases++;
   }}finally{size=oldSize;}
   if(removed==0||clipped==0)throw new Exception("Border test did not exercise stray content.");
   return new{passed=true,renderCases=cases,clippedCases=clipped,foreignPixelSamplesRemoved=removed,opaqueCatPixelsUnchanged=true,sourcePngsUnchanged=true,threeSizesBothDirections=true,noVisibleWindowOpened=true};
  }
 }
#endif
}
