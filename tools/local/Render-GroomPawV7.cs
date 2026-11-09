// Headless candidate exporter. Original grooming pixels outside the small
// duplicate-grounded-paw mask are copied exactly; generated whole-frame drift
// is never introduced into the grooming sequence.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Web.Script.Serialization;

internal static class GroomPawV7
{
    static readonly double[][] shifts = {
        new[]{-4.12322998046875,0.727447509765625},new[]{-2.0789794921875,-1.629730224609375},
        new[]{-1.6923065185546875,0.551483154296875},new[]{-2.913330078125,-0.2900390625},
        new[]{0.56597900390625,0.20068359375},new[]{-1.259613037109375,0.40704345703125},
        new[]{-1.34539794921875,0.74774169921875},new[]{-2.8079833984375,0.2978515625},
        new[]{0.0,0.0},new[]{-3.31707763671875,0.169677734375},
        new[]{-0.6583251953125,-0.85943603515625},new[]{0.8317108154296875,0.293243408203125},
        new[]{0.30609130859375,1.00152587890625},new[]{-0.0732421875,0.727783203125},
        new[]{-0.7548980712890625,1.003265380859375},new[]{0.3273162841796875,-0.004119873046875}
    };
    static double Clamp(double v){return Math.Max(0,Math.Min(1,v));}
    static double Smooth(double v){v=Clamp(v);return v*v*(3-2*v);}
    static string Hash(string p){using(SHA256 h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(p))).Replace("-","").ToLowerInvariant();}
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
        if(c[0]<0.5)return Color.FromArgb(0,0,0,0);
        return Color.FromArgb((int)Math.Round(c[0]),(int)Math.Round(c[1]/c[0]),(int)Math.Round(c[2]/c[0]),(int)Math.Round(c[3]/c[0]));
    }
    static Color Mix(Color a,Color b,double w)
    {
        double alpha=a.A*(1-w)+b.A*w;if(alpha<0.5)return Color.FromArgb(0,0,0,0);
        return Color.FromArgb((int)Math.Round(alpha),
            (int)Math.Round((a.R*a.A*(1-w)+b.R*b.A*w)/alpha),
            (int)Math.Round((a.G*a.A*(1-w)+b.G*b.A*w)/alpha),
            (int)Math.Round((a.B*a.A*(1-w)+b.B*b.A*w)/alpha));
    }
    static int Main(string[] args)
    {
        try
        {
            string root=Path.GetFullPath(args[0]);string folder=Path.Combine(root,"draft-assets/style-consistency-v7/groom");
            string output=Path.Combine(folder,"frames");Directory.CreateDirectory(output);
            string source=Path.Combine(folder,"generated-paw-correction-source.png");List<object> cases=new List<object>();
            using(Bitmap patch=new Bitmap(source))for(int frame=0;frame<16;frame++)
            {
                string original=Path.Combine(root,"art/frames/groom/"+frame.ToString("000")+".png");string before=Hash(original);
                using(Bitmap basis=new Bitmap(original))using(Bitmap result=basis.Clone(new Rectangle(0,0,basis.Width,basis.Height),PixelFormat.Format32bppArgb))
                {
                    int changed=0,outsideChanged=0;double dx=shifts[frame][0],dy=shifts[frame][1];
                    for(int y=895;y<958;y++)for(int x=380;x<463;x++)
                    {
                        double bx=x+dx,by=y+dy;
                        double weight=Smooth((bx-382)/9)*Smooth((457-bx)/8)*Smooth((by-898)/12);
                        if(weight<=0)continue;
                        // Uniform registration to the generated correction source: SIFT/RANSAC,
                        //104 inliers. This sampling affects only the explicit local paw mask.
                        double gx=1.6888853386012581*bx-0.020470970079164444*by-109.11147920136943;
                        // Register the newly drawn belly fringe to the old body support,
                        // rather than inheriting the generated source's lower fur edge.
                        double gy=0.020470970079164444*bx+1.6888853386012581*by-491.2961382904216+15;
                        result.SetPixel(x,y,Mix(basis.GetPixel(x,y),Sample(patch,gx,gy),weight));
                    }
                    for(int y=0;y<1024;y++)for(int x=0;x<1024;x++)if(result.GetPixel(x,y)!=basis.GetPixel(x,y))
                    {
                        changed++;if(x<380||x>=463||y<895||y>=958)outsideChanged++;
                    }
                    // The actual supporting paw and all face/raised-paw/tail pixels stay exact.
                    for(int y=890;y<958;y++)for(int x=464;x<545;x++)
                        if(result.GetPixel(x,y)!=basis.GetPixel(x,y))throw new Exception("Supporting paw changed.");
                    if(outsideChanged!=0||changed<200)throw new Exception("Correction escaped its mask or was absent.");
                    string path=Path.Combine(output,frame.ToString("000")+".png");result.Save(path,ImageFormat.Png);
                    if(Hash(original)!=before)throw new Exception("Original source changed.");
                    cases.Add(new{frame=frame,source="art/frames/groom/"+frame.ToString("000")+".png",sourceSHA256=before,
                        candidate="draft-assets/style-consistency-v7/groom/frames/"+frame.ToString("000")+".png",candidateSHA256=Hash(path),
                        changedPixels=changed,pixelsOutsidePawMaskIdentical=true,supportingPawIdentical=true,registrationShiftTo008=shifts[frame]});
                }
            }
            object report=new{passed=true,headless=true,windowsCreated=0,frameCount=16,ownerApproved=false,runtimeIntegrated=false,
                originalSourcesUnmodified=true,maskBounds=new[]{380,895,463,958},seconds=3.5,loop=true,
                correction="Remove duplicate grounded washing paw; retain raised paw, supporting paw, head, body, tail and original style outside the local paw mask.",
                generatedSource="draft-assets/style-consistency-v7/groom/generated-paw-correction-source.png",generatedSHA256=Hash(source),cases=cases};
            File.WriteAllText(Path.Combine(folder,"qa.json"),new JavaScriptSerializer().Serialize(report));
            Console.WriteLine("Passed16 local grooming corrections; original pixels identical outside paw mask; no windows created.");return 0;
        }
        catch(Exception ex){Console.Error.WriteLine(ex);return 1;}
    }
}
