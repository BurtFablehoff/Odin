using System;
using System.Collections.Generic;
using System.Drawing;

namespace OdinV2
{
    // Bounds are relative to the support anchor, not to the transparent canvas.
    internal sealed class ScreenEdges
    {
        private readonly ArtPack art;
        private readonly Dictionary<string,PointF> envelopes=new Dictionary<string,PointF>();
        public const int Gap=4;
        public ScreenEdges(ArtPack assets){art=assets;}
        private static string Family(string clip)
        {
            if(clip.StartsWith("look_"))return clip.Substring(5);
            if(clip.EndsWith("_eyes") || clip.EndsWith("_tail"))return clip.Substring(0,clip.Length-5);
            return clip;
        }
        public PointF Margins(string clip,int size)
        {
            string family=Family(clip);PointF envelope;
            if(!envelopes.TryGetValue(family,out envelope))
            {
                double left=0,right=0;bool found=false;
                foreach(KeyValuePair<string,ClipInfo> pair in art.Manifest.clips)
                {
                    if(Family(pair.Key)!=family)continue;
                    for(int frame=0;frame<pair.Value.frames;frame++)
                    {
                        Rectangle bounds=RunningScale.VisibleBounds(art.Get(pair.Key,frame));
                        if(bounds.IsEmpty)continue;
                        bounds.Offset(RunningMotion.Offset(pair.Key,frame),0);
                        left=Math.Max(left,art.Manifest.foot_anchor[0]-bounds.Left);
                        right=Math.Max(right,bounds.Right-art.Manifest.foot_anchor[0]);found=true;
                    }
                }
                if(!found)return new PointF((float)(size*RunningScale.LeftMargin),(float)(size*RunningScale.RightMargin));
                envelope=new PointF((float)(left/art.Manifest.width),(float)(right/art.Manifest.width));
                envelopes.Add(family,envelope);
            }
            int pixels=RunningScale.Pixels(size,family);
            return new PointF(envelope.X*pixels+Gap,envelope.Y*pixels+Gap);
        }
        public static object Test(ArtPack art)
        {
            ScreenEdges edges=new ScreenEdges(art);int checkedFrames=0;
            foreach(int size in new[]{480,640,800})
            foreach(Rectangle area in new[]{new Rectangle(0,0,1920,1080),new Rectangle(-1920,-120,1920,1080)})
            foreach(KeyValuePair<string,ClipInfo> pair in art.Manifest.clips)
            {
                PointF margins=edges.Margins(pair.Key,size);
                CatMind cat=new CatMind(area,5);cat.Clip=pair.Key;cat.VisibleMargins=delegate(string ignored){return margins;};
                double scale=RunningScale.Pixels(size,Family(pair.Key))/(double)art.Manifest.width;
                for(int index=0;index<pair.Value.frames;index++)
                {
                    Rectangle b=RunningScale.VisibleBounds(art.Get(pair.Key,index));
                    b.Offset(RunningMotion.Offset(pair.Key,index),0);
                    cat.X=area.Left-10000;cat.ClampHorizontal();
                    double visibleLeft=cat.X+(b.Left-art.Manifest.foot_anchor[0])*scale;
                    if(visibleLeft<area.Left+Gap-.02)throw new Exception("Left edge clips visible fur: "+pair.Key);
                    cat.X=area.Right+10000;cat.ClampHorizontal();
                    double visibleRight=cat.X+(b.Right-art.Manifest.foot_anchor[0])*scale;
                    if(visibleRight>area.Right-Gap+.02)throw new Exception("Right edge clips visible fur: "+pair.Key);
                    checkedFrames++;
                }
            }
            foreach(int size in new[]{480,640,800})
            {
                CatMind cat=new CatMind(new Rectangle(-1920,0,1920,1080),7);
                cat.VisibleMargins=delegate(string clip){return edges.Margins(clip,size);};
                cat.MoveHome(cat.Area,cat.Area.Left-10000,cat.Area.Bottom);
                Rectangle sitting=RunningScale.VisibleBounds(art.Get("sit",0));double scale=size/(double)art.Manifest.width;
                double leftGap=cat.X+(sitting.Left-art.Manifest.foot_anchor[0])*scale-cat.Area.Left;
                if(leftGap>12)throw new Exception("Seated Odin still stops far from the left edge.");
                cat.MoveHome(cat.Area,cat.Area.Right+10000,cat.Area.Bottom);
                double rightGap=cat.Area.Right-(cat.X+(sitting.Right-art.Manifest.foot_anchor[0])*scale);
                if(rightGap>12)throw new Exception("Seated Odin still stops far from the right edge.");
                cat.X=cat.MinX;cat.Rest("run_right",60);cat.Clamp();
                if(cat.X<cat.MinX-.01)throw new Exception("Changing to a wider pose did not clamp.");
                cat.Rest("sit",60);cat.Area=new Rectangle(-100,0,300,900);cat.X=-5000;cat.Clamp();
                if(cat.MinX>cat.MaxX+.01 || Double.IsNaN(cat.X))throw new Exception("Narrow monitor bounds are invalid.");
            }
            // Zoomies must not run in place against a screen edge (targets picked with sitting margins).
            foreach(int size in new[]{480,640,800})foreach(bool fromLeft in new[]{true,false})
            {
                CatMind cat=new CatMind(new Rectangle(0,0,1920,1080),11);
                cat.VisibleMargins=delegate(string clip){return new PointF(clip.StartsWith("run")?size*.62f:size*.22f,clip.StartsWith("run")?size*.62f:size*.22f);};
                cat.X=fromLeft?cat.MinX:cat.MaxX;cat.Zoomies();
                double stalled=0,lastX=cat.X;
                for(int i=0;i<600 && cat.Clip.StartsWith("run");i++)
                {
                    cat.Step(.1,Point.Empty);
                    if(cat.Clip.StartsWith("run")&&Math.Abs(cat.X-lastX)<.01){stalled+=.1;if(stalled>.35)throw new Exception("Zoomies ran in place against an edge.");}
                    else stalled=0;
                    lastX=cat.X;
                }
            }
            PointF small=edges.Margins("sit",480);
            return new{passed=true,framePlacementsChecked=checkedFrames,defaultSittingLeftMargin=small.X,
                previousLeftMargin=480*RunningScale.LeftMargin,allSizes=true,negativeMonitor=true,
                dragBothEdges=true,poseChanges=true,narrowMonitor=true,stableGazeAndTailEnvelope=true};
        }
    }
}
