using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
namespace WinterDesk {
internal sealed class WindowInfo {internal IntPtr Handle;internal Rectangle Rect;internal List<Rectangle> Blockers;}
internal sealed class WindowTracker : IDisposable {

 readonly object departureLock=new object();readonly HashSet<IntPtr> departures=new HashSet<IntPtr>();
 internal static bool IsDepartureEvent(uint evt,int obj,int child){return obj==0&&child==0&&(evt==0x16||evt==0x8001||evt==0x8003);}
 internal HashSet<IntPtr> TakeDepartures(){lock(departureLock){var result=new HashSet<IntPtr>(departures);departures.Clear();return result;}}
 readonly Native.EventProc callback;readonly List<IntPtr> hooks=new List<IntPtr>();internal bool Dirty=true;readonly uint pid=(uint)Process.GetCurrentProcess().Id;
 internal WindowTracker(){callback=Event;Hook(3,3);Hook(0x16,0x17);Hook(0x8000,0x8003);Hook(0x800B,0x800B);Hook(0x8004,0x8005);}
 void Hook(uint a,uint b){IntPtr h=Native.SetWinEventHook(a,b,IntPtr.Zero,callback,0,0,2);if(h!=IntPtr.Zero)hooks.Add(h);}
 void Event(IntPtr hook,uint evt,IntPtr hwnd,int obj,int child,uint thread,uint timestamp){if(hwnd!=IntPtr.Zero&&!Layer.Handles.Contains(hwnd)&&(obj==0||evt==3||evt==0x16)){Dirty=true;if(IsDepartureEvent(evt,obj,child))lock(departureLock)departures.Add(hwnd);}}
 internal List<WindowInfo> Snapshot(){var result=new List<WindowInfo>();var above=new List<Rectangle>();Native.EnumWindows(delegate(IntPtr h,IntPtr arg){
  if(Layer.Handles.Contains(h)||!Native.IsWindowVisible(h)||Native.IsIconic(h))return true;
  uint owner;Native.GetWindowThreadProcessId(h,out owner);var ownTitle=new StringBuilder(256);Native.GetWindowText(h,ownTitle,256);if(owner==pid)return true;
  int cloaked;if(Native.GetCloaked(h,14,out cloaked,4)==0&&cloaked!=0)return true;
  string cls=Native.Class(h);if(cls=="Progman"||cls=="WorkerW"||cls=="Shell_TrayWnd"||cls=="Shell_SecondaryTrayWnd"||cls=="tooltips_class32")return true;
  var r=Native.Bounds(h);if(r.Width<20||r.Height<20)return true;var title=new StringBuilder(256);Native.GetWindowText(h,title,256);
  int styles=Native.GetStyle(h,-20);bool eligible=(styles&0x80)==0&&title.Length>0&&r.Width>=180&&r.Height>=80;
  if(eligible)result.Add(new WindowInfo{Handle=h,Rect=r,Blockers=new List<Rectangle>(above)});
  if((styles&0x20)==0)above.Add(r);return true;
 },IntPtr.Zero);Dirty=false;return result;}
 public void Dispose(){foreach(var h in hooks)Native.UnhookWinEvent(h);}
}
}
