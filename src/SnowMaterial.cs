using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace WinterDesk {
internal static class SnowMaterial {
 internal static int Padding(SnowModel m){return (int)Math.Ceiling(18*m.Scale);}
 internal static int DropSpace(SnowModel m){return (int)Math.Ceiling(16*m.Scale);}
 internal static GraphicsPath Outline(SnowModel m,bool window){int pad=window?Padding(m):0;float baseline=m.Height;var top=new PointF[m.Depth.Length+1];for(int i=0;i<m.Depth.Length;i++)top[i]=new PointF(pad+Math.Min(m.Width,i*m.Cell),baseline-m.Depth[i]);top[top.Length-1]=new PointF(pad+m.Width,baseline-m.Depth[m.Depth.Length-1]);var path=new GraphicsPath();path.AddCurve(top,.28f);float right=pad+m.Width;
  if(window){float rw=Math.Min(pad-2,m.Depth[m.Depth.Length-1]*.60f),rh=Math.Min(12*m.Scale,m.Depth[m.Depth.Length-1]*.32f),lw=Math.Min(pad-2,m.Depth[0]*.60f),lh=Math.Min(12*m.Scale,m.Depth[0]*.32f);
   path.AddBezier(right,top[top.Length-1].Y,right+rw,top[top.Length-1].Y+2*m.Scale,right+rw,baseline+rh,right+rw*.05f,baseline+rh);
   path.AddBezier(right+rw*.05f,baseline+rh,right-rw*.55f,baseline+rh,right-rw*.3f,baseline,right-rw,baseline);
   path.AddLine(right-rw,baseline,pad+lw,baseline);
   path.AddBezier(pad+lw,baseline,pad+lw*.3f,baseline,pad+lw*.55f,baseline+lh,pad-lw*.05f,baseline+lh);
   path.AddBezier(pad-lw*.05f,baseline+lh,pad-lw,baseline+lh,pad-lw,top[0].Y+2*m.Scale,pad,top[0].Y);
  }else{path.AddLine(right,top[top.Length-1].Y,right,baseline);path.AddLine(right,baseline,0,baseline);path.AddLine(0,baseline,top[0].X,top[0].Y);}path.CloseFigure();return path;
 }
 internal static Bitmap Render(SnowModel m,bool window=false){int pad=window?Padding(m):0,drop=window?DropSpace(m):0;var result=Artwork.Canvas(m.Width+pad*2,m.Height+drop);using(var g=Graphics.FromImage(result))using(var outline=Outline(m,window)){g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
  using(var paint=new LinearGradientBrush(new Rectangle(0,0,result.Width,result.Height),Color.FromArgb(255,253,246),Color.FromArgb(160,180,196),90))g.FillPath(paint,outline);
  var state=g.Save();g.SetClip(outline);var texture=DrawnArt.BankTexture();float depth=Math.Max(2,m.Peak),sourceHeight=Math.Min(texture.Height*.6f,(depth+drop)*texture.Width/result.Width);g.DrawImage(texture,new RectangleF(0,m.Height-depth,result.Width,depth+drop),new RectangleF(0,texture.Height*.3f,texture.Width,sourceHeight),GraphicsUnit.Pixel);
  using(var glaze=new LinearGradientBrush(new Rectangle(0,0,1,result.Height),Color.FromArgb(35,255,252,239),Color.FromArgb(5,150,172,195),90))g.FillPath(glaze,outline);g.Restore(state);
 }m.Dirty=false;return result;}
}
}
