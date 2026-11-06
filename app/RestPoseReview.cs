#if ODIN_REST_REVIEW
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;
[assembly: AssemblyTitle("Odin Sitting Pose Review")]
[assembly: AssemblyVersion("3.1.2.11")]
namespace OdinV2
{
    public sealed class RestPoseAsset { public string key,label; public int[] size,bounds; }
    public sealed class RestPoseManifest { public int version; public RestPoseAsset[] poses; }
    internal sealed class RestPoseCanvas : Control
    {
        public Bitmap Image; public Rectangle BoundsInImage;
        public RestPoseCanvas() { DoubleBuffered=true; BackColor=Color.FromArgb(40,43,48); }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e); if(Image==null)return;
            float height=Math.Min(ClientSize.Height*.86f,ClientSize.Width*.90f);
            float scale=height/BoundsInImage.Height;
            if(BoundsInImage.Width*scale>ClientSize.Width*.94f)scale=ClientSize.Width*.94f/BoundsInImage.Width;
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
        int index;
        public RestPoseReview()
        {
            using(Stream resource=Assembly.GetExecutingAssembly().GetManifestResourceStream("Odin.RestReview"))
            using(ZipArchive zip=new ZipArchive(resource,ZipArchiveMode.Read))
            {
                using(StreamReader reader=new StreamReader(zip.GetEntry("manifest.json").Open()))manifest=new JavaScriptSerializer().Deserialize<RestPoseManifest>(reader.ReadToEnd());
                foreach(ZipArchiveEntry entry in zip.Entries)if(entry.FullName.EndsWith(".png"))
                    using(Stream stream=entry.Open())using(Bitmap image=new Bitmap(stream))images.Add(entry.FullName,new Bitmap(image));
            }
            Verify();Text="Odin - new sitting poses v1";Font=new Font("Segoe UI",10);ClientSize=new Size(1100,680);MinimumSize=new Size(850,540);StartPosition=FormStartPosition.CenterScreen;
            TableLayoutPanel layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12),ColumnCount=2,RowCount=4};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,40));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,46));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,74));
            Label title=new Label{Dock=DockStyle.Fill,Text="Sitting pose review - likeness, orientation and proportions",Font=new Font(Font,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft};layout.Controls.Add(title,0,0);layout.SetColumnSpan(title,2);
            FlowLayoutPanel controls=new FlowLayoutPanel{Dock=DockStyle.Fill};choices.DropDownStyle=ComboBoxStyle.DropDownList;choices.Width=290;
            foreach(RestPoseAsset pose in manifest.poses)choices.Items.Add(pose.label);
            choices.SelectedIndexChanged+=delegate{SelectPose(choices.SelectedIndex);};
            Button previous=new Button{Text="Previous",AutoSize=true},next=new Button{Text="Next",AutoSize=true};
            previous.Click+=delegate{choices.SelectedIndex=(index+manifest.poses.Length-1)%manifest.poses.Length;};next.Click+=delegate{choices.SelectedIndex=(index+1)%manifest.poses.Length;};
            controls.Controls.AddRange(new Control[]{previous,choices,next});layout.Controls.Add(controls,0,1);layout.SetColumnSpan(controls,2);
            layout.Controls.Add(Group("New pose draft",current),0,2);layout.Controls.Add(Group("Existing Odin sitting sprite",baseline),1,2);
            status.Dock=DockStyle.Fill;layout.Controls.Add(status,0,3);layout.SetColumnSpan(status,2);Controls.Add(layout);
            RestPoseAsset reference=manifest.poses[0];baseline.Image=images[reference.key];baseline.BoundsInImage=Rect(reference.bounds);choices.SelectedIndex=1;
            FormClosed+=delegate{foreach(Bitmap image in images.Values)image.Dispose();};
        }
        static Rectangle Rect(int[] b){return new Rectangle(b[0],b[1],b[2],b[3]);}
        static GroupBox Group(string title,Control canvas){GroupBox group=new GroupBox{Text=title,Dock=DockStyle.Fill};canvas.Dock=DockStyle.Fill;group.Controls.Add(canvas);return group;}
        void SelectPose(int value)
        {
            index=value;RestPoseAsset pose=manifest.poses[index];current.Image=images[pose.key];current.BoundsInImage=Rect(pose.bounds);current.Invalidate();
            status.Text=pose.label+". Draft for visual approval; not installed in Odin.\nBoth views fit the same ear-to-ground height for comparison. Check head/body proportions, coat and tail.\nWalking/3D remain on hold. Scratching is removed from the upgrade plan.";
        }
        public object Verify()
        {
            if(manifest==null||manifest.version!=1||manifest.poses.Length!=4)throw new Exception("Expected baseline plus three sitting poses.");
            foreach(RestPoseAsset pose in manifest.poses)
            {
                Bitmap image=images[pose.key];Rectangle bounds=Rect(pose.bounds);
                if(image.Width!=pose.size[0]||image.Height!=pose.size[1]||bounds.Width<50||bounds.Height<50||!new Rectangle(Point.Empty,image.Size).Contains(bounds))throw new Exception("Invalid sprite registration: "+pose.key);
                if(image.GetPixel(0,0).A!=0)throw new Exception("Opaque background corner: "+pose.key);
            }
            return new{passed=true,baseline=1,newPoses=3,embeddedAssets=true,sourceDimensionsChecked=true,registrationBoundsChecked=true,artApprovalPending=true};
        }
        object VerifyControls(string screenshot)
        {
            for(int i=0;i<4;i++){choices.SelectedIndex=i;if(index!=i||current.Image==null)throw new Exception("Pose selection failed.");}
            choices.SelectedIndex=2;Application.DoEvents();using(Bitmap image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(0,0,Width,Height));image.Save(screenshot);}
            return new{passed=true,posesSelected=4,screenshot=screenshot};
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
