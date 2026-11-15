using System;
using System.Collections.Generic;
using System.Drawing;
namespace WinterDesk {
internal enum PetState { Held,Falling,Landing,GettingUp,Dusting,Perched,Running,Sliding }
internal sealed class Ledge {internal IntPtr Handle;internal Rectangle Bounds;internal List<Rectangle> Blockers=new List<Rectangle>();internal bool Supports(double x){if(x<Bounds.Left+8||x>Bounds.Right-8)return false;foreach(var b in Blockers)if(b.Contains((int)x,Bounds.Top+1))return false;return true;}}
internal sealed class PetMotion {
 internal PetState State=PetState.Held;internal double X,Y,VX,VY,Age,Time,FallStart,SupportOffset,RunDistance,SlideStartX;internal float Scale;internal IntPtr Support;internal int Landings,Direction=1;double offsetX,offsetY,lastDragTime,lastX,lastY;
 internal PetMotion(double x,double y,float scale){X=x;Y=y;Scale=scale;FallStart=y;}
 internal void Grab(double mx,double my){State=PetState.Held;Age=0;Support=IntPtr.Zero;offsetX=mx-X;offsetY=my-Y;VX=VY=0;lastDragTime=Time;lastX=X;lastY=Y;}
 internal void Drag(double mx,double my){double dt=Math.Max(.016,Time-lastDragTime),nx=mx-offsetX,ny=my-offsetY;VX=Math.Max(-600*Scale,Math.Min(600*Scale,(nx-lastX)/dt));VY=Math.Max(-700*Scale,Math.Min(700*Scale,(ny-lastY)/dt));X=nx;Y=ny;lastX=X;lastY=Y;lastDragTime=Time;}
 internal void Drop(List<Ledge> ledges){FallStart=Y;foreach(var l in ledges)if(l.Supports(X)&&Math.Abs(Y-l.Bounds.Top)<24*Scale){Support=l.Handle;SupportOffset=X-l.Bounds.Left;Y=l.Bounds.Top;Land();return;}Support=IntPtr.Zero;State=PetState.Falling;Age=0;VY=Math.Max(-250*Scale,VY*.25);VX*=.25;}
 void Land(){State=PetState.Landing;Age=0;RunDistance=0;VX=VY=0;Landings++;}
 void Fall(){Support=IntPtr.Zero;State=PetState.Falling;Age=0;FallStart=Y;VY=80*Scale;VX=Direction*85*Scale;}
 internal void Tick(double dt,List<Ledge> ledges,Func<double,double> floor){Time+=dt;if(State==PetState.Held)return;Ledge support=null;if(Support!=IntPtr.Zero){support=ledges.Find(l=>l.Handle==Support);if(support==null)Fall();else{double next=support.Bounds.Left+SupportOffset;double delta=next-X;X=next;SlideStartX+=delta;Y=support.Bounds.Top;}}
  if(State==PetState.Sliding){Age+=dt;double t=Math.Min(1,Age/.38);X=SlideStartX+Direction*28*Scale*t;Y=(support==null?Y:support.Bounds.Top)+18*Scale*t*t;if(support!=null)SupportOffset=X-support.Bounds.Left;if(t>=1)Fall();return;}
  if(State==PetState.Falling){double left=Math.Min(.2,dt);while(left>0){double step=Math.Min(left,1.0/120),old=Y;VY=Math.Min(1350*Scale,VY+1450*Scale*step);X+=VX*step;Y+=VY*step;double landing=floor(X);Ledge candidate=null;foreach(var l in ledges)if(l.Supports(X)&&old<=l.Bounds.Top&&Y>=l.Bounds.Top&&l.Bounds.Top<landing){landing=l.Bounds.Top;candidate=l;}if(Y>=landing&&VY>=0){Y=landing;if(candidate!=null){Support=candidate.Handle;SupportOffset=X-candidate.Bounds.Left;}Land();break;}left-=step;}return;}
  Age+=dt;if(Support==IntPtr.Zero){double ground=floor(X);if(Y<ground-16*Scale&&(State==PetState.Running||State==PetState.Perched)){Fall();return;}if(Y>ground)Y=ground;}
  if(State==PetState.Landing&&Age>.42){State=PetState.GettingUp;Age=0;}else if(State==PetState.GettingUp&&Age>.62){State=PetState.Dusting;Age=0;}else if(State==PetState.Dusting&&Age>.48){State=PetState.Running;Age=0;}
  if(State==PetState.Running){VX+=(Direction*110*Scale-VX)*(1-Math.Exp(-dt*9));double distance=VX*dt;X+=distance;RunDistance+=Math.Abs(distance);if(support!=null){SupportOffset=X-support.Bounds.Left;if(Direction>0?X>=support.Bounds.Right-8*Scale:X<=support.Bounds.Left+8*Scale){State=PetState.Sliding;Age=0;SlideStartX=X;}}else Y=floor(X);}
 }
 internal int Pose {get{return State==PetState.Falling?(int)(Time*12)%4:State==PetState.Sliding?4:14;}}
}
internal sealed class Pet:IDisposable {internal Layer Window=new Layer();internal PetMotion Motion;internal Atlas Atlas;internal int Frame=-1;internal Actor Origin;internal bool Disposed;public void Dispose(){if(Disposed)return;Disposed=true;if(Origin!=null&&Origin.Holding==this)Origin.Holding=null;Window.SetInput(false);Window.Dispose();}}
}
