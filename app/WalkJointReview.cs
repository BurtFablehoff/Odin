#if ODIN_JOINT_REVIEW
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: AssemblyTitle("Odin Walk Joint Review")]
[assembly: AssemblyVersion("3.1.2.7")]

namespace OdinV2
{
    public sealed class JointLeg { public string side,evidence;public int markedPixels;public double[] foot,observedPaw,poseOffset;public double[][] jointPath; }
    public sealed class JointFrame { public int number;public double seconds;public string source,diagram;public Dictionary<string,JointLeg> legs;public string[] drawOrder; }
    public sealed class JointManifest { public int version,frameMilliseconds,rearLeftForwardOffset,frontLeftBackOffset;public JointFrame[] frames;public string scope,appearanceMaster,appearancePhoto;public double[] hipAnchor,shoulderAnchor; }
    internal sealed class JointReviewData : IDisposable
    {
        public readonly JointManifest Manifest;
        private readonly Dictionary<string,Bitmap> images=new Dictionary<string,Bitmap>();
        public JointReviewData()
        {
            using(Stream resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("Odin.WalkReview"))
            {
                if(resource==null)throw new InvalidDataException("The embedded walking references are missing.");
                using(ZipArchive zip=new ZipArchive(resource,ZipArchiveMode.Read))
                {
                    ZipArchiveEntry manifest=zip.GetEntry("manifest.json");
                    using(StreamReader reader=new StreamReader(manifest.Open()))Manifest=new JavaScriptSerializer().Deserialize<JointManifest>(reader.ReadToEnd());
                    foreach(ZipArchiveEntry entry in zip.Entries)
                    {
                        if(!entry.FullName.EndsWith(".png") && !entry.FullName.EndsWith(".jpg"))continue;
                        using(Stream stream=entry.Open())using(Bitmap image=new Bitmap(stream))images.Add(entry.FullName,new Bitmap(image));
                    }
                }
            }
            if(Manifest==null || Manifest.version!=5 || Manifest.frames.Length!=32)throw new InvalidDataException("Expected 32 adjusted RL and FL review poses.");
        }
        private static double Distance(double[] a,double[] b){return Math.Sqrt(Math.Pow(a[0]-b[0],2)+Math.Pow(a[1]-b[1],2));}
        public Bitmap Get(string name){return images[name];}
        public object Verify()
        {
            int legs=0,frames=0;
            foreach(JointFrame frame in Manifest.frames)
            {
                if(frame.number!=++frames || frame.drawOrder.Length!=5 || String.Join(",",frame.drawOrder)!="FL,RL,BODY,RR,FR")throw new Exception("Frame order or far/body/near depth order changed.");
                foreach(string id in new[]{"FR","FL","RL","RR"})
                {
                    if(!frame.legs.ContainsKey(id))throw new Exception("Missing leg identity: "+id);
                    JointLeg leg=frame.legs[id];bool rear=id.StartsWith("R");double[] anchor=rear?Manifest.hipAnchor:Manifest.shoulderAnchor;
                    if(leg.jointPath==null || leg.jointPath.Length<1 || Distance(leg.jointPath[0],anchor)>.001)throw new Exception("Leg does not begin at shared attachment: "+id);
                    if(leg.observedPaw!=null)
                    {
                        double shift=id=="RL"?Manifest.rearLeftForwardOffset:id=="FL"?Manifest.frontLeftBackOffset:0;
                        if(leg.poseOffset==null || leg.poseOffset[0]!=shift || leg.poseOffset[1]!=0)throw new Exception("Unexpected pose adjustment: "+id);
                        double[] target={leg.observedPaw[0]+shift,leg.observedPaw[1]};
                        if(Distance(leg.jointPath[leg.jointPath.Length-1],target)>.001)throw new Exception("Requested paw position changed: "+id);
                    }
                    legs++;
                }
                if(Get(frame.source).Size!=new Size(382,264) || Get(frame.diagram).Size!=new Size(382,264))throw new Exception("Frame registration dimensions changed.");
            }
            if(Get(Manifest.appearanceMaster).Width<100 || Get(Manifest.appearancePhoto).Width<100)throw new Exception("Odin appearance references are missing.");
            return new{passed=true,videoFrames=frames,jointGuideFrames=frames,legIdentityRecords=legs,sharedHipAndShoulder=true,rearLeftForwardOffset=Manifest.rearLeftForwardOffset,frontLeftBackOffset=Manifest.frontLeftBackOffset,nearVideoPawsPreserved=true,allImagesEmbedded=true,correctDepthOrder=true,scope=Manifest.scope};
        }
        public void Dispose(){foreach(Bitmap image in images.Values)image.Dispose();images.Clear();}
    }
    internal sealed class WalkJointReviewWindow : Form
    {
        private readonly JointReviewData data;
        private readonly PictureBox source=new PictureBox(),guide=new PictureBox();
        private readonly Label frameLabel=new Label(),status=new Label();
        private readonly Button play=new Button();private readonly TrackBar slider=new TrackBar();
        private readonly ComboBox speed=new ComboBox();private readonly Timer timer=new Timer();
        private int index;private bool updating;
        public WalkJointReviewWindow(JointReviewData references)
        {
            data=references;Text="Odin - walk review v5 - FL moved back";Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterScreen;
            ClientSize=new Size(1120,700);MinimumSize=new Size(820,620);AutoScaleMode=AutoScaleMode.Dpi;
            TableLayoutPanel layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=7,Padding=new Padding(12)};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            foreach(float height in new float[]{36,76,0,60,138,52,26})layout.RowStyles.Add(height==0?new RowStyle(SizeType.Percent,100):new RowStyle(SizeType.Absolute,height));
            Label title=new Label{Text="Walk review v5 - orange FL moved back; shared shoulder remains fixed",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,Font=new Font(Font,FontStyle.Bold)};
            layout.Controls.Add(title,0,0);layout.SetColumnSpan(title,2);
            FlowLayoutPanel controls=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true};
            Button previous=new Button{Text="Previous",AutoSize=true},next=new Button{Text="Next",AutoSize=true};
            play.Text="Play";play.AutoSize=true;
            previous.Click+=delegate{Stop();Select(index-1);};next.Click+=delegate{Stop();Select(index+1);};
            play.Click+=delegate{if(timer.Enabled)Stop();else{timer.Start();play.Text="Pause";}};
            speed.DropDownStyle=ComboBoxStyle.DropDownList;speed.Width=150;speed.Items.AddRange(new object[]{"Recorded timing","Half speed"});speed.SelectedIndex=0;
            speed.SelectedIndexChanged+=delegate{timer.Interval=data.Manifest.frameMilliseconds*(speed.SelectedIndex==1?2:1);};
            frameLabel.AutoSize=true;frameLabel.Padding=new Padding(8,7,0,0);
            slider.Minimum=0;slider.Maximum=31;slider.TickFrequency=1;slider.Width=335;slider.Height=42;
            slider.ValueChanged+=delegate{if(!updating){Stop();Select(slider.Value);}};
            controls.Controls.AddRange(new Control[]{previous,play,next,speed,frameLabel,slider});layout.Controls.Add(controls,0,1);layout.SetColumnSpan(controls,2);
            layout.Controls.Add(ImageGroup("Original video - your leg markings",source),0,2);
            layout.Controls.Add(ImageGroup("Leg mode - shared H hip / S shoulder",guide),1,2);
            Label legend=new Label{Dock=DockStyle.Fill,Text="FR = front right (blue)   FL = front left (orange)   RL = rear left (green)   RR = rear right (purple)\nH: both rear legs share the hip. S: both front legs share the shoulder.\nVideo markings remain your original left-green / right-red.",AutoSize=false};
            layout.Controls.Add(legend,0,3);layout.SetColumnSpan(legend,2);
            PictureBox appearance=new PictureBox{Image=data.Get(data.Manifest.appearanceMaster)},photo=new PictureBox{Image=data.Get(data.Manifest.appearancePhoto)};
            layout.Controls.Add(ImageGroup("Odin's approved body - appearance reference",appearance),0,4);
            layout.Controls.Add(ImageGroup("Your Odin photo - face and proportions",photo),1,4);
            status.Dock=DockStyle.Fill;status.AutoSize=false;layout.Controls.Add(status,0,5);layout.SetColumnSpan(status,2);
            Label footer=new Label{Dock=DockStyle.Fill,Text="Approval scope: orange FL moved back 8px with its shared shoulder fixed. Finished furred sprites come next."};
            layout.Controls.Add(footer,0,6);layout.SetColumnSpan(footer,2);Controls.Add(layout);
            timer.Interval=data.Manifest.frameMilliseconds;timer.Tick+=delegate{Select(index+1);};
            FormClosed+=delegate{timer.Stop();timer.Dispose();};Select(0);
        }
        private static GroupBox ImageGroup(string label,PictureBox picture)
        {
            GroupBox group=new GroupBox{Text=label,Dock=DockStyle.Fill,Padding=new Padding(6)};
            picture.Dock=DockStyle.Fill;picture.SizeMode=PictureBoxSizeMode.Zoom;picture.BackColor=Color.FromArgb(39,42,47);group.Controls.Add(picture);return group;
        }
        private void Stop(){timer.Stop();play.Text="Play";}
        private void Select(int frame)
        {
            index=(frame+32)%32;JointFrame item=data.Manifest.frames[index];
            source.Image=data.Get(item.source);guide.Image=data.Get(item.diagram);
            frameLabel.Text="Frame "+item.number.ToString("00")+" / 32";
            updating=true;slider.Value=index;updating=false;
            string note="";foreach(KeyValuePair<string,JointLeg> leg in item.legs)if(leg.Value.evidence!="owner trace")note+=leg.Key+": "+leg.Value.evidence+". ";
            status.Text="FL back 8px; RL forward 16px. Near limbs and pose timing retained. H hip / S shoulder remain shared.\n"+note;
        }
        public object VerifyControls(string screenshot)
        {
            for(int i=0;i<32;i++){Select(i);if(slider.Value!=i || source.Image==null || guide.Image==null || !frameLabel.Text.Contains((i+1).ToString("00")))throw new Exception("Frame selection failed.");}
            Select(31);Select(index+1);if(index!=0)throw new Exception("Loop boundary failed.");
            slider.Value=16;if(index!=16)throw new Exception("Slider did not select frame17.");
            speed.SelectedIndex=1;if(timer.Interval!=134)throw new Exception("Half-speed setting failed.");
            play.PerformClick();if(!timer.Enabled)throw new Exception("Playback did not start.");Stop();if(timer.Enabled)throw new Exception("Playback did not pause.");
            Select(4);Application.DoEvents();
            if(screenshot!=null)using(Bitmap image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(0,0,Width,Height));image.Save(screenshot,System.Drawing.Imaging.ImageFormat.Png);}
            return new{passed=true,framesSelected=32,slider=true,wrap=true,playPause=true,halfSpeed=true,screenshot=screenshot};
        }
    }
    internal static class WalkJointReview
    {
        [STAThread] public static int Main(string[] args)
        {
            try
            {
                using(JointReviewData data=new JointReviewData())
                {
                    if(args.Length>1 && args[0]=="--self-test"){File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(data.Verify()));return 0;}
                    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                    using(WalkJointReviewWindow window=new WalkJointReviewWindow(data))
                    {
                        if(args.Length>1 && args[0]=="--ui-test")
                        {
                            object result=null;Exception failure=null;
                            window.Shown+=delegate{try{result=window.VerifyControls(args.Length>2?args[2]:null);}catch(Exception e){failure=e;}finally{window.BeginInvoke(new Action(window.Close));}};
                            Application.Run(window);if(failure!=null)throw failure;File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(result));return 0;
                        }
                        Application.Run(window);
                    }
                }
                return 0;
            }
            catch(Exception error)
            {
                if(args.Length>1)File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(new{passed=false,error=error.ToString()}));
                else MessageBox.Show(error.Message,"Odin walk review");return 1;
            }
        }
    }
}
#endif
