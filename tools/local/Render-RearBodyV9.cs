// Registered generated body candidate; original head, tail and support retained.
// Invoked with PowerShell Add-Type in memory. No executable or app is created.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

public static class RearBodyV9
{
    static double Clamp(double n){return Math.Max(0,Math.Min(1,n));}
    static double Smooth(double n){n=Clamp(n);return n*n*(3-2*n);}
    static string Hash(string p){using(SHA256 h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
    static double Weight(int x,int y)
    {
        if(x<440||x>=820||y<=760||y>=1128)return 0;
        double weight=Smooth((y-760)/40.0)*Smooth((1128-y)/28.0);
        // Tail is in front of the body. Retain it and the old lower-left overlap
        // exactly; feather only the body above/right of that protected region.
        weight*=1-Smooth((y-975)/50.0)*(1-Smooth((x-690)/25.0));
        return weight;
    }
    static Color Sample(Bitmap im,double x,double y)
    {
        int x0=(int)Math.Floor(x),y0=(int)Math.Floor(y);double fx=x-x0,fy=y-y0;
        double[] c=new double[4];
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
        string folder=Path.Combine(root,"draft-assets/style-consistency-v9/rear");
        string source=Path.Combine(root,"draft-assets/style-consistency-v7/focused-rear/rear-attempt2.png");
        string generated=Path.Combine(folder,"generated-fuller-source.png");
        string candidate=Path.Combine(folder,"rear-fuller-candidate.png");
        string originalHash=Hash(source),generatedHash=Hash(generated);int changed=0,outsideChanged=0;
        // ONE uniform scale for generated draft registration to unchanged scene
        // height/support. No width-only stretch, per-part anatomy warp, cropping
        // of the underside, or resizing the complete final original sprite.
        double registration=634.0/568.0;
        using(Bitmap basis=new Bitmap(source))using(Bitmap draft=new Bitmap(generated))
        using(Bitmap result=basis.Clone(new Rectangle(0,0,basis.Width,basis.Height),PixelFormat.Format32bppArgb))
        {
            for(int y=761;y<1128;y++)for(int x=440;x<820;x++)
            {
                double w=Weight(x,y);if(w<=0)continue;
                double sx=630+(x-630)*registration,sy=461+(y-571)*registration;
                result.SetPixel(x,y,Mix(basis.GetPixel(x,y),Sample(draft,sx,sy),w));
            }
            for(int y=0;y<basis.Height;y++)for(int x=0;x<basis.Width;x++)
                if(result.GetPixel(x,y)!=basis.GetPixel(x,y))
                {
                    changed++;if(Weight(x,y)==0)outsideChanged++;
                    if(y<=760||y>=1128||(x<=690&&y>=1025))throw new Exception("Protected head, tail or contact changed.");
                }
            if(changed<1000||outsideChanged!=0)throw new Exception("Body correction missing or escaped its mask.");
            result.Save(candidate,ImageFormat.Png);
        }
        if(Hash(source)!=originalHash||Hash(generated)!=generatedHash)throw new Exception("A source file changed.");
        object report=new{passed=true,headless=true,windowsCreated=0,ownerApproved=false,runtimeIntegrated=false,
            originalSource="draft-assets/style-consistency-v7/focused-rear/rear-attempt2.png",originalSHA256=originalHash,
            generatedSource="draft-assets/style-consistency-v9/rear/generated-fuller-source.png",generatedSHA256=generatedHash,
            candidate="draft-assets/style-consistency-v9/rear/rear-fuller-candidate.png",candidateSHA256=Hash(candidate),
            changedPixels=changed,outsideBodyMaskChanged=outsideChanged,headEarsExact=true,tailExact=true,contactExact=true,
            canvasWidth=1254,canvasHeight=1254,supportY=1139,scaleAtSmall=0.35073723591549294,
            generatedDraftRegistration=new{scale=registration,sourceTop=461,targetTop=571,sourceSupport=1095,targetSupport=1139,
                method="Uniform generated-draft scale/translation within local body mask only; final original head/tail/contact stay pixel-exact"},
            mask=new{editableBounds=new[]{440,761,820,1128},protectedHeadBottom=760,protectedContactTop=1128,
                protectedTail=new[]{0,1025,691,1254},feather="40px upper body,28px lower contact,50px/25px around existing tail overlap"}};
        File.WriteAllText(Path.Combine(folder,"qa-renderer.json"),new JavaScriptSerializer().Serialize(report));
    }
}
