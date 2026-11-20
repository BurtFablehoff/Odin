#if ODIN_DEV
using System;using System.Collections.Generic;using System.Drawing;
namespace OdinV2 {
 internal sealed partial class OdinWindow {
  internal object CheckStretchTail(){
   DevActionClip c=latestArt.ActionClip("stretch");if(c.frames!=27||Math.Abs(c.seconds-4.6)>.000001)throw new Exception("Stretch tail revision is incomplete.");
   for(int i=0;i<c.frames;i++){DevArtFrame f;latestArt.TryGet("stretch",i,out f);if(f.Pose.key=="stretch-original/025.png"||f.Pose.key=="stretch-original/026.png"||f.Pose.key=="stretch-original/027.png")throw new Exception("Redundant seated variant still selected.");}
   int cases=0,drawn=0;
   foreach(string held in new[]{"sit","loaf","lay"}){
    mind.DevHeldState=null;mind.Rest("sit",600);for(int i=0;i<2000&&mind.Clip!="sit";i++)mind.Step(.005,Point.Empty);mind.DevSelectState(held);for(int i=0;i<2000&&mind.Clip!=held;i++)mind.Step(.005,Point.Empty);
    mind.Stretch();for(int i=0;i<2000&&mind.Clip!="stretch";i++)mind.Step(.005,Point.Empty);bool finalSeen=false;var after=new List<string>();
    for(int i=0;i<2000;i++){
     if(mind.Clip=="stretch"){
      SpriteChoice choice=ChooseSprite(new Point(-20000,-20000));SpritePlacement p=PlaceSprite(choice);if(p.Latest==null||p.Latest.Pose.clip!="stretch")throw new Exception("Held stretch used atlas fallback.");
      if(choice.Frame==c.frames-1){DevArtFrame sit;latestArt.TryGet("sit",0,out sit);GroomBoundary(p.Latest,sit);SceneLayout scene=LayoutScene(choice,p);using(Bitmap b=new Bitmap(scene.Scene.Width,scene.Scene.Height))using(Graphics g=Graphics.FromImage(b))DrawScene(g,scene,p.Latest.Image);finalSeen=true;drawn++;}
     }else if(finalSeen){if(after.Count==0||after[after.Count-1]!=mind.Clip)after.Add(mind.Clip);if(mind.Clip==held)break;}
     mind.Step(.005,Point.Empty);
    }
    if(!finalSeen||mind.Clip!=held)throw new Exception("Held stretch did not settle back correctly.");
    if(held!="sit"&&(after.Count==0||after[0]!="sit_to_"+held))throw new Exception("Held stretch snapped directly from a seated endpoint to another rest state.");cases++;
   }
   mind.DevHeldState=null;
   return new{passed=true,redundantSeatedVariantsSelected=false,finalImageMatchesHeldSitting=true,heldMenuCases=cases,finalSittingSamplesRendered=drawn,heldLoafAndLayUseTransitions=true,seconds=4.6,noVisibleWindowOpened=true,ownerMotionReviewPending=true};
  }
 }
}
#endif
