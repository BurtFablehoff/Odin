#if ODIN_DEV
using System;using System.Drawing;using System.IO;using System.IO.Compression;using System.Web.Script.Serialization;
namespace OdinV2 {
 internal static class GroomScaleReview {
  internal static object Run(DevArtwork latest,string baseline){int compared=0;double maxRear=0;
   using(FileStream file=File.OpenRead(baseline))using(ZipArchive z=new ZipArchive(file,ZipArchiveMode.Read)){
    DevArtManifest old;using(StreamReader r=new StreamReader(z.GetEntry("manifest.json").Open()))old=new JavaScriptSerializer().Deserialize<DevArtManifest>(r.ReadToEnd());
    foreach(DevArtEntry prior in old.actions){DevArtFrame current;if(!latest.TryGet(prior.clip,prior.frame,out current))throw new Exception("Missing prior action frame.");DevArtEntry e=current.Pose;
     if(e.sha256!=prior.sha256||e.key!=prior.key||e.supportY!=prior.supportY)throw new Exception("Groom scale correction changed pixels or floor registration.");
     if(prior.clip=="groom"&&prior.frame==12){
      if(Math.Abs(e.renderWidthAtSmall/prior.renderWidthAtSmall-.95)>.000001)throw new Exception("Unexpected grooming scale correction.");
      int rear=current.Image.Width;for(int y=0;y<current.Image.Height;y++)for(int x=0;x<rear;x++)if(current.Image.GetPixel(x,y).A>128){rear=x;break;}
      foreach(int size in new[]{480,640,800}){double a=(rear-prior.anchorX)*prior.renderWidthAtSmall/current.Image.Width*size/480.0,b=(rear-e.anchorX)*e.renderWidthAtSmall/current.Image.Width*size/480.0;maxRear=Math.Max(maxRear,Math.Abs(a-b));if(Math.Abs(a-b)>.002)throw new Exception("Corrected grooming frame slides at the rear pin.");}
     }else if(e.renderWidthAtSmall!=prior.renderWidthAtSmall||e.anchorX!=prior.anchorX)throw new Exception("Unrelated frame placement changed.");compared++;
    }
    foreach(DevActionClip prior in old.actionClips){DevActionClip c=latest.ActionClip(prior.clip);if(c.frames!=prior.frames||c.seconds!=prior.seconds||c.loop!=prior.loop)throw new Exception("Animation timeline changed.");if(prior.frameSeconds!=null)for(int i=0;i<prior.frames;i++)if(c.frameSeconds[i]!=prior.frameSeconds[i])throw new Exception("Authored frame timing changed.");}
   }
   return new{passed=true,changedDrawing=13,scaleReductionPercent=5,actionsCompared=compared,sourcePngsPreserved=true,unrelatedPlacementAndTimingsPreserved=true,maxRearPinDriftPx=maxRear,ownerMotionReviewPending=true};
  }
 }
}
#endif
