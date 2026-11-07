#if ODIN_CONTACT_REVIEW
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: AssemblyTitle("Odin Approved Contact Review")]
[assembly: AssemblyVersion("3.1.2.18")]
namespace OdinV2
{
    public sealed class ContactPose
    {
        public string key,label,sha256,notes,kind,phase;
        public int[] bounds,motionBounds;
        public int id,eyeFrame;
        public bool airborne;
        public float supportY,renderWidthAtSmall;
    }
    public sealed class ContactManifest { public ContactPose reference; public ContactPose[] poses,blinks,motions;public int reviewVersion; }
    internal sealed class ContactCanvas : Control
    {
        public Bitmap Sprite; public ContactPose Pose;
        public int DisplaySize=480; public bool WindowTop;
        public ContactCanvas(){DoubleBuffered=true;BackColor=Color.FromArgb(34,42,52);}
        public float Floor {get{return Height-65;}}
        public RectangleF Destination()
        {
            float centre=Pose.bounds[0]+Pose.bounds[2]/2f;
            return PetFramework.SpriteSupport.Place(Sprite.Size,new PointF(centre,Pose.supportY),
                new PointF(Width/2f,Floor),Pose.renderWidthAtSmall*DisplaySize/480f);
        }



        public void Fit(Size viewport)
        {
            if(Sprite==null)return;
            float scale=Pose.renderWidthAtSmall*DisplaySize/480f/Sprite.Width;
            int[] bounds=Pose.motionBounds??Pose.bounds;float centre=Pose.bounds[0]+Pose.bounds[2]/2f;
            float reach=Math.Max(centre-bounds[0],bounds[0]+bounds[2]-centre);
            Size=new Size(Math.Max(viewport.Width,(int)Math.Ceiling(2*reach*scale)+60),
                Math.Max(viewport.Height,(int)Math.Ceiling((Pose.supportY-bounds[1])*scale)+115));
            Invalidate();
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);if(Sprite==null)return;
            if(!Pose.airborne){
            using(Brush brush=new SolidBrush(WindowTop?Color.FromArgb(223,229,238):Color.FromArgb(21,27,36)))
                e.Graphics.FillRectangle(brush,0,Floor,Width,65);
            using(Pen pen=new Pen(Color.FromArgb(95,221,213),2))e.Graphics.DrawLine(pen,0,Floor,Width,Floor);
            using(Brush brush=new SolidBrush(Color.FromArgb(83,100,124)))
                for(int x=20;x<Width-10;x+=42)
                    if(WindowTop)e.Graphics.FillRectangle(brush,x,Floor+23,7,2);
                    else e.Graphics.FillRectangle(brush,x,Floor+22,15,17);
            }
            e.Graphics.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            e.Graphics.DrawImage(Sprite,Destination(),new Rectangle(Point.Empty,Sprite.Size),GraphicsUnit.Pixel);
        }

    }
    internal sealed class ContactReview : Form
    {
        readonly Dictionary<string,Bitmap> images=new Dictionary<string,Bitmap>();
        readonly Dictionary<string,Bitmap> eyeComposites=new Dictionary<string,Bitmap>();
        readonly Dictionary<string,Bitmap> motionComposites=new Dictionary<string,Bitmap>();
        readonly List<ContactPose> activeMotions=new List<ContactPose>();
        int motionPoseId=-1;bool updating;
        readonly ContactManifest manifest;
        readonly ContactCanvas current=new ContactCanvas(),reference=new ContactCanvas();
        readonly ComboBox poses=new ComboBox(),sizes=new ComboBox(),eyes=new ComboBox(),motion=new ComboBox();
        readonly CheckBox windowTop=new CheckBox();
        readonly Label status=new Label();
        readonly Panel currentHost=new Panel(),referenceHost=new Panel();
        public ContactReview()
        {
            using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Odin.ContactReview"))
            using(ZipArchive zip=new ZipArchive(stream,ZipArchiveMode.Read))
            {
                using(StreamReader reader=new StreamReader(zip.GetEntry("manifest.json").Open()))
                    manifest=new JavaScriptSerializer().Deserialize<ContactManifest>(reader.ReadToEnd());
                List<ContactPose> all=new List<ContactPose>(manifest.poses);all.Add(manifest.reference);
                if(manifest.blinks!=null)all.AddRange(manifest.blinks);
                if(manifest.motions!=null)all.AddRange(manifest.motions);
                foreach(ContactPose pose in all)
                {
                    byte[] bytes;
                    using(Stream input=zip.GetEntry(pose.key).Open())using(MemoryStream copy=new MemoryStream()){input.CopyTo(copy);bytes=copy.ToArray();}
                    using(SHA256 sha=SHA256.Create())
                        if(BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=pose.sha256)
                            throw new Exception("Approved source hash mismatch: "+pose.key);
                    using(MemoryStream input=new MemoryStream(bytes))using(Bitmap image=new Bitmap(input))images.Add(pose.key,new Bitmap(image));
                }
            }
            foreach(ContactPose blink in manifest.blinks)
                eyeComposites.Add(blink.key,BlinkEyeRenderer.Compose(images["state-"+blink.id.ToString("00")+".png"],images[blink.key],blink.id));
            foreach(ContactPose variant in manifest.motions)
                motionComposites.Add(variant.key,PoseVariantRenderer.Compose(images["state-"+variant.id.ToString("00")+".png"],images[variant.key],variant.id,variant.kind,1));
            Text="Odin - registered gaze, tail and eyelids - review v"+manifest.reviewVersion;ClientSize=new Size(1280,790);MinimumSize=new Size(1050,650);
            Font=new Font("Segoe UI",10);StartPosition=FormStartPosition.CenterScreen;
            TableLayoutPanel layout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12),ColumnCount=2,RowCount=4};
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,38));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,82));
            Label title=new Label{Dock=DockStyle.Fill,Text=manifest.poses.Length+" numbered states - revised sizes, latest fur and floor contact",Font=new Font(Font,FontStyle.Bold)};
            layout.Controls.Add(title,0,0);layout.SetColumnSpan(title,2);
            FlowLayoutPanel controls=new FlowLayoutPanel{Dock=DockStyle.Fill};poses.DropDownStyle=sizes.DropDownStyle=eyes.DropDownStyle=motion.DropDownStyle=ComboBoxStyle.DropDownList;
            poses.Width=280;sizes.Width=125;
            ConfigureChoice(poses);ConfigureChoice(sizes);ConfigureChoice(eyes);foreach(ContactPose pose in manifest.poses)poses.Items.Add(pose.label);
            eyes.Width=170;eyes.Items.AddRange(new object[]{"Eyes: open","Eyes: half closed","Eyes: closed"});eyes.SelectedIndexChanged+=delegate{UpdatePose();};
            ConfigureChoice(motion);motion.Width=160;motion.SelectedIndexChanged+=delegate{UpdatePose();};
            sizes.Items.AddRange(new object[]{"Small (480)","Medium (640)","Large (800)"});windowTop.Text="Window top";windowTop.AutoSize=true;
            poses.SelectedIndexChanged+=delegate{UpdatePose();};
            sizes.SelectedIndexChanged+=delegate{UpdatePose();};windowTop.CheckedChanged+=delegate{UpdatePose();};
            controls.Controls.AddRange(new Control[]{poses,sizes,eyes,motion,windowTop});layout.Controls.Add(controls,0,1);layout.SetColumnSpan(controls,2);
            layout.Controls.Add(Group("Latest artwork",currentHost,current),0,2);
            layout.Controls.Add(Group("Latest sitting - visual size reference",referenceHost,reference),1,2);
            status.Dock=DockStyle.Fill;layout.Controls.Add(status,0,3);layout.SetColumnSpan(status,2);Controls.Add(layout);
            reference.Pose=manifest.reference;reference.Sprite=images[manifest.reference.key];
            eyes.SelectedIndex=0;sizes.SelectedIndex=0;poses.SelectedIndex=0;Resize+=delegate{Fit();};Shown+=delegate{Fit();};
            FormClosed+=delegate{foreach(Bitmap image in motionComposites.Values)image.Dispose();foreach(Bitmap image in eyeComposites.Values)image.Dispose();foreach(Bitmap image in images.Values)image.Dispose();};
        }
        static GroupBox Group(string title,Panel host,ContactCanvas canvas)
        {
            GroupBox group=new GroupBox{Text=title,Dock=DockStyle.Fill};host.Dock=DockStyle.Fill;host.AutoScroll=true;
            host.Controls.Add(canvas);group.Controls.Add(host);return group;
        }
        static void ConfigureChoice(ComboBox choice)
        {
            choice.DrawMode=DrawMode.OwnerDrawFixed;choice.ItemHeight=22;
            choice.DrawItem+=delegate(object sender,DrawItemEventArgs e)
            {
                e.DrawBackground();if(e.Index<0)return;
                TextRenderer.DrawText(e.Graphics,Convert.ToString(choice.Items[e.Index]),e.Font,e.Bounds,e.ForeColor,
                    TextFormatFlags.Left|TextFormatFlags.VerticalCenter);e.DrawFocusRectangle();
            };
        }
        void Fit()
        {
            currentHost.AutoScrollPosition=referenceHost.AutoScrollPosition=Point.Empty;
            current.Fit(new Size(Math.Max(1,currentHost.ClientSize.Width-22),Math.Max(1,currentHost.ClientSize.Height-22)));
            reference.Fit(new Size(Math.Max(1,referenceHost.ClientSize.Width-22),Math.Max(1,referenceHost.ClientSize.Height-22)));
        }



        void UpdatePose()
        {
            if(updating||poses.SelectedIndex<0 || sizes.SelectedIndex<0)return;
            updating=true;try{
            ContactPose pose=manifest.poses[poses.SelectedIndex];current.Pose=pose;current.Sprite=images[pose.key];
            if(motionPoseId!=pose.id)
            {
                motionPoseId=pose.id;activeMotions.Clear();motion.Items.Clear();motion.Items.Add("Still");
                foreach(ContactPose variant in manifest.motions)if(variant.id==pose.id){activeMotions.Add(variant);motion.Items.Add(variant.label);}
                motion.SelectedIndex=0;motion.Enabled=activeMotions.Count>0;
            }
            eyes.Enabled=pose.id>=1&&pose.id<=3&&motion.SelectedIndex==0&&eyeComposites.ContainsKey("blink-"+pose.id+"-1.png");
            if(eyes.Enabled&&eyes.SelectedIndex>0)current.Sprite=eyeComposites["blink-"+pose.id+"-"+eyes.SelectedIndex+".png"];
            if(motion.SelectedIndex>0)current.Sprite=motionComposites[activeMotions[motion.SelectedIndex-1].key];
            current.DisplaySize=reference.DisplaySize=new[]{480,640,800}[sizes.SelectedIndex];current.WindowTop=reference.WindowTop=windowTop.Checked;
            status.Text=pose.notes+(motion.SelectedIndex>0?" | "+activeMotions[motion.SelectedIndex-1].label:"")+(pose.airborne?"\nAirborne sample; no floor contact.":"\nCyan is the floor. Only paws may overhang; #4 sleeping tail may also overhang.")+"\nStatic size/contact review; artwork is not installed in the companion.";
            Fit();
            }finally{updating=false;}
        }
        public object Verify()
        {
            if(manifest.poses.Length!=14)throw new Exception("Expected 14 active numbered states.");
            HashSet<int> ids=new HashSet<int>();
            int placements=0;
            foreach(ContactPose pose in manifest.poses)
            {
                if(pose.id==5||pose.id==9||pose.id==10||!ids.Add(pose.id))throw new Exception("Retired or duplicate review ID.");
                Bitmap image=images[pose.key];
                if(pose.supportY<=0||pose.supportY>=image.Height||image.GetPixel(0,0).A>8)throw new Exception("Invalid source/support: "+pose.key);
                foreach(int size in new[]{480,640,800})
                {
                    RectangleF rect=PetFramework.SpriteSupport.Place(image.Size,new PointF(0,pose.supportY),new PointF(-300,340),pose.renderWidthAtSmall*size/480f);
                    if(Math.Abs(rect.Top+rect.Height*pose.supportY/image.Height-340)>.0003)throw new Exception("Support moved.");
                    placements++;
                }
            }
            int eyelids=0;
            foreach(ContactPose blink in manifest.blinks)
            {
                BlinkEyeRenderer.Verify(images["state-"+blink.id.ToString("00")+".png"],eyeComposites[blink.key],blink.id);eyelids++;
            }
            int layerCases=0;
            foreach(ContactPose variant in manifest.motions)
                foreach(double amount in new[]{.25,.5,1.0})
                {
                    Bitmap basis=images["state-"+variant.id.ToString("00")+".png"];
                    using(Bitmap rendered=PoseVariantRenderer.Compose(basis,images[variant.key],variant.id,variant.kind,amount))
                        PoseVariantRenderer.Verify(basis,rendered,variant.id,variant.kind);
                    layerCases++;
                }
            return new{passed=true,poses=manifest.poses.Length,placementsChecked=placements,embeddedSourceHashesChecked=true,noAnimation=true,eyelidFrames=eyelids,pixelsOutsideEyelidsIdentical=true,silhouetteAlphaUnchanged=true,registeredMotionKeyframes=manifest.motions.Length,layerCases=layerCases,pixelsOutsideHeadOrTailIdentical=true,pawsProtected=true,supportMath=SpriteSupportTest.Run()};
        }
        object VerifyUI(string screenshot)
        {
            int cases=0;
            for(int p=0;p<manifest.poses.Length;p++)for(int s=0;s<3;s++)for(int w=0;w<2;w++)for(int eye=0;eye<(manifest.poses[p].id<=3?3:1);eye++)
            {
                poses.SelectedIndex=p;sizes.SelectedIndex=s;windowTop.Checked=w==1;eyes.SelectedIndex=eye;UpdatePose();Application.DoEvents();
                Bitmap expected=eye>0?eyeComposites["blink-"+manifest.poses[p].id+"-"+eye+".png"]:images[manifest.poses[p].key];
                if(current.Sprite!=expected||current.DisplaySize!=new[]{480,640,800}[s])throw new Exception("Controls failed.");
                RectangleF dest=current.Destination();float scale=dest.Width/current.Sprite.Width;
                int[] checkBounds=current.Pose.motionBounds??current.Pose.bounds;
                RectangleF visible=new RectangleF(dest.Left+checkBounds[0]*scale,dest.Top+checkBounds[1]*scale,checkBounds[2]*scale,checkBounds[3]*scale);
                if(visible.Left<0||visible.Right>current.Width||visible.Top<0||visible.Bottom>current.Height)throw new Exception("Sprite clipped.");cases++;
            }
            foreach(ContactPose variant in manifest.motions)
            {
                int index=Array.FindIndex(manifest.poses,delegate(ContactPose pose){return pose.id==variant.id;});poses.SelectedIndex=index;UpdatePose();
                motion.SelectedIndex=activeMotions.IndexOf(variant)+1;
                Rectangle bounds=PoseVariantRenderer.VisibleBounds(motionComposites[variant.key]);
                for(int size=0;size<3;size++)for(int surface=0;surface<2;surface++)
                {
                    sizes.SelectedIndex=size;windowTop.Checked=surface==1;UpdatePose();Application.DoEvents();
                    if(current.Sprite!=motionComposites[variant.key])throw new Exception("Wrong registered movement selected.");
                    RectangleF dest=current.Destination();float scale=dest.Width/current.Sprite.Width;
                    if(dest.Left+bounds.Left*scale<0||dest.Right-(current.Sprite.Width-bounds.Right)*scale>current.Width||dest.Top+bounds.Top*scale<0||dest.Top+bounds.Bottom*scale>current.Height)
                        throw new Exception("Registered movement clipped: "+variant.key);
                    cases++;
                }
            }
            poses.SelectedIndex=0;sizes.SelectedIndex=0;eyes.SelectedIndex=2;motion.SelectedIndex=0;windowTop.Checked=true;UpdatePose();Application.DoEvents();
            using(Bitmap first=new Bitmap(current.Width,current.Height))using(Bitmap later=new Bitmap(current.Width,current.Height))
            {
                current.DrawToBitmap(first,new Rectangle(Point.Empty,first.Size));
                System.Diagnostics.Stopwatch elapsed=System.Diagnostics.Stopwatch.StartNew();
                while(elapsed.ElapsedMilliseconds<700){Application.DoEvents();System.Threading.Thread.Sleep(10);}
                current.DrawToBitmap(later,new Rectangle(Point.Empty,later.Size));
                for(int y=0;y<first.Height;y++)for(int x=0;x<first.Width;x++)if(first.GetPixel(x,y)!=later.GetPixel(x,y))throw new Exception("Static pose moved.");
            }
            using(Bitmap bitmap=new Bitmap(Width,Height)){DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(screenshot);}
            return new{passed=true,controlCases=cases,allSizes=true,bothSurfaces=true,threeEyelidStates=true,registeredGazeAndTail=true,noSpriteClipping=true,staticFramesPixelIdentical=true,noAutomaticAnimation=true,screenshot=screenshot};
        }
        [STAThread] static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
                using(ContactReview form=new ContactReview())
                {
                    if(args.Length>1&&args[0]=="--self-test"){File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(form.Verify()));return 0;}
                    if(args.Length>1&&args[0]=="--export-blinks")
                    {
                        Directory.CreateDirectory(args[1]);
                        foreach(ContactPose pose in form.manifest.poses)if(pose.id<=3)
                            form.images[pose.key].Save(Path.Combine(args[1],"blink-"+pose.id+"-0.png"));
                        foreach(KeyValuePair<string,Bitmap> pair in form.eyeComposites)pair.Value.Save(Path.Combine(args[1],pair.Key));
                        return 0;
                    }
                    if(args.Length>1&&args[0]=="--export-variants")
                    {
                        Directory.CreateDirectory(args[1]);
                        foreach(ContactPose pose in form.manifest.poses)if(pose.id<=4)
                            form.images[pose.key].Save(Path.Combine(args[1],"base-"+pose.id+".png"));
                        foreach(ContactPose variant in form.manifest.motions)
                        {
                            bool diagonalOnly=args.Length>2&&args[2]=="diagonal";
                            if(diagonalOnly&&(variant.kind!="gaze"||!variant.phase.Contains("-")))continue;
                            for(int step=1;step<=4;step++)
                            {
                                if(diagonalOnly&&step!=4)continue;
                                using(Bitmap rendered=PoseVariantRenderer.Compose(form.images["state-"+variant.id.ToString("00")+".png"],form.images[variant.key],variant.id,variant.kind,step/4.0))
                                    rendered.Save(Path.Combine(args[1],variant.key.Replace(".png", "-"+step+".png")));
                            }
                        }
                        return 0;
                    }
                    if(args.Length>2&&args[0]=="--ui-test")
                    {
                        Exception failure=null;object report=null;
                        form.Shown+=delegate{try{report=form.VerifyUI(args[2]);}catch(Exception e){failure=e;}finally{form.BeginInvoke(new Action(form.Close));}};
                        Application.Run(form);if(failure!=null)throw failure;File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(report));return 0;
                    }
                    Application.Run(form);return 0;
                }
            }
            catch(Exception e){if(args.Length>1)File.WriteAllText(args[1],e.ToString());else MessageBox.Show(e.Message,"Odin contact review");return 1;}
        }
    }
}
#endif
