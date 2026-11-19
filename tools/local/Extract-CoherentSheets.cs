// A21/A22: extract discrete drawn cells only. No synthesized motion or pixel deformation.
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
class ExtractCoherentSheets {
 static void Main(string[] args) {
  string folder=Path.Combine(args[0],"draft-assets/coherent-motion-v11");
  Directory.CreateDirectory(Path.Combine(folder,"cells"));
  foreach(string name in new[]{"sit-loaf-sheet-b","sit-lay-sheet","loaf-lay-sheet"})using(Bitmap source=new Bitmap(Path.Combine(folder,"raw/"+name+".png"))) {
   if(source.Width%3!=0||source.Height%2!=0)throw new Exception("Invalid sheet layout.");
   int h=source.Height/2;
   for(int row=0;row<2;row++) {
    int[] cuts=new[]{0,source.Width/3,source.Width*2/3,source.Width};
    for(int k=1;k<3;k++) {
     double best=double.MaxValue;int center=cuts[k];
     for(int x=center-70;x<=center+70;x++) {
      int occupied=0;for(int y=row*h;y<(row+1)*h;y++)if(source.GetPixel(x,y).A>128)occupied++;
      double cost=occupied+Math.Abs(x-center)*.001;if(cost<best){best=cost;cuts[k]=x;}
     }
    }
    for(int col=0;col<3;col++)using(Bitmap cell=source.Clone(new Rectangle(cuts[col],row*h,cuts[col+1]-cuts[col],h),PixelFormat.Format32bppArgb))cell.Save(Path.Combine(folder,"cells/"+name+"-"+(row*3+col)+".png"),ImageFormat.Png);
   }
  }
 }
}
