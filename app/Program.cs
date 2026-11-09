using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace OdinV2
{
    internal static class Program
    {
#if ODIN_DEV
        internal const string WindowTitle="Odin DEV - latest registered artwork";
        private const string SingletonName="Local\\OdinCompanionDevLatestArtwork";
#else
        internal const string WindowTitle="Odin 3.2.0 - desktop companion";
        private const string SingletonName="Local\\OdinCompanionPortableV3";
#endif
        internal const int BringHomeMessage=0x8121;
        private static void Write(string path,object value){string json=new JavaScriptSerializer().Serialize(value);if(path==null)Console.WriteLine(json);else File.WriteAllText(path,json);}
        [STAThread] private static int Main(string[] args)
        {
            try
            {
                // Part 5c: the promoted pack's checks run on the production exe too.
                if(args.Length>1&&args[0]=="--test-body-stability"){using(DevArtwork latest=new DevArtwork()){object report=latest.BodyStability();Write(args[1],report);return (bool)report.GetType().GetProperty("passed").GetValue(report,null)?0:1;}}
                if(args.Length>1&&args[0]=="--test-latest-art"){using(DevArtwork latest=new DevArtwork()){Write(args[1],latest.Verify());return 0;}}
                if(args.Length>1 && args[0]=="--apply-update")return AutoUpdater.Apply(args[1]);
                if(args.Length>1 && args[0]=="--check-updates"){ReleaseUpdate found=AutoUpdater.Check();Write(args[1],new{passed=true,repository=AutoUpdater.Repository,currentVersion=AutoUpdater.CurrentVersion,updateAvailable=found!=null,availableVersion=found==null?null:found.Version});return 0;}
                if(args.Length>1 && args[0]=="--test-updater"){Write(args[1],AutoUpdater.Test(Path.Combine(Path.GetDirectoryName(args[1]),"updater-fixtures")));return 0;}
                if(args.Length>2 && args[0]=="--update-health"){AutoUpdater.HealthFile=args[1];AutoUpdater.HealthToken=args[2];}
                if(args.Length>0 && args[0]=="--test-behavior"){Write(args.Length>1?args[1]:null,Tests.Behavior());return 0;}
                if(args.Length>0 && args[0]=="--test-support-anchor"){Write(args.Length>1?args[1]:null,SpriteSupportTest.Run());return 0;}
                using(ArtPack art=new ArtPack())
                {
#if ODIN_DEV
                    if(args.Length>1&&args[0]=="--export-dev-jumps")
                    {
                        using(DevArtwork latest=new DevArtwork())
                        {
                            object report=DevJumpPreview.Run(art,latest,args[1]);if(args.Length>2)Write(args[2],report);
                        }
                        return 0;
                    }
#endif
                    if(args.Length>1 && args[0]=="--test-static-rest"){Write(args[1],StaticRestTest.Run(art));return 0;}
                    if(args.Length>1 && args[0]=="--test-screen-edges"){Write(args[1],ScreenEdges.Test(art));return 0;}
                    if(args.Length>1 && args[0]=="--routine-time"){Write(args[1],Tests.RoutineTime(art,args.Length>2?Double.Parse(args[2],System.Globalization.CultureInfo.InvariantCulture):168,8));return 0;}
                    if(args.Length>0 && args[0]=="--check-assets"){Write(args.Length>1?args[1]:null,Tests.Assets(art));return 0;}
                    if(args.Length>0 && args[0]=="--self-test"){Write(args.Length>1?args[1]:null,new{behavior=Tests.Behavior(art.Manifest.clips.Keys),artwork=Tests.Assets(art),shadows=Tests.Shadows(art),idleLooking=Tests.IdleLooking(),sittingPrototype=Tests.SittingPrototype(),eyelids=Tests.Eyelids(art),heldRestStates=Tests.HeldRestStates(art),runningScale=RunningScale.Test(art),runningMotion=RunningMotion.Test(),startup=Array.IndexOf(args,"--skip-registry-test")>=0?(object)new{skipped=true,reason="Registry test explicitly skipped by caller"}:WindowsStartup.Test(),clockGaze=ClockGaze.Test(art),perchStates=Tests.PerchStates(art),sittingGaze=SittingGaze.Test(art),restEffects=RestEffectsTest.Run(art),taskbarOrder=TaskbarOrder.Test()});return 0;}
                    if(args.Length>1 && args[0]=="--preview"){Tests.Preview(art,args[1]);return 0;}
                    try{Native.SetProcessDpiAwarenessContext(new IntPtr(-4));}catch(EntryPointNotFoundException){Native.SetProcessDPIAware();}
                    Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
#if ODIN_DEV
                    if(args.Length>1 && args[0]=="--test-dev-art")
                    {
                        // Headless: artwork pack checks plus the DEV menu listing (the window is constructed, never shown).
                        Dictionary<string,object> merged=new Dictionary<string,object>();object menus;
                        using(DevArtwork latest=new DevArtwork()){object verified=latest.Verify();foreach(System.Reflection.PropertyInfo property in verified.GetType().GetProperties())merged[property.Name]=property.GetValue(verified,null);}
                        using(OdinWindow window=new OdinWindow(art,true))menus=window.CheckDevMenuListing();
                        merged["devMenus"]=menus;Write(args[1],merged);return (bool)menus.GetType().GetProperty("passed").GetValue(menus,null)?0:1;
                    }
                    if(args.Length>1 && args[0]=="--test-follow-mouse")
                    {
                        // Headless: the window is constructed (same DPI mode as the app) but never shown.
                        object report;using(OdinWindow window=new OdinWindow(art,true))report=window.CheckFollowMouse();
                        Write(args[1],report);return (bool)report.GetType().GetProperty("passed").GetValue(report,null)?0:1;
                    }
                    if(args.Length>1 && args[0]=="--test-perch-zoomies")
                    {
                        // Headless: CatMind cases, then held states and stills through the DEV menu on the per-tick path (never shown).
                        object report;using(OdinWindow window=new OdinWindow(art,true))report=window.CheckPerchZoomies();
                        Write(args[1],report);return (bool)report.GetType().GetProperty("passed").GetValue(report,null)?0:1;
                    }
#endif
                    if(args.Length>1 && args[0]=="--test-jump-tilt")
                    {
                        // Headless real CatMind flights through the app's render path; optional args[2] saves the sampled canvases.
                        object report;using(OdinWindow window=new OdinWindow(art,true))report=window.CheckJumpTilt(args.Length>2?args[2]:null);
                        Write(args[1],report);return (bool)report.GetType().GetProperty("passed").GetValue(report,null)?0:1;
                    }
                    if(args.Length>1 && args[0]=="--test-run-motion")
                    {
                        // Headless real zoomies, one 16 ms tick at a time; args[2] (default <report dir>/run-motion-frames) gets one rendered cycle per direction.
                        string frames=args.Length>2?args[2]:Path.Combine(Path.GetDirectoryName(Path.GetFullPath(args[1])),"run-motion-frames");
                        object report;using(OdinWindow window=new OdinWindow(art,true))report=window.CheckRunMotion(frames);
                        Write(args[1],report);return (bool)report.GetType().GetProperty("passed").GetValue(report,null)?0:1;
                    }
                    if(args.Length>1 && args[0]=="--eyes-native-test")
                    {
                        object result=null;Exception eyeError=null;using(OdinWindow window=new OdinWindow(art,true,true))
                        {
                            window.Shown+=delegate{try{result=window.CheckEyeRendering(Path.Combine(Path.GetDirectoryName(args[1]),"rendered-eyes"));}catch(Exception error){eyeError=error;}finally{window.BeginInvoke(new Action(window.Close));}};Application.Run(window);
                        }
                        if(eyeError!=null)throw eyeError;Write(args[1],result);return 0;
                    }
#if ODIN_DEV
                    if(args.Length>1 && args[0]=="--dev-controls-test")
                    {
                        object result=null;Exception failure=null;using(OdinWindow window=new OdinWindow(art,true))
                        {
                            window.Shown+=delegate{try{result=window.CheckDevControls();}catch(Exception error){failure=error;}finally{window.BeginInvoke(new Action(window.Close));}};Application.Run(window);
                        }
                        if(failure!=null)throw failure;Write(args[1],result);return 0;
                    }
#endif
                    if(args.Length>1 && args[0]=="--test-menus")
                    {
                        // 3.2.0 menus (Hatt 2026-11-08): headless, the window is constructed and never shown.
                        object result;using(OdinWindow window=new OdinWindow(art,true))result=window.CheckMenus();
                        Write(args[1],result);return 0;
                    }
                    if(args.Length>0 && args[0]=="--native-test"){Write(args.Length>1?args[1]:null,Tests.NativePerches(art.Manifest));return 0;}
                    if(args.Length>0 && args[0]=="--menu-test")
                    {
                        object result=null;Exception menuError=null;using(OdinWindow window=new OdinWindow(art,true))
                        {
                            window.Shown+=delegate{try{result=window.CheckMenuDismissal();}catch(Exception error){menuError=error;}finally{window.BeginInvoke(new Action(window.Close));}};Application.Run(window);
                        }
                        if(menuError!=null)throw menuError;Write(args.Length>1?args[1]:null,result);return 0;
                    }
                    if(args.Length>0 && args[0]=="--tray-test")
                    {
                        IntPtr prior=Native.GetForegroundWindow();bool activated=false;long painted;
                        using(OdinWindow window=new OdinWindow(art,false))
                        {
                            window.SmokeSeconds=12;window.Shown+=delegate{activated=prior!=Native.GetForegroundWindow();};Application.Run(window);painted=window.PaintedFrames;
                        }
                        if(activated || painted<20)throw new Exception("Companion activated unexpectedly or failed to render.");
                        Write(args.Length>1?args[1]:null,new{passed=true,paintedFrames=painted,stoleFocus=activated,notificationIconLifecycle="created and disposed",session=Process.GetCurrentProcess().SessionId});return 0;
                    }
                    if(args.Length>0 && args[0]=="--smoke-test")
                    {
                        // Warm WinForms' process-wide font/menu caches before checking for a leak.
                        using(OdinWindow warmup=new OdinWindow(art,true)){warmup.SmokeSeconds=7;Application.Run(warmup);}
                        uint before=Native.GetGuiResources(Process.GetCurrentProcess().Handle,0);long paints;double seconds;
                        using(OdinWindow window=new OdinWindow(art,true)){Application.Run(window);paints=window.PaintedFrames;seconds=window.Mind.Clock;}
                        uint after=Native.GetGuiResources(Process.GetCurrentProcess().Handle,0);
                        if(paints<20 || seconds<10 || after>before+2)throw new Exception("Desktop rendering or GDI lifecycle check failed: "+paints+" paints; routine "+seconds+"s; GDI "+before+" -> "+after);
                        Write(args.Length>1?args[1]:null,new{passed=true,paintedFrames=paints,routineSeconds=seconds,gdiBefore=before,gdiAfter=after});return 0;
                    }
                    bool prototype=args.Length>0 && args[0]=="--sitting-prototype";
#if SITTING_PROTOTYPE
                    prototype=true;
#endif
                    if(prototype && args.Length>1)Write(args[1],new{phase="acquiring-singleton",pid=Process.GetCurrentProcess().Id});
                    bool created;using(Mutex singleton=new Mutex(true,prototype?"Local\\OdinSittingPrototype":SingletonName,out created))
                    {
                        if(prototype && args.Length>1)Write(args[1],new{phase="singleton-acquired",created=created,pid=Process.GetCurrentProcess().Id});
                        if(!created){IntPtr existing=Native.FindWindow(null,prototype?"Odin - sitting prototype":WindowTitle);if(existing!=IntPtr.Zero)Native.PostMessage(existing,BringHomeMessage,IntPtr.Zero,IntPtr.Zero);return 0;}
                        using(OdinWindow window=new OdinWindow(art,false,prototype))
                        {
                            if(prototype && args.Length>1)window.Shown+=delegate{Write(args[1],new{phase="shown",pid=Process.GetCurrentProcess().Id,title=window.Text,handle=window.Handle.ToInt64(),visible=window.Visible});};
                            Application.Run(window);
                        }
                    }
                }
                return 0;
            }
            catch(Exception error)
            {
                if(!String.IsNullOrEmpty(AutoUpdater.HealthFile)){AutoUpdater.FailHealth(error.Message);return 1;}
                // Test runs name their report first (--self-test <report> --skip-registry-test, --test-jump-tilt <report> <frames-dir>).
                if(args.Length>1)try{Write(args[0]=="--self-test" || args[0].StartsWith("--test-")?args[1]:args[args.Length-1],new{passed=false,error=error.ToString()});}catch{}
                else MessageBox.Show(error.Message,"Odin couldn't start",MessageBoxButtons.OK,MessageBoxIcon.Error);
                return 1;
            }
        }
    }
}
