// Headless capture of the registered preview renderer. Never overwrites source art.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

namespace OdinV2
{
    public sealed class FamilyPart { public string source,sha256,phase;public double weight;public bool artApproved; }
    public sealed class FamilyBase { public string source,sha256;public int poseId;public double supportY,scaleAtSmall; }
    public sealed class FamilyFrame
    {
        public string clip,family,legacySource,legacySHA256,renderer;
        public string @base;
        public int frame,poseId;
        public double supportY,scaleAtSmall,angleDegrees;
        public bool previewAvailable,variantArtApproved;
        public FamilyPart[] parts;
    }
    public sealed class FamilyPlan { public int frameCount;public Dictionary<string,FamilyBase> bases;public FamilyFrame[] frames; }
    internal static class FamilyFrameExporter
    {
        static readonly Dictionary<string,Bitmap> images=new Dictionary<string,Bitmap>();
        static readonly HashSet<string> checkedPaths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        static string root;
        static string PathFor(string relative)
        {
            string path=Path.GetFullPath(Path.Combine(root,relative.Replace('/',Path.DirectorySeparatorChar)));
            if(!path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Source escaped project root.");
            return path;
        }
        static string Hash(string path)
        {
            using(SHA256 sha=SHA256.Create())using(Stream stream=File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        }
        static void Check(string relative,string expected)
        {
            if(Hash(PathFor(relative))!=expected)throw new Exception("Source hash changed: "+relative);
            checkedPaths.Add(relative);
        }
        static Bitmap Load(string relative)
        {
            if(!images.ContainsKey(relative))using(Bitmap original=new Bitmap(PathFor(relative)))images.Add(relative,new Bitmap(original));
            return images[relative];
        }
        static byte[] Pixels(Bitmap image)
        {
            BitmapData data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
            try{if(data.Stride!=image.Width*4)throw new Exception("Unexpected pixel stride.");byte[] result=new byte[data.Stride*image.Height];Marshal.Copy(data.Scan0,result,0,result.Length);return result;}
            finally{image.UnlockBits(data);}
        }
        static Bitmap BlendDraft(FamilyPart[] parts)
        {
            if(parts.Length==1)return (Bitmap)Load(parts[0].source).Clone();
            double total=0;Bitmap first=Load(parts[0].source);byte[][] sources=new byte[parts.Length][];
            for(int i=0;i<parts.Length;i++)
            {
                Bitmap image=Load(parts[i].source);if(image.Size!=first.Size)throw new Exception("Cardinal canvases do not match.");
                total+=parts[i].weight;sources[i]=Pixels(image);
            }
            if(Math.Abs(total-1)>.00000001)throw new Exception("Directional weights do not sum to one.");
            byte[] output=new byte[sources[0].Length];
            for(int offset=0;offset<output.Length;offset+=4)
            {
                double alpha=0;for(int j=0;j<parts.Length;j++)alpha+=sources[j][offset+3]*parts[j].weight;
                output[offset+3]=(byte)Math.Round(alpha);
                for(int channel=0;channel<3;channel++)
                {
                    double weighted=0;for(int j=0;j<parts.Length;j++)weighted+=sources[j][offset+channel]*sources[j][offset+3]*parts[j].weight;
                    output[offset+channel]=alpha<=0?(byte)0:(byte)Math.Round(weighted/alpha);
                }
            }
            Bitmap result=(Bitmap)first.Clone();BitmapData data=result.LockBits(new Rectangle(Point.Empty,result.Size),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
            try{Marshal.Copy(output,0,data.Scan0,output.Length);}finally{result.UnlockBits(data);}return result;
        }
        static int Main(string[] args)
        {
            if(args.Length<3||args.Length>4){Console.Error.WriteLine("Usage: exporter projectRoot planJson outputDirectory [nearest-cardinal|blend-qa]");return 2;}
            try
            {
                root=Path.GetFullPath(args[0]).TrimEnd(Path.DirectorySeparatorChar);
                string output=Path.GetFullPath(args[2]);
                if(!output.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new Exception("Output must remain inside the project.");
                string directionMode=args.Length==4?args[3]:"nearest-direction";
                if(directionMode!="nearest-direction"&&directionMode!="nearest-cardinal"&&directionMode!="blend-qa")throw new Exception("Unknown directional preview mode.");
                JavaScriptSerializer json=new JavaScriptSerializer();FamilyPlan plan=json.Deserialize<FamilyPlan>(File.ReadAllText(args[1]));
                if(plan.frameCount!=73||plan.frames.Length!=73)throw new Exception("Expected all73 resting slots.");
                foreach(FamilyBase basis in plan.bases.Values)Check(basis.source,basis.sha256);
                HashSet<string> slots=new HashSet<string>(),distinctGaze=new HashSet<string>();List<object> exported=new List<object>();int layerCases=0,baseAliases=0,approximated=0;double maximumError=0;
                foreach(FamilyFrame frame in plan.frames)
                {
                    string slot=frame.clip+"/"+frame.frame.ToString("000");if(!slots.Add(slot))throw new Exception("Duplicate slot.");
                    if(!frame.previewAvailable)throw new Exception("Unavailable source direction: "+slot);
                    Check(frame.legacySource,frame.legacySHA256);
                    foreach(FamilyPart part in frame.parts)Check(part.source,part.sha256);
                    FamilyPart[] renderParts=frame.parts;double renderedAngle=frame.angleDegrees,error=0;
                    if(frame.renderer=="gaze"&&(directionMode=="nearest-cardinal"||directionMode=="nearest-direction"))
                    {
                        if(frame.parts.Length!=1)throw new Exception("Default gaze plan must select one actual face.");
                        FamilyPart chosen=frame.parts[0];string phase=chosen.phase;
                        string[] names={"up","up-right","right","down-right","down","down-left","left","up-left"};
                        int direction=Array.IndexOf(names,phase);if(direction<0)throw new Exception("Unknown gaze direction.");
                        renderParts=new[]{chosen};renderedAngle=direction*45;
                        double distance=Math.Abs(frame.angleDegrees-renderedAngle)%360;error=Math.Min(distance,360-distance);
                        maximumError=Math.Max(maximumError,error);if(error>.000001)approximated++;
                        distinctGaze.Add(frame.family+"/"+phase);
                    }
                    Bitmap basis=Load(frame.@base);string directory=Path.Combine(output,frame.clip);Directory.CreateDirectory(directory);
                    string target=Path.Combine(directory,frame.frame.ToString("000")+".png");int changed=0;
                    if(frame.renderer=="base"){File.Copy(PathFor(frame.@base),target,true);baseAliases++;}
                    else if(frame.renderer=="eyelid")
                    {
                        using(Bitmap rendered=BlinkEyeRenderer.Compose(basis,Load(frame.parts[0].source),frame.poseId))
                        {changed=BlinkEyeRenderer.Verify(basis,rendered,frame.poseId);rendered.Save(target,ImageFormat.Png);layerCases++;}
                    }
                    else
                    {
                        using(Bitmap draft=BlendDraft(renderParts))using(Bitmap rendered=PoseVariantRenderer.Compose(basis,draft,frame.poseId,frame.renderer,1))
                        {changed=PoseVariantRenderer.Verify(basis,rendered,frame.poseId,frame.renderer);rendered.Save(target,ImageFormat.Png);layerCases++;}
                    }
                    using(Bitmap rendered=new Bitmap(target))
                    {
                        if(rendered.Size!=basis.Size||frame.supportY<=0||frame.supportY>=rendered.Height)throw new Exception("Invalid native size or support.");
                        Rectangle bounds=PoseVariantRenderer.VisibleBounds(rendered);
                        if(bounds.Left<=0||bounds.Top<=0||bounds.Right>=rendered.Width||bounds.Bottom>=rendered.Height)throw new Exception("Source silhouette clipped: "+slot);
                        exported.Add(new{clip=frame.clip,frame=frame.frame,poseId=frame.poseId,source=target.Substring(root.Length+1).Replace('\\','/'),sha256=Hash(target),nativeSize=new[]{rendered.Width,rendered.Height},supportY=frame.supportY,scaleAtSmall=frame.scaleAtSmall,renderer=frame.renderer,variantArtApproved=frame.variantArtApproved,changedLayerPixels=changed,requestedAngleDegrees=frame.renderer=="gaze"?(double?)frame.angleDegrees:null,renderedAngleDegrees=frame.renderer=="gaze"?(double?)renderedAngle:null,angularErrorDegrees=error,approximateDirection=error>.000001,bounds=new[]{bounds.Left,bounds.Top,bounds.Right,bounds.Bottom}});
                    }
                }
                // Verify that exporting did not change the73 original files or any approved input.
                foreach(FamilyFrame frame in plan.frames){Check(frame.legacySource,frame.legacySHA256);foreach(FamilyPart part in frame.parts)Check(part.source,part.sha256);}
                foreach(FamilyBase basis in plan.bases.Values)Check(basis.source,basis.sha256);
                object report=new{passed=true,reviewOnly=true,runtimeIntegrated=false,frameCount=exported.Count,checkedSourceFiles=checkedPaths.Count,registeredLayerFrames=layerCases,exactBaseAliases=baseAliases,pixelsOutsideRegisteredLayersIdentical=true,pawsProtected=true,sourceHashesPreserved=true,nativeSizeAndSupportChecked=true,transparentMarginsChecked=true,directionMode=directionMode,actualDistinctGazeKeyframes=distinctGaze.Count,logicalGazeSlots=45,approximatedGazeSlots=approximated,maximumAngularErrorDegrees=maximumError,not45IndependentlyRenderedHeadDirections=true,frames=exported};
                File.WriteAllText(Path.Combine(output,"render-report.json"),json.Serialize(report));
                Console.WriteLine(json.Serialize(new{passed=true,frameCount=exported.Count,checkedSourceFiles=checkedPaths.Count,registeredLayerFrames=layerCases,exactBaseAliases=baseAliases,directionMode=directionMode,actualDistinctGazeKeyframes=distinctGaze.Count,approximatedGazeSlots=approximated,maximumAngularErrorDegrees=maximumError,output=output}));return 0;
            }
            catch(Exception error){Console.Error.WriteLine(error);return 1;}
            finally{foreach(Bitmap image in images.Values)image.Dispose();}
        }
    }
}
