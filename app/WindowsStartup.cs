using System;
using System.IO;
using Microsoft.Win32;
using System.Windows.Forms;

namespace OdinV2
{
    internal static class WindowsStartup
    {
        private const string RunKey=@"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string ValueName="Odin";
        internal static string Command(string executable)
        {
            if(!Path.IsPathRooted(executable) || !File.Exists(executable) || executable.IndexOf('"')>=0)throw new IOException("The Odin executable path is unavailable.");
            return "\""+executable+"\"";
        }
        private static void Write(RegistryKey key,bool enabled,string executable)
        {
            if(enabled)key.SetValue(ValueName,Command(executable),RegistryValueKind.String);
            else key.DeleteValue(ValueName,false);
        }
        public static void Set(bool enabled,string executable)
        {
            using(RegistryKey key=Registry.CurrentUser.CreateSubKey(RunKey))Write(key,enabled,executable);
        }
        public static object Test()
        {
            string path=@"Software\OdinCompanionStartupTest-"+Guid.NewGuid().ToString("N");
            try
            {
                using(RegistryKey key=Registry.CurrentUser.CreateSubKey(path))
                {
                    string exe=System.Reflection.Assembly.GetExecutingAssembly().Location;
                    key.SetValue("Unrelated","preserve");Write(key,true,exe);
                    if((string)key.GetValue(ValueName)!="\""+exe+"\"")throw new Exception("Startup path is not quoted.");
                    Write(key,false,exe);Write(key,false,exe);
                    if(key.GetValue(ValueName)!=null || (string)key.GetValue("Unrelated")!="preserve")throw new Exception("Startup toggle did not preserve unrelated entries.");
                    Preferences defaults=new Preferences();
                    if(!defaults.StartWithWindows)throw new Exception("Startup must default to enabled.");
                    Preferences saved=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Preferences>("{\"StartWithWindows\":false}");
                    if(saved.StartWithWindows)throw new Exception("Saved opt-out was lost.");
                    Preferences legacy=new System.Web.Script.Serialization.JavaScriptSerializer().Deserialize<Preferences>("{\"Size\":480}");
                    if(!legacy.StartWithWindows)throw new Exception("Existing settings did not receive startup default.");
                }
            }
            finally{Registry.CurrentUser.DeleteSubKeyTree(path,false);}
            return new{passed=true,defaultEnabled=true,quotedExecutable=true,optOutPreserved=true,isolatedRegistryTest=true};
        }
    }
    internal sealed partial class OdinWindow
    {
        private ToolStripMenuItem startupItem;
        private bool settingStartup;
        private void AddStartupMenu()
        {
            startupItem=new ToolStripMenuItem("Start with Windows"){Checked=settings.StartWithWindows,CheckOnClick=true};
#if ODIN_DEV
            startupItem.Checked=false;startupItem.Enabled=false;startupItem.ToolTipText="The main version starts with Windows; development copies never register themselves.";
#else
            startupItem.CheckedChanged+=delegate
            {
                if(settingStartup)return;
                try
                {
                    if(!settings.Ephemeral)WindowsStartup.Set(startupItem.Checked,Application.ExecutablePath);
                    settings.StartWithWindows=startupItem.Checked;settings.Save();
                }
                catch(Exception error)
                {
                    settingStartup=true;startupItem.Checked=settings.StartWithWindows;settingStartup=false;
                    MessageBox.Show("Could not change Windows startup: "+error.Message,"Odin",MessageBoxButtons.OK,MessageBoxIcon.Information);
                }
            };
#endif
            menu.Items.Add(startupItem);
        }
        private void ApplyStartupPreference()
        {
#if !ODIN_DEV
            if(settings.Ephemeral)return;
            try{WindowsStartup.Set(settings.StartWithWindows,Application.ExecutablePath);}
            catch(Exception error){tray.ShowBalloonTip(4000,"Odin startup","Could not apply Windows startup: "+error.Message,ToolTipIcon.Info);}
#endif
        }
    }
}
