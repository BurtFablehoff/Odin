// Independent, headless geometry QA. Loads the development executable in memory;
// never calls its entry point or creates a window.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Web.Script.Serialization;
class DevArtGeometryQA
{
    const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static object Field(object value,string name){return value.GetType().GetField(name,Flags).GetValue(value);}
    static object Call(object value,string name,params object[] arguments){return value.GetType().GetMethod(name,Flags).Invoke(value,arguments);}
    static Rectangle Bounds(Bitmap image)
    {
        BitmapData data=image.LockBits(new Rectangle(Point.Empty,image.Size),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
        try
        {
            if(data.Stride!=image.Width*4)throw new Exception("Unexpected bitmap stride.");
            byte[] bytes=new byte[data.Stride*image.Height];Marshal.Copy(data.Scan0,bytes,0,bytes.Length);
            int left=image.Width,right=-1,top=image.Height,bottom=-1;
            for(int y=0;y<image.Height;y++)for(int x=0;x<image.Width;x++)if(bytes[y*data.Stride+x*4+3]>8)
            {left=Math.Min(left,x);right=Math.Max(right,x);top=Math.Min(top,y);bottom=Math.Max(bottom,y);}
            if(right<0)throw new Exception("Empty development image.");
            return Rectangle.FromLTRB(left,top,right+1,bottom+1);
        }
        finally{image.UnlockBits(data);}
    }
    static int placements,headChecks,stillFrames,gazeFrames,actionFrames;
    static double minimumGap=Double.MaxValue,maximumSupportError,maximumHeadError;
    static void Check(object art,object frame,string clip,int stillId,string label)
    {
        Bitmap image=(Bitmap)Field(frame,"Image");object pose=Field(frame,"Pose");Rectangle visible=Bounds(image);
        foreach(int size in new[]{480,640,800})foreach(int areaLeft in new[]{0,-1920})foreach(bool rightEdge in new[]{false,true})
        {
            object[] marginArgs={clip,size,stillId,PointF.Empty};
            if(!(bool)Call(art,"TryMargins",marginArgs))throw new Exception("No development margins for "+label);
            PointF margins=(PointF)marginArgs[3];
            double x=rightEdge?areaLeft+1920-margins.Y:areaLeft+margins.X;
            Rectangle destination=(Rectangle)Call(frame,"Destination",size,x,900.0,0.0);
            double scale=destination.Width/(double)image.Width;
            double left=destination.Left+visible.Left*scale,right=destination.Left+visible.Right*scale;
            double gap=rightEdge?areaLeft+1920-right:left-areaLeft;
            if(gap<2)throw new Exception("Latest visible fur crosses its 4px edge margin: "+label+" gap="+gap);
            minimumGap=Math.Min(minimumGap,gap);
            double support=destination.Top+Convert.ToDouble(Field(pose,"supportY"))*destination.Height/image.Height;
            maximumSupportError=Math.Max(maximumSupportError,Math.Abs(support-900));
            if(Math.Abs(support-900)>2)throw new Exception("Latest support moved: "+label);
            int id=(int)Field(pose,"id");
            if(stillId>0&&id<=4)
            {
                string family=new[]{"sit","loaf","lay","sleep"}[id-1];object[] headArgs={family,size,x,900.0,PointF.Empty};
                if(!(bool)Call(art,"TryHeadPoint",headArgs))throw new Exception("No latest head tracking point.");
                PointF head=(PointF)headArgs[4];int[] crop=(int[])Field(pose,"headCrop");
                double hx=destination.Left+(crop[0]+crop[2])/2.0*destination.Width/image.Width;
                double hy=destination.Top+(crop[1]+crop[3])/2.0*destination.Height/image.Height;
                double error=Math.Max(Math.Abs(head.X-hx),Math.Abs(head.Y-hy));maximumHeadError=Math.Max(maximumHeadError,error);
                if(error>2)throw new Exception("Head tracking does not match latest native sprite placement: "+label);
                headChecks++;
            }
            placements++;
        }
    }
    static int Main(string[] args)
    {
        try
        {
            Assembly assembly=Assembly.Load(File.ReadAllBytes(args[0]));
            Type type=assembly.GetType("OdinV2.DevArtwork",true);
            using(IDisposable artwork=(IDisposable)Activator.CreateInstance(type,true))
            {
                Array poses=(Array)type.GetProperty("Poses",Flags).GetValue(artwork,null);
                foreach(object pose in poses)
                {
                    int id=(int)Field(pose,"id");Check(artwork,Call(artwork,"Still",id),"sit",id,"still-"+id);stillFrames++;
                }
                foreach(string family in new[]{"sit","loaf","lay"})
                    for(int frame=0;frame<(family=="sit"?19:family=="loaf"?12:14);frame++)
                    {
                        object[] getArgs={"look_"+family,frame,null};
                        if(!(bool)Call(artwork,"TryGet",getArgs))throw new Exception("Missing latest directional frame.");
                        Check(artwork,getArgs[2],"look_"+family,0,family+"-"+frame);gazeFrames++;
                    }
                if(type.GetMethod("HasActionFamily",Flags)!=null)foreach(string clip in new[]{"jump_left","jump_right"})
                    if((bool)Call(artwork,"HasActionFamily",clip))for(int frame=0;frame<16;frame++)
                    {
                        object[] getArgs={clip,frame,null};
                        if(!(bool)Call(artwork,"TryGet",getArgs))throw new Exception("Missing updated action frame.");
                        Check(artwork,getArgs[2],clip,0,clip+"-"+frame);actionFrames++;
                        System.Collections.IDictionary cache=(System.Collections.IDictionary)Field(artwork,"actionCache");
                        if(cache.Count>8)throw new Exception("Action bitmap cache grew beyond its limit.");
                    }
            }
            File.WriteAllText(args[1],new JavaScriptSerializer().Serialize(new{passed=true,headless=true,windowsCreated=0,
                latestStillFrames=stillFrames,latestGazeFrames=gazeFrames,latestActionFrames=actionFrames,edgeAndSupportPlacements=placements,
                allSizes=true,negativeMonitor=true,bothHorizontalEdges=true,actualBitmapAlphaBounds=true,
                minimumVisibleGap=minimumGap,maximumSupportError=maximumSupportError,headTrackingChecks=headChecks,maximumHeadTrackingError=maximumHeadError}));return 0;
        }
        catch(Exception error){File.WriteAllText(args[1],error.ToString());return 1;}
    }
}
