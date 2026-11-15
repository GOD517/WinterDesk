using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace WinterDesk {
// One connected watercolor grain, with seeded contour, translucent edge and shaded volume.
internal static class VolumeSnow {
 internal const int LegacyFamilies=12,FamilyCount=LegacyFamilies*20;
 internal static int Family(int seed){return (seed&int.MaxValue)%FamilyCount;}
 internal static Bitmap Flake(int size,int seed){var b=Artwork.Canvas(size,size);if(size<2)return b;var r=new Random(seed);int n=9+Family(seed)%7;var points=new PointF[n];double stretch=.65+r.NextDouble()*.32,angle=r.NextDouble()*6.28;for(int i=0;i<n;i++){double a=i*Math.PI*2/n,rad=size*(.31+r.NextDouble()*.10),x=Math.Cos(a)*rad,y=Math.Sin(a)*rad*stretch;points[i]=new PointF((float)(size*.5+x*Math.Cos(angle)-y*Math.Sin(angle)),(float)(size*.5+x*Math.Sin(angle)+y*Math.Cos(angle)));}using(var g=Graphics.FromImage(b)){g.SmoothingMode=SmoothingMode.AntiAlias;using(var path=new GraphicsPath()){path.AddClosedCurve(points,.35f);using(var wash=new PathGradientBrush(path)){wash.CenterPoint=new PointF(size*.38f,size*.33f);wash.CenterColor=Color.FromArgb(250,255,255,255);wash.SurroundColors=new[]{Color.FromArgb(175,221,237,248)};g.FillPath(wash,path);}g.SetClip(path);for(int i=0;i<5;i++){float x=(float)r.NextDouble()*size,y=(float)r.NextDouble()*size;using(var wash=new SolidBrush(Color.FromArgb(12+r.Next(15),209+r.Next(20),228+r.Next(20),250)))g.FillEllipse(wash,x-size*.2f,y-size*.2f,size*.5f,size*.4f);}using(var highlight=new SolidBrush(Color.FromArgb(80,255,255,255)))g.FillEllipse(highlight,size*.28f,size*.22f,size*.27f,size*.22f);}}return b;}
}
}
