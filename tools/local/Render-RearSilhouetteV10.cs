// Candidate-only headless composition: original-front silhouette with generated back-coat layer.
// Uniform skull/pinna registration only; no width-only or body deformation.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Web.Script.Serialization;
using System.Security.Cryptography;
public static class RearSilhouetteV10
{
    const double Scale=.933;
    const double TranslateX=-54.692,TranslateY=-37.579;
    const double BodyScale=1.05,BodyX=-127.2,BodyY=-221.5;
    static double Clamp(double n){return Math.Max(0,Math.Min(1,n));}
    static double Smooth(double n){n=Clamp(n);return n*n*(3-2*n);}
    static string Hash(string p){using(SHA256 s=SHA256.Create())return BitConverter.ToString(s.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
    static bool Tail(int x,int y)
    {
        return y>=790&&y<=958&&x>=220&&x<390;
    }
    static Color Sample(Bitmap image,double x,double y)
    {
        int x0=(int)Math.Floor(x),y0=(int)Math.Floor(y);double fx=x-x0,fy=y-y0;double[] p=new double[4];
        for(int j=0;j<2;j++)for(int i=0;i<2;i++)
        {
            int px=x0+i,py=y0+j;if(px<0||py<0||px>=image.Width||py>=image.Height)continue;
            Color c=image.GetPixel(px,py);double w=(i==0?1-fx:fx)*(j==0?1-fy:fy);p[0]+=w*c.A;p[1]+=w*c.A*c.R;p[2]+=w*c.A*c.G;p[3]+=w*c.A*c.B;
        }
        if(p[0]<.5)return Color.FromArgb(0,0,0,0);
        return Color.FromArgb((int)Math.Round(p[0]),(int)Math.Round(p[1]/p[0]),(int)Math.Round(p[2]/p[0]),(int)Math.Round(p[3]/p[0]));
    }
    static Color Mix(Color a,Color b,double weight)
    {
        double alpha=a.A*(1-weight)+b.A*weight;if(alpha<.5)return Color.FromArgb(0,0,0,0);
        return Color.FromArgb((int)Math.Round(alpha),(int)Math.Round((a.R*a.A*(1-weight)+b.R*b.A*weight)/alpha),(int)Math.Round((a.G*a.A*(1-weight)+b.G*b.A*weight)/alpha),(int)Math.Round((a.B*a.A*(1-weight)+b.B*b.A*weight)/alpha));
    }
    public static void Render(string root)
    {
        string folder=Path.Combine(root,"draft-assets/consistency-v10/rear"),original=Path.Combine(root,"art/frames/sit/000.png"),generated=Path.Combine(folder,"generated-source.png"),candidate=Path.Combine(folder,"rear-front-silhouette-candidate.png");
        string aHash=Hash(original),bHash=Hash(generated);int changed=0,tailChanges=0,alphaChanges=0,fallback=0;
        using(Bitmap a=new Bitmap(original))using(Bitmap b=new Bitmap(generated))
        using(Bitmap result=a.Clone(new Rectangle(0,0,a.Width,a.Height),PixelFormat.Format32bppArgb))
        {
            if(a.Width!=1024||a.Height!=1024||b.Width!=1254||b.Height!=1254)throw new Exception("Unexpected dimensions");
            for(int y=490;y<960;y++)for(int x=360;x<700;x++)
            {
                Color basis=a.GetPixel(x,y);if(Tail(x,y))continue;
                Color head=Sample(b,(x-TranslateX)/Scale,(y-TranslateY)/Scale);
                Color coat=Sample(b,(x-BodyX)/BodyScale,(y-BodyY)/BodyScale);
                Color body=Color.FromArgb(basis.A,coat.R,coat.G,coat.B);
                if(y>=760&&x>=440&&basis.A>128&&coat.A<8)fallback++;
                // Join where the actual head and original body silhouettes have
                // matching right contours (native630..700), not down at760
                // where the extra head-ruff alpha made a horizontal ledge.
                Color pixel=y<=630?head:(y>=700?body:Mix(head,body,Smooth((y-630)/70.0)));
                // Original tail lies in front. Feather its anatomical overlap
                // into the new back coat; only the free tail stays exact.
                if(y>=790&&x<440)pixel=Mix(basis,pixel,Smooth((x-390)/50.0));
                result.SetPixel(x,y,pixel);
            }
            for(int y=0;y<a.Height;y++)for(int x=0;x<a.Width;x++)
            {
                Color p=a.GetPixel(x,y),q=result.GetPixel(x,y);
                if(y>=700&&p.A!=q.A)alphaChanges++;
                if(p!=q){changed++;if(Tail(x,y)||x<360||x>=700||y<490||y>=960)tailChanges++;}
            }
            if(alphaChanges!=0||tailChanges!=0)throw new Exception("Protected alpha/tail/outside body changed");
            result.Save(candidate,ImageFormat.Png);
            result.Save(Path.Combine(folder,"rear.png"),ImageFormat.Png);
        }
        if(Hash(original)!=aHash||Hash(generated)!=bHash)throw new Exception("Source changed");
        File.WriteAllText(Path.Combine(folder,"qa-renderer.json"),new JavaScriptSerializer().Serialize(new{passed=true,headless=true,windowsCreated=0,approved=false,runtimeIntegrated=false,candidate="draft-assets/consistency-v10/rear/rear.png",candidateSHA256=Hash(candidate),changedPixels=changed,originalBodyAlphaChanges=alphaChanges,freeTailAndOutsideChanges=tailChanges,denseBodyPixelsWithoutGeneratedCoverage=fallback,originalFloorSupportY=944,originalBodySilhouetteExactRows=new[]{700,959},headUsesActualBackRGBA=true,headRegistration=new{uniformScale=Scale,x=TranslateX,y=TranslateY,transitionRows=new[]{630,700},method="Solid-pinna/skull-core manual registration; join at matching head/body contours, not ruff-boundary cut"},bodyCoatSampling=new{uniformScale=BodyScale,x=BodyX,y=BodyY,method="Full-coverage generated back-coat inside unchanged original body silhouette"},contactPatchRemoved=true,caveat="Original rounded paw/hock tips retained at base; root explicitly preferred these over the rejected contact-patch flap. Free tail exact and body overlap feathered."}));
    }
}
