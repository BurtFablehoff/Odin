using System;
using System.Drawing;

namespace PetFramework
{
    // Place a static sprite using its actual support point, not its canvas bottom.
    public static class SpriteSupport
    {
        public static RectangleF Place(Size image,PointF sourceSupport,PointF screenSupport,float renderWidth)
        {
            if(image.Width<=0 || image.Height<=0 || renderWidth<=0 || sourceSupport.Y<0 || sourceSupport.Y>image.Height)
                throw new ArgumentOutOfRangeException("image", "Invalid sprite registration.");
            float scale=renderWidth/image.Width;
            return new RectangleF(screenSupport.X-sourceSupport.X*scale,screenSupport.Y-sourceSupport.Y*scale,
                renderWidth,image.Height*scale);
        }
    }
}
