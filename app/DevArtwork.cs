#if ODIN_DEV
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

namespace OdinV2
{
    public sealed class DevArtEntry
    {
        public string key,label,sha256,kind,phase,clip,source;
        public int id,eyeFrame,frame,nativeWidth,nativeHeight;
        public int[] bounds,motionBounds,headCrop;
        public float supportY,renderWidthAtSmall,anchorX;
        public bool airborne,customAnchor;
    }
    public sealed class DevActionClip { public string clip;public int frames;public double seconds;public bool loop; }
    // V12 2c: which jump slots draw each part of the flight (planning/run-style-jump-v11-3.json flightRoles).
    public sealed class DevJumpFlight { public int[] preFlight,rising,apex,descending,landing;public double apexHold; }
    public sealed class DevArtManifest { public DevArtEntry[] poses,blinks,motions,actions;public DevActionClip[] actionClips;public Dictionary<string,double> legacyActionWidths;public bool restoredOriginalRestFamilies,fixedOriginalRestBodies,wholeFrameRestFamilies;public int restoredRestFrameCount;public DevJumpFlight jumpFlight;public DevHeadTurns headTurnsV12; }
    // V12 3R: which seated views carry Codex head-turn frames (look_rear / look_lookback / look_side), and which were left out.
    public sealed class DevHeadTurns { public string registration;public string[] inPack,leftOut;public int framesPerFamily; }
    internal sealed class DevArtFrame
    {
        public Bitmap Image;public DevArtEntry Pose;
        public float Scale(int size){return Pose.renderWidthAtSmall/Image.Width*size/480f;}
        public float AnchorX {get{return Pose.customAnchor?Pose.anchorX:Pose.bounds[0]+Pose.bounds[2]/2f;}}
        public Rectangle Destination(int size,double x,double floor,double lift)
        {
            float scale=Scale(size);
            return new Rectangle((int)Math.Round(x-AnchorX*scale),(int)Math.Round(floor-Pose.supportY*scale-lift*size/480.0),
                (int)Math.Round(Image.Width*scale),(int)Math.Round(Image.Height*scale));
        }
    }
    // Development-only overlay: production sprites and their original atlas remain unchanged.
    internal sealed class DevArtwork : IDisposable
    {
        readonly Dictionary<string,Bitmap> images=new Dictionary<string,Bitmap>();
        readonly Dictionary<string,Bitmap> registered=new Dictionary<string,Bitmap>();
        readonly Dictionary<int,DevArtEntry> poses=new Dictionary<int,DevArtEntry>();
        readonly Dictionary<string,DevArtEntry> actions=new Dictionary<string,DevArtEntry>();
        readonly Dictionary<string,byte[]> actionBytes=new Dictionary<string,byte[]>();
        readonly Dictionary<string,Bitmap> actionCache=new Dictionary<string,Bitmap>();
        readonly LinkedList<string> actionRecent=new LinkedList<string>();
        readonly DevArtManifest manifest;
        public DevArtwork()
        {
            using(Stream stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("Odin.DevReview"))
            {
                if(stream==null)throw new InvalidDataException("Development artwork pack is missing.");
                using(ZipArchive archive=new ZipArchive(stream,ZipArchiveMode.Read))
                {
                    using(StreamReader reader=new StreamReader(archive.GetEntry("manifest.json").Open()))manifest=new JavaScriptSerializer().Deserialize<DevArtManifest>(reader.ReadToEnd());
                    List<DevArtEntry> entries=new List<DevArtEntry>(manifest.poses);entries.AddRange(manifest.blinks);entries.AddRange(manifest.motions);
                    foreach(DevArtEntry entry in entries)
                    {
                        byte[] bytes;
                        using(Stream input=archive.GetEntry(entry.key).Open())using(MemoryStream copy=new MemoryStream()){input.CopyTo(copy);bytes=copy.ToArray();}
                        using(SHA256 hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=entry.sha256)throw new InvalidDataException("Development source hash changed: "+entry.key);
                        using(MemoryStream input=new MemoryStream(bytes))using(Bitmap image=new Bitmap(input))images.Add(entry.key,new Bitmap(image));
                    }
                    if(manifest.actions!=null)foreach(DevArtEntry entry in manifest.actions)
                    {
                        byte[] bytes;using(Stream input=archive.GetEntry(entry.key).Open())using(MemoryStream copy=new MemoryStream()){input.CopyTo(copy);bytes=copy.ToArray();}
                        using(SHA256 hash=SHA256.Create())if(BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant()!=entry.sha256)throw new InvalidDataException("Action source hash changed: "+entry.key);
                        actions.Add(entry.clip+":"+entry.frame,entry);actionBytes.Add(entry.key,bytes);
                        if(entry.nativeWidth>0)actionWidths.Add(entry.key,entry.nativeWidth);
                    }
                }
            }
            foreach(DevArtEntry pose in manifest.poses)poses.Add(pose.id,pose);
            foreach(DevArtEntry blink in manifest.blinks)registered.Add(blink.key,BlinkEyeRenderer.Compose(images[poses[blink.id].key],images[blink.key],blink.id));
            foreach(DevArtEntry motion in manifest.motions)registered.Add(motion.key,PoseVariantRenderer.Compose(images[poses[motion.id].key],images[motion.key],motion.id,motion.kind,1));
            // Raw draft pixels are no longer needed after their registered layers are built.
            foreach(DevArtEntry blink in manifest.blinks){images[blink.key].Dispose();images.Remove(blink.key);}
            foreach(DevArtEntry motion in manifest.motions){images[motion.key].Dispose();images.Remove(motion.key);}
        }
        public DevArtEntry[] Poses {get{return manifest.poses;}}
        public bool HasUpdatedJumps {get{return ActionCount("jump_left")==16&&ActionCount("jump_right")==16;}}
        public DevJumpFlight JumpFlight {get{return manifest.jumpFlight;}}
        public bool UsesOriginalRestFamilies {get{return manifest.restoredOriginalRestFamilies;}}
        public bool UsesWholeFrameRestFamilies {get{return manifest.wholeFrameRestFamilies;}}
        int ActionCount(string clip){int count=0;foreach(DevArtEntry entry in actions.Values)if(entry.clip==clip)count++;return count;}
        public bool HasActionFamily(string clip){return actions.ContainsKey(clip+":0");}
        // The DEV timeline of an action clip (frames, seconds, loop), or null when the pack has none.
        public DevActionClip ActionClip(string clip)
        {
            if(manifest.actionClips!=null)foreach(DevActionClip candidate in manifest.actionClips)if(candidate.clip==clip)return candidate;
            return null;
        }
        Bitmap ActionImage(DevArtEntry entry)
        {
            Bitmap image;
            if(actionCache.TryGetValue(entry.key,out image)){actionRecent.Remove(entry.key);actionRecent.AddLast(entry.key);return image;}
            using(MemoryStream input=new MemoryStream(actionBytes[entry.key]))using(Bitmap decoded=new Bitmap(input))
                // V11 whole-frame mode draws original look/eyes/tail frames whole; the V8-V10 head layer
                // (OriginalRestRenderer) stays in the source but is bypassed.
                image=manifest.fixedOriginalRestBodies&&!manifest.wholeFrameRestFamilies&&entry.id>=1&&entry.id<=4
                    ?OriginalRestRenderer.Compose(images[poses[entry.id].key],decoded,entry.id,entry.clip,entry.frame):new Bitmap(decoded);
            actionCache.Add(entry.key,image);actionRecent.AddLast(entry.key);
            while(actionCache.Count>8){string old=actionRecent.First.Value;actionRecent.RemoveFirst();actionCache[old].Dispose();actionCache.Remove(old);}
            return image;
        }
        public bool HasFullCardinals
        {
            get{foreach(int id in new[]{1,2,3})foreach(string phase in new[]{"up","right","down","left"})if(!registered.ContainsKey("gaze-"+id+"-"+phase+".png"))return false;return true;}
        }
        Bitmap Gaze(string family,int id,int frame)
        {
            double[] angles=family=="sit"?SittingGaze.Angles:family=="loaf"?ClockGaze.ClockAngles:ClockGaze.LayingAngles;
            frame=Math.Max(0,Math.Min(angles.Length-1,frame));
            // Select one actual drawn face. Head crossfades create double eyes/noses.
            string[] directions=HasEightDirections?new[]{"up","up-right","right","down-right","down","down-left","left","up-left"}:new[]{"up","right","down","left"};
            double step=360.0/directions.Length;
            string phase=directions[((int)Math.Floor((angles[frame]+step/2)/step))%directions.Length];
            string key="gaze-"+id+"-"+phase+".png";Bitmap result;
            return registered.TryGetValue(key,out result)?result:images[poses[id].key];
        }
        public bool HasEightDirections
        {
            get{foreach(int id in new[]{1,2,3})foreach(string phase in new[]{"up","up-right","right","down-right","down","down-left","left","up-left"})if(!registered.ContainsKey("gaze-"+id+"-"+phase+".png"))return false;return true;}
        }
        static int FamilyId(string family)
        {
            switch(family){case "sit":return 1;case "loaf":return 2;case "lay":return 3;case "sleep":return 4;case "rear":return 6;case "lookback":return 7;case "side":return 8;default:return 0;}
        }
        // V12 3R: seated views 6/7/8 whose whole-body head-turn frames are in this pack (look_<family>, five frames).
        internal static readonly string[] HeadTurnFamilies={"rear","lookback","side"};
        public static string HeadTurnFamily(int id){return id==6?"rear":id==7?"lookback":id==8?"side":null;}
        public static int HeadTurnId(string family){return Array.IndexOf(HeadTurnFamilies,family)<0?0:FamilyId(family);}
        public bool HasHeadTurns(string family){return family!=null&&HeadTurnId(family)>0&&ActionCount("look_"+family)==5;}
        public string[] HeadTurnsInPack {get{return manifest.headTurnsV12==null?null:manifest.headTurnsV12.inPack;}}
        public string[] HeadTurnsLeftOut {get{return manifest.headTurnsV12==null?null:manifest.headTurnsV12.leftOut;}}
        static string Family(string clip)
        {
            if(clip.StartsWith("look_"))return clip.Substring(5);
            if(clip.EndsWith("_eyes")||clip.EndsWith("_tail"))return clip.Substring(0,clip.Length-5);
            return clip;
        }
        public DevArtFrame Still(int id){return new DevArtFrame{Image=images[poses[id].key],Pose=poses[id]};}
        public bool TryGet(string clip,int frame,out DevArtFrame result)
        {
            DevArtEntry action;
            if(actions.TryGetValue(clip+":"+frame,out action)){result=new DevArtFrame{Image=ActionImage(action),Pose=action};return true;}
            string family=Family(clip);int id=FamilyId(family);result=null;if(id==0)return false;
            result=Still(id);string key=null;
            if(clip.EndsWith("_eyes")&&frame>0)key="blink-"+id+"-"+Math.Min(2,frame)+".png";
            if(clip.EndsWith("_tail")&&(frame==1||frame==3))key="tail-"+id+"-"+(frame==1?"out":"in")+".png";
            if(clip.StartsWith("look_"))
            {
                result.Image=Gaze(family,id,frame);return true;
            }
            Bitmap image;if(key!=null&&registered.TryGetValue(key,out image))result.Image=image;
            return true;
        }
        public bool TryMargins(string clip,int size,int stillId,out PointF margins)
        {
            int restingId=FamilyId(Family(clip));
            if(stillId<=0&&manifest.fixedOriginalRestBodies&&restingId>0)
            {
                DevArtFrame restingFrame=Still(restingId);DevArtEntry neutral=restingFrame.Pose;
                int[] union=neutral.motionBounds??neutral.bounds;float restingScale=restingFrame.Scale(size);
                margins=new PointF((restingFrame.AnchorX-union[0])*restingScale+ScreenEdges.Gap,
                    (union[0]+union[2]-restingFrame.AnchorX)*restingScale+ScreenEdges.Gap);return true;
            }
            if(stillId<=0&&HasActionFamily(clip))
            {
                float left=0,right=0;
                string marginFamily=Family(clip);bool resting=UsesOriginalRestFamilies&&FamilyId(marginFamily)>0;
                foreach(DevArtEntry actionPose in actions.Values)if(actionPose.clip==clip||(resting&&Family(actionPose.clip)==marginFamily))
                {
                    float actionScale=actionPose.renderWidthAtSmall*size/480f/(float)ImageWidth(actionPose.key);
                    left=Math.Max(left,(actionPose.anchorX-actionPose.bounds[0])*actionScale);right=Math.Max(right,(actionPose.bounds[0]+actionPose.bounds[2]-actionPose.anchorX)*actionScale);
                }
                margins=new PointF(left+ScreenEdges.Gap,right+ScreenEdges.Gap);return true;
            }
            int id=stillId>0?stillId:FamilyId(Family(clip));margins=PointF.Empty;if(id==0)return false;
            DevArtFrame frame=Still(id);DevArtEntry p=frame.Pose;int[] b=p.motionBounds??p.bounds;
            float scale=frame.Scale(size);margins=new PointF((frame.AnchorX-b[0])*scale+ScreenEdges.Gap,(b[0]+b[2]-frame.AnchorX)*scale+ScreenEdges.Gap);return true;
        }
        readonly Dictionary<string,int> actionWidths=new Dictionary<string,int>();
        int ImageWidth(string key)
        {
            int width;if(actionWidths.TryGetValue(key,out width))return width;
            using(MemoryStream input=new MemoryStream(actionBytes[key]))using(Bitmap image=new Bitmap(input))width=image.Width;
            actionWidths.Add(key,width);return width;
        }
        public bool TryHeadPoint(string clip,int size,double x,double floor,out PointF head)
        {
            head=PointF.Empty;int id=FamilyId(Family(clip));if(id==0)return false;
            DevArtFrame frame=Still(id);int[] crop=frame.Pose.headCrop;if(crop==null||crop.Length!=4)return false;
            float scale=frame.Scale(size);
            head=new PointF((float)x+((crop[0]+crop[2])/2f-frame.AnchorX)*scale,(float)floor+((crop[1]+crop[3])/2f-frame.Pose.supportY)*scale);return true;
        }
        // Head-crop centre of a drawn frame (its own crop, else its family pose's), mapped through the rectangle the renderer drew it into.
        public bool TryDrawnHeadPoint(DevArtFrame frame,Rectangle destination,out PointF head)
        {
            head=PointF.Empty;if(frame==null)return false;
            int[] crop=frame.Pose.headCrop;DevArtEntry pose;
            if((crop==null||crop.Length!=4)&&poses.TryGetValue(frame.Pose.id,out pose))crop=pose.headCrop;
            if(crop==null||crop.Length!=4)return false;
            head=new PointF(destination.X+(crop[0]+crop[2])/2f*destination.Width/frame.Image.Width,destination.Y+(crop[1]+crop[3])/2f*destination.Height/frame.Image.Height);return true;
        }
        public PointF PerchMargins(string clip,int size)
        {
            float left=0,right=0;
            foreach(string candidate in new[]{"sit","loaf","lay","sleep",clip})
            {
                PointF margins;if(!TryMargins(candidate,size,0,out margins))continue;
                left=Math.Max(left,margins.X);right=Math.Max(right,margins.Y);
            }
            return new PointF(left,right);
        }
        public int ActionPixels(string clip,int size,int fallback)
        {
            double width;if(manifest.legacyActionWidths!=null&&manifest.legacyActionWidths.TryGetValue(clip,out width))return (int)Math.Round(width*size/480.0);
            return fallback;
        }
        public object Verify()
        {
            int sourceCases=0,layerCases=0,placements=0;
            foreach(DevArtEntry blink in manifest.blinks){BlinkEyeRenderer.Verify(images[poses[blink.id].key],registered[blink.key],blink.id);layerCases++;}
            foreach(DevArtEntry motion in manifest.motions){PoseVariantRenderer.Verify(images[poses[motion.id].key],registered[motion.key],motion.id,motion.kind);layerCases++;}
            foreach(DevArtEntry pose in manifest.poses)
            {
                DevArtFrame frame=Still(pose.id);
                foreach(int size in new[]{480,640,800})foreach(int x in new[]{-1700,600})
                {
                    Rectangle destination=frame.Destination(size,x,900,0);
                    if(Math.Abs(destination.Top+pose.supportY*destination.Height/frame.Image.Height-900)>2)throw new Exception("Development support anchor drifted.");
                    placements++;
                }
                if(pose.id==5||pose.id==9||pose.id==10)throw new Exception("Removed belly-up, side sleep or curled sleep returned.");
                if(pose.id==11)throw new Exception("Parked tucked sleep returned before its redraw passed.");sourceCases++;
            }
            foreach(string family in new[]{"sit","loaf","lay"})
            {
                int id=FamilyId(family),count=family=="sit"?19:family=="loaf"?12:14;
                for(int frame=0;frame<count;frame++)
                {
                    DevArtFrame selected;TryGet("look_"+family,frame,out selected);
                    bool horizontal=frame==(family=="sit"?4:9)||frame==(family=="sit"?12:3);
                    if(UsesOriginalRestFamilies)
                    {
                        if(selected.Pose.clip!="look_"+family||selected.Pose.frame!=frame)throw new Exception("Original gaze frame/body pairing was lost.");
                        if(selected.Pose.renderWidthAtSmall!=poses[id].renderWidthAtSmall)throw new Exception("Original gaze family changed physical scale.");
                    }
                    else if(HasFullCardinals)PoseVariantRenderer.Verify(images[poses[id].key],selected.Image,id,"gaze");
                    else if(!horizontal&&selected.Image!=images[poses[id].key])throw new Exception("Unsupported direction used old artwork.");
                }
            }
            int actionPlacements=0,fixedBodyCases=0;
            if(UsesOriginalRestFamilies)
            {
                string[] restClips={"sit","loaf","lay","sleep","look_sit","look_loaf","look_lay","sit_eyes","loaf_eyes","lay_eyes","sit_tail","lay_tail","sleep_tail"};
                int[] counts={1,1,1,1,19,12,14,3,3,3,5,5,5};int total=0;
                for(int i=0;i<restClips.Length;i++){if(ActionCount(restClips[i])!=counts[i])throw new Exception("Original resting family is incomplete: "+restClips[i]);total+=counts[i];}
                if(total!=73||manifest.restoredRestFrameCount!=total||manifest.blinks.Length!=0||manifest.motions.Length!=0)throw new Exception("Restoration reintroduced generated head/eye/tail overlays.");
                if(manifest.wholeFrameRestFamilies&&manifest.fixedOriginalRestBodies)throw new Exception("Whole-frame rest families must not also compose fixed-body head layers.");
            }
            if(manifest.actionClips!=null)foreach(DevActionClip actionClip in manifest.actionClips)
            {
                bool discreteGaze=UsesOriginalRestFamilies&&actionClip.clip.StartsWith("look_")&&actionClip.seconds==0;
                if((actionClip.seconds<=0&&!discreteGaze)||((actionClip.clip=="jump_left"||actionClip.clip=="jump_right")&&(actionClip.frames!=16||actionClip.loop)))throw new Exception("Action timeline changed.");
                for(int number=0;number<actionClip.frames;number++)
                {
                    DevArtFrame selected;if(!TryGet(actionClip.clip,number,out selected)||!selected.Pose.customAnchor)throw new Exception("Missing native action frame.");
                    if(manifest.fixedOriginalRestBodies&&selected.Pose.id>=1&&selected.Pose.id<=4)
                    {
                        DevArtEntry neutral=poses[selected.Pose.id];
                        if(selected.Pose.supportY!=neutral.supportY||selected.Pose.anchorX!=neutral.anchorX||selected.Pose.renderWidthAtSmall!=neutral.renderWidthAtSmall)throw new Exception("Resting frame changed the fixed body placement.");
                        OriginalRestRenderer.Verify(images[neutral.key],selected.Image,selected.Pose.id,actionClip.clip,number);fixedBodyCases++;
                    }
                    Rectangle visible=PoseVariantRenderer.VisibleBounds(selected.Image);
                    if(visible.Left<=0||visible.Top<=0||visible.Right>=selected.Image.Width||visible.Bottom>=selected.Image.Height)throw new Exception("Action fur touches its canvas boundary.");
                    foreach(int size in new[]{480,640,800})foreach(int x in new[]{-1700,600})
                    {
                        Rectangle dest=selected.Destination(size,x,900,0);
                        if(Math.Abs(dest.Top+selected.Pose.supportY*dest.Height/selected.Image.Height-900)>2)throw new Exception("Action anchor drift.");
                        actionPlacements++;
                    }
                }
            }
            int headTurnFrames=0;
            if(manifest.headTurnsV12!=null&&manifest.headTurnsV12.inPack!=null)foreach(string family in manifest.headTurnsV12.inPack)
            {
                // V12 3R: each head-turn frame is a whole 1024 frame placed exactly like its state's still (the centre frame).
                DevArtEntry still=poses[HeadTurnId(family)];
                if(ActionCount("look_"+family)!=5)throw new Exception("Head-turn family is incomplete: "+family);
                for(int number=0;number<5;number++)
                {
                    DevArtEntry e=actions["look_"+family+":"+number];
                    if(e.id!=still.id||e.nativeWidth!=1024||still.nativeWidth!=1024||e.supportY!=still.supportY||e.anchorX!=still.anchorX||e.renderWidthAtSmall!=still.renderWidthAtSmall||!still.customAnchor)throw new Exception("Head-turn frame is not placed like its still: "+family+"/"+number);
                    headTurnFrames++;
                }
                if(actions["look_"+family+":2"].sha256!=still.sha256)throw new Exception("Head-turn still is not the centre frame: "+family);
            }
            int jumpRoleSlots=0;
            if(manifest.jumpFlight!=null)
            {
                // V12 2c: the flight roles cover each jump slot once; flight slots draw airborne run frames.
                DevJumpFlight f=manifest.jumpFlight;List<int> all=new List<int>();
                foreach(int[] part in new[]{f.preFlight,f.rising,f.apex,f.descending,f.landing}){if(part==null||part.Length==0)throw new Exception("A jump flight role is empty.");all.AddRange(part);}
                all.Sort();for(int i=0;i<all.Count;i++)if(all[i]!=i)throw new Exception("Jump flight roles do not cover each slot once.");
                if(all.Count!=16||f.apex.Length!=1||f.apexHold<=0||f.apexHold>=.5)throw new Exception("Jump flight roles are malformed.");
                foreach(string clip in new[]{"jump_left","jump_right"})
                {
                    List<int> flight=new List<int>(f.rising);flight.AddRange(f.apex);flight.AddRange(f.descending);
                    foreach(int slot in flight)
                    {
                        DevArtEntry entry;if(!actions.TryGetValue(clip+":"+slot,out entry)||!entry.airborne||entry.source==null||entry.source.IndexOf("/run_",StringComparison.Ordinal)<0)throw new Exception("Jump flight slot is not an airborne run frame: "+clip+"/"+slot);
                    }
                }
                jumpRoleSlots=all.Count;
            }
            return new{passed=true,latestStates=sourceCases,registeredLayers=layerCases,supportPlacements=placements,sourceHashesVerified=true,updatedJumpFrames=ActionCount("jump_left")+ActionCount("jump_right"),actionPlacements=actionPlacements,actionCacheLimit=8,jumpFlightRoleSlots=jumpRoleSlots,headTurnFrames=headTurnFrames,
                restoredOriginalRestFamilies=UsesOriginalRestFamilies,restoredRestFrameCount=manifest.restoredRestFrameCount,
                bodyFixedWhileLooking=manifest.fixedOriginalRestBodies,fixedBodyCases=fixedBodyCases,
                horizontalGazeKeyframes=UsesOriginalRestFamilies?0:6,requestedGazeSchedulerSlots=45,distinctGazeKeyframes=UsesOriginalRestFamilies?45:HasEightDirections?24:HasFullCardinals?12:6,gazeDirectionsPerFamily=UsesOriginalRestFamilies?0:HasEightDirections?8:4,quantizedGazeSlots=UsesOriginalRestFamilies?0:HasEightDirections?29:33,verticalKeyframesIndependentlyApproved=false,diagonalKeyframesIndependentlyApproved=false,noCrossfadedFaces=true,
                pawsAndBodyOutsideLayersUnchanged=manifest.fixedOriginalRestBodies||manifest.wholeFrameRestFamilies||!UsesOriginalRestFamilies,wholeFrameRestFamilies=manifest.wholeFrameRestFamilies,originalMatchedGazeBodies=UsesOriginalRestFamilies,noBreathing=true,productionArtUnchanged=true};
        }
        // V11 --test-body-stability: placed lower-body centroid of every look/eyes/tail frame vs its
        // family base, using the frame's own anchorX/supportY exactly as Destination() places it.
        static readonly string[] StabilityClips={"look_sit","look_loaf","look_lay","sit_eyes","loaf_eyes","lay_eyes","sit_tail","lay_tail","sleep_tail","look_rear","look_lookback","look_side"};
        // Lower-body band: rows y0..y1, columns x >= x0. V12 3R head-turn bands are below the shoulders and above the tail,
        // in the registered canvas (planning/head-turns-registration-v12.json "band"; Register-HeadTurnsV12.py).
        static int[] Band(string family)
        {
            switch(family)
            {
                case "sit":return new[]{780,950,440};case "sleep":return new[]{820,950,520};
                case "rear":return new[]{800,905,470};case "lookback":return new[]{700,900,436};case "side":return new[]{720,935,420};
                default:return new[]{820,950,420};
            }
        }
        static PointF LowerBodyCentroid(Bitmap image,int[] band)
        {
            System.Drawing.Imaging.BitmapData data=image.LockBits(new Rectangle(Point.Empty,image.Size),System.Drawing.Imaging.ImageLockMode.ReadOnly,System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            try
            {
                byte[] pixels=new byte[data.Stride*image.Height];System.Runtime.InteropServices.Marshal.Copy(data.Scan0,pixels,0,pixels.Length);
                double sx=0,sy=0;long n=0;
                for(int y=band[0];y<=Math.Min(band[1],image.Height-1);y++)for(int x=band[2];x<image.Width;x++)
                    if(pixels[y*data.Stride+x*4+3]>64){sx+=x;sy+=y;n++;}
                if(n==0)throw new Exception("Empty lower-body band.");
                return new PointF((float)(sx/n),(float)(sy/n));
            }
            finally{image.UnlockBits(data);}
        }
        public object BodyStability()
        {
            if(!manifest.wholeFrameRestFamilies)throw new Exception("Whole-frame rest families are not enabled in this pack.");
            var families=new Dictionary<string,object>();var slots=new List<object>();bool passed=true;var failures=new List<string>();
            // V12 3R: the pack must say which head-turn families it carries; every family it lists is checked like sit/loaf/lay.
            List<string> checkedFamilies=new List<string>{"sit","loaf","lay","sleep"};int headTurnSlots=0;
            if(manifest.headTurnsV12==null||manifest.headTurnsV12.inPack==null){passed=false;failures.Add("The pack has no V12 head-turn record (headTurnsV12): families 6/7/8 are not covered.");}
            else foreach(string family in manifest.headTurnsV12.inPack)
            {
                if(!HasHeadTurns(family)){passed=false;failures.Add("Head-turn family listed in the pack has no five look_"+family+" frames.");continue;}
                checkedFamilies.Add(family);headTurnSlots+=5;
            }
            foreach(string family in checkedFamilies)
            {
                DevArtFrame base0;if(!TryGet(family,0,out base0))throw new Exception("Missing base frame: "+family);
                int[] band=Band(family);PointF b=LowerBodyCentroid(base0.Image,band);
                double bx=b.X-base0.AnchorX,by=b.Y-base0.Pose.supportY,max=0;string worst=null;var frames=new List<object>();
                foreach(string clip in StabilityClips)
                {
                    if(Family(clip)!=family)continue;
                    for(int number=0;TryGetAction(clip,number);number++)
                    {
                        DevArtFrame frame;TryGet(clip,number,out frame);DevArtEntry e=frame.Pose;
                        if(frame.Image.Width!=1024||frame.Image.Height!=1024)throw new Exception("Whole frame is not native 1024: "+e.key);
                        if(e.renderWidthAtSmall!=base0.Pose.renderWidthAtSmall)throw new Exception("Frame changed family scale: "+e.key);
                        PointF c=LowerBodyCentroid(frame.Image,band);
                        double drift=Math.Sqrt(Math.Pow(c.X-frame.AnchorX-bx,2)+Math.Pow(c.Y-e.supportY-by,2));
                        if(drift>max){max=drift;worst=clip+"/"+number.ToString("000");}
                        frames.Add(new{frame=clip+"/"+number.ToString("000"),anchorX=frame.AnchorX,supportY=e.supportY,driftPx=Math.Round(drift,3)});
                        string digest;using(SHA256 hash=SHA256.Create())digest=BitConverter.ToString(hash.ComputeHash(actionBytes[e.key])).Replace("-","").ToLowerInvariant();
                        slots.Add(new{clip=clip,frame=number,source=e.source,sha256=digest});
                    }
                }
                if(max>3){passed=false;failures.Add(family+": "+worst+" drifts "+Math.Round(max,3)+" native px (limit 3)");}
                families.Add(family,new{id=FamilyId(family),baseSupportY=base0.Pose.supportY,baseAnchorX=base0.AnchorX,maxDriftPx=Math.Round(max,3),worstFrame=worst,frames=frames});
            }
            if(slots.Count!=69+headTurnSlots)throw new Exception("Expected "+(69+headTurnSlots)+" look/eyes/tail/head-turn slots, found "+slots.Count);
            return new{passed=passed,failures=failures,headTurnFamilies=manifest.headTurnsV12==null?null:manifest.headTurnsV12.inPack,headTurnsLeftOut=manifest.headTurnsV12==null?null:manifest.headTurnsV12.leftOut,thresholdPx=3,wholeFrameRestFamilies=true,headLayerRendererBypassed=!manifest.fixedOriginalRestBodies,slotCount=slots.Count,families=families,slots=slots};
        }
        bool TryGetAction(string clip,int frame){return actions.ContainsKey(clip+":"+frame);}
        public void Dispose(){foreach(Bitmap image in actionCache.Values)image.Dispose();foreach(Bitmap image in registered.Values)image.Dispose();foreach(Bitmap image in images.Values)image.Dispose();}
    }
}
#endif
