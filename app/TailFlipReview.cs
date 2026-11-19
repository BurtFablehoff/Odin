#if ODIN_DEV
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace OdinV2
{
    internal static class TailFlipReview
    {
        static double[] TailMeasure(Bitmap image,int id)
        {
            Rectangle r=id==4?new Rectangle(230,780,290,190):new Rectangle(220,id==3?750:790,200,id==3?220:180);
            double alpha=0;int left=image.Width,right=-1;
            for(int x=r.Left;x<r.Right;x++)for(int y=r.Top;y<r.Bottom;y++){
                int a=image.GetPixel(x,y).A;alpha+=a;if(a>128){left=Math.Min(left,x);right=Math.Max(right,x);}
            }
            return new[]{alpha,(double)left,(double)right};
        }
        internal static int ProtectedDifferences(Bitmap a,Bitmap b,Rectangle allowed)
        {
            int count=0;
            for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++)if(!allowed.Contains(x,y)&&a.GetPixel(x,y).ToArgb()!=b.GetPixel(x,y).ToArgb())count++;
            return count;
        }
        internal static object Run(DevArtwork latest,string directory)
        {
            Directory.CreateDirectory(directory);int checkedFrames=0;int count=latest.TailFrameCount;double maxMassChange=0;var records=new System.Collections.Generic.List<object>();
            foreach(string family in new[]{"sit","lay","sleep"})
            {
                DevArtFrame neutral;latest.TryGet(family,0,out neutral);int id=neutral.Pose.id;
                using(Bitmap held=(Bitmap)neutral.Image.Clone())
                {
                    for(int f=0;f<count;f++)
                    {
                        DevArtFrame frame;latest.TryGet(family+"_tail",f,out frame);
                        if(ProtectedDifferences(frame.Image,held,latest.TailPixelRegion(id))!=0)throw new Exception("Tail flip changes protected body pixels: "+family);
                        if(id==3)for(int y=875;y<950;y++)for(int x=345;x<420;x++)if(frame.Image.GetPixel(x,y).ToArgb()!=held.GetPixel(x,y).ToArgb())throw new Exception("Tail flip changes the resting rear paw.");
                        if(frame.AnchorX!=neutral.AnchorX||frame.Pose.supportY!=neutral.Pose.supportY||frame.Scale(480)!=neutral.Scale(480))throw new Exception("Tail flip changes registration.");
                        int changed=0;Rectangle r=latest.TailPixelRegion(id);
                        for(int y=r.Top;y<r.Bottom;y++)for(int x=r.Left;x<r.Right;x++)if(frame.Image.GetPixel(x,y).ToArgb()!=held.GetPixel(x,y).ToArgb())changed++;
                        if((f==0||f==count-1)&&changed!=0)throw new Exception("Tail does not return exactly to neutral.");
                        if(f==count/2&&changed<2000)throw new Exception("Tail does not visibly move.");
                        if(latest.UsesSubtleTailTip){
                            double[] a=TailMeasure(held,id),b=TailMeasure(frame.Image,id);double change=Math.Abs(b[0]-a[0])/a[0]*100;
                            if(change>2||a[1]!=b[1]||a[2]!=b[2])throw new Exception("Tip flick thins or extends the resting tail: "+family+" "+change);
                            maxMassChange=Math.Max(maxMassChange,change);
                        }
                        foreach(bool dark in new[]{true,false})
                        {
                            string folder=Path.Combine(directory,family+(dark?"-dark":"-light"));Directory.CreateDirectory(folder);
                            using(Bitmap canvas=new Bitmap(560,380))using(Graphics g=Graphics.FromImage(canvas))
                            {
                                g.Clear(dark?Color.FromArgb(27,29,34):Color.FromArgb(245,245,242));
                                using(Brush band=new SolidBrush(dark?Color.FromArgb(50,53,60):Color.FromArgb(205,210,216)))g.FillRectangle(band,0,310,560,70);
                                g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                g.DrawImage(frame.Image,frame.Destination(480,260,310,0));
                                using(Font font=new Font("Segoe UI",11))using(Brush brush=new SolidBrush(dark?Color.White:Color.Black))g.DrawString(family+" - tail flip and return",font,brush,12,12);
                                canvas.Save(Path.Combine(folder,f.ToString("00")+".png"),ImageFormat.Png);
                            }
                            int[] delays=new int[count];for(int i=0;i<count;i++)delays[i]=(int)Math.Round((i+1)*70.0/count)-(int)Math.Round(i*70.0/count);delays[0]=65;delays[count-1]=70;
                            File.WriteAllText(Path.Combine(folder,"delays.txt"),String.Join(",",delays));
                        }
                        records.Add(new{family=family,frame=f,changedTailPixels=changed,protectedPixelsChanged=0});checkedFrames++;
                    }
                }
            }
            return new{passed=true,frames=checkedFrames,records=records,bodyHeadPawsUnchanged=true,exactNeutralReturn=true,subtleTipOnly=latest.UsesSubtleTailTip,maxTailAlphaAreaChangePct=maxMassChange,pixelChecksDoNotApproveAnatomy=true};
        }
    }
    internal sealed partial class OdinWindow
    {
        internal object CheckTailFlipRendering()
        {
            int automatic=0,manual=0;int count=latestArt.TailFrameCount;
            foreach(string family in new[]{"sit","lay","sleep"})
            {
                SelectDevState(family);for(int i=0;i<600&&mind.Clip!=family;i++)mind.Step(.02,Point.Empty);
                if(mind.Clip!=family)throw new Exception("Tail test did not reach held state.");
                devGaze=-1;TailFlick tail=TailFor(family);tail.Step(0,false,true);
                for(int i=0;i<20000&&!tail.Busy;i++)tail.Step(.01,true,true);
                bool[] seen=new bool[count];
                for(int i=0;i<100&&tail.Busy;i++)
                {
                    SpriteChoice choice=ChooseSprite(new Point(-20000,-20000));
                    if(tail.Frame>0&&(choice.DisplayClip!=family+"_tail"||choice.DisplayFrame!=tail.Frame))throw new Exception("Automatic tail selected the wrong frame.");
                    SpritePlacement placed=PlaceSprite(choice);if(placed.Latest==null)throw new Exception("Automatic tail used atlas fallback.");
                    seen[tail.Frame]=true;tail.Step(.01,true,true);
                }
                foreach(bool found in seen)if(!found)throw new Exception("Automatic tail skipped a motion frame.");
                automatic+=count;
                for(int f=0;f<count;f++)
                {
                    PreviewDevAnimation(family+"_tail",false,false,f);SpriteChoice choice=ChooseSprite(Point.Empty);SpritePlacement placed=PlaceSprite(choice);
                    if(placed.Latest==null||choice.DisplayFrame!=f||placed.Latest.Pose.frame!=f)throw new Exception("Manual tail used the wrong frame.");
                    SceneLayout layout=LayoutScene(choice,placed);
                    using(Bitmap output=new Bitmap(layout.Scene.Width,layout.Scene.Height))using(Graphics g=Graphics.FromImage(output))DrawScene(g,layout,placed.Latest.Image);
                    manual++;
                }
            }
            return new{passed=true,automaticFrames=automatic,manualFramesRendered=manual,noVisibleWindowOpened=true};
        }
    }
}
#endif
