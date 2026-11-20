#if ODIN_DEV
using System;using System.Collections.Generic;using System.Drawing;
namespace OdinV2 {
 internal sealed partial class OdinWindow {
  static void GroomBoundary(DevArtFrame a,DevArtFrame b){if(a.Pose.sha256!=b.Pose.sha256||a.Pose.anchorX!=b.Pose.anchorX||a.Pose.supportY!=b.Pose.supportY||a.Pose.renderWidthAtSmall!=b.Pose.renderWidthAtSmall)throw new Exception("Grooming boundary changes source/placement.");}
  internal object CheckGroomReturn(){
   DevArtFrame sit,first,last,entryStart,entryEnd,exitStart,exitEnd;
   latestArt.TryGet("sit",0,out sit);latestArt.TryGet("groom",0,out first);latestArt.TryGet("groom",latestArt.ActionClip("groom").frames-1,out last);
   latestArt.TryGet("sit_to_groom",0,out entryStart);latestArt.TryGet("sit_to_groom",latestArt.ActionClip("sit_to_groom").frames-1,out entryEnd);latestArt.TryGet("groom_to_sit",0,out exitStart);latestArt.TryGet("groom_to_sit",latestArt.ActionClip("groom_to_sit").frames-1,out exitEnd);
   if(entryStart==null||entryEnd==null||exitStart==null||exitEnd==null)throw new Exception("Missing grooming entry/return.");
   GroomBoundary(entryStart,sit);GroomBoundary(entryEnd,first);GroomBoundary(exitStart,last);GroomBoundary(exitEnd,sit);
   int cases=0,rendered=0,finishedCycles=0;
   foreach(string from in new[]{"sit","loaf","lay"}){
    mind.DevHeldState=null;mind.Rest("sit",600);for(int i=0;i<2000&&mind.Clip!="sit";i++)mind.Step(.005,Point.Empty);mind.Rest(from,600);for(int i=0;i<2000&&mind.Clip!=from;i++)mind.Step(.005,Point.Empty);
    double x=mind.X,y=mind.FootY;mind.Groom();double began=mind.Clock;var seen=new HashSet<string>();int lastLick=-1;bool returning=false;
    while(mind.Clock-began<40){string phase=mind.Clip;if(phase=="sit"&&returning)break;
     if(phase=="sit_to_groom"||phase=="groom"||phase=="groom_to_sit"){
      SpriteChoice choice=ChooseSprite(new Point(-20000,-20000));SpritePlacement p=PlaceSprite(choice);if(p.Latest==null||p.Latest.Pose.clip!=phase||p.Latest.Pose.frame!=choice.Frame)throw new Exception("Grooming used atlas fallback.");
      if(phase=="groom")lastLick=choice.Frame;
      if(phase=="groom_to_sit"&&!returning){if(lastLick!=latestArt.ActionClip("groom").frames-1)throw new Exception("Grooming cut off the final lick frame.");returning=true;finishedCycles++;}
      if(seen.Add(phase+":"+choice.Frame)){SceneLayout scene=LayoutScene(choice,p);using(Bitmap b=new Bitmap(scene.Scene.Width,scene.Scene.Height))using(Graphics g=Graphics.FromImage(b))DrawScene(g,scene,p.Latest.Image);rendered++;}
     }
     mind.Step(.005,Point.Empty);
    }
    int expected=latestArt.ActionClip("sit_to_groom").frames+latestArt.ActionClip("groom").frames+latestArt.ActionClip("groom_to_sit").frames;
    if(mind.Clip!="sit"||seen.Count!=expected||mind.X!=x||mind.FootY!=y)throw new Exception("Groom route omitted frames, moved root or failed to return.");cases++;
   }
   // A pose request mid-lick finishes its current cycle and lowers the paw first.
   mind.DevHeldState=null;mind.Groom();for(int i=0;i<2000&&mind.Clip!="groom";i++)mind.Step(.005,Point.Empty);mind.Step(.37,Point.Empty);double age=mind.Age;mind.Rest("loaf",600);if(mind.Clip!="groom"||mind.Age!=age)throw new Exception("Pose request interrupted/restarted a lick.");bool exit=false;int endFrame=-1;
   for(int i=0;i<3000&&mind.Clip!="loaf";i++){if(mind.Clip=="groom")endFrame=art.Frame("groom",mind.AnimationAge);if(mind.Clip=="groom_to_sit"){exit=true;if(endFrame!=latestArt.ActionClip("groom").frames-1)throw new Exception("Interrupted groom skipped its final lick.");}mind.Step(.005,Point.Empty);}
   if(!exit||mind.Clip!="loaf")throw new Exception("Interrupted groom bypassed the return.");
   return new{passed=true,entryStates=cases,actualCompanionFramesRendered=rendered,allLickFramesSelected=true,allFourBoundariesMatchSourceAndPlacement=true,completeLickCyclesBeforeReturn=finishedCycles,poseRequestFinishesLickCycle=true,rootPositionPreserved=true,noAtlasFallback=true,noVisibleWindowOpened=true,ownerMotionReviewPending=true};
  }
 }
}
#endif
