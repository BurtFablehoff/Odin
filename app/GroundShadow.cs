using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace OdinV2
{
    internal static class GroundShadow
    {
        public static RectangleF Bounds(ArtPack art,string clip,int size,double rootX,double floorY,double height)
        {
            double scale=size/(double)art.Manifest.width;double[] footprint=art.Manifest.clips[clip].shadow_footprint;
            double centre=footprint!=null?footprint[0]:512,width=footprint!=null?footprint[1]:330;
            double spread=1+Math.Min(300,Math.Max(0,height))/800;
            float w=(float)Math.Max(28,width*scale*spread),h=(float)((9+Math.Min(250,Math.Max(0,height))*.025)*size/640.0);
            float x=(float)(rootX+(centre-art.Manifest.foot_anchor[0])*scale);
            return new RectangleF(x-w/2,(float)(floorY-h*.6),w,h);
        }
        public static Rectangle SceneBounds(Rectangle sprite,RectangleF shadow)
        {
            Rectangle result=Rectangle.Union(sprite,Rectangle.Ceiling(shadow));result.Inflate(2,2);return result;
        }
        private static void Ellipse(Graphics graphics,RectangleF bounds,int alpha)
        {
            if(alpha<1)return;
            using(GraphicsPath path=new GraphicsPath())
            {
                path.AddEllipse(bounds);
                using(PathGradientBrush brush=new PathGradientBrush(path))
                {
                    brush.CenterColor=Color.FromArgb(alpha,0,0,0);brush.SurroundColors=new[]{Color.FromArgb(0,0,0,0)};
                    graphics.FillPath(brush,path);
                }
            }
        }
        public static void Draw(Graphics graphics,RectangleF bounds,double height)
        {
            double fade=Math.Exp(-Math.Max(0,height)/130);Ellipse(graphics,bounds,(int)Math.Round(29*fade));
            RectangleF contact=new RectangleF(bounds.X+bounds.Width*.16f,bounds.Y+bounds.Height*.29f,bounds.Width*.68f,bounds.Height*.42f);
            Ellipse(graphics,contact,(int)Math.Round(35*fade));
        }
    }
}
