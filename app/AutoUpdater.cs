using System;
using System.Collections;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;

namespace OdinV2
{
    internal sealed class ReleaseUpdate
    {
        public string Version,Url,Sha256,StagedPath,OldSha256;public long Size;
    }
    internal sealed class UpdatePlan
    {
        public int Protocol=1,ParentPid;public long ParentStartTicks;public string Target,ExpectedOldSha256,NewSha256,Version,Token,HealthFile;
    }
    internal static class AutoUpdater
    {
        public const string Repository="BurtFablehoff/Odin";
        public const string LatestApi="https://api.github.com/repos/BurtFablehoff/Odin/releases/latest";
        public static readonly string Cache=Path.Combine(Preferences.Folder,"Updates");
        public static string HealthFile,HealthToken;
        public static string CurrentVersion {get{return Assembly.GetExecutingAssembly().GetName().Version.ToString(3);}}
        public static string Hash(string file){using(SHA256 sha=SHA256.Create())using(FileStream stream=File.OpenRead(file))return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();}
        private static bool Inside(string path,string directory){string root=Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar)+Path.DirectorySeparatorChar;return Path.GetFullPath(path).StartsWith(root,StringComparison.OrdinalIgnoreCase);}
        private static Dictionary<string,object> Json(string text){return new JavaScriptSerializer().Deserialize<Dictionary<string,object>>(text);}
        private static string Text(Dictionary<string,object> value,string key){object item;return value.TryGetValue(key,out item)&&item!=null?Convert.ToString(item):"";}
        private static bool Flag(Dictionary<string,object> value,string key){object item;return value.TryGetValue(key,out item)&&item is bool&&(bool)item;}
        public static bool AssetUrl(string value)
        {
            Uri uri;if(!Uri.TryCreate(value,UriKind.Absolute,out uri))return false;
            return uri.Scheme=="https" && uri.Host.Equals("github.com",StringComparison.OrdinalIgnoreCase) && uri.AbsolutePath.StartsWith("/BurtFablehoff/Odin/releases/download/",StringComparison.OrdinalIgnoreCase) && String.IsNullOrEmpty(uri.UserInfo);
        }
        public static ReleaseUpdate Parse(string json,Version current)
        {
            Dictionary<string,object> release=Json(json);if(Flag(release,"draft")||Flag(release,"prerelease"))return null;
            string tag=Text(release,"tag_name");if(!Regex.IsMatch(tag,"^v?[0-9]+\\.[0-9]+\\.[0-9]+(\\.[0-9]+)?$"))return null;
            Version next;if(!Version.TryParse(tag.TrimStart('v'),out next)||Normalize(next)<=Normalize(current))return null;
            object list;if(!release.TryGetValue("assets",out list)||!(list is IEnumerable))return null;
            foreach(object item in (IEnumerable)list)
            {
                Dictionary<string,object> asset=item as Dictionary<string,object>;if(asset==null||Text(asset,"name")!="Odin.exe"||Text(asset,"state")!="uploaded")continue;
                string url=Text(asset,"browser_download_url"),digest=Text(asset,"digest");long size;
                if(!AssetUrl(url)||!Int64.TryParse(Text(asset,"size"),out size)||size<1000000||size>200000000)throw new InvalidDataException("The Odin release download is invalid.");
                if(!Regex.IsMatch(digest,"^sha256:[a-fA-F0-9]{64}$"))throw new InvalidDataException("The Odin release is still being prepared. Its verification digest is missing.");
                return new ReleaseUpdate{Version=Normalize(next).ToString(3),Url=url,Sha256=digest.Substring(7).ToLowerInvariant(),Size=size};
            }
            return null;
        }
        private static Version Normalize(Version value){return new Version(value.Major,value.Minor,Math.Max(0,value.Build),Math.Max(0,value.Revision));}
        private static HttpWebRequest Request(string url)
        {
            ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
            HttpWebRequest request=(HttpWebRequest)WebRequest.Create(url);request.UserAgent="Odin-Companion/"+CurrentVersion;request.Timeout=25000;request.ReadWriteTimeout=25000;request.AllowAutoRedirect=true;
            request.Accept="application/vnd.github+json";return request;
        }
        public static ReleaseUpdate Check()
        {
            string json;
            try{using(WebResponse response=Request(LatestApi).GetResponse())using(StreamReader reader=new StreamReader(response.GetResponseStream()))json=reader.ReadToEnd();}
            catch(WebException error){HttpWebResponse response=error.Response as HttpWebResponse;if(response!=null && response.StatusCode==HttpStatusCode.NotFound)return null;throw;}
            return Parse(json,Assembly.GetExecutingAssembly().GetName().Version);
        }
        public static void Download(ReleaseUpdate update,string target)
        {
            string directory=Path.Combine(Cache,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);string file=Path.Combine(directory,"Odin-update.exe");long total=0;
            try
            {
                using(WebResponse response=Request(update.Url).GetResponse())using(Stream input=response.GetResponseStream())using(FileStream output=new FileStream(file,FileMode.CreateNew,FileAccess.Write,FileShare.None))
                {byte[] buffer=new byte[65536];int count;while((count=input.Read(buffer,0,buffer.Length))>0){total+=count;if(total>update.Size)throw new InvalidDataException("Update download is larger than expected.");output.Write(buffer,0,count);}}
                if(total!=update.Size||!String.Equals(Hash(file),update.Sha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Update verification failed; the current Odin was kept.");
                FileVersionInfo info=FileVersionInfo.GetVersionInfo(file);if(info.ProductName!="Odin Companion"||Normalize(new Version(info.FileVersion))!=Normalize(new Version(update.Version)))throw new InvalidDataException("Downloaded file is not the expected Odin version.");
                if((File.GetAttributes(target)&FileAttributes.ReadOnly)!=0)throw new IOException("Odin.exe is read-only. Keep it in a folder you can update.");
                string probe=Path.Combine(Path.GetDirectoryName(target),".odin-update-check-"+Guid.NewGuid().ToString("N"));
                try{using(FileStream writable=new FileStream(probe,FileMode.CreateNew,FileAccess.Write)){writable.WriteByte(0);}}finally{if(File.Exists(probe))File.Delete(probe);}
                update.StagedPath=file;update.OldSha256=Hash(target);
            }
            catch{try{File.Delete(file);}catch{}throw;}
        }
        public static bool Start(ReleaseUpdate update,string target)
        {
            if(update==null || String.IsNullOrEmpty(update.StagedPath))return false;
            Process parent=Process.GetCurrentProcess();UpdatePlan plan=new UpdatePlan{Target=Path.GetFullPath(target),ParentPid=parent.Id,ParentStartTicks=parent.StartTime.ToUniversalTime().Ticks,ExpectedOldSha256=update.OldSha256,NewSha256=update.Sha256,Version=update.Version,Token=Guid.NewGuid().ToString("N"),HealthFile=Path.Combine(Path.GetDirectoryName(update.StagedPath),"healthy.json")};
            string file=Path.Combine(Path.GetDirectoryName(update.StagedPath),"plan.json");File.WriteAllText(file,new JavaScriptSerializer().Serialize(plan),Encoding.UTF8);
            ProcessStartInfo start=new ProcessStartInfo(update.StagedPath,"--apply-update "+Quote(file)){UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,WorkingDirectory=Path.GetDirectoryName(update.StagedPath)};
            return Process.Start(start)!=null;
        }
        internal static string Quote(string value){if(value.Contains("\""))throw new InvalidDataException("Invalid path.");return "\""+value+"\"";}
        public static void FailHealth(string error)
        {
            if(!String.IsNullOrEmpty(HealthFile) && Inside(HealthFile,Cache) && Regex.IsMatch(HealthToken??"","^[a-f0-9]{32}$"))File.WriteAllText(HealthFile,new JavaScriptSerializer().Serialize(new{healthy=false,token=HealthToken,error=error}),Encoding.UTF8);
        }
        public static void MarkHealthy()
        {
            if(String.IsNullOrEmpty(HealthFile))return;
            if(!Inside(HealthFile,Cache)||!Regex.IsMatch(HealthToken??"","^[a-f0-9]{32}$"))throw new InvalidDataException("Invalid update health receipt.");
            File.WriteAllText(HealthFile,new JavaScriptSerializer().Serialize(new{healthy=true,token=HealthToken,version=CurrentVersion,processId=Process.GetCurrentProcess().Id}),Encoding.UTF8);
        }
        internal static void Replace(string staged,string target,string backup)
        {
            File.Copy(target,backup,true);string adjacent=target+".update-"+Guid.NewGuid().ToString("N");
            try{File.Copy(staged,adjacent,false);File.Replace(adjacent,target,null);}
            finally{if(File.Exists(adjacent))File.Delete(adjacent);}
        }
        internal static void Restore(string backup,string target)
        {
            string adjacent=target+".rollback-"+Guid.NewGuid().ToString("N");
            try{File.Copy(backup,adjacent,false);File.Replace(adjacent,target,null);}
            finally{if(File.Exists(adjacent))File.Delete(adjacent);}
        }
        public static int Apply(string file)
        {
            if(!Inside(file,Cache))throw new InvalidDataException("Update plan must be in Odin's update cache.");
            UpdatePlan plan=new JavaScriptSerializer().Deserialize<UpdatePlan>(File.ReadAllText(file));string staged=Assembly.GetExecutingAssembly().Location,directory=Path.GetDirectoryName(file),backup=Path.Combine(directory,"Odin-previous.exe"),status=Path.Combine(directory,"result.json");bool replaced=false,parentGone=false;Process child=null;
            try
            {
                if(plan.Protocol!=1||!Inside(plan.HealthFile,directory)||!Regex.IsMatch(plan.Token??"","^[a-f0-9]{32}$")||!Path.IsPathRooted(plan.Target)||!plan.Target.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)||Path.GetFullPath(plan.Target).Equals(Path.GetFullPath(staged),StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid update plan.");
                if(!String.Equals(Hash(staged),plan.NewSha256,StringComparison.OrdinalIgnoreCase)||!String.Equals(Hash(plan.Target),plan.ExpectedOldSha256,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("The verified Odin files changed before installation.");
                FileVersionInfo old=FileVersionInfo.GetVersionInfo(plan.Target),next=FileVersionInfo.GetVersionInfo(staged);
                if(old.ProductName!="Odin Companion"||next.ProductName!="Odin Companion"||Normalize(new Version(next.FileVersion))<=Normalize(new Version(old.FileVersion))||Normalize(new Version(next.FileVersion))!=Normalize(new Version(plan.Version)))throw new InvalidDataException("Update version or identity mismatch.");
                try
                {
                    Process parent=Process.GetProcessById(plan.ParentPid);
                    if(!parent.HasExited)
                    {
                        try{if(parent.StartTime.ToUniversalTime().Ticks!=plan.ParentStartTicks || !parent.MainModule.FileName.Equals(plan.Target,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Update parent identity changed.");}
                        catch(Win32Exception){if(!parent.HasExited)throw;}
                        catch(InvalidOperationException){if(!parent.HasExited)throw;}
                        IntPtr own=Native.FindWindow(null,Program.WindowTitle);uint owner;
                        if(own!=IntPtr.Zero){Native.GetWindowThreadProcessId(own,out owner);if(owner==plan.ParentPid)Native.PostMessage(own,0x10,IntPtr.Zero,IntPtr.Zero);}
                        if(!parent.WaitForExit(30000))throw new IOException("Odin did not close in time; the original file was kept.");
                    }
                }
                catch(ArgumentException){}
                parentGone=true;Replace(staged,plan.Target,backup);replaced=true;
                ProcessStartInfo start=new ProcessStartInfo(plan.Target,"--update-health "+Quote(plan.HealthFile)+" "+plan.Token){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(plan.Target)};child=Process.Start(start);
                DateTime deadline=DateTime.UtcNow.AddSeconds(25);bool healthy=false;
                while(DateTime.UtcNow<deadline)
                {
                    if(File.Exists(plan.HealthFile)){Dictionary<string,object> receipt=Json(File.ReadAllText(plan.HealthFile));healthy=Flag(receipt,"healthy")&&Text(receipt,"token")==plan.Token&&Text(receipt,"version")==plan.Version;if(healthy || !String.IsNullOrEmpty(Text(receipt,"error")))break;}
                    if(child==null||child.HasExited)break;Thread.Sleep(250);
                }
                if(!healthy)throw new IOException("The new Odin did not become ready; reverting to the previous version.");
                File.WriteAllText(status,new JavaScriptSerializer().Serialize(new{success=true,version=plan.Version,target=plan.Target}),Encoding.UTF8);return 0;
            }
            catch(Exception error)
            {
                if(replaced)
                {
                    if(child!=null && !child.HasExited){child.Kill();child.WaitForExit(5000);}
                    Restore(backup,plan.Target);Process.Start(new ProcessStartInfo(plan.Target){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(plan.Target)});
                }
                if(parentGone && !replaced)try{Process.Start(new ProcessStartInfo(plan.Target){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(plan.Target)});}catch{}
                File.WriteAllText(status,new JavaScriptSerializer().Serialize(new{success=false,rolledBack=replaced,error=error.Message}),Encoding.UTF8);return 1;
            }
        }
        public static object Test(string folder)
        {
            Directory.CreateDirectory(folder);string digest=new string('a',64),valid="{\"tag_name\":\"v9.0.0\",\"assets\":[{\"name\":\"Odin.exe\",\"state\":\"uploaded\",\"size\":44000000,\"digest\":\"sha256:"+digest+"\",\"browser_download_url\":\"https://github.com/BurtFablehoff/Odin/releases/download/v9.0.0/Odin.exe\"}]}";
            if(Parse(valid,new Version(3,0,3))==null||Parse(valid,new Version(9,0,0))!=null)throw new Exception("Version comparison failed.");
            if(Parse(valid.Replace("\"assets\"","\"prerelease\":true,\"assets\""),new Version(3,0,3))!=null)throw new Exception("Prerelease accepted.");
            bool bad=false;try{Parse(valid.Replace("sha256:"+digest,"sha256:bad"),new Version(3,0,3));}catch(InvalidDataException){bad=true;}if(!bad)throw new Exception("Missing digest was accepted.");
            if(AssetUrl("https://example.com/Odin.exe")||AssetUrl("http://github.com/BurtFablehoff/Odin/releases/download/v9/Odin.exe"))throw new Exception("Untrusted asset URL accepted.");
            string target=Path.Combine(folder,"fixture-target.exe"),staged=Path.Combine(folder,"fixture-new.exe"),backup=Path.Combine(folder,"fixture-old.exe");File.WriteAllText(target,"old");File.WriteAllText(staged,"new");Replace(staged,target,backup);if(File.ReadAllText(target)!="new"||File.ReadAllText(backup)!="old")throw new Exception("Atomic replacement failed.");Restore(backup,target);if(File.ReadAllText(target)!="old")throw new Exception("Rollback failed.");
            return new{passed=true,repository=Repository,versionChecks=true,prereleaseRejected=true,digestRequired=true,untrustedUrlRejected=true,atomicReplacement=true,rollback=true};
        }
    }
    internal sealed class UpdateCoordinator
    {
        private readonly string target;private int busy;private DateTime next=DateTime.UtcNow.AddSeconds(20);private volatile ReleaseUpdate ready;private volatile string message;private volatile bool notify;
        public UpdateCoordinator(string path){target=path;}
        public string TakeMessage(){string value=message;message=null;return value;}
        public ReleaseUpdate Ready {get{return ready;}}
        public bool ManualReady {get{return notify;}}
        public void Tick(bool enabled){if(enabled && DateTime.UtcNow>=next)Check(false);}
        public void Check(bool manual)
        {
            if(Interlocked.CompareExchange(ref busy,1,0)!=0)return;next=DateTime.UtcNow.AddHours(6);notify=manual;
            ThreadPool.QueueUserWorkItem(delegate
            {
                try{ReleaseUpdate update=AutoUpdater.Check();if(update==null){if(notify)message="Odin is up to date.";return;}AutoUpdater.Download(update,target);ready=update;}
                catch(Exception error){if(notify)message="Update unavailable: "+error.Message;}
                finally{Interlocked.Exchange(ref busy,0);}
            });
        }
        public bool Install(){ReleaseUpdate update=ready;if(update==null)return false;ready=null;return AutoUpdater.Start(update,target);}
    }
}
