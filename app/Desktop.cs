using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace OdinV2
{
    internal static class Native
    {
        [StructLayout(LayoutKind.Sequential)] public struct XY { public int X,Y; public XY(int x,int y){X=x;Y=y;} }
        [StructLayout(LayoutKind.Sequential)] public struct WH { public int W,H;public WH(int w,int h){W=w;H=h;} }
        [StructLayout(LayoutKind.Sequential)] public struct Rect { public int Left,Top,Right,Bottom; public Rectangle Value { get{return Rectangle.FromLTRB(Left,Top,Right,Bottom);} } }
        [StructLayout(LayoutKind.Sequential,Pack=1)] public struct Blend { public byte Op,Flags,Alpha,Format; }
        public delegate bool EnumProc(IntPtr hwnd,IntPtr param);
        [DllImport("user32.dll")] public static extern bool EnumWindows(EnumProc callback,IntPtr param);
        [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool IsIconic(IntPtr hwnd);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hwnd,int command);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern IntPtr FindWindow(string className,string title);
        [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam);
        [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd,out Rect rectangle);
        [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd,int attribute,out Rect rectangle,int size);
        [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr hwnd,int attribute,out int value,int size);
        [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint processId);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] public static extern int GetClassName(IntPtr hwnd,StringBuilder result,int max);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr window);
        [DllImport("user32.dll")] public static extern IntPtr WindowFromPoint(XY point);
        [DllImport("user32.dll")] public static extern IntPtr GetAncestor(IntPtr hwnd,uint flags);
        [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hwnd,int index);
        [DllImport("user32.dll")] public static extern short GetAsyncKeyState(int key);
        [DllImport("user32.dll")] public static extern int GetSystemMetrics(int index);
        [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr window);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr window,IntPtr dc);
        [DllImport("gdi32.dll")] public static extern IntPtr CreateCompatibleDC(IntPtr dc);
        [DllImport("gdi32.dll")] public static extern IntPtr SelectObject(IntPtr dc,IntPtr bitmap);
        [DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr obj);
        [DllImport("gdi32.dll")] public static extern bool DeleteDC(IntPtr dc);
        [DllImport("user32.dll")] public static extern bool UpdateLayeredWindow(IntPtr window,IntPtr screen,ref XY location,ref WH size,IntPtr source,ref XY origin,int key,ref Blend blend,int flags);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr icon);
        [DllImport("user32.dll")] public static extern uint GetGuiResources(IntPtr process,uint flags);
        public static bool Bounds(IntPtr hwnd,out Rectangle rectangle)
        {
            Rect rect;rectangle=Rectangle.Empty;
            if(hwnd==IntPtr.Zero || !IsWindowVisible(hwnd) || IsIconic(hwnd))return false;
            try {int cloaked;if(DwmGetWindowAttribute(hwnd,14,out cloaked,4)==0 && cloaked!=0)return false;}catch(DllNotFoundException){}
            try {if(DwmGetWindowAttribute(hwnd,9,out rect,Marshal.SizeOf(typeof(Rect)))==0){rectangle=rect.Value;return true;}}catch(DllNotFoundException){}
            if(!GetWindowRect(hwnd,out rect))return false;rectangle=rect.Value;return true;
        }
        public static List<WindowSurface> Surfaces(int ownProcess)
        {
            List<WindowSurface> results=new List<WindowSurface>();
            EnumWindows(delegate(IntPtr hwnd,IntPtr unused)
            {
                uint pid;GetWindowThreadProcessId(hwnd,out pid);if(pid==ownProcess)return true;
                if((GetWindowLong(hwnd,-20)&0x80)!=0)return true;
                Rectangle rect;if(!Bounds(hwnd,out rect) || rect.Width<300 || rect.Height<140)return true;
                StringBuilder kind=new StringBuilder(128);GetClassName(hwnd,kind,128);
                string cls=kind.ToString();if(cls=="Progman" || cls=="WorkerW" || cls.StartsWith("Shell_"))return true;
                // Only land on an exposed title-bar edge, never through another window.
                IntPtr top=GetAncestor(WindowFromPoint(new XY(rect.Left+rect.Width/2,rect.Top+12)),2);
                if(top!=hwnd){uint overlay;GetWindowThreadProcessId(top,out overlay);if(overlay!=ownProcess)return true;}
                results.Add(new WindowSurface(hwnd.ToInt64(),rect));return true;
            },IntPtr.Zero);
            return results;
        }
    }
    public sealed class Preferences
    {
        [ScriptIgnore] public bool Ephemeral;
        public int Size=640;public bool WindowPerches=true;public bool ClickPounces=true;public bool Watch=true;public bool AutoUpdates=true;
        public static string Folder { get {return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"OdinCompanionV3");} }
        public static Preferences Load()
        {
            try {Preferences p=new JavaScriptSerializer().Deserialize<Preferences>(File.ReadAllText(Path.Combine(Folder,"settings.json")));if(p!=null){p.Size=Math.Max(480,Math.Min(800,p.Size));return p;}}catch{}
            return new Preferences();
        }
        public void Save(){if(Ephemeral)return;try{Directory.CreateDirectory(Folder);File.WriteAllText(Path.Combine(Folder,"settings.json"),new JavaScriptSerializer().Serialize(this));}catch{}}
    }
    internal sealed class OdinWindow : Form
    {
        private readonly ArtPack art;private readonly CatMind mind;private readonly Preferences settings;
        private readonly Timer timer=new Timer();private readonly Stopwatch watch=Stopwatch.StartNew();
        private readonly ClickGesture clickGesture=new ClickGesture();
        private readonly SittingBehavior sitting;
        private readonly ContextMenuStrip menu=new ContextMenuStrip();private readonly NotifyIcon tray=new NotifyIcon();private Icon trayIcon;
        private readonly int ownProcess=Process.GetCurrentProcess().Id;
        private UpdateCoordinator updates;private double idleSuppressUntil;private readonly bool prototype;
        private double lastTime,windowsAt,watchAt,gazeWeight,gazeAngle,lastGazeWeight=-1,lastGazeAngle=-100;
        private string drawnClip="";private int drawnFrame=-1;private Point drawnLocation=new Point(int.MinValue,int.MinValue);
        private Rectangle foregroundBounds;private IntPtr foreground;
        private bool dragging,didDrag;private Point dragPointer;private double dragX,dragFoot;
        private double smokePoseAt;private int smokePose;
        private int drawnLook=-1;private double drawnBreath=-1;
        private int menuPreviousButtons;private IntPtr menuPreviousForeground;private bool menuTesting;
        private int size;private Bitmap canvas;private IntPtr screenDC,memoryDC;
        public long PaintedFrames;public double SmokeSeconds;
        public CatMind Mind {get{return mind;}}
        public OdinWindow(ArtPack assets,bool smoke,bool sittingPrototype=false)
        {
            art=assets;prototype=sittingPrototype;settings=smoke || prototype?new Preferences{Ephemeral=true}:Preferences.Load();size=settings.Size;
            sitting=new SittingBehavior(smoke?285:Environment.TickCount,art.Manifest.clips.ContainsKey("sit_eyes"));
            if(!smoke && !prototype)updates=new UpdateCoordinator(System.Reflection.Assembly.GetExecutingAssembly().Location);
            mind=new CatMind(Screen.FromPoint(Cursor.Position).WorkingArea,Environment.TickCount);
            mind.Margin=size*.5;mind.TopClearance=size*.46;mind.Clamp();
            mind.ConfigureArt(art.Manifest.clips.Keys);mind.PerchesEnabled=settings.WindowPerches && mind.HasJumps;mind.ClickPouncesEnabled=settings.ClickPounces && mind.HasPounces;
            mind.SetWatching(settings.Watch);mind.SittingPrototype=prototype;if(prototype)mind.Rest("sit",240);
            Text=prototype?"Odin - sitting prototype":Program.WindowTitle;FormBorderStyle=FormBorderStyle.None;ShowInTaskbar=false;TopMost=true;AutoScaleMode=AutoScaleMode.None;
            StartPosition=FormStartPosition.Manual;ClientSize=new Size(size,size);
            RecreateCanvas();BuildMenu();menu.AutoClose=true;
            menu.Opening+=delegate{menuPreviousForeground=Native.GetForegroundWindow();menuPreviousButtons=MouseButtonsDown();};
            menu.Opened+=delegate{if(!menuTesting){Native.SetForegroundWindow(menu.Handle);menu.Focus();}};
            menu.KeyDown+=delegate(object sender,KeyEventArgs key){if(key.KeyCode==Keys.Escape){menu.Close(ToolStripDropDownCloseReason.Keyboard);key.Handled=true;key.SuppressKeyPress=true;}};
            menu.Closed+=delegate
            {
                if(menuTesting)return;IntPtr current=Native.GetForegroundWindow();Rectangle prior;
                if((current==menu.Handle || current==Handle || current==IntPtr.Zero) && Native.Bounds(menuPreviousForeground,out prior))Native.SetForegroundWindow(menuPreviousForeground);
            };
            MouseDown+=CatDown;MouseMove+=CatMove;MouseUp+=CatUp;
            timer.Interval=16;timer.Tick+=Tick;
            Shown+=delegate
            {
                screenDC=Native.GetDC(IntPtr.Zero);memoryDC=Native.CreateCompatibleDC(screenDC);
                clickGesture.Observe((Native.GetAsyncKeyState(Native.GetSystemMetrics(23)!=0?2:1)&0x8000)!=0,Cursor.Position,watch.Elapsed.TotalSeconds);
                lastTime=watch.Elapsed.TotalSeconds;PaintSprite(true);timer.Start();
                if(!smoke)
                {
                    using(Bitmap small=new Bitmap(32,32,PixelFormat.Format32bppPArgb))
                    {
                        using(Graphics g=Graphics.FromImage(small)){g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(art.Get("sit",0),new Rectangle(0,0,32,32),art.HeadRect("sit"),GraphicsUnit.Pixel);}
                        IntPtr icon=small.GetHicon();using(Icon borrowed=Icon.FromHandle(icon))trayIcon=(Icon)borrowed.Clone();Native.DestroyIcon(icon);tray.Icon=trayIcon;
                    }
                    tray.Text=prototype?"Odin - sitting prototype":"Odin - right-click for his routine and settings";tray.ContextMenuStrip=menu;tray.Visible=true;if(!prototype)AutoUpdater.MarkHealthy();
                    tray.MouseClick+=delegate(object s,MouseEventArgs e){if(e.Button==MouseButtons.Left)ReturnHome();};
                }
            };
            FormClosed+=delegate
            {
                timer.Stop();timer.Dispose();tray.Visible=false;tray.Dispose();if(trayIcon!=null)trayIcon.Dispose();menu.Dispose();
                if(memoryDC!=IntPtr.Zero)Native.DeleteDC(memoryDC);if(screenDC!=IntPtr.Zero)Native.ReleaseDC(IntPtr.Zero,screenDC);if(canvas!=null)canvas.Dispose();
            };
            if(smoke)SmokeSeconds=12;
        }
        protected override bool ShowWithoutActivation {get{return true;}}
        protected override CreateParams CreateParams {get{CreateParams cp=base.CreateParams;cp.ExStyle|=0x80000|0x08000000|0x80;return cp;}}
        protected override void WndProc(ref Message m)
        {
            if(m.Msg==Program.BringHomeMessage){ReturnHome();m.Result=IntPtr.Zero;return;}
            if(m.Msg==0x21){m.Result=new IntPtr(3);return;}
            base.WndProc(ref m);
        }
        private void RecreateCanvas(){if(canvas!=null)canvas.Dispose();canvas=new Bitmap(size,size,PixelFormat.Format32bppPArgb);drawnFrame=-1;ClientSize=new Size(size,size);if(mind!=null){mind.Margin=size*.5;mind.TopClearance=size*.46;mind.Clamp();}}
        private void Item(string title,Action action){menu.Items.Add(title,null,delegate{action();});}
        private ToolStripMenuItem Toggle(string title,bool value,Action<bool> change)
        {
            ToolStripMenuItem item=new ToolStripMenuItem(title){Checked=value,CheckOnClick=true};item.CheckedChanged+=delegate{change(item.Checked);settings.Save();};menu.Items.Add(item);return item;
        }
        private void BuildMenu()
        {
            ToolStripMenuItem heading=new ToolStripMenuItem("Odin - a relaxed little companion"){Enabled=false};menu.Items.Add(heading);
            Toggle("Pause Odin",false,delegate(bool p){mind.Paused=p;});
            menu.Items.Add(new ToolStripSeparator());
            Item("Look at me",delegate{mind.LookAtYou();idleSuppressUntil=watch.Elapsed.TotalSeconds+30;});
            Item("Settle into a loaf",delegate{mind.Rest("loaf",150);});Item("Have a nap",delegate{mind.Rest("sleep",210);});
            Item("Wash a paw",mind.Groom);Item("Stretch",mind.Stretch);
            Item("Walk to a new spot",mind.Wander);Item("Zoomies!",mind.Zoomies);
            if(mind.HasCrab)Item("Playful sideways bounces",mind.CrabBounce);
            Item("Try a window perch",delegate{mind.UpdateWindows(Native.Surfaces(ownProcess));if(!mind.TryPerch(true)){tray.ShowBalloonTip(3500,"No low window ledge yet","Move a window down from the top of the screen so Odin has room to perch.",ToolTipIcon.Info);}});
            menu.Items.Add(new ToolStripSeparator());
            Toggle("Notice my mouse and moving windows",settings.Watch,delegate(bool v){settings.Watch=v;mind.SetWatching(v);});
            Toggle("Occasional window perches",settings.WindowPerches,delegate(bool v){settings.WindowPerches=v;mind.PerchesEnabled=v;if(!v)mind.LeavePerch();});
            if(mind.HasPounces)Toggle("Very occasional click pounces",settings.ClickPounces,delegate(bool v){settings.ClickPounces=v;mind.ClickPouncesEnabled=v;});
            ToolStripMenuItem sizes=new ToolStripMenuItem("Odin's size");menu.Items.Add(sizes);
            foreach(int choice in new[]{480,640,800})
            {
                int selected=choice;ToolStripMenuItem item=new ToolStripMenuItem(choice==480?"Small":choice==640?"Normal":"Large"){Checked=size==choice};sizes.DropDownItems.Add(item);
                item.Click+=delegate{size=selected;settings.Size=selected;settings.Save();foreach(ToolStripMenuItem sibling in sizes.DropDownItems)sibling.Checked=sibling==item;RecreateCanvas();PaintSprite(true);};
            }
            Item("Bring Odin here",ReturnHome);Item("Move to next monitor",delegate
            {
                Screen[] screens=Screen.AllScreens;int index=Array.FindIndex(screens,s=>s.WorkingArea==mind.Area);Rectangle area=screens[(index+1+screens.Length)%screens.Length].WorkingArea;
                mind.MoveHome(area,area.Left+area.Width*.6,area.Bottom);
            });
            if(updates!=null)
            {
                menu.Items.Add(new ToolStripSeparator());Item("Check for updates",delegate{updates.Check(true);tray.ShowBalloonTip(2000,"Odin updates","Checking for a newer Odin...",ToolTipIcon.Info);});
                Toggle("Automatic updates",settings.AutoUpdates,delegate(bool enabled){settings.AutoUpdates=enabled;});
            }
            menu.Items.Add(new ToolStripSeparator());Item("Quit Odin",Close);
        }
        private void ReturnHome(){Rectangle area=Screen.FromPoint(Cursor.Position).WorkingArea;mind.MoveHome(area,Cursor.Position.X,area.Bottom);PaintSprite(true);}
        private void CatDown(object sender,MouseEventArgs e)
        {
            if(e.Button==MouseButtons.Right){menu.Show(this,e.Location);return;}if(e.Button!=MouseButtons.Left)return;
            dragging=true;didDrag=false;dragPointer=Cursor.Position;dragX=mind.X;dragFoot=mind.FootY;Capture=true;
        }
        private void CatMove(object sender,MouseEventArgs e)
        {
            if(!dragging)return;Point point=Cursor.Position;int dx=point.X-dragPointer.X,dy=point.Y-dragPointer.Y;
            if(Math.Abs(dx)+Math.Abs(dy)>7)didDrag=true;
            if(didDrag)
            {
                Rectangle area=Screen.FromPoint(point).WorkingArea;
                mind.MoveHome(area,dragX+dx,area.Bottom);PaintSprite(true);
            }
        }
        private void CatUp(object sender,MouseEventArgs e)
        {
            if(e.Button!=MouseButtons.Left)return;dragging=false;Capture=false;
            if(!didDrag)mind.Notice(Cursor.Position,false);
        }
        private static int MouseButtonsDown()
        {
            return ((Native.GetAsyncKeyState(1)&0x8000)!=0?1:0)|((Native.GetAsyncKeyState(2)&0x8000)!=0?2:0)|((Native.GetAsyncKeyState(4)&0x8000)!=0?4:0);
        }
        private static bool MenuContains(ToolStripDropDown dropdown,Point point)
        {
            if(!dropdown.Visible)return false;if(dropdown.Bounds.Contains(point))return true;
            foreach(ToolStripItem item in dropdown.Items)
            {
                ToolStripDropDownItem child=item as ToolStripDropDownItem;
                if(child!=null && child.HasDropDownItems && child.DropDown.Visible && MenuContains(child.DropDown,point))return true;
            }
            return false;
        }
        private void DismissMenu(Point point,int buttons,bool escape)
        {
            if(menu.Visible)
            {
                if(escape)menu.Close(ToolStripDropDownCloseReason.Keyboard);
                else if((buttons&~menuPreviousButtons)!=0 && !MenuContains(menu,point))menu.Close(ToolStripDropDownCloseReason.AppClicked);
            }
            menuPreviousButtons=buttons;
        }
        internal object CheckMenuDismissal()
        {
            menuTesting=true;
            try
            {
                menu.Show(this,new Point(10,10));menuPreviousButtons=0;
                Point inside=new Point(menu.Left+30,menu.Top+30),outside=new Point(menu.Right+60,menu.Bottom+60);
                DismissMenu(inside,1,false);if(!menu.Visible)throw new Exception("A click inside the menu closed it prematurely.");
                DismissMenu(outside,0,false);DismissMenu(outside,1,false);if(menu.Visible)throw new Exception("Click outside did not dismiss the menu.");
                menu.Show(this,new Point(10,10));DismissMenu(inside,0,true);if(menu.Visible)throw new Exception("Escape did not dismiss the menu.");
                foreach(ToolStripItem item in menu.Items)
                {
                    ToolStripMenuItem child=item as ToolStripMenuItem;if(child==null || child.Text!="Odin's size")continue;
                    menu.Show(this,new Point(10,10));child.ShowDropDown();menuPreviousButtons=0;
                    Point nested=new Point(child.DropDown.Left+10,child.DropDown.Top+10);DismissMenu(nested,1,false);
                    if(!menu.Visible || !child.DropDown.Visible)throw new Exception("Submenu click was mistaken for outside click.");
                    menu.Close();break;
                }
                return new{passed=true,escapeCloses=true,outsideClickCloses=true,insideClickRetained=true,submenusRetained=true};
            }
            finally{menu.Close();menuTesting=false;}
        }
        private void Tick(object sender,EventArgs e)
        {
            double now=watch.Elapsed.TotalSeconds,dt=Math.Max(0,now-lastTime);lastTime=now;Point pointer=Cursor.Position;
            DismissMenu(pointer,MouseButtonsDown(),(Native.GetAsyncKeyState(27)&0x8000)!=0);
            if(SmokeSeconds>0)
            {
                if(now>=smokePoseAt)
                {
                    string[] checks={"sit","loaf","lay","sleep","groom","stretch","run_right","run_left","walk_right","walk_left"};
                    string name=checks[smokePose++%checks.Length];if(mind.Supports(name))mind.Rest(name,2);
                    smokePoseAt=now+.65;
                }
                mind.Notice(new PointF((float)(mind.X+250*Math.Sin(now*2)),(float)(mind.FootY-220-100*Math.Cos(now*1.7))),true);
            }
            if(!dragging && !menu.Visible)
            {
                // Sample the visible cat, excluding the large transparent canvas
                // and its low-alpha contact shadow.
                mind.ObserveHover(IsOverCat(pointer));
                double remaining=dt;
                while(remaining>0.000001){double step=Math.Min(.1,remaining);mind.Step(step,pointer);remaining-=step;}
            }
            bool down=(Native.GetAsyncKeyState(Native.GetSystemMetrics(23)!=0?2:1)&0x8000)!=0;
            if(clickGesture.Observe(down,pointer,now) && !dragging && !didDrag && !menu.Visible && !Bounds.Contains(pointer))mind.ClickReleased(pointer,false);
            if(!down && !dragging)didDrag=false;
            if(now>=windowsAt)
            {
                mind.UpdateWindows(Native.Surfaces(ownProcess));windowsAt=now+.25;
                Rectangle area=Screen.FromPoint(new Point((int)mind.X,(int)mind.FootY-1)).WorkingArea;
                if(!Screen.AllScreens.AnyArea(mind.Area))mind.MoveHome(area,area.Left+area.Width*.6,area.Bottom);
            }
            if(settings.Watch && now>=watchAt)
            {
                watchAt=now+.15;IntPtr window=Native.GetForegroundWindow();Rectangle rect;
                if(Native.Bounds(window,out rect))
                {
                    if(window==foreground && !foregroundBounds.IsEmpty && (Math.Abs(rect.Left-foregroundBounds.Left)+Math.Abs(rect.Top-foregroundBounds.Top)+Math.Abs(rect.Width-foregroundBounds.Width)+Math.Abs(rect.Height-foregroundBounds.Height)>4))mind.Notice(new PointF(rect.Left+rect.Width*.5f,rect.Top+Math.Min(80,rect.Height*.25f)),true);
                    foreground=window;foregroundBounds=rect;
                }
            }
            double target=mind.AttentionActive && settings.Watch && art.Manifest.clips.ContainsKey("look_"+mind.Clip)?1:0;
            gazeWeight+=(target-gazeWeight)*Math.Min(1,dt*5);
            if(target>0)
            {
                Rectangle head=art.HeadRect(mind.Clip);double headY=mind.FootY-(art.AnchorY(mind.Clip,0)-(head.Top+head.Height*.5))*size/art.Manifest.width;
                double dx=mind.AttentionTarget.X-mind.X,dy=mind.AttentionTarget.Y-headY;
                double a=Math.Atan2(dx,-dy);double delta=a-gazeAngle;while(delta>Math.PI)delta-=Math.PI*2;while(delta<-Math.PI)delta+=Math.PI*2;gazeAngle+=delta*Math.Min(1,dt*4);
            }
            if(!mind.Paused && !dragging && !menu.Visible)sitting.Step(dt,mind.Clip=="sit",mind.AttentionActive,mind.ViewerAcknowledgement || now<idleSuppressUntil);
            if(updates!=null && SmokeSeconds<=0)
            {
                updates.Tick(settings.AutoUpdates);string updateMessage=updates.TakeMessage();if(updateMessage!=null)tray.ShowBalloonTip(4500,"Odin updates",updateMessage,ToolTipIcon.Info);
                if(updates.Ready!=null && (settings.AutoUpdates || updates.ManualReady) && !dragging && !menu.Visible && CatMind.IsRest(mind.Clip))
                {
                    try{if(updates.Install()){Close();return;}}catch(Exception error){tray.ShowBalloonTip(4500,"Odin update kept this version",error.Message,ToolTipIcon.Info);}
                }
            }
            PaintSprite(false);
            if(SmokeSeconds>0 && now>=SmokeSeconds)Close();
        }
        private bool IsOverCat(Point point)
        {
            if(canvas==null || !Bounds.Contains(point))return false;
            int x=point.X-Left,y=point.Y-Top;
            return x>=0 && y>=0 && x<canvas.Width && y<canvas.Height && canvas.GetPixel(x,y).A>=80;
        }
        internal object CheckEyeRendering(string folder)
        {
            if(!art.Manifest.clips.ContainsKey("sit_eyes"))throw new Exception("Approved B35 eye frames are missing.");
            Directory.CreateDirectory(folder);mind.Rest("sit",3600);mind.SetWatching(false);mind.ObserveHover(false);idleSuppressUntil=watch.Elapsed.TotalSeconds+3600;
            double root=mind.X,floor=mind.FootY;
            for(int i=0;i<36000 && !sitting.Napping;i++)sitting.Step(.05,true,false,false);
            if(!sitting.Napping)throw new Exception("A seeded sitting nap never began.");
            sitting.Step(.12,true,false,false);PaintSprite(true);
            if(sitting.EyeFrame!=1 || drawnLook!=17)throw new Exception("The half-closed eye expression was not rendered.");
            Point contact=drawnLocation;canvas.Save(Path.Combine(folder,"half-closed.png"),ImageFormat.Png);
            sitting.Step(.16,true,false,false);PaintSprite(true);
            if(sitting.EyeFrame!=2 || drawnLook!=18 || drawnLocation!=contact)throw new Exception("Closing the eyes shifted the seated body.");
            canvas.Save(Path.Combine(folder,"closed.png"),ImageFormat.Png);
            mind.ObserveHover(true);sitting.Step(.35,true,false,true);PaintSprite(true);
            if(sitting.Napping || sitting.EyeFrame!=0 || drawnLook!=-1 || drawnLocation!=contact || mind.X!=root || mind.FootY!=floor)throw new Exception("Hover wake did not keep the same seated body and support.");
            canvas.Save(Path.Combine(folder,"hover-awake.png"),ImageFormat.Png);
            return new{passed=true,approvedBatch="35",embeddedEyeFramesRendered=true,halfClosed=true,closed=true,hoverWakesGently=true,bodyAndSupportStationary=true,session=Process.GetCurrentProcess().SessionId};
        }
        private void PaintSprite(bool force)
        {
            if(memoryDC==IntPtr.Zero)return;
            string clip=mind.Clip;int frame=mind.IsCrabBounce?0:art.Frame(clip,mind.IsAirborne?mind.Age/mind.Duration*art.Manifest.clips[clip].seconds:mind.AnimationAge);
            string look="look_"+clip;bool viewer=mind.ViewerAcknowledgement;
            bool looking=!viewer && mind.AttentionActive && settings.Watch && art.Manifest.clips.ContainsKey(look);
            // One complete connected pose at a time. No overlapping head silhouettes.
            int lookFrame=looking?(int)Math.Round((((-gazeAngle)/(2*Math.PI))%1+1)%1*16)%16:-1;
            // Cursor/window tracking above is unchanged and always takes priority.
            if(!viewer && !looking && clip=="sit" && watch.Elapsed.TotalSeconds>=idleSuppressUntil && sitting.Gaze.Frame!=16){looking=true;lookFrame=sitting.Gaze.Frame;}
            string displayClip=looking?look:clip;int displayFrame=looking?lookFrame:frame;
            if(clip=="sit" && !looking && sitting.EyeFrame>0 && art.Manifest.clips.ContainsKey("sit_eyes")){displayClip="sit_eyes";displayFrame=sitting.EyeFrame;}
            double scale=size/(double)art.Manifest.width;Point spriteLocation=new Point((int)Math.Round(mind.X-art.Manifest.foot_anchor[0]*scale),(int)Math.Round(mind.FootY-art.AnchorY(displayClip,displayFrame)*scale-mind.VisualLift*(size/480.0)));
            double shadowHeight=Math.Max(0,mind.ShadowSurfaceY-mind.FootY)+mind.VisualLift*(size/480.0);
            RectangleF shadow=GroundShadow.Bounds(art,clip,size,mind.X,mind.ShadowSurfaceY,shadowHeight);
            Rectangle scene=GroundShadow.SceneBounds(new Rectangle(spriteLocation,new Size(size,size)),shadow);Point location=scene.Location;
            Point spriteOffset=new Point(spriteLocation.X-scene.Left,spriteLocation.Y-scene.Top);

            double breath=(clip=="sleep" || clip=="belly_sleep")?(.5-.5*Math.Cos(mind.AnimationAge*Math.PI*2/8))*.35:0;
            int visualState=looking?lookFrame:displayClip=="sit_eyes"?16+displayFrame:-1;
            bool gazeChanged=visualState!=drawnLook || Math.Abs(breath-drawnBreath)>.03;
            if(!force && clip==drawnClip && frame==drawnFrame && location==drawnLocation && !gazeChanged)return;
            if(canvas.Width!=scene.Width || canvas.Height!=scene.Height){canvas.Dispose();canvas=new Bitmap(scene.Width,scene.Height,PixelFormat.Format32bppPArgb);}
            using(Graphics g=Graphics.FromImage(canvas))
            {
                g.CompositingMode=CompositingMode.SourceCopy;g.Clear(Color.Transparent);g.CompositingMode=CompositingMode.SourceOver;
                g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
                RectangleF localShadow=shadow;localShadow.Offset(-scene.Left,-scene.Top);GroundShadow.Draw(g,localShadow,shadowHeight);
                Bitmap sprite=art.Get(displayClip,displayFrame);
                if((clip=="sleep" || clip=="belly_sleep"))
                {
                    // One continuous image and a fixed lower edge. Less than half a pixel of breath.
                    g.DrawImage(sprite,new RectangleF(spriteOffset.X,spriteOffset.Y-(float)breath,size,(float)(size+breath)),new RectangleF(0,0,art.Manifest.width,art.Manifest.height),GraphicsUnit.Pixel);
                }
                else g.DrawImage(sprite,new Rectangle(spriteOffset.X,spriteOffset.Y,size,size),0,0,art.Manifest.width,art.Manifest.height,GraphicsUnit.Pixel);
            }
            IntPtr bitmap=canvas.GetHbitmap(Color.FromArgb(0));IntPtr old=Native.SelectObject(memoryDC,bitmap);
            try
            {
                Native.XY destination=new Native.XY(location.X,location.Y),origin=new Native.XY(0,0);Native.WH dimensions=new Native.WH(canvas.Width,canvas.Height);Native.Blend blend=new Native.Blend{Op=0,Flags=0,Alpha=255,Format=1};
                if(!Native.UpdateLayeredWindow(Handle,screenDC,ref destination,ref dimensions,memoryDC,ref origin,0,ref blend,2))throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }
            finally {Native.SelectObject(memoryDC,old);Native.DeleteObject(bitmap);}
            drawnClip=clip;drawnFrame=frame;drawnLocation=location;drawnLook=visualState;drawnBreath=breath;lastGazeAngle=gazeAngle;lastGazeWeight=gazeWeight;PaintedFrames++;
        }
    }
    internal static class ScreenExtensions
    {
        public static bool AnyArea(this Screen[] screens,Rectangle area){foreach(Screen s in screens)if(s.WorkingArea==area)return true;return false;}
    }
}
