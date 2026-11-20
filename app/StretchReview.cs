#if ODIN_DEV
using System;using System.Drawing;
namespace OdinV2 {
 internal sealed partial class OdinWindow {
  internal object CheckStretchReturn(){
   DevActionClip clip=latestArt.ActionClip("stretch");if(clip==null||clip.frames<3||Math.Abs(clip.seconds-4.6)>.000001)throw new Exception("Incomplete registered stretch.");
   DevArtFrame sitting;latestArt.TryGet("sit",0,out sitting);
   foreach(int i in new[]{0,clip.frames-1}){DevArtFrame e;latestArt.TryGet("stretch",i,out e);if(e.Pose.sha256!=sitting.Pose.sha256||e.Pose.anchorX!=sitting.Pose.anchorX||e.Pose.supportY!=sitting.Pose.supportY||e.Pose.renderWidthAtSmall!=sitting.Pose.renderWidthAtSmall)throw new Exception("Stretch boundary differs from exact held sitting.");}
   int cases=0,frames=0;
   foreach(string from in new[]{"sit","loaf","lay","sleep"}){
    mind.DevHeldState=null;mind.Rest("sit",600);for(int i=0;i<2000&&mind.Clip!="sit";i++)mind.Step(.005,Point.Empty);mind.Rest(from,600);for(int i=0;i<2000&&mind.Clip!=from;i++)mind.Step(.005,Point.Empty);mind.Stretch();for(int i=0;i<2000&&mind.Clip!="stretch";i++)mind.Step(.005,Point.Empty);
    if(mind.Clip!="stretch")throw new Exception("Stretch entry failed.");double x=mind.X,y=mind.FootY,began=mind.Clock;bool[] seen=new bool[clip.frames];
    while(mind.Clip=="stretch"&&mind.Clock-began<6){SpriteChoice choice=ChooseSprite(new Point(-20000,-20000));SpritePlacement p=PlaceSprite(choice);if(p.Latest==null||p.Latest.Pose.clip!="stretch"||p.Latest.Pose.frame!=choice.Frame)throw new Exception("Stretch used atlas fallback.");if(!seen[choice.Frame]){SceneLayout scene=LayoutScene(choice,p);using(Bitmap b=new Bitmap(scene.Scene.Width,scene.Scene.Height))using(Graphics g=Graphics.FromImage(b))DrawScene(g,scene,p.Latest.Image);seen[choice.Frame]=true;frames++;}mind.Step(.005,Point.Empty);}
    if(mind.Clip!="sit"||Math.Abs(mind.Clock-began-4.6)>.011||mind.X!=x||mind.FootY!=y)throw new Exception("Stretch return moved the root, changed duration or missed sitting.");foreach(bool shown in seen)if(!shown)throw new Exception("Stretch skipped a drawn frame.");cases++;
   }
   return new{passed=true,entryStates=cases,actualCompanionFramesRendered=frames,framesPerStretch=clip.frames,seconds=4.6,originalDrawings=clip.frames-2,exactHeldSitAtBothBoundaries=true,noAtlasFallback=true,rootPositionPreserved=true,ownerMotionReviewPending=true,noVisibleWindowOpened=true};
  }
 }
}
#endif
