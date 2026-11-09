using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace OdinV2
{
    // Floor rule (planning/RULES.md, F1-F3): the floor line is the underside of Odin's body; only his toes and tail hang past it,
    // and they hang OVER the taskbar, drawn on top of it. The taskbar is itself an always-on-top window, and Windows raises it
    // above Odin whenever the user touches it (Start, the tray, a pinned app); from then on the taskbar paints over his toes.
    // V12 3e (Hatt, 2026-11-08: "toes and tail used to hang over the edge"): every quarter second Odin looks at the z-order and
    // moves back above the taskbar, unless another always-on-top window above him overlaps him (the Start menu, a flyout, a
    // notification, a tooltip): those stay on top, so he never covers what the user just opened.
    internal sealed class TopWindow
    {
        public IntPtr Handle;public string Class;public Rectangle Bounds;public bool Topmost;public uint Process;
        public TopWindow(IntPtr handle,string cls,Rectangle bounds,bool topmost,uint process){Handle=handle;Class=cls;Bounds=bounds;Topmost=topmost;Process=process;}
    }
    internal static class TaskbarOrder
    {
        [DllImport("user32.dll")] static extern IntPtr GetTopWindow(IntPtr hwnd);
        [DllImport("user32.dll")] static extern IntPtr GetWindow(IntPtr hwnd,uint command);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int cx,int cy,uint flags);
        [DllImport("user32.dll")] static extern int GetClassName(IntPtr hwnd,StringBuilder name,int capacity);
        [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr hwnd,int index);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint process);
        const uint NextInZOrder=2,NoSize=1,NoMove=2,NoActivate=0x10;
        static readonly IntPtr Topmost=new IntPtr(-1);
        public static int Raised;
        public static bool IsTaskbar(string cls){return cls=="Shell_TrayWnd" || cls=="Shell_SecondaryTrayWnd";}
        // The decision on a z-ordered list, front window first: raise when a taskbar is above Odin and no other always-on-top
        // window of another process above him overlaps him. Pure, so --self-test checks it on made-up lists.
        public static bool ShouldRaise(IList<TopWindow> zOrder,IntPtr self,uint ownProcess,Rectangle selfBounds)
        {
            bool taskbarAbove=false;
            foreach(TopWindow w in zOrder)
            {
                if(w.Handle==self)return taskbarAbove;
                if(!w.Topmost || w.Process==ownProcess)continue;
                if(IsTaskbar(w.Class)){taskbarAbove=true;continue;}
                if(w.Bounds.Width>1 && w.Bounds.Height>1 && w.Bounds.IntersectsWith(selfBounds))return false;
            }
            return false;
        }
        // Visible, uncloaked top-level windows, front first (Native.Bounds leaves out hidden, minimised and cloaked windows).
        public static List<TopWindow> Snapshot()
        {
            List<TopWindow> result=new List<TopWindow>();
            for(IntPtr hwnd=GetTopWindow(IntPtr.Zero);hwnd!=IntPtr.Zero;hwnd=GetWindow(hwnd,NextInZOrder))
            {
                Rectangle bounds;if(!Native.Bounds(hwnd,out bounds))continue;
                StringBuilder name=new StringBuilder(128);GetClassName(hwnd,name,128);uint process;GetWindowThreadProcessId(hwnd,out process);
                result.Add(new TopWindow(hwnd,name.ToString(),bounds,(GetWindowLong(hwnd,-20)&8)!=0,process));
            }
            return result;
        }
        // Odin's quarter-second check; true when he moved back above the taskbar.
        public static bool Check(IntPtr self,uint ownProcess,Rectangle selfBounds)
        {
            if(!ShouldRaise(Snapshot(),self,ownProcess,selfBounds))return false;
            SetWindowPos(self,Topmost,0,0,0,0,NoSize|NoMove|NoActivate);Raised++;return true;
        }
        public static object Test()
        {
            IntPtr odin=new IntPtr(100),taskbar=new IntPtr(200),start=new IntPtr(300),menu=new IntPtr(400),helper=new IntPtr(500),app=new IntPtr(600);
            Rectangle cat=new Rectangle(1799,948,484,484);uint own=42;
            TopWindow tray=new TopWindow(taskbar,"Shell_TrayWnd",new Rectangle(0,1392,3440,48),true,4488);
            TopWindow self=new TopWindow(odin,"WindowsForms10.Window.8.app.0",cat,true,own);
            TopWindow startMenu=new TopWindow(start,"Windows.UI.Core.CoreWindow",new Rectangle(1420,560,600,832),true,9000);
            TopWindow ownMenu=new TopWindow(menu,"WindowsForms10.Window.20808.app.0",new Rectangle(1900,1100,240,300),true,own);
            TopWindow tiny=new TopWindow(helper,"ThumbnailDeviceHelperWnd",new Rectangle(0,0,1,1),true,4488);
            TopWindow plain=new TopWindow(app,"Chrome_WidgetWin_1",new Rectangle(0,0,3440,1392),false,7000);
            TopWindow popupAway=new TopWindow(start,"Xaml_WindowedPopupClass",new Rectangle(100,100,300,200),true,9000);
            List<object> cases=new List<object>();
            Action<string,bool,TopWindow[]> expect=delegate(string name,bool raise,TopWindow[] order)
            {
                bool got=ShouldRaise(new List<TopWindow>(order),odin,own,cat);
                if(got!=raise)throw new Exception("Taskbar order case '"+name+"': expected raise="+raise+", got "+got);
                cases.Add(new{name=name,raise=raise});
            };
            expect("taskbar above Odin: raise",true,new[]{tiny,tray,self,plain});
            expect("Odin already above the taskbar: leave",false,new[]{tiny,self,tray,plain});
            expect("Start menu open above Odin and over him: leave it on top",false,new[]{startMenu,tray,self,plain});
            expect("another topmost window above but not touching him: raise",true,new[]{popupAway,tray,self});
            expect("his own menu above him: raise past the taskbar anyway",true,new[]{ownMenu,tray,self});
            expect("a plain window above in the list: ignored",true,new[]{plain,tray,self});
            expect("no taskbar at all: leave",false,new[]{self,plain});
            expect("Odin not in the list: leave",false,new[]{tray,plain});
            return new{passed=true,cases=cases,rule="planning/RULES.md F1-F3: toes and tail hang over the taskbar edge, drawn on top of it"};
        }
    }
}
