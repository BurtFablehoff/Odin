// Headless original-source registration QA. No Forms, handles or app entrypoint.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Security.Cryptography;
using System.Web.Script.Serialization;
using OdinV2;

public static class OriginalRestV8QA
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Field(object value,string name){return value.GetType().GetField(name,Flags).GetValue(value);}
    static object Call(object value,string name,params object[] args){return value.GetType().GetMethod(name,Flags).Invoke(value,args);}
    static byte[] Pixels(Bitmap image)
    {
        BitmapData data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try{byte[] result=new byte[data.Stride*image.Height];Marshal.Copy(data.Scan0,result,0,result.Length);return result;}finally{image.UnlockBits(data);}
    }
    static string Hash(string path){using(SHA256 hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant();}
    public static string Run(string root)
    {
        string output=Path.Combine(root,"local-data/reference-review/original-rest-v8/rendered");Directory.CreateDirectory(output);
        List<object> cases=new List<object>();int gazes=0,eyes=0,tails=0,still=0;long protectedPixels=0,protectedAlphaPixels=0;
        string[] clips={"sit","loaf","lay","sleep","look_sit","look_loaf","look_lay","sit_eyes","loaf_eyes","lay_eyes","sit_tail","lay_tail","sleep_tail"};
        int[] counts={1,1,1,1,19,12,14,3,3,3,5,5,5};
        for(int c=0;c<clips.Length;c++)
        {
            string clip=clips[c],family=clip.StartsWith("look_")?clip.Substring(5):clip.EndsWith("_eyes")||clip.EndsWith("_tail")?clip.Substring(0,clip.Length-5):clip;
            int pose=family=="sit"?1:family=="loaf"?2:family=="lay"?3:4;
            string basePath=Path.Combine(root,"art/frames/"+family+"/000.png");
            using(Bitmap basis=new Bitmap(basePath))
            {
                byte[] original=Pixels(basis);
                for(int frame=0;frame<counts[c];frame++)
                {
                    string path=Path.Combine(root,"art/frames/"+clip+"/"+frame.ToString("000")+".png"),hash=Hash(path);
                    using(Bitmap source=new Bitmap(path))using(Bitmap rendered=OriginalRestRenderer.Compose(basis,source,pose,clip,frame))
                    {
                        int changed=OriginalRestRenderer.Verify(basis,rendered,pose,clip,frame);byte[] result=Pixels(rendered);
                        // Independent full-width protected-body/floor check rather
                        // than trusting the renderer's own weight implementation.
                        int protectedTop=pose==1?752:pose==2?878:pose==3?862:920;
                        if(clip.StartsWith("look_"))
                        {
                            for(int y=protectedTop;y<1024;y++)for(int x=0;x<1024;x++)
                            {int index=(y*1024+x)*4;for(int k=0;k<4;k++)if(original[index+k]!=result[index+k])throw new Exception("Independent fixed-body pixel check failed: "+clip+":"+frame);protectedPixels++;}
                            int silhouetteTop=pose==1?735:pose==2?865:850;
                            for(int y=silhouetteTop;y<1024;y++)for(int x=0;x<1024;x++)
                            {int index=(y*1024+x)*4;if(original[index+3]!=result[index+3])throw new Exception("Independent lower-neck/body alpha check failed: "+clip+":"+frame);protectedAlphaPixels++;}
                            gazes++;
                        }
                        else if(clip.EndsWith("_eyes"))eyes++;
                        else if(clip.EndsWith("_tail"))tails++;
                        else{if(changed!=0)throw new Exception("Neutral original pixels changed.");still++;}
                        if(Hash(path)!=hash)throw new Exception("Original source was edited.");
                        string directory=Path.Combine(output,clip);Directory.CreateDirectory(directory);rendered.Save(Path.Combine(directory,frame.ToString("000")+".png"),ImageFormat.Png);
                        cases.Add(new{clip=clip,frame=frame,changedPixels=changed,sourceSHA256=hash,outsideMaskExact=true,fixedBodyFloor=clip.StartsWith("look_"),alphaExact=clip.EndsWith("_eyes")});
                    }
                }
            }
        }
        if(gazes!=45||eyes!=9||tails!=15||still!=4)throw new Exception("Incomplete original family coverage.");
        return new JavaScriptSerializer().Serialize(new{passed=true,headless=true,windowsCreated=0,sourcePixelsModified=false,gazes=gazes,eyes=eyes,tails=tails,neutralStills=still,independentProtectedPixelCases=protectedPixels,independentProtectedAlphaCases=protectedAlphaPixels,all73FramesRendered=true,cases=cases});
    }
    public static string RunRuntime(string root,string executable)
    {
        // Load the compiled app as an assembly; never invoke Main or create an
        // OdinWindow. Inspect the actual same cache/renderer used by PaintSprite.
        Assembly assembly=Assembly.LoadFile(Path.GetFullPath(executable));
        Type type=assembly.GetType("OdinV2.DevArtwork",true);int frames=0,placements=0;long exactBody=0,exactAlpha=0;
        using(IDisposable artwork=(IDisposable)Activator.CreateInstance(type,true))
        {
            object manifest=Field(artwork,"manifest");if(!(bool)Field(manifest,"fixedOriginalRestBodies"))throw new Exception("Compiled runtime did not enable fixed original bodies.");
            foreach(string family in new[]{"sit","loaf","lay"})
            {
                int pose=family=="sit"?1:family=="loaf"?2:3,count=pose==1?19:pose==2?12:14;
                object neutral=Call(artwork,"Still",pose),neutralPose=Field(neutral,"Pose");byte[] baseline=Pixels((Bitmap)Field(neutral,"Image"));
                for(int frame=0;frame<count;frame++)
                {
                    object[] args={"look_"+family,frame,null};if(!(bool)Call(artwork,"TryGet",args))throw new Exception("Compiled runtime gaze missing.");
                    object selected=args[2],selectedPose=Field(selected,"Pose");Bitmap rendered=(Bitmap)Field(selected,"Image");
                    if((string)Field(selectedPose,"clip")!="look_"+family||(int)Field(selectedPose,"frame")!=frame)throw new Exception("Compiled runtime selected the wrong head source.");
                    foreach(string coordinate in new[]{"anchorX","supportY","renderWidthAtSmall"})if(!Field(selectedPose,coordinate).Equals(Field(neutralPose,coordinate)))throw new Exception("Compiled gaze root/support/scale differs from neutral: "+coordinate);
                    byte[] result=Pixels(rendered);int bodyTop=pose==1?752:pose==2?878:862,alphaTop=pose==1?735:pose==2?865:850;
                    for(int y=bodyTop;y<1024;y++)for(int x=0;x<1024;x++)
                    {int i=(y*1024+x)*4;for(int c=0;c<4;c++)if(baseline[i+c]!=result[i+c])throw new Exception("Compiled runtime gaze body/floor moved: "+family+":"+frame);exactBody++;}
                    for(int y=alphaTop;y<1024;y++)for(int x=0;x<1024;x++)
                    {int i=(y*1024+x)*4;if(baseline[i+3]!=result[i+3])throw new Exception("Compiled runtime body silhouette moved.");exactAlpha++;}
                    foreach(int size in new[]{480,640,800})foreach(double floor in new[]{900.0,550.0})foreach(double x in new[]{600.0,-1700.0})
                    {
                        Rectangle a=(Rectangle)Call(neutral,"Destination",size,x,floor,0.0),b=(Rectangle)Call(selected,"Destination",size,x,floor,0.0);
                        if(a!=b)throw new Exception("Compiled gaze shifted its entire canvas against taskbar/window: "+family+":"+frame);
                        placements++;
                    }
                    System.Collections.IDictionary cache=(System.Collections.IDictionary)Field(artwork,"actionCache");if(cache.Count>8)throw new Exception("Compiled action cache exceeded8 bitmaps.");
                    frames++;
                }
            }
        }
        if(frames!=45||placements!=540)throw new Exception("Incomplete compiled runtime coverage.");
        return new JavaScriptSerializer().Serialize(new{passed=true,headless=true,windowsCreated=0,actualCompiledRuntime=true,entryPointInvoked=false,gazeFrames=frames,identicalDestinationCases=placements,protectedBodyPixelCases=exactBody,protectedAlphaCases=exactAlpha,taskbarFloor=900,windowFloor=550,allSizes=true,negativeMonitor=true,rootSupportScaleMatchNeutral=true,cacheLimit=8,executable=executable,sha256=Hash(executable)});
    }
}
