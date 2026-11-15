using System;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace WinterDesk {
internal static class SnowConstruction {
 internal static void PaintBall(Graphics g,float x,float baseline,float radius,double angle){SnowBallArt.Draw(g,x,baseline,radius,angle);}

 internal static Rectangle Bounds(SnowmanMotion m){float scale=m.Scale,cs=m.ChildScale;double site=m.TargetX+m.BuildDirection*(47*cs+75*scale);int px=(int)(Math.Min(site,m.RollOrigin)-160*cs-100*scale),w=(int)(Math.Abs(site-m.RollOrigin)+320*cs+200*scale),h=Math.Max(80,(int)(240*cs+130*scale)),py=(int)m.Y-h;w=Math.Max(80,w);return new Rectangle(px,py,w,h);}
 internal static Bitmap Render(SnowmanMotion m,Rectangle bounds){float scale=m.Scale,cs=m.ChildScale;double site=m.TargetX+m.BuildDirection*(47*cs+75*scale);int px=bounds.X,w=bounds.Width,h=bounds.Height;var b=Artwork.Canvas(w,h);using(var g=Graphics.FromImage(b)){g.SmoothingMode=SmoothingMode.AntiAlias;float[] rr={47*cs,33*cs,22*cs};float baseline=h;for(int k=0;k<m.PlacedBalls;k++){PaintBall(g,(float)(site-px),baseline,rr[k],0);baseline-=rr[k]*2;}int active=m.ActiveBall;if(active>=0){float r=rr[active],x=(float)(m.X-px+m.BuildDirection*(r+75*scale)),foot=h;bool placing=m.State==SnowmanState.PlaceBase||m.State==SnowmanState.PlaceMiddle||m.State==SnowmanState.PlaceHead;if(!m.Carrying&&!placing)r*=(float)(.22+.78*m.BallProgress);if(m.Carrying){x=(float)(m.X-px+m.BuildDirection*82*scale);foot=h-90*scale;}if(placing){double t=Math.Min(1,m.Age/(active==0?1:active==1?1.3:1.4));float finalFoot=h;for(int k=0;k<active;k++)finalFoot-=rr[k]*2;x+=(float)((site-px-x)*t);foot+=(float)((finalFoot-foot)*t);}PaintBall(g,x,foot,r,m.Time*2);}}return b;}
}
}
