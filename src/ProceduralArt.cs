using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace WinterDesk {
internal static class Artwork {
 internal static int NewSeed(){return Guid.NewGuid().GetHashCode();}
 internal static Bitmap Canvas(int w,int h){return new Bitmap(Math.Max(1,w),Math.Max(1,h),PixelFormat.Format32bppPArgb);}
 static void Setup(Graphics g){g.SmoothingMode=SmoothingMode.AntiAlias;g.CompositingQuality=CompositingQuality.HighQuality;g.PixelOffsetMode=PixelOffsetMode.HighQuality;}
 internal static float Noise(float x,int seed){int i=(int)Math.Floor(x);float f=x-i;f=f*f*(3-2*f);return Hash(i,seed)*(1-f)+Hash(i+1,seed)*f;}
 static float Hash(int x,int seed){uint n=unchecked((uint)(x*374761393+seed));n=(n^(n>>13))*1274126177;return (n^(n>>16))/(float)uint.MaxValue;}
 internal static Bitmap Garland(int width,int height,float scale,int phase,bool snow){return Garland(width,height,scale,phase,snow,73101);}
 internal static Bitmap Garland(int width,int height,float scale,int phase,bool snow,int seed){return WatercolorTheme.Garland(width,height,phase,seed);}
}
}
