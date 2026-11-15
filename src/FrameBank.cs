using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Drawing;
using System.Diagnostics;
using System.Threading;
namespace WinterDesk {
// GDI+ preparation is serialized off the UI thread. Native DIBs are committed on the UI thread.
internal static class FramePreparation {
 sealed class Job {internal Func<Bitmap> Build;internal Action<Bitmap> Commit;internal Action Fail;internal Func<bool> Cancelled;}
 sealed class Result {internal Job Job;internal Bitmap Image;}
 static readonly BlockingCollection<Job> jobs=new BlockingCollection<Job>(1024);
 static readonly BlockingCollection<Result> results=new BlockingCollection<Result>(16);
 static readonly Thread thread;static volatile bool stopped;
 static FramePreparation(){thread=new Thread(Work){IsBackground=true,Name="WinterDesk artwork preparation"};thread.Start();}
 internal static bool Enqueue(Func<Bitmap> build,Action<Bitmap> commit,Action fail,Func<bool> cancelled){return !stopped&&jobs.TryAdd(new Job{Build=build,Commit=commit,Fail=fail,Cancelled=cancelled});}
 static void Work(){foreach(var job in jobs.GetConsumingEnumerable()){if(stopped)break;if(job.Cancelled())continue;Bitmap image=null;try{image=job.Build();if(job.Cancelled()){image.Dispose();continue;}while(!stopped&&!results.TryAdd(new Result{Job=job,Image=image},50)){}if(stopped)image.Dispose();}catch(Exception e){if(image!=null)image.Dispose();RuntimeLog.Error("art preparation",e);if(!stopped)results.Add(new Result{Job=job});}}}
 internal static void Pump(double milliseconds=2){var timer=Stopwatch.StartNew();Result r;while(timer.Elapsed.TotalMilliseconds<milliseconds&&results.TryTake(out r)){if(r.Job.Cancelled()){if(r.Image!=null)r.Image.Dispose();continue;}if(r.Image==null)r.Job.Fail();else{try{r.Job.Commit(r.Image);}finally{r.Image.Dispose();}}}}
 internal static void Stop(){stopped=true;jobs.CompleteAdding();Result r;while(results.TryTake(out r))if(r.Image!=null)r.Image.Dispose();thread.Join(2000);while(results.TryTake(out r))if(r.Image!=null)r.Image.Dispose();}
}
internal sealed class FrameBank:IDisposable {
 readonly Func<int,Bitmap> build;readonly Func<int,string> identity;readonly Dictionary<string,Sprite> live=new Dictionary<string,Sprite>();readonly LinkedList<string> order=new LinkedList<string>();readonly HashSet<string> pending=new HashSet<string>();readonly Dictionary<string,double> retry=new Dictionary<string,double>();readonly int capacity;readonly long budget;long bytes;volatile bool disposed;int expected;internal readonly int Length;
 internal FrameBank(int length,Func<int,Bitmap> factory,Func<int,string> key,int limit=96,long maxBytes=16777216){Length=length;build=factory;identity=key;capacity=limit;budget=maxBytes;}
 internal int Count {get{return live.Count;}}internal long Bytes {get{return bytes;}}
 internal bool Has(int index){return live.ContainsKey(identity(index));}
 internal void Request(int index){if(disposed)return;string key=identity(index);double after;if(live.ContainsKey(key)||pending.Contains(key)||retry.TryGetValue(key,out after)&&RuntimeLog.Seconds<after)return;pending.Add(key);if(!FramePreparation.Enqueue(()=>build(index),image=>{pending.Remove(key);if(disposed)return;Sprite sprite=new Sprite(image);if(live.ContainsKey(key)){sprite.Dispose();return;}live.Add(key,sprite);order.AddLast(key);bytes+=(long)sprite.Width*sprite.Height*4;Trim();},()=>{pending.Remove(key);retry[key]=RuntimeLog.Seconds+10;},()=>disposed))pending.Remove(key);}
 internal Sprite this[int index]{get{if(disposed)return null;string key=identity(index);Sprite image;if(live.TryGetValue(key,out image)){order.Remove(key);order.AddLast(key);return image;}Request(index);return null;}}
 internal void Warm(){var unique=new HashSet<string>();for(int i=0;i<Length;i++){unique.Add(identity(i));Request(i);}expected=unique.Count;}
 internal bool Ready {get{return pending.Count==0&&live.Count>=Math.Max(1,expected);}}
 void Trim(){while(order.Count>1&&(order.Count>capacity||bytes>budget)){string key=order.First.Value;order.RemoveFirst();var sprite=live[key];bytes-=(long)sprite.Width*sprite.Height*4;live.Remove(key);sprite.Dispose();}}
 public void Dispose(){disposed=true;foreach(var s in live.Values)s.Dispose();live.Clear();order.Clear();pending.Clear();bytes=0;}
}
internal sealed class SpriteCache<TKey>:IDisposable {
 readonly Dictionary<TKey,Sprite> live=new Dictionary<TKey,Sprite>();readonly LinkedList<TKey> order=new LinkedList<TKey>();readonly int capacity;readonly long budget;long bytes;
 internal SpriteCache(int count,long maxBytes){capacity=count;budget=maxBytes;}
 internal int Count {get{return live.Count;}}internal long Bytes {get{return bytes;}}
 internal bool TryGetValue(TKey key,out Sprite sprite){if(!live.TryGetValue(key,out sprite))return false;order.Remove(key);order.AddLast(key);return true;}
 internal void Add(TKey key,Sprite sprite){live.Add(key,sprite);order.AddLast(key);bytes+=(long)sprite.Width*sprite.Height*4;while(order.Count>1&&(order.Count>capacity||bytes>budget)){TKey old=order.First.Value;order.RemoveFirst();var value=live[old];bytes-=(long)value.Width*value.Height*4;live.Remove(old);value.Dispose();}}
 internal void Clear(){foreach(var s in live.Values)s.Dispose();live.Clear();order.Clear();bytes=0;}
 public void Dispose(){Clear();}
}
}
