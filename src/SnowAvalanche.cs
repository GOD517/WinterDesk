using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
namespace WinterDesk {
internal sealed class SnowFallMotion {
 internal double X,Y,VX,VY,Age,Area;internal readonly float Scale;internal bool Landed;
 internal SnowFallMotion(double x,double y,double area,float scale,double vx=0,double vy=20){X=x;Y=y;Area=area;Scale=scale;VX=vx;VY=vy*scale;}
 internal void LandAt(double y){Y=y;VY=0;Landed=true;}
 internal void Tick(double dt){if(Landed)return;dt=Math.Max(0,Math.Min(.15,dt));Age+=dt;X+=VX*dt;Y+=VY*dt+425*Scale*dt*dt;VY+=850*Scale*dt;}
}
internal sealed class FallingBank:IDisposable {internal Layer Window=new Layer();internal Sprite Image;internal SnowFallMotion Motion;public void Dispose(){Window.Dispose();Image.Dispose();}}
internal static class WindowSnowRelease {
 internal static bool ShouldRelease(bool windowExists,bool minimized,bool visible,bool notified,double area){return area>1&&(!windowExists||minimized||!visible||notified);}
 internal static double[] SliceAreas(SnowModel model,int count){var result=new double[count];double total=0;for(int i=0;i<model.Depth.Length;i++){double x=Math.Min(model.Width-1,i*model.Cell+model.Cell*.5);int n=Math.Min(count-1,(int)(x*count/model.Width));result[n]+=model.Depth[i]*model.Cell;total+=model.Depth[i]*model.Cell;}return result;}
}
internal sealed partial class Controller {
 readonly List<FallingBank> avalanches=new List<FallingBank>();
 void ClearAvalanches(){foreach(var a in avalanches)a.Dispose();avalanches.Clear();}
 void ReleaseWindowSnow(Surface surface){double area=surface.Model.Mass*surface.Model.Cell;if(!WindowSnowRelease.ShouldRelease(false,false,false,true,area))return;int count=Math.Max(1,Math.Min(16,(int)Math.Ceiling(surface.Width/(95*surface.Scale))));double[] pieces=WindowSnowRelease.SliceAreas(surface.Model,count);using(var image=surface.Model.Render(true)){for(int n=0;n<count;n++){int pad=surface.SnowPadding,left=n==0?0:n*surface.Width/count+pad,right=n==count-1?image.Width:(n+1)*surface.Width/count+pad;if(right<=left||pieces[n]<1)continue;using(var crop=image.Clone(new Rectangle(left,0,right-left,image.Height),PixelFormat.Format32bppPArgb)){var bank=new FallingBank{Image=new Sprite(crop),Motion=new SnowFallMotion(surface.Bounds.Left-pad+(left+right)*.5,surface.BaseY+SnowMaterial.DropSpace(surface.Model),pieces[n],surface.Scale,(random.NextDouble()-.5)*12*surface.Scale,10+random.NextDouble()*24)};avalanches.Add(bank);bank.Window.Paint(bank.Image,(int)(bank.Motion.X-bank.Image.Width*.5),(int)(bank.Motion.Y-bank.Image.Height));bank.Window.Show(true);}}Array.Clear(surface.Model.Depth,0,surface.Model.Depth.Length);surface.Model.Dirty=true;}
 }
 void AnimateAvalanches(double dt,double now){if(avalanches.Count==0)return;var surfaces=AllSurfaces();for(int i=avalanches.Count-1;i>=0;i--){var chunk=avalanches[i];var m=chunk.Motion;double old=m.Y;m.Tick(dt);Surface target=null;double landing=double.MaxValue;foreach(var s in surfaces){int x=(int)m.X-s.Bounds.Left;if(x<0||x>=s.Width)continue;bool blocked=false;foreach(var b in s.Blockers)if(b.Contains((int)m.X,s.BaseY))blocked=true;if(blocked)continue;double top=s.BaseY-s.Model.LevelAt(x);if(old<=top+1&&m.Y>=top&&top<landing){landing=top;target=s;}}if(target!=null){m.LandAt(landing);target.Model.ReturnMass((int)m.X-target.Bounds.Left,m.Area);target.LastPaint=0;chunk.Dispose();avalanches.RemoveAt(i);continue;}if(m.Y>System.Windows.Forms.SystemInformation.VirtualScreen.Bottom+chunk.Image.Height||m.Age>12){chunk.Dispose();avalanches.RemoveAt(i);continue;}chunk.Window.Move((int)(m.X-chunk.Image.Width*.5),(int)(m.Y-chunk.Image.Height));chunk.Window.Show(!screens.Exists(r=>r.FullScreen&&r.Screen.Bounds.Contains((int)m.X,(int)m.Y)));}}
}
}
