// Lossless storage framing only. Original pixels and original runtime viewport are retained.
using System;using System.Drawing;using System.Drawing.Imaging;using System.IO;using System.Runtime.InteropServices;
class PadReviewedFrames {
 static void Main(string[] args){Directory.CreateDirectory(args[1]);foreach(string file in Directory.GetFiles(args[0],"*.png"))using(Bitmap source=new Bitmap(file))using(Bitmap padded=new Bitmap(source.Width+32,source.Height+32,PixelFormat.Format32bppArgb)){
  BitmapData a=source.LockBits(new Rectangle(0,0,source.Width,source.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb),b=padded.LockBits(new Rectangle(0,0,padded.Width,padded.Height),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
  try{byte[] row=new byte[source.Width*4];for(int y=0;y<source.Height;y++){Marshal.Copy(IntPtr.Add(a.Scan0,y*a.Stride),row,0,row.Length);Marshal.Copy(row,0,IntPtr.Add(b.Scan0,(y+16)*b.Stride+64),row.Length);}}finally{source.UnlockBits(a);padded.UnlockBits(b);}
  padded.Save(Path.Combine(args[1],Path.GetFileName(file)),ImageFormat.Png);
 }}
}
