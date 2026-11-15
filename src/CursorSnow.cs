using System;
namespace WinterDesk {
internal static class CursorSnow {
 internal static void Push(ref double vx,ref double vy,double x,double y,double mx,double my,float scale,double dt){double dx=x-mx,dy=y-my,radius=110*scale,d=Math.Sqrt(dx*dx+dy*dy);if(d>=radius)return;double force=1-d/radius,den=Math.Max(10*scale,d);vx+=dx/den*1800*scale*force*dt;vy+=(dy/den*900-2100)*scale*force*dt;vx=Math.Max(-420*scale,Math.Min(420*scale,vx));vy=Math.Max(-380*scale,Math.Min(500*scale,vy));}
}
}
