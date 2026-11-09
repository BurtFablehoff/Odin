// Sitting's narrow right-flank edge only. Full-frame generation drift is not used.
// Compile in memory with Add-Type; this exporter never creates a native window.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

public static class SitEdgeV10
{
    static double Clamp(double n){return Math.Max(0,Math.Min(1,n));}
    static double Smooth(double n){n=Clamp(n);return n*n*(3-2*n);}
    static string Hash(string p){using(SHA256 h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
    static int[] DenseEdges(Bitmap im)
    {
        int[] edges=new int[im.Height];
        for(int y=0;y<im.Height;y++){edges[y]=-1;for(int x=im.Width-1;x>=0;x--)if(im.GetPixel(x,y).A>128){edges[y]=x;break;}}
        return edges;
    }
    static double Weight(int x,int y,int edge)
    {
        if(y<=725||y>=936||edge<0||x<=edge-16||x>=edge+11)return 0;
        return Smooth((x-(edge-16))/7.0)*Smooth(((edge+11)-x)/4.0)*Smooth((y-725)/20.0)*Smooth((936-y)/14.0);
    }
    static Color Sample(Bitmap im,double x,double y)
    {
        int x0=(int)Math.Floor(x),y0=(int)Math.Floor(y);double fx=x-x0,fy=y-y0;double[] c=new double[4];
        for(int j=0;j<2;j++)for(int i=0;i<2;i++)
        {
            int px=x0+i,py=y0+j;if(px<0||py<0||px>=im.Width||py>=im.Height)continue;
            Color p=im.GetPixel(px,py);double w=(i==0?1-fx:fx)*(j==0?1-fy:fy);
            c[0]+=p.A*w;c[1]+=p.R*p.A*w;c[2]+=p.G*p.A*w;c[3]+=p.B*p.A*w;
        }
        if(c[0]<.5)return Color.FromArgb(0,0,0,0);
        return Color.FromArgb((int)Math.Round(c[0]),(int)Math.Round(c[1]/c[0]),(int)Math.Round(c[2]/c[0]),(int)Math.Round(c[3]/c[0]));
    }
    static Color Mix(Color a,Color b,double w)
    {
        double alpha=a.A*(1-w)+b.A*w;if(alpha<.5)return Color.FromArgb(0,0,0,0);
        return Color.FromArgb((int)Math.Round(alpha),
            (int)Math.Round((a.R*a.A*(1-w)+b.R*b.A*w)/alpha),
            (int)Math.Round((a.G*a.A*(1-w)+b.G*b.A*w)/alpha),
            (int)Math.Round((a.B*a.A*(1-w)+b.B*b.A*w)/alpha));
    }
    public static void Render(string root)
    {
        string folder=Path.Combine(root,"draft-assets/consistency-v10/sit-edge");
        string source=Path.Combine(root,"art/frames/sit/000.png"),generated=Path.Combine(folder,"generated-source.png");
        string originalHash=Hash(source),generatedHash=Hash(generated),candidate=Path.Combine(folder,"sit.png");
        int changed=0,outside=0;int[] edges;
        using(Bitmap basis=new Bitmap(source))using(Bitmap draft=new Bitmap(generated))
        using(Bitmap result=basis.Clone(new Rectangle(0,0,basis.Width,basis.Height),PixelFormat.Format32bppArgb))
        {
            edges=DenseEdges(basis);
            for(int y=726;y<936;y++)for(int x=edges[y]-15;x<edges[y]+11;x++)
            {
                double w=Weight(x,y,edges[y]);if(w<=0)continue;
                // 158 unchanged-feature inliers register generated1254 source to
                // original1024. The narrow patch is translated4.5nativepx inward
                // to remove generation's extra flank width, not resize the body.
                double targetX=x+4.5;
                double sx=1.2248861906135111*targetX+0.00015236177341441777*y+0.3158560354472689;
                double sy=-0.00015236177341441777*targetX+1.2248861906135111*y+0.2040567897505823;
                result.SetPixel(x,y,Mix(basis.GetPixel(x,y),Sample(draft,sx,sy),w));
            }
            for(int y=0;y<1024;y++)for(int x=0;x<1024;x++)if(result.GetPixel(x,y)!=basis.GetPixel(x,y))
            {
                changed++;if(Weight(x,y,edges[y])<=0)outside++;
                if(y<=725||y>=936||x<600)throw new Exception("Protected original artwork changed.");
            }
            if(outside!=0||changed<200)throw new Exception("Right-edge patch missing or escaped its mask.");
            result.Save(candidate,ImageFormat.Png);
        }
        if(Hash(source)!=originalHash||Hash(generated)!=generatedHash)throw new Exception("An original/source PNG changed.");
        object report=new{passed=true,headless=true,windowsCreated=0,ownerApproved=false,runtimeIntegrated=false,
            source="art/frames/sit/000.png",sourceSHA256=originalHash,generatedSource="draft-assets/consistency-v10/sit-edge/generated-source.png",generatedSHA256=generatedHash,
            candidate="draft-assets/consistency-v10/sit-edge/sit.png",candidateSHA256=Hash(candidate),changedPixels=changed,outsideMaskChanged=outside,
            canvas=new[]{1024,1024},rootX=512,supportY=944,sourceOriginalUntouched=true,headEarsFaceExact=true,pawsTailUndersideExact=true,
            mask=new{minYExclusive=725,maxYExclusive=936,innerPixels=16,outerPixels=11,denseAlphaThreshold=128,basisDenseEdges=edges,
                formula="smooth((x-(edge-16))/7)*smooth((edge+11-x)/4)*smooth((y-725)/20)*smooth((936-y)/14)",
                note="Only narrow right rim is editable; original denseedge follows exact originalbody, floorband>=936 retained."},
            registration=new{type="Uniform source similarity transform from unchanged features; narrow patch translated inward4.5px to cancel generated width drift",inliers=158}};
        File.WriteAllText(Path.Combine(folder,"qa-renderer.json"),new JavaScriptSerializer().Serialize(report));
    }
}
