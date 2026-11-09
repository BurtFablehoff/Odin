using System;
using System.Drawing;
namespace OdinV2
{
    internal static class SpriteSupportTest
    {
        public static object Run()
        {
            int checks=0;
            foreach(int width in new[]{1024,1254,1983})
            foreach(int size in new[]{480,640,800})
            foreach(int floor in new[]{-120,340,1080})
            foreach(float fraction in new[]{0f,.72f,.9140625f,1f})
            {
                Size image=new Size(width,width==1983?793:width);
                PointF source=new PointF(width*.51f,image.Height*fraction);
                RectangleF rect=PetFramework.SpriteSupport.Place(image,source,new PointF(-300,floor),size);
                if(Math.Abs(rect.Top+rect.Height*fraction-floor)>.0003)throw new Exception("Static support misplaced.");
                if(Math.Abs(rect.Left+rect.Width*source.X/image.Width+300)>.0003)throw new Exception("Horizontal support misplaced.");checks++;
            }
            return new{passed=true,placementsChecked=checks,nonSquareCanvas=true,negativeMonitor=true,allDisplaySizes=true,staticPlacement=true};
        }
    }
}
