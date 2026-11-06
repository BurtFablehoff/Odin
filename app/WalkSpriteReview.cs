#if ODIN_SPRITE_REVIEW
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: AssemblyTitle("Odin Walking Sprite Review")]
[assembly: AssemblyVersion("3.1.2.9")]
namespace OdinV2
{
    // Fit every complete still to the same logical canvas. Generated PNG
    // dimensions may vary slightly; the display must not change body scale.
    internal sealed class RegisteredSpriteBox : PictureBox
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            if (Image == null) return;
            float scale = Math.Min(ClientSize.Width / 1512f, ClientSize.Height / 1040f);
            float width = 1512 * scale, height = 1040 * scale;
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(Image, new RectangleF((ClientSize.Width-width)/2, (ClientSize.Height-height)/2, width, height));
        }
    }
    internal sealed class SpriteReviewWindow : Form
    {
        readonly Dictionary<string, Bitmap> images = new Dictionary<string, Bitmap>();
        readonly PictureBox sprite = new RegisteredSpriteBox(), guide = new PictureBox();
        readonly TrackBar slider = new TrackBar();
        readonly Label status = new Label();
        readonly Button play = new Button();
        readonly Timer timer = new Timer();
        readonly ComboBox speed = new ComboBox();
        int index;
        public SpriteReviewWindow()
        {
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("Odin.SpriteReview"))
            using (ZipArchive zip = new ZipArchive(resource, ZipArchiveMode.Read))
                foreach (ZipArchiveEntry entry in zip.Entries)
                    if (entry.FullName.EndsWith(".png"))
                        using (Stream stream = entry.Open())
                        using (Bitmap bitmap = new Bitmap(stream)) images.Add(entry.FullName, new Bitmap(bitmap));
            VerifyAssets();
            Text = "Odin - furred walking review - frames 05 to 08";
            Font = new Font("Segoe UI", 10); ClientSize = new Size(1150, 640);
            MinimumSize = new Size(850, 520); StartPosition = FormStartPosition.CenterScreen;
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(12) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 72)); layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
            Label title = new Label { Dock = DockStyle.Fill, Text = "Walking sprites: short forward motion study (05-08)", Font = new Font(Font, FontStyle.Bold), TextAlign = ContentAlignment.MiddleLeft };
            layout.Controls.Add(title, 0, 0); layout.SetColumnSpan(title, 2);
            FlowLayoutPanel controls = new FlowLayoutPanel { Dock = DockStyle.Fill };
            Button previous = new Button { Text = "Previous", AutoSize = true }, next = new Button { Text = "Next", AutoSize = true };
            play.Text = "Play segment"; play.AutoSize = true;
            previous.Click += delegate { Stop(); SelectFrame(Math.Max(0, index - 1)); };
            next.Click += delegate { Stop(); SelectFrame(Math.Min(3, index + 1)); };
            play.Click += delegate { if (timer.Enabled) Stop(); else { if (index == 3) SelectFrame(0); timer.Start(); play.Text = "Pause"; } };
            speed.DropDownStyle = ComboBoxStyle.DropDownList; speed.Width = 150;
            speed.Items.AddRange(new object[] { "Recorded timing", "Half speed", "Slow inspection" }); speed.SelectedIndex = 1;
            speed.SelectedIndexChanged += delegate { timer.Interval = speed.SelectedIndex == 0 ? 67 : speed.SelectedIndex == 1 ? 134 : 400; };
            slider.Minimum = 0; slider.Maximum = 3; slider.Width = 220; slider.ValueChanged += delegate { Stop(); SelectFrame(slider.Value); };
            controls.Controls.AddRange(new Control[] { previous, play, next, speed, slider });
            layout.Controls.Add(controls, 0, 1); layout.SetColumnSpan(controls, 2);
            layout.Controls.Add(ImageGroup("Furred sprite", sprite), 0, 2); layout.Controls.Add(ImageGroup("Matching leg-placement guide", guide), 1, 2);
            status.Dock = DockStyle.Fill; layout.Controls.Add(status, 0, 3); layout.SetColumnSpan(status, 2); Controls.Add(layout);
            timer.Interval = 134; timer.Tick += delegate { if (index == 3) Stop(); else SelectFrame(index + 1); };
            FormClosed += delegate { timer.Dispose(); foreach (Bitmap image in images.Values) image.Dispose(); };
            SelectFrame(0);
        }
        static GroupBox ImageGroup(string name, PictureBox picture)
        {
            GroupBox group = new GroupBox { Text = name, Dock = DockStyle.Fill };
            picture.Dock = DockStyle.Fill; picture.SizeMode = PictureBoxSizeMode.Zoom; picture.BackColor = Color.FromArgb(40, 43, 48);
            group.Controls.Add(picture); return group;
        }
        void Stop() { timer.Stop(); play.Text = "Play segment"; }
        void SelectFrame(int value)
        {
            index = value; string number = (index + 5).ToString("00");
            sprite.Image = images["frame" + number + ".png"]; guide.Image = images["guide" + number + ".png"];
            // Update without invoking the user-slider stop handler during playback.
            if (slider.Value != index) { bool running = timer.Enabled; slider.Value = index; if (running) { timer.Start(); play.Text = "Pause"; } }
            status.Text = "Frame " + number + " / 05-08. FR blue / RR purple are near; FL orange / RL green are far.\n" +
                "Approved frame05 appearance; new frames06-08 await motion review. Playback stops at08: this is a segment, not a complete cycle.";
        }
        public object VerifyAssets()
        {
            for (int n = 5; n <= 8; n++)
            {
                Bitmap image = images["frame" + n.ToString("00") + ".png"];
                Size expected = (n == 6 || n == 7) ? new Size(1524, 1032) : new Size(1512, 1040);
                if (image.Size != expected) throw new Exception("Unexpected source dimensions at frame " + n);
                if (image.GetPixel(0, 0).A != 0) throw new Exception("Sprite corner is not transparent.");
                if (images["guide" + n.ToString("00") + ".png"].Size != new Size(382, 264)) throw new Exception("Guide registration differs.");
            }
            return new { passed = true, embeddedSprites = 4, embeddedGuides = 4, logicalCanvasWidth = 1512, logicalCanvasHeight = 1040, sourceDimensionsChecked = true, transparentCorners = true };
        }
        public object VerifyControls(string screenshot)
        {
            for (int i = 0; i < 4; i++) { slider.Value = i; if (index != i || sprite.Image == null || guide.Image == null) throw new Exception("Frame selection failed."); }
            play.PerformClick(); if (!timer.Enabled || index != 0) throw new Exception("Segment restart failed."); Stop();
            speed.SelectedIndex = 2; if (timer.Interval != 400) throw new Exception("Inspection speed failed.");
            SelectFrame(1); Application.DoEvents();
            using (Bitmap bitmap = new Bitmap(Width, Height)) { DrawToBitmap(bitmap, new Rectangle(0, 0, Width, Height)); bitmap.Save(screenshot); }
            return new { passed = true, framesSelected = 4, restart = true, playPause = true, slowInspection = true, screenshot = screenshot };
        }
        [STAThread] static int Main(string[] args)
        {
            try
            {
                Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
                using (SpriteReviewWindow window = new SpriteReviewWindow())
                {
                    if (args.Length > 1 && args[0] == "--self-test") { File.WriteAllText(args[1], new JavaScriptSerializer().Serialize(window.VerifyAssets())); return 0; }
                    if (args.Length > 2 && args[0] == "--ui-test")
                    {
                        Exception failure = null; object result = null;
                        window.Shown += delegate { try { result = window.VerifyControls(args[2]); } catch (Exception e) { failure = e; } finally { window.BeginInvoke(new Action(window.Close)); } };
                        Application.Run(window); if (failure != null) throw failure;
                        File.WriteAllText(args[1], new JavaScriptSerializer().Serialize(result)); return 0;
                    }
                    Application.Run(window); return 0;
                }
            }
            catch (Exception error) { if (args.Length > 1) File.WriteAllText(args[1], error.ToString()); else MessageBox.Show(error.Message); return 1; }
        }
    }
}
#endif
