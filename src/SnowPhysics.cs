using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
namespace WinterDesk {
// A one-dimensional height field. Dragging transfers mass, it does not erase snow.
internal sealed class SnowModel:IDisposable {
 public void Dispose(){}
 internal readonly int Seed;internal readonly float[] Depth;internal readonly int Width,Cell;internal readonly float Scale;internal readonly int Height;internal float MaxDepth;internal bool Dirty=true;internal double UntilWorker;readonly Random random;
 internal SnowModel(int width,float scale,int seed=0){Width=width;Scale=scale;Cell=Math.Max(3,(int)(4*scale));Height=(int)(140*scale);MaxDepth=120*scale;Depth=new float[(width+Cell-1)/Cell];Seed=seed==0?Artwork.NewSeed():seed;random=new Random(Seed);for(int i=0;i<Depth.Length;i++)Depth[i]=(float)random.NextDouble()*scale;}
 internal float Peak {get{float peak=0;foreach(float d in Depth)peak=Math.Max(peak,d);return peak;}}
 internal float Mean {get{float sum=0;foreach(float d in Depth)sum+=d;return sum/Depth.Length;}}
 internal double Mass {get{double sum=0;foreach(float d in Depth)sum+=d;return sum;}}
 internal float LevelAt(int x){double position=Math.Max(0,Math.Min(Depth.Length-1,x/(double)Cell));int a=(int)position,b=Math.Min(Depth.Length-1,a+1);return (float)(Depth[a]+(Depth[b]-Depth[a])*(position-a));}
 internal void Accumulate(double seconds){Accumulate(seconds,false);}
 internal void Accumulate(double seconds,bool storm){if(seconds<=0)return;float t=(float)Math.Min(seconds,60);for(int i=0;i<Depth.Length;i++){float limit=NaturalLimit(i);if(Depth[i]<limit)Depth[i]=Math.Min(limit,Depth[i]+t*(storm?1.2f:.48f)*Scale*(.2f+Artwork.Noise(i*.06f,Seed)));else Depth[i]=Math.Max(limit,Depth[i]-t*.06f*Scale);}Settle(2);Round(2);Dirty=true;}
 float NaturalLimit(int i){float a=Artwork.Noise(i*.021f,Seed),b=Artwork.Noise(i*.063f,Seed^173);return Scale*(2+37*a*a*a+9*b*b);}

 void AddMass(int center,double mass,int radius){
  if(mass<=0)return;int a=Math.Max(0,center-radius),b=Math.Min(Depth.Length-1,center+radius);double left=mass;
  for(int pass=0;pass<8&&left>.00001;pass++){double weights=0;for(int i=a;i<=b;i++)if(Depth[i]<MaxDepth-.0001)weights+=Math.Exp(-Math.Pow((i-center)/(double)Math.Max(1,radius),2)*3);if(weights==0)break;double used=0;
   for(int i=a;i<=b;i++){if(Depth[i]>=MaxDepth-.0001)continue;double w=Math.Exp(-Math.Pow((i-center)/(double)Math.Max(1,radius),2)*3);float add=(float)Math.Min(MaxDepth-Depth[i],left*w/weights);Depth[i]+=add;used+=add;}left-=used;
  }
  // At a crowded destination, spill into neighbouring cells instead of losing volume.
  if(left>.001){for(int distance=radius+1;distance<Depth.Length&&left>.001;distance++){int[] indices={center-distance,center+distance};foreach(int i in indices)if(i>=0&&i<Depth.Length){float add=(float)Math.Min(MaxDepth-Depth[i],left);if(add>0){Depth[i]+=add;left-=add;}}}}
 }
 internal void Deposit(int x,float amount){if(x<0||x>=Width)return;int index=x/Cell;if(Depth[index]<NaturalLimit(index))AddMass(index,amount*Scale,Math.Max(2,(int)(9*Scale/Cell)));Dirty=true;}
 internal void Pile(int from,int to,int radius){if(from==to)return;from=Math.Max(0,Math.Min(Width-1,from));to=Math.Max(0,Math.Min(Width-1,to));
  int left=Math.Max(0,(Math.Min(from,to)-radius/2)/Cell),right=Math.Min(Depth.Length-1,(Math.Max(from,to)+radius/2)/Cell);double moved=0;
  for(int i=left;i<=right;i++){float take=Depth[i]*Math.Min(.75f,Math.Abs(to-from)/(float)Math.Max(1,radius));Depth[i]-=take;moved+=take;}
  int target=Math.Max(0,Math.Min(Depth.Length-1,to/Cell));AddMass(target,moved,Math.Max(3,radius/Cell));Settle(6);Round(5);Dirty=true;
 }
 internal void Settle(int passes){float slope=Cell*.58f;for(int p=0;p<passes;p++){for(int i=0;i<Depth.Length-1;i++){float d=Depth[i]-Depth[i+1];if(Math.Abs(d)>slope){float transfer=(Math.Abs(d)-slope)*.23f;if(d>0){Depth[i]-=transfer;Depth[i+1]+=transfer;}else{Depth[i]+=transfer;Depth[i+1]-=transfer;}}}}}
 void Round(int passes){var delta=new float[Depth.Length];for(int p=0;p<passes;p++){Array.Clear(delta,0,delta.Length);for(int i=0;i<Depth.Length-1;i++){float amount=(Depth[i]-Depth[i+1])*.18f;delta[i]-=amount;delta[i+1]+=amount;}for(int i=0;i<Depth.Length;i++)Depth[i]+=delta[i];}}
 internal void SettleAfterDrag(){Settle(6);Round(5);Dirty=true;}
 internal void Blizzard(){for(int i=0;i<Depth.Length;i++)Depth[i]=Scale*(14+32*Artwork.Noise(i*.027f,Seed)+17*Artwork.Noise(i*.10f,Seed^981));Settle(4);Dirty=true;}
 internal static void Transfer(SnowModel source,SnowModel target){double ratio=source.Width/(double)target.Width;for(int i=0;i<target.Depth.Length;i++){double from=i*target.Cell*ratio,to=Math.Min(target.Width,(i+1)*target.Cell)*ratio,total=0;int first=Math.Max(0,(int)(from/source.Cell)),last=Math.Min(source.Depth.Length-1,(int)(to/source.Cell));for(int j=first;j<=last;j++){double overlap=Math.Max(0,Math.Min(to,(j+1)*source.Cell)-Math.Max(from,j*source.Cell));total+=source.Depth[j]*overlap;}target.Depth[i]=(float)(total/target.Cell);}double remaining=source.Mass*source.Cell-target.Mass*target.Cell;if(remaining>0)target.ReturnMass(target.Width/2,remaining);target.Dirty=true;}
 internal void ReturnMass(int x,double area){AddMass(Math.Max(0,Math.Min(Depth.Length-1,x/Cell)),area/Cell,Math.Max(4,(int)(25*Scale/Cell)));Dirty=true;}
 internal void Sweep(int left,int right){int a=Math.Max(0,left/Cell),b=Math.Min(Depth.Length-1,right/Cell);for(int i=a;i<=b;i++)Depth[i]=Math.Min(Depth[i],1.2f*Scale);Dirty=true;}
 internal Bitmap Render(bool window=false){return SnowMaterial.Render(this,window);}

}
internal sealed class Surface : IDisposable {
 internal readonly int Width;internal string Key;internal IntPtr Target;internal Layer Snow,Light;internal SnowModel Model;internal Sprite SnowImage;internal FrameBank LightFrames;
 internal Rectangle Bounds;internal int BaseY;internal float Scale;internal bool IsDesktop,Visible,Dragging;internal int DragX;internal List<Rectangle> Blockers=new List<Rectangle>();internal double LastPaint;internal int ClipHash,LightPhase=-1;internal int LightX,LightY,LightWidth,LightHeight,LightMode=-1;internal bool LightAllowed;
 internal Surface(string key,IntPtr target,int width,float scale,bool desktop){Width=width;Key=key;Target=target;Scale=scale;IsDesktop=desktop;if(desktop){Snow=new Layer();Model=new SnowModel(width,scale);}else {Light=new Layer();Snow=new Layer();Model=new SnowModel(width,scale);}}
 internal int SnowPadding {get{return IsDesktop?0:SnowMaterial.Padding(Model);}}
 internal Rectangle SnowBounds {get{return new Rectangle(Bounds.X-SnowPadding,BaseY-Model.Height,Width+SnowPadding*2,Model.Height+(IsDesktop?0:SnowMaterial.DropSpace(Model)));}}
 internal void PaintSnow(double now){if(Model==null)return;var bounds=SnowBounds;double interval=Dragging?.016:.12;if(SnowImage==null||Model.Dirty&&now-LastPaint>=interval){using(var b=Model.Render(!IsDesktop)){if(SnowImage==null||SnowImage.Width!=b.Width||SnowImage.Height!=b.Height){if(SnowImage!=null)SnowImage.Dispose();SnowImage=new Sprite(b);}else SnowImage.Upload(b);Snow.Paint(SnowImage,bounds.X,bounds.Y);}LastPaint=now;}else Snow.Move(bounds.X,bounds.Y);}
 internal void ClipLayers(){if(Snow!=null&&Model!=null)Snow.Clip(SnowBounds,Blockers);if(Light==null||LightFrames==null)return;int hash=Bounds.GetHashCode();foreach(var b in Blockers)hash=unchecked(hash*31+b.GetHashCode());if(hash==ClipHash)return;ClipHash=hash;Light.Clip(new Rectangle(LightX,LightY,LightWidth,LightHeight),Blockers);}
 internal void Hide(){if(Snow!=null)Snow.Show(false);if(Light!=null)Light.Show(false);}
 public void Dispose(){if(Model!=null)Model.Dispose();if(Snow!=null)Snow.Dispose();if(Light!=null)Light.Dispose();if(SnowImage!=null)SnowImage.Dispose();if(LightFrames!=null)LightFrames.Dispose();}
}
internal sealed class HourlySchedule {
 readonly Random random;internal double Next;
 internal HourlySchedule(Random r,double now){random=r;Schedule(now);}
 internal void Schedule(double now){Next=now+2700+random.NextDouble()*1800;}
 internal bool IsDue(double now){return now>=Next;}
}
}
