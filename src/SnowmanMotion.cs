using System;
using System.Collections.Generic;
namespace WinterDesk {
internal enum SnowmanState { Falling,Landing,Awakening,Waiting,Approach,RollBase,PlaceBase,FetchMiddle,RollMiddle,CarryMiddle,PlaceMiddle,FetchHead,RollHead,CarryHead,PlaceHead,Stack,Wave,Leaving,Idle,Done }
internal sealed class SnowmanMotion {
 internal double X,Y,VY,Age,Time,TargetX,HomeX,SupportOffset,EscapeAt=6,RollOrigin;internal IntPtr Support;
 internal readonly float Scale;internal readonly bool Builder;internal SnowmanState State;internal int Direction=1,ExitDirection,ChildrenBuilt,Scarf,Generation;internal bool ChildPending;internal double ChildMultiplier=.5;internal float SizeLimit=3.2f;internal int PlacedBalls,BuildDirection=1;
 internal SnowmanMotion(double x,double y,float scale,bool builder,int direction){X=x;Y=y;HomeX=x;Scale=scale;Builder=builder;Direction=ExitDirection=direction<0?-1:1;State=SnowmanState.Falling;}
 void Change(SnowmanState state){State=state;Age=0;}
 // A hand-built snowman inherits the physical base of the stack, rather than the desktop floor.
 internal void AdoptStackBase(double x,double baseline,double desktopFloor,List<Ledge> ledges){X=x;Y=baseline;HomeX=x;Support=IntPtr.Zero;SupportOffset=0;VY=0;Ledge best=null;double distance=Math.Max(6,24*Scale);foreach(var l in ledges){double gap=Math.Abs(baseline-l.Bounds.Top);if(l.Supports(x)&&gap<=distance){best=l;distance=gap;}}if(best!=null){Support=best.Handle;SupportOffset=x-best.Bounds.Left;Y=best.Bounds.Top;Change(SnowmanState.Awakening);}else if(Math.Abs(baseline-desktopFloor)<=Math.Max(6,12*Scale)){Y=desktopFloor;Change(SnowmanState.Awakening);}else Change(SnowmanState.Falling);}
 internal void Grab(){Support=IntPtr.Zero;VY=0;PlacedBalls=0;}
 internal void Drop(List<Ledge> ledges){Support=IntPtr.Zero;VY=0;PlacedBalls=0;Change(SnowmanState.Falling);foreach(var l in ledges)if(l.Supports(X)&&Math.Abs(Y-l.Bounds.Top)<24*Scale){Support=l.Handle;SupportOffset=X-l.Bounds.Left;Y=l.Bounds.Top;Change(SnowmanState.Landing);break;}}
 bool Move(double goal,double dt){double distance=goal-X;Direction=distance<0?-1:1;X+=Math.Sign(distance)*Math.Min(Math.Abs(distance),85*Scale*dt);return Math.Abs(goal-X)<.5;}
 void BeginBuild(double left,double right){double room=right-left;int dir=right-X>=X-left?1:-1;double span=Math.Min(160*Scale,room*.4);TargetX=Math.Max(left+22*Scale,Math.Min(right-22*Scale,X+dir*span*.35));RollOrigin=Math.Max(left+20*Scale,Math.Min(right-20*Scale,TargetX-dir*span));Direction=dir;BuildDirection=dir;PlacedBalls=0;Change(SnowmanState.Approach);}
 internal void Tick(double dt,double floor,double left,double right){Tick(dt,floor,left,right,new List<Ledge>());}
 internal void Tick(double dt,double floor,double left,double right,List<Ledge> ledges){dt=Math.Max(0,Math.Min(.1,dt));Age+=dt;Time+=dt;if(State==SnowmanState.Done)return;
  Ledge support=null;if(Support!=IntPtr.Zero){support=ledges.Find(l=>l.Handle==Support);if(support==null||!support.Supports(support.Bounds.Left+SupportOffset)){Support=IntPtr.Zero;PlacedBalls=0;Change(SnowmanState.Falling);}else{double nx=support.Bounds.Left+SupportOffset,dx=nx-X;X=nx;TargetX+=dx;RollOrigin+=dx;HomeX+=dx;Y=support.Bounds.Top;}}
  if(State==SnowmanState.Falling){double remain=dt;while(remain>0){double step=Math.Min(remain,1.0/120),old=Y;VY=Math.Min(1200*Scale,VY+1100*Scale*step);Y+=VY*step;double landing=floor;Ledge target=null;foreach(var l in ledges)if(l.Supports(X)&&old<=l.Bounds.Top&&Y>=l.Bounds.Top&&l.Bounds.Top<landing){landing=l.Bounds.Top;target=l;}if(Y>=landing&&VY>=0){Y=landing;VY=0;HomeX=X;if(target!=null){Support=target.Handle;SupportOffset=X-target.Bounds.Left;}Change(SnowmanState.Landing);Direction=ExitDirection;break;}remain-=step;}return;}
  if(support==null)Y=floor;
  double lft=support==null?left:support.Bounds.Left,rgt=support==null?right:support.Bounds.Right;
  if(State==SnowmanState.Landing&&Age>.42)Change(SnowmanState.Awakening);
  else if(State==SnowmanState.Awakening&&Age>.70){if(ChildrenBuilt>0)Change(SnowmanState.Leaving);else{EscapeAt=Math.Min(EscapeAt,Time+.12);Change(SnowmanState.Waiting);}}
  else if(State==SnowmanState.Waiting&&Time>=EscapeAt){if(Builder&&Generation<1&&ChildrenBuilt==0)BeginBuild(lft,rgt);else{Direction=ExitDirection;Change(SnowmanState.Leaving);}}
  else if(State==SnowmanState.Approach){if(Move(RollOrigin,dt))Change(SnowmanState.RollBase);}
  else if(State==SnowmanState.RollBase){X=RollOrigin+(TargetX-RollOrigin)*Math.Min(1,Age/3.8);Direction=TargetX>=RollOrigin?1:-1;if(Age>=3.8)Change(SnowmanState.PlaceBase);}
  else if(State==SnowmanState.PlaceBase&&Age>1){PlacedBalls=1;Change(SnowmanState.FetchMiddle);}
  else if(State==SnowmanState.FetchMiddle){if(Move(RollOrigin,dt))Change(SnowmanState.RollMiddle);}
  else if(State==SnowmanState.RollMiddle){Direction=TargetX>=RollOrigin?1:-1;X=RollOrigin+(TargetX-RollOrigin)*.55*Math.Min(1,Age/3.5);if(Age>=3.5)Change(SnowmanState.CarryMiddle);}
  else if(State==SnowmanState.CarryMiddle){if(Move(TargetX,dt))Change(SnowmanState.PlaceMiddle);}
  else if(State==SnowmanState.PlaceMiddle&&Age>1.3){PlacedBalls=2;Change(SnowmanState.FetchHead);}
  else if(State==SnowmanState.FetchHead){if(Move(RollOrigin,dt))Change(SnowmanState.RollHead);}
  else if(State==SnowmanState.RollHead){Direction=TargetX>=RollOrigin?1:-1;X=RollOrigin+(TargetX-RollOrigin)*.45*Math.Min(1,Age/3.2);if(Age>=3.2)Change(SnowmanState.CarryHead);}
  else if(State==SnowmanState.CarryHead){if(Move(TargetX,dt))Change(SnowmanState.PlaceHead);}
  else if(State==SnowmanState.PlaceHead&&Age>1.4){PlacedBalls=3;Change(SnowmanState.Stack);}
  else if(State==SnowmanState.Stack&&Age>.5){if(ChildrenBuilt==0){ChildrenBuilt=1;ChildPending=true;}PlacedBalls=0;Change(SnowmanState.Wave);}
  else if(State==SnowmanState.Wave&&Age>1.1){Direction=ExitDirection;Change(SnowmanState.Leaving);}
  else if(State==SnowmanState.Leaving){Direction=ExitDirection;X+=ExitDirection*125*Scale*dt;if(X<left-120*Scale||X>right+120*Scale)Change(SnowmanState.Done);}
  if(support!=null){SupportOffset=X-support.Bounds.Left;if(!support.Supports(X)){Support=IntPtr.Zero;VY=0;PlacedBalls=0;Change(SnowmanState.Falling);}}
 }
 internal bool CanTeam {get{return Builder&&Generation<1&&ChildrenBuilt==0&&State==SnowmanState.Waiting&&Scale*2<=SizeLimit;}}
 internal static bool CanJoin(SnowmanMotion a,SnowmanMotion b){return a!=b&&a.CanTeam&&b.CanTeam&&a.Support==b.Support&&Math.Abs(a.Y-b.Y)<12*Math.Max(a.Scale,b.Scale)&&Math.Abs(a.Scale-b.Scale)<a.Scale*.15&&Math.Abs(a.X-b.X)<220*a.Scale;}
 internal int Pose {get{switch(State){case SnowmanState.Falling:return 100+(int)(Time*36)%64;case SnowmanState.Landing:return 464+Math.Min(7,(int)(Age/.42*8));case SnowmanState.Awakening:return 472+Math.Min(7,(int)(Age/.70*8));case SnowmanState.Approach:case SnowmanState.FetchMiddle:case SnowmanState.FetchHead:case SnowmanState.Leaving:return 400+(int)(Time*48)%64;case SnowmanState.RollBase:case SnowmanState.RollMiddle:case SnowmanState.RollHead:return 200+(int)(Time*48)%64;case SnowmanState.CarryMiddle:case SnowmanState.CarryHead:return 300+(int)(Time*40)%64;case SnowmanState.PlaceBase:case SnowmanState.PlaceMiddle:case SnowmanState.PlaceHead:case SnowmanState.Stack:return 364+Math.Min(31,(int)(Age*28));case SnowmanState.Wave:return 14;default:return 15;}}}
 internal float ChildScale {get{return (float)Math.Max(.24,Math.Min(SizeLimit,Scale*ChildMultiplier));}}
 internal int ActiveBall {get{switch(State){case SnowmanState.RollBase:case SnowmanState.PlaceBase:return 0;case SnowmanState.RollMiddle:case SnowmanState.CarryMiddle:case SnowmanState.PlaceMiddle:return 1;case SnowmanState.RollHead:case SnowmanState.CarryHead:case SnowmanState.PlaceHead:return 2;default:return -1;}}}
 internal bool Carrying {get{return State==SnowmanState.CarryMiddle||State==SnowmanState.CarryHead||State==SnowmanState.PlaceMiddle||State==SnowmanState.PlaceHead;}}
 internal double BallProgress {get{return Math.Min(1,Age/(ActiveBall==0?3.8:ActiveBall==1?3.5:3.2));}}
 internal int BuildStage {get{return PlacedBalls>0||ActiveBall>=0||State==SnowmanState.Stack?Math.Max(PlacedBalls,ActiveBall):-1;}}
}
}
