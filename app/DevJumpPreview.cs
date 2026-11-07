#if ODIN_DEV
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Web.Script.Serialization;

namespace OdinV2
{
    // Headless capture of the real CatMind flight and the same latest-art placement used by Desktop.
    // This never creates a Form, Control, desktop window, or synthetic replacement trajectory.
    internal static class DevJumpPreview
    {
        const int Width=1280,Height=1000,Floor=900,FramesPerSecond=30;
        static Rectangle Visible(Bitmap image)
        {
            return PoseVariantRenderer.VisibleBounds(image);
        }
        static object Direction(ArtPack atlas,DevArtwork latest,string output,string clip,bool right)
        {
            if(!latest.HasActionFamily(clip))throw new Exception("Updated jump family is unavailable: "+clip);
            Rectangle area=new Rectangle(0,0,Width,Floor);
            Rectangle windowBounds=right?new Rectangle(700,550,450,300):new Rectangle(130,550,450,300);
            long windowId=right?701:702;
            CatMind mind=new CatMind(area,17);
            mind.ConfigureArt(atlas.Manifest.clips.Keys);
            ScreenEdges originalMargins=new ScreenEdges(atlas);
            mind.VisibleMargins=delegate(string current)
            {
                PointF margins;
                return latest.TryMargins(current,480,0,out margins)?margins:originalMargins.Margins(current,480);
            };
            mind.PerchVisibleMargins=delegate(string current){return latest.PerchMargins(current,480);};
            mind.WatchEnabled=false;
            mind.MoveHome(area,right?350:930,Floor);
            mind.UpdateWindows(new List<WindowSurface>{new WindowSurface(windowId,windowBounds)});
            if(!mind.TryPerch(true)||mind.Clip!=clip)throw new Exception("Real perch flight did not select expected direction: "+clip);
            string directory=Path.Combine(output,clip);Directory.CreateDirectory(directory);
            Dictionary<string,Rectangle> nativeBounds=new Dictionary<string,Rectangle>();
            HashSet<int> indices=new HashSet<int>();List<object> captures=new List<object>();
            int captureCount=(int)Math.Ceiling((mind.Duration+.2)*FramesPerSecond)+1;double maximumOvershoot=0;
            for(int number=0;number<captureCount;number++)
            {
                // Match Desktop.PaintSprite: clamp and select its normalized airborne timeline.
                mind.ClampHorizontal();mind.Clamp();
                string selectedClip=mind.Clip;
                double timeline=mind.IsAirborne?mind.Age/mind.Duration*atlas.Manifest.clips[selectedClip].seconds:mind.AnimationAge;
                int frameNumber=atlas.Frame(selectedClip,timeline);
                DevArtFrame frame;
                if(!latest.TryGet(selectedClip,frameNumber,out frame)||frame==null)
                    throw new Exception("Capture would fall back to old artwork: "+selectedClip+":"+frameNumber);
                if(mind.IsAirborne)
                {
                    if(selectedClip!=clip||frame.Pose.clip!=clip||frame.Pose.frame!=frameNumber)
                        throw new Exception("Capture selected mismatched updated jump art.");
                    indices.Add(frameNumber);
                }
                Rectangle destination=frame.Destination(480,mind.X,mind.FootY,mind.VisualLift);
                string key=selectedClip+":"+frameNumber;Rectangle bounds;
                if(!nativeBounds.TryGetValue(key,out bounds)){bounds=Visible(frame.Image);nativeBounds.Add(key,bounds);}
                float scaleX=destination.Width/(float)frame.Image.Width,scaleY=destination.Height/(float)frame.Image.Height;
                RectangleF visible=new RectangleF(destination.Left+bounds.Left*scaleX,destination.Top+bounds.Top*scaleY,
                    bounds.Width*scaleX,bounds.Height*scaleY);
                double overshoot=Math.Max(Math.Max(-visible.Left,visible.Right-Width),Math.Max(-visible.Top,visible.Bottom-Height));
                maximumOvershoot=Math.Max(maximumOvershoot,overshoot);
                if(overshoot>.01)throw new Exception("Actual updated jump sprite clips the capture canvas: "+clip+":"+frameNumber);
                if(Math.Abs(mind.FootY-windowBounds.Top)<.01&&(mind.PerchedWindow==windowId||!frame.Pose.airborne))
                    if(visible.Left<windowBounds.Left-1||visible.Right>windowBounds.Right+1)
                        throw new Exception("Supported body or tail extends past the window top: "+clip+":"+frameNumber);
                string name=number.ToString("000")+".png";
                using(Bitmap capture=new Bitmap(Width,Height,PixelFormat.Format32bppArgb))
                using(Graphics graphics=Graphics.FromImage(capture))
                {
                    graphics.Clear(Color.FromArgb(34,42,52));
                    using(Brush windowBrush=new SolidBrush(Color.FromArgb(61,73,88)))graphics.FillRectangle(windowBrush,windowBounds);
                    using(Brush floorBrush=new SolidBrush(Color.FromArgb(223,229,238)))graphics.FillRectangle(floorBrush,0,Floor,Width,Height-Floor);
                    using(Pen support=new Pen(Color.FromArgb(95,221,213),2))
                    {
                        graphics.DrawLine(support,0,Floor,Width,Floor);
                        graphics.DrawLine(support,windowBounds.Left,windowBounds.Top,windowBounds.Right,windowBounds.Top);
                    }
                    graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
                    graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
                    graphics.DrawImage(frame.Image,destination,new Rectangle(Point.Empty,frame.Image.Size),GraphicsUnit.Pixel);
                    capture.Save(Path.Combine(directory,name),ImageFormat.Png);
                }
                captures.Add(new{capture=number,timeSeconds=number/(double)FramesPerSecond,file=clip+"/"+name,
                    clip=selectedClip,frame=frameNumber,updatedArt=true,airborne=mind.IsAirborne,
                    x=mind.X,footY=mind.FootY,visualLift=mind.VisualLift,perchedWindow=mind.PerchedWindow,
                    destination=new[]{destination.Left,destination.Top,destination.Width,destination.Height},
                    nativeVisibleBounds=new[]{bounds.Left,bounds.Top,bounds.Width,bounds.Height},
                    visibleBounds=new[]{visible.Left,visible.Top,visible.Width,visible.Height}});
                mind.Step(1.0/FramesPerSecond,new Point(-10000,-10000));
            }
            if(indices.Count!=16)throw new Exception("Real flight did not visit all16updated frame indices: "+clip);
            if(mind.PerchedWindow!=windowId||mind.IsAirborne||Math.Abs(mind.FootY-windowBounds.Top)>.001)
                throw new Exception("Real CatMind flight did not land on the synthetic window surface.");
            int[] visited=new int[indices.Count];indices.CopyTo(visited);Array.Sort(visited);
            return new{clip=clip,passed=true,physicsSource="Real CatMind.TryPerch + CatMind.Step; Desktop normalized airborne atlas.Frame timing",
                framesPerSecond=FramesPerSecond,captureCount=captures.Count,jumpFrameIndices=visited,
                expectedWindowId=windowId,landedWindowId=mind.PerchedWindow,landedAtX=mind.X,landedFootY=mind.FootY,
                windowBounds=new[]{windowBounds.Left,windowBounds.Top,windowBounds.Width,windowBounds.Height},
                allUpdatedJumpFramesUsed=true,noOldArtworkFallback=true,noCanvasClipping=true,maximumCanvasOvershoot=maximumOvershoot,
                captures=captures};
        }
        public static object Run(ArtPack atlas,DevArtwork latest,string outDir)
        {
            string output=Path.GetFullPath(outDir);Directory.CreateDirectory(output);
            try
            {
                if(!latest.HasUpdatedJumps)throw new Exception("Both complete updated jump families are required for motion capture.");
                object right=Direction(atlas,latest,output,"jump_right",true);
                object left=Direction(atlas,latest,output,"jump_left",false);
                object report=new{passed=true,headless=true,windowsCreated=0,realCatMindPhysics=true,
                    canvas=new[]{Width,Height},floorY=Floor,framesPerSecond=FramesPerSecond,
                    changedSourceSpritePixels=false,directions=new[]{left,right}};
                File.WriteAllText(Path.Combine(output,"motion-report.json"),new JavaScriptSerializer{MaxJsonLength=4000000}.Serialize(report));
                return report;
            }
            catch(Exception error)
            {
                File.WriteAllText(Path.Combine(output,"motion-report.json"),new JavaScriptSerializer().Serialize(new{passed=false,headless=true,windowsCreated=0,error=error.ToString()}));
                throw;
            }
        }
    }
}
#endif
