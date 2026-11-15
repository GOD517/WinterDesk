using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace WinterDesk {
internal static class WatercolorTheme {
 static Bitmap decor,paper,button;static Icon icon;
 internal static Bitmap Decor {get{if(decor==null)using(var stream=typeof(WatercolorTheme).Assembly.GetManifestResourceStream("WinterDesk.DecorSheet"))using(var b=new Bitmap(stream))decor=new Bitmap(b);return decor;}}
 internal static Bitmap Paper {get{if(paper==null)paper=Decor.Clone(new Rectangle(0,(int)(Decor.Height*.516),Decor.Width,(int)(Decor.Height*.24)),PixelFormat.Format32bppPArgb);return paper;}}
 internal static Bitmap Button {get{if(button==null)button=Decor.Clone(new Rectangle(0,(int)(Decor.Height*.78),Decor.Width,(int)(Decor.Height*.22)),PixelFormat.Format32bppPArgb);return button;}}
 internal static Icon AppIcon {get{if(icon==null)using(var s=typeof(WatercolorTheme).Assembly.GetManifestResourceStream("WinterDesk.PenguinIcon"))using(var original=new Icon(s))icon=(Icon)original.Clone();return icon;}}
 // Seeded, nonperiodic cubic cable spans: uneven sags, tight twists and loose loops.
 internal static Bitmap Garland(int width,int height,int phase,int seed,int mode=2){
  var b=Artwork.Canvas(width,height);if(mode==0)return b;var random=new Random(seed);float cy=height*.46f;var lamps=new System.Collections.Generic.List<PointF>();
  using(var g=Graphics.FromImage(b)){g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
   for(int wire=0;wire<2;wire++){var points=new System.Collections.Generic.List<PointF>();float x=-5,y=cy+(float)(random.NextDouble()-.5)*height*.15f;points.Add(new PointF(x,y));
    while(x<width+5){float span=85+(float)random.NextDouble()*50,endX=Math.Min(width+5,x+span),endY=cy+(float)(random.NextDouble()-.5)*height*.16f;float bend=(float)(.22f+(float)random.NextDouble()*.14f)*height;int kind=0;bool loop=false;
     var p0=new PointF(x,y);var p1=new PointF(x+span*(kind==4?1.12f:loop?.90f:.27f),Math.Max(-2,Math.Min(height+2,y+bend)));var p2=new PointF(x+span*(kind==4?-.12f:loop?.10f:.73f),Math.Max(-2,Math.Min(height+2,endY+bend*.85f)));var p3=new PointF(endX,endY);points.Add(p1);points.Add(p2);points.Add(p3);
     if(wire==0){int count=Math.Max(1,(int)(span/(28+random.Next(5))));for(int n=0;n<count;n++){float t=(n+.45f+(float)random.NextDouble()*.1f)/count,u=1-t;lamps.Add(new PointF(u*u*u*p0.X+3*u*u*t*p1.X+3*u*t*t*p2.X+t*t*t*p3.X,u*u*u*p0.Y+3*u*u*t*p1.Y+3*u*t*t*p2.Y+t*t*t*p3.Y));}}
     x=endX;y=endY;
    }
    using(var path=new GraphicsPath()){path.AddBeziers(points.ToArray());using(var shadow=new Pen(Color.FromArgb(115,14,20,16),Math.Max(1.6f,height*.062f)))g.DrawPath(shadow,path);using(var cable=new Pen(Color.FromArgb(245,47,55,46),Math.Max(1.05f,height*.042f)))g.DrawPath(cable,path);using(var rim=new Pen(Color.FromArgb(100,122,131,114),.5f))g.DrawPath(rim,path);}
   }
   Color[] palette={Color.FromArgb(255,89,61),Color.FromArgb(111,238,130),Color.FromArgb(138,186,255),Color.FromArgb(255,221,136)};int lamp=0;
   foreach(var point in lamps){if(point.X<8||point.X>width-8)continue;float x=point.X,y=point.Y+(float)(random.NextDouble()-.5)*height*.12f;Color color=mode==3?Color.FromArgb(255,75,61):mode==4?Color.FromArgb(93,241,124):mode==5?Color.FromArgb(255,214,140):palette[random.Next(palette.Length)];
    double t=phase/(double)GarlandModes.Frames*Math.PI*2;float strength=mode==2?(float)(.23+.77*Math.Pow(.5+.5*Math.Sin(t+lamp*1.7),2)):mode==3||mode==4?(phase<8?1:.20f):mode==5?(float)(.18+.82*(.5-.5*Math.Cos(t))):mode==6?(float)(.12+.88*Math.Pow(.5+.5*Math.Cos(t-lamp*.8),6)):1;
    using(var stem=new Pen(Color.FromArgb(235,43,49,41),1.15f))g.DrawLine(stem,x,point.Y,x,y);float radius=Math.Max(2.2f,height*.145f);
    for(int k=5;k>=1;k--){float glow=radius*(1+k*.70f);using(var halo=new SolidBrush(Color.FromArgb(Math.Max(1,(int)((15+(5-k)*8)*strength)),color)))g.FillEllipse(halo,x-glow,y-glow,glow*2,glow*2);}
    var save=g.Save();g.TranslateTransform(x,y);g.RotateTransform((float)(random.NextDouble()-.5)*16);using(var path=new GraphicsPath()){path.AddEllipse(-radius,-radius*1.25f,radius*2,radius*2.5f);using(var glass=new PathGradientBrush(path)){glass.CenterPoint=new PointF(-radius*.23f,-radius*.30f);glass.CenterColor=Color.FromArgb((int)(170+85*strength),255,251,223);glass.SurroundColors=new[]{Color.FromArgb((int)(110+145*strength),color)};g.FillPath(glass,path);}using(var rim=new Pen(Color.FromArgb(100,color),.7f))g.DrawPath(rim,path);}using(var socket=new SolidBrush(Color.FromArgb(240,44,53,40)))g.FillRectangle(socket,-radius*.5f,-radius*1.5f,radius,radius*.55f);using(var shine=new SolidBrush(Color.FromArgb((int)(70+160*strength),255,255,248)))g.FillEllipse(shine,-radius*.48f,-radius*.75f,radius*.4f,radius*.75f);g.Restore(save);lamp++;
   }
  }return b;
 }
 internal static void StyleMenu(ContextMenuStrip menu){menu.Renderer=new WatercolorMenu();menu.Font=new Font("Georgia",9);menu.ForeColor=Color.FromArgb(72,51,46);menu.BackColor=Color.FromArgb(246,238,219);}
 internal static void Release(){if(decor!=null)decor.Dispose();if(paper!=null)paper.Dispose();if(button!=null)button.Dispose();if(icon!=null)icon.Dispose();decor=paper=button=null;icon=null;}
}
internal sealed class WatercolorMenu:ToolStripProfessionalRenderer {
 protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e){e.Graphics.Clear(Color.FromArgb(247,240,222));e.Graphics.DrawImage(WatercolorTheme.Paper,e.AffectedBounds);}
 protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){if(e.Item.Selected)e.Graphics.DrawImage(WatercolorTheme.Button,new Rectangle(0,0,e.Item.Width,e.Item.Height));}
 protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){e.TextColor=e.Item.Selected?Color.White:Color.FromArgb(73,48,43);base.OnRenderItemText(e);}
}
internal sealed class WaterButton:Button {
 internal bool Active;
 internal WaterButton(){FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.FromArgb(246,238,218);ForeColor=Color.White;Cursor=Cursors.Hand;SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.UserPaint,true);}
 protected override void OnPaint(PaintEventArgs e){e.Graphics.Clear(BackColor);var rect=new Rectangle(0,0,Width,Height);e.Graphics.DrawImage(WatercolorTheme.Button,rect);if(Active)using(var wash=new SolidBrush(Color.FromArgb(40,255,223,159)))e.Graphics.FillRectangle(wash,rect);TextRenderer.DrawText(e.Graphics,Text,Font,rect,ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.WordBreak);if(Focused)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(7,7,Width-14,Height-14),ForeColor,BackColor);}
}
}
