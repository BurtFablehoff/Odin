#if ODIN_REST_REVIEW
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;
[assembly: AssemblyTitle("Odin Resting Pose Review")]
[assembly: AssemblyVersion("3.1.2.14")]
namespace OdinV2
{
    public sealed class RestPoseAsset { public string key,label,referenceKey; public string[] frames; public int[] size,bounds,referenceBounds; public double headWidth,referenceHeadWidth,scaleReference; }
    public sealed class RestPoseManifest { public int version; public bool approved; public string title,baselineLabel,reviewHint; public RestPoseAsset[] poses; }
    internal sealed class RestPoseCanvas : Control
    {
        public Bitmap Image; public Rectangle BoundsInImage;
        public double ReferenceHeadWidth,DisplayHeadWidth;
        public RestPoseCanvas() { DoubleBuffered=true; BackColor=Color.FromArgb(40,43,48); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); if(Image==null)return;
            float height=Math.Min(ClientSize.Height*.86f,ClientSize.Width*.90f);
            float scale=height/BoundsInImage.Height;
            if(BoundsInImage.Width*scale>ClientSize.Width*.94f)scale=ClientSize.Width*.94f/BoundsInImage.Width;
            if(ReferenceHeadWidth>0&&DisplayHeadWidth>0)scale=(float)(DisplayHeadWidth/ReferenceHeadWidth);
            float width=BoundsInImage.Width*scale,drawHeight=BoundsInImage.Height*scale;
            e.Graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(Image,new RectangleF((ClientSize.Width-width)/2,ClientSize.Height*.94f-drawHeight,width,drawHeight),BoundsInImage,GraphicsUnit.Pixel);
        }
    }
    internal sealed class RestPoseReview : Form
    {
        readonly Dictionary<string,Bitmap> images=new Dictionary<string,Bitmap>();
        readonly RestPoseManifest manifest;
        readonly RestPoseCanvas current=new RestPoseCanvas(),baseline=new RestPoseCanvas();
        readonly ComboBox choices=new ComboBox(); readonly Label status=new Label();
        readonly Timer breathing=new Timer(); readonly Button play=new Button(); readonly TrackBar frameSlider=new TrackBar(); readonly Label frameLabel=new Label();
        readonly SleepBreathing quietBreathing=new SleepBreathing(Environment.TickCount^0x2011);
        int index,frameIndex; bool updatingFrame;
        public RestPoseReview()
        {
            using(Stream resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("Odin.RestReview"))
            using(ZipArchive zip=new ZipArchive(resource,ZipArchiveMode.Read))
            {
                using(StreamReader reader=new StreamReader(zip.GetEntry("manifest.json").Open()))manifest=new JavaScriptSerializer().Deserialize<RestPoseManifest>(reader.ReadToEnd());
                foreach(ZipArchiveEntry entry in zip.Entries)if(entry.FullName.EndsWith(".png"))
                    using(Stream stream=entry.Open())using(Bitmap image=new Bitmap(stream))images.Add(entry.FullName,new Bitmap(image));
            }
            Verify();Text=manifest.title??"Odin - new sitting poses v1";Font=new Font("Segoe UI",10);ClientSize=new Size(1100,680);MinimumSize=new Size(850,540);StartPosition=FormStartPosition.CenterScreen;
            TableLayoutPanel layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12),ColumnCount=2,RowCount=4};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,40));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,manifest.version==3?88:46));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,74));
            Label title=new Label{Dock=DockStyle.Fill,Text=manifest.title??"Sitting pose review - likeness, orientation and proportions",Font=new Font(Font,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft};layout.Controls.Add(title,0,0);layout.SetColumnSpan(title,2);
            FlowLayoutPanel controls=new FlowLayoutPanel{Dock=DockStyle.Fill};choices.DropDownStyle=ComboBoxStyle.DropDownList;choices.Width=290;
            foreach(RestPoseAsset pose in manifest.poses)choices.Items.Add(pose.label);
            choices.SelectedIndexChanged+=delegate{SelectPose(choices.SelectedIndex);};
            Button previous=new Button{Text="Previous",AutoSize=true},next=new Button{Text="Next",AutoSize=true};
            previous.Click+=delegate{choices.SelectedIndex=(index+manifest.poses.Length-1)%manifest.poses.Length;};next.Click+=delegate{choices.SelectedIndex=(index+1)%manifest.poses.Length;};
            controls.Controls.AddRange(new Control[]{previous,choices,next});layout.Controls.Add(controls,0,1);layout.SetColumnSpan(controls,2);
            if(manifest.version==3)
            {
                play.Text="Play once";play.AutoSize=true;play.Click+=delegate{if(quietBreathing.Busy)StopBreathing();else{quietBreathing.PreviewOnce();SelectFrame(0);breathing.Start();play.Text="Stop preview";}};
                frameSlider.Width=210;frameSlider.ValueChanged+=delegate{if(!updatingFrame){StopBreathing();SelectFrame(frameSlider.Value);}};
                frameLabel.AutoSize=true;frameLabel.Padding=new Padding(4,7,0,0);
                controls.Controls.AddRange(new Control[]{play,frameSlider,frameLabel});
                breathing.Interval=400;breathing.Tick+=delegate{quietBreathing.Step(.4,manifest.poses[index].frames.Length>1,true);SelectFrame(quietBreathing.Frame);play.Text=quietBreathing.Busy?"Stop preview":"Play once";};
            }
            layout.Controls.Add(Group(manifest.version==3?"Occasional resting action":manifest.approved?"Approved pose - sizing review":"New pose draft",current),0,2);layout.Controls.Add(Group(manifest.baselineLabel??"Existing Odin sitting sprite",baseline),1,2);
            status.Dock=DockStyle.Fill;layout.Controls.Add(status,0,3);layout.SetColumnSpan(status,2);Controls.Add(layout);
            RestPoseAsset reference=manifest.poses[0];baseline.Image=images[reference.key];baseline.BoundsInImage=Rect(reference.bounds);baseline.ReferenceHeadWidth=ScaleReference(reference);choices.SelectedIndex=manifest.version==3?0:1;
            Resize+=delegate{UpdateScale();};Shown+=delegate{UpdateScale();};
            FormClosed+=delegate{breathing.Dispose();foreach(Bitmap image in images.Values)image.Dispose();};
        }
        static Rectangle Rect(int[] b){return new Rectangle(b[0],b[1],b[2],b[3]);}
        static double ScaleReference(RestPoseAsset pose){return pose.scaleReference>0?pose.scaleReference:pose.headWidth;}
        static GroupBox Group(string title,Control canvas){GroupBox group=new GroupBox{Text=title,Dock=DockStyle.Fill};canvas.Dock=DockStyle.Fill;group.Controls.Add(canvas);return group;}
        void SelectPose(int value)
        {
            index=value;RestPoseAsset pose=manifest.poses[index];current.Image=images[pose.key];current.BoundsInImage=Rect(pose.bounds);current.ReferenceHeadWidth=ScaleReference(pose);UpdateScale();
            if(manifest.version==3)
            {
                StopBreathing();baseline.Image=images[pose.referenceKey];baseline.BoundsInImage=Rect(pose.referenceBounds);baseline.ReferenceHeadWidth=pose.referenceHeadWidth;
                updatingFrame=true;frameSlider.Maximum=pose.frames.Length-1;frameSlider.Value=0;updatingFrame=false;
                SelectFrame(0);play.Enabled=pose.frames.Length>1;UpdateScale();
                if(play.Enabled){quietBreathing.Step(0,true,true);breathing.Start();}
            }
            status.Text=pose.label+(manifest.version==3?". Approved artwork; quiet timing review.":manifest.approved?". Artwork approved; checking registration and scale.":". Draft for visual approval; not installed in Odin.")+"\n"+(manifest.reviewHint??"Both views fit the same ear-to-ground height for comparison. Check head/body proportions, coat and tail.")+"\nWalking/3D remain on hold. Scratching is removed from the upgrade plan.";
        }
        void StopBreathing(){breathing.Stop();quietBreathing.Reset();play.Text="Play once";}
        void SelectFrame(int value)
        {
            frameIndex=value;RestPoseAsset pose=manifest.poses[index];current.Image=images[pose.frames[value]];current.Invalidate();
            updatingFrame=true;frameSlider.Value=value;updatingFrame=false;frameLabel.Text="Frame "+(value+1)+" / "+pose.frames.Length;
        }
        void UpdateScale()
        {
            if(current.ReferenceHeadWidth>0&&baseline.ReferenceHeadWidth>0)
            {
                double target=140;
                foreach(RestPoseCanvas canvas in new[]{current,baseline})
                {
                    Rectangle crop=canvas.BoundsInImage;
                    target=Math.Min(target,Math.Max(1,canvas.ClientSize.Width)*.92*canvas.ReferenceHeadWidth/crop.Width);
                    target=Math.Min(target,Math.Max(1,canvas.ClientSize.Height)*.86*canvas.ReferenceHeadWidth/crop.Height);
                }
                current.DisplayHeadWidth=baseline.DisplayHeadWidth=target;
            }
            current.Invalidate();baseline.Invalidate();
        }
        public object Verify()
        {
            if(manifest==null||manifest.version<1||manifest.version>3||manifest.poses==null||manifest.poses.Length<2||manifest.poses.Length>8||(manifest.version==3&&manifest.poses.Length!=3))throw new Exception("Unexpected resting review manifest.");
            int motionFrames=0,staticPoses=0;
            foreach(RestPoseAsset pose in manifest.poses)
            {
                Bitmap image=images[pose.key];Rectangle bounds=Rect(pose.bounds);
                if(image.Width!=pose.size[0]||image.Height!=pose.size[1]||bounds.Width<50||bounds.Height<50||!new Rectangle(Point.Empty,image.Size).Contains(bounds))throw new Exception("Invalid sprite registration: "+pose.key);
                if(image.GetPixel(0,0).A!=0)throw new Exception("Opaque background corner: "+pose.key);
                if(manifest.version>=2&&(pose.headWidth<50||pose.headWidth>bounds.Width))throw new Exception("Invalid head-size reference: "+pose.key);
                if(manifest.version==3)
                {
                    if(pose.frames==null||(pose.frames.Length!=8&&pose.frames.Length!=1))throw new Exception("Expected eight breathing frames or a static hold.");
                    foreach(string name in pose.frames)if(images[name].Size!=image.Size)throw new Exception("Breathing canvas changed within pose.");
                    Bitmap reference=images[pose.referenceKey];if(!new Rectangle(Point.Empty,reference.Size).Contains(Rect(pose.referenceBounds)))throw new Exception("Reference registration invalid.");
                    if(pose.frames.Length==1)staticPoses++;else motionFrames+=pose.frames.Length;
                }
            }
            return new{passed=true,poses=manifest.poses.Length,motionFrames=motionFrames,staticPoses=staticPoses,embeddedAssets=true,sourceDimensionsChecked=true,registrationBoundsChecked=true,artApprovalPending=!manifest.approved&&manifest.version!=3,quietPolicy=manifest.version==3?SleepBreathingTest.Run():null};
        }
        object VerifyControls(string screenshot)
        {
            int selected=0;
            for(int i=0;i<manifest.poses.Length;i++)
            {
                choices.SelectedIndex=i;if(index!=i||current.Image==null)throw new Exception("Pose selection failed.");
                if(manifest.version==3)for(int f=0;f<manifest.poses[i].frames.Length;f++){frameSlider.Value=f;if(frameIndex!=f)throw new Exception("Frame selection failed.");selected++;}
            }
            choices.SelectedIndex=manifest.version==3?0:Math.Min(2,manifest.poses.Length-1);
            int playedFrames=0;
            if(manifest.version==3)
            {
                breathing.Interval=25;play.PerformClick();if(!breathing.Enabled||!quietBreathing.Busy)throw new Exception("Playback failed.");
                HashSet<int> observed=new HashSet<int>();System.Diagnostics.Stopwatch elapsed=System.Diagnostics.Stopwatch.StartNew();
                while(elapsed.ElapsedMilliseconds<450){Application.DoEvents();observed.Add(frameIndex);System.Threading.Thread.Sleep(5);}
                StopBreathing();breathing.Interval=400;playedFrames=observed.Count;
                if(playedFrames!=8)throw new Exception("Timed playback did not visit the complete loop.");SelectFrame(4);
            }
            Application.DoEvents();using(Bitmap image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(0,0,Width,Height));image.Save(screenshot);}
            return new{passed=true,posesSelected=manifest.poses.Length,framesSelected=selected,timedPlaybackFrames=playedFrames,playPause=manifest.version==3,screenshot=screenshot};
        }
        [STAThread] static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                using(RestPoseReview window=new RestPoseReview())
                {
                    if(args.Length>1&&args[0]=="--self-test"){File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(window.Verify()));return 0;}
                    if(args.Length>2&&args[0]=="--ui-test")
                    {
                        Exception failure=null;object result=null;window.Shown+=delegate{try{result=window.VerifyControls(args[2]);}catch(Exception e){failure=e;}finally{window.BeginInvoke(new Action(window.Close));}};
                        Application.Run(window);if(failure!=null)throw failure;File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(result));return 0;
                    }
                    Application.Run(window);return 0;
                }
            }
            catch(Exception error){if(args.Length>1)File.WriteAllText(args[1],error.ToString());else MessageBox.Show(error.Message,"Odin sitting review");return 1;}
        }
    }
}
#endif
