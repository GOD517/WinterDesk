using System;
namespace WinterDesk {
// Remove only snow actually intersected by the swept circular ball, in screen coordinates.
internal static class SnowContact {
 internal static double Collect(SnowModel model,int left,int baseline,double fromX,double fromY,double toX,double toY,double radius){if(radius<=0)return 0;double dx=toX-fromX,dy=toY-fromY,length=dx*dx+dy*dy;int first=Math.Max(0,(int)Math.Floor((Math.Min(fromX,toX)-radius-left)/model.Cell)),last=Math.Min(model.Depth.Length-1,(int)Math.Floor((Math.Max(fromX,toX)+radius-left)/model.Cell));double area=0;
  for(int i=first;i<=last;i++){float depth=model.Depth[i];if(depth<=0)continue;double cx=left+(i+.5)*model.Cell,top=baseline-depth;double t=length<.0001?0:Math.Max(0,Math.Min(1,((cx-fromX)*dx+(top-fromY)*dy)/length));double x=fromX+dx*t,y=fromY+dy*t,horizontal=Math.Max(0,Math.Abs(cx-x)-model.Cell*.5);if(horizontal>=radius)continue;double reach=Math.Sqrt(radius*radius-horizontal*horizontal),overlap=Math.Min(baseline,y+reach)-Math.Max(top,y-reach);if(overlap<=0)continue;float take=(float)Math.Min(depth,overlap*.55);model.Depth[i]-=take;area+=take*model.Cell;}
  if(area>0)model.Dirty=true;return area;
 }
}
}
