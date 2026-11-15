using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace WinterDesk {
internal static class SnowBallArt {
 internal static void Draw(Graphics graphics,float x,float baseline,float radius,double angle,int variant=0){if(radius<=0)return;var save=graphics.Save();graphics.SmoothingMode=SmoothingMode.AntiAlias;graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;graphics.TranslateTransform(x,baseline-radius);graphics.RotateTransform((float)(angle*180/Math.PI));using(var fill=new SolidBrush(Color.FromArgb(255,249,249,244)))graphics.FillEllipse(fill,-radius*.94f,-radius*.94f,radius*1.88f,radius*1.88f);float extent=radius*128f/124f;graphics.DrawImage(DrawnArt.BallTexture(variant),new RectangleF(-extent,-extent,extent*2,extent*2));graphics.Restore(save);}
}
}
