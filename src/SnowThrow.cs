using System;
using System.Drawing;
using System.Collections.Generic;
namespace WinterDesk {
internal sealed class ThrowGesture {
 double x,y,time,lastMove;bool ready;internal double VX,VY;
 internal void Begin(double px,double py,double now){x=px;y=py;time=lastMove=now;VX=VY=0;ready=true;}
 internal void Move(double px,double py,double now,float scale){if(!ready){Begin(px,py,now);return;}double dt=now-time;if(dt<.002)return;double dx=px-x,dy=py-y,gain=1-Math.Exp(-dt*28),cap=1800*scale;VX+=(Math.Max(-cap,Math.Min(cap,dx/dt))-VX)*gain;VY+=(Math.Max(-cap,Math.Min(cap,dy/dt))-VY)*gain;if(Math.Abs(dx)+Math.Abs(dy)>.25)lastMove=now;x=px;y=py;time=now;}
 internal void Release(double now,out double vx,out double vy){double fade=Math.Exp(-Math.Max(0,now-lastMove-.06)*16);vx=VX*fade;vy=VY*fade;}
}
internal struct BallHit {internal bool Hit;internal double T,X,Y,NX,NY;internal Ledge Window;}
internal static class SnowThrowPhysics {
 static bool Slab(double p,double d,double min,double max,double nx,double ny,ref double enter,ref double leave,ref double normalX,ref double normalY){if(Math.Abs(d)<1e-8)return p>=min&&p<=max;double a=(min-p)/d,b=(max-p)/d;double sign=1;if(a>b){double t=a;a=b;b=t;sign=-1;}if(a>enter){enter=a;normalX=nx*sign;normalY=ny*sign;}leave=Math.Min(leave,b);return enter<=leave;}
 internal static BallHit Sweep(double x,double y,double nx,double ny,double radius,List<Ledge> windows){var best=new BallHit{T=2};foreach(var w in windows){double left=w.Bounds.Left-radius,right=w.Bounds.Right+radius,top=w.Bounds.Top-radius,bottom=w.Bounds.Bottom+radius;
 // A ball picked up over a window may leave it freely; only entering a surface is a collision.
 if(x>=left&&x<=right&&y>=top&&y<=bottom)continue;double t=0,end=1,normalX=0,normalY=0;if(!Slab(x,nx-x,left,right,-1,0,ref t,ref end,ref normalX,ref normalY)||!Slab(y,ny-y,top,bottom,0,-1,ref t,ref end,ref normalX,ref normalY)||t<0||t>1||t>=best.T)continue;double cx=x+(nx-x)*t,cy=y+(ny-y)*t,px=cx-normalX*radius,py=cy-normalY*radius;bool blocked=false;foreach(var b in w.Blockers)if(b.Contains((int)px,(int)py))blocked=true;if(blocked)continue;best=new BallHit{Hit=true,T=t,X=px,Y=py,NX=normalX,NY=normalY,Window=w};}return best;}
}
internal sealed class SnowImpact:IDisposable {
 internal Layer Window=new Layer();internal Sprite Image;internal IntPtr Target;internal double X,Y,OffsetX,OffsetY,Radius,Age,NX,NY,PaintedAt=-1;internal int Variant;
 internal static Bitmap Render(double radius,double age,int variant,double nx,double ny){int size=(int)Math.Ceiling(radius*7+100);var image=Artwork.Canvas(size,size);using(var g=Graphics.FromImage(image)){float center=size*.5f,fade=(float)Math.Max(0,1-Math.Max(0,age-.40)/.75);if(fade>0){var save=g.Save();g.TranslateTransform(center,center);g.RotateTransform((float)(Math.Atan2(ny,nx)*180/Math.PI));float squash=(float)(.52+.30*Math.Exp(-age*16));g.ScaleTransform(squash,1.12f);SnowBallArt.Draw(g,0,(float)radius,(float)radius,0,variant);for(int k=0;k<5;k++){float angle=k*1.2566f;float rx=(float)(Math.Cos(angle)*radius*.65),ry=(float)(Math.Sin(angle)*radius*.65);SnowBallArt.Draw(g,rx,ry+(float)radius*.34f,(float)radius*.34f,angle,(variant+k)%8);}g.Restore(save);}for(int i=0;i<10;i++){double angle=i*2.39996,velocity=radius*(1.1+(i%3)*.5)+30;float x=center+(float)(Math.Cos(angle)*velocity*age),y=center+(float)(Math.Sin(angle)*velocity*age+110*age*age);float r=(float)Math.Max(0,radius*(.10+.025*(i%3))*(1-age/1.15));if(r>.2)SnowBallArt.Draw(g,x,y+r,r,angle+age*2,(variant+i)%8);}}if(age>.4){float opacity=(float)Math.Max(0,1-(age-.4)/.75);using(var faded=Artwork.Canvas(size,size))using(var g=Graphics.FromImage(faded))using(var attr=new System.Drawing.Imaging.ImageAttributes()){var matrix=new System.Drawing.Imaging.ColorMatrix();matrix.Matrix33=opacity;attr.SetColorMatrix(matrix);g.DrawImage(image,new Rectangle(0,0,size,size),0,0,size,size,GraphicsUnit.Pixel,attr);image.Dispose();return (Bitmap)faded.Clone();}}return image;}
 internal void Paint(){using(var b=Render(Radius,Age,Variant,NX,NY)){if(Image==null||Image.Width!=b.Width||Image.Height!=b.Height){if(Image!=null)Image.Dispose();Image=new Sprite(b);}else Image.Upload(b);Window.Paint(Image,(int)(X-b.Width/2),(int)(Y-b.Height/2));}Window.Show(true);}
 public void Dispose(){Window.Dispose();if(Image!=null)Image.Dispose();}
}
internal sealed partial class Controller {
 readonly List<SnowImpact> snowImpacts=new List<SnowImpact>();
 void HitWindow(SnowBall ball,BallHit hit){var impact=new SnowImpact{X=hit.X,Y=hit.Y,Target=hit.Window.Handle,OffsetX=hit.X-hit.Window.Bounds.Left,OffsetY=hit.Y-hit.Window.Bounds.Top,Radius=Math.Min(90,ball.Radius),Variant=ball.PaintVariant,NX=hit.NX,NY=hit.NY};if(snowImpacts.Count>=12){snowImpacts[0].Dispose();snowImpacts.RemoveAt(0);}snowImpacts.Add(impact);impact.Paint();snowBalls.Remove(ball);ball.Dispose();}
 void AnimateSnowImpacts(double dt){for(int i=snowImpacts.Count-1;i>=0;i--){var impact=snowImpacts[i];impact.Age+=dt;var window=ledges.Find(l=>l.Handle==impact.Target);if(window==null||impact.Age>=1.15){impact.Dispose();snowImpacts.RemoveAt(i);continue;}impact.X=window.Bounds.Left+impact.OffsetX;impact.Y=window.Bounds.Top+impact.OffsetY;if(impact.PaintedAt<0||impact.Age-impact.PaintedAt>=1.0/60){impact.Paint();impact.PaintedAt=impact.Age;}impact.Window.Show(!screens.Exists(r=>r.FullScreen&&r.Screen.Bounds.Contains((int)impact.X,(int)impact.Y)));}}
 void ClearSnowImpacts(){foreach(var impact in snowImpacts)impact.Dispose();snowImpacts.Clear();}
}
}
