using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
namespace WinterDesk {
// One outstanding UI frame at most. Missed deadlines are skipped, never queued in a burst.
internal sealed class FrameClock : IDisposable {
 readonly Control dispatch;Thread worker;IntPtr timer;volatile bool running;int pending,interval=17;long delivered;internal event EventHandler Tick;internal bool HighResolution;
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode)] static extern IntPtr CreateWaitableTimerEx(IntPtr attributes,string name,uint flags,uint access);
 [DllImport("kernel32.dll")] static extern bool SetWaitableTimer(IntPtr timer,ref long due,int period,IntPtr callback,IntPtr state,bool resume);
 [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr handle,uint timeout);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
 internal FrameClock(Control control){dispatch=control;timer=CreateWaitableTimerEx(IntPtr.Zero,null,2,0x1F0003);HighResolution=timer!=IntPtr.Zero;if(timer==IntPtr.Zero)timer=CreateWaitableTimerEx(IntPtr.Zero,null,0,0x1F0003);if(timer==IntPtr.Zero)throw new System.ComponentModel.Win32Exception();}
 internal int Interval {get{return Volatile.Read(ref interval);}set{int old=Interlocked.Exchange(ref interval,value);if(value<old&&timer!=IntPtr.Zero){long due=-1;SetWaitableTimer(timer,ref due,0,IntPtr.Zero,IntPtr.Zero,false);}}}
 internal double TargetFps {get{return Interval==8?120:Interval==11?90:Interval==17?60:Interval==33?30:1000.0/Interval;}}
 internal long Delivered {get{return Interlocked.Read(ref delivered);}}
 internal void Start(){if(running)return;running=true;worker=new Thread(Loop){IsBackground=true,Name="WinterDesk frame scheduler"};worker.Start();}
 internal void Stop(){running=false;if(timer!=IntPtr.Zero){long due=-1;SetWaitableTimer(timer,ref due,0,IntPtr.Zero,IntPtr.Zero,false);}}
 void Loop(){double next=Stopwatch.GetTimestamp(),lastRate=0;while(running){double rate=TargetFps,now=Stopwatch.GetTimestamp();if(rate!=lastRate){next=now;lastRate=rate;}next+=Stopwatch.Frequency/rate;if(next<now)next=now+Stopwatch.Frequency/rate;long due=-(long)Math.Max(1,(next-now)*10000000/Stopwatch.Frequency);SetWaitableTimer(timer,ref due,0,IntPtr.Zero,IntPtr.Zero,false);WaitForSingleObject(timer,1000);if(!running)break;
   if(Interlocked.CompareExchange(ref pending,1,0)==0){try{dispatch.BeginInvoke((Action)delegate{try{if(!running||dispatch.IsDisposed)return;Interlocked.Increment(ref delivered);var handler=Tick;if(handler!=null)handler(this,EventArgs.Empty);}finally{Interlocked.Exchange(ref pending,0);}});}catch(InvalidOperationException){Interlocked.Exchange(ref pending,0);}}
  }}
 public void Dispose(){Stop();if(worker!=null)worker.Join(1000);if(timer!=IntPtr.Zero){CloseHandle(timer);timer=IntPtr.Zero;}}
}
}
