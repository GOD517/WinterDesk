using System;
using System.Drawing;
using System.Collections.Generic;
namespace WinterDesk {
internal static class GarlandModes {
 internal const int Count=7,Frames=16;
 internal static string Label(int mode){return "Гирлянды: "+new[]{"выключены","разноцветные","мерцание","красные","зелёные","дыхание","бегущие огни"}[mode%Count];}
 internal static int Phase(double now,int mode){return mode<2?0:(int)(now*10)%Frames;}
}
internal static class WindowDecor {
 // Keep the icon/system menu and the complete caption-button rectangle free, including glow.
 internal static Rectangle Band(Rectangle window,Rectangle caption,float scale){
  int left=window.Left+(int)Math.Ceiling(34*scale),right=window.Right-(int)Math.Ceiling(180*scale);
  if(!caption.IsEmpty&&caption.Width>0&&caption.Left>window.Left&&caption.Right<=window.Right+12*scale)right=Math.Min(window.Right-(int)(12*scale),caption.Left-(int)Math.Ceiling(12*scale));
  if(right-left<55*scale)return Rectangle.Empty;
  return new Rectangle(left,window.Top+(int)(4*scale),right-left,Math.Max(20,(int)(28*scale)));
 }
 internal static Rectangle Caption(IntPtr handle){Native.RECT c,w;try{if(Native.GetWindowRect(handle,out w)&&Native.DwmGetWindowAttribute(handle,5,out c,16)==0&&c.R>c.L&&c.B>c.T)return Rectangle.FromLTRB(w.L+c.L,w.T+c.T,w.L+c.R,w.T+c.B);}catch{}return Rectangle.Empty;}
}
internal sealed class WindowSnowmanSchedule {
 readonly double interval;internal double Next;
 internal WindowSnowmanSchedule(double seconds=600){if(seconds<=0)throw new ArgumentOutOfRangeException("seconds");interval=seconds;Next=seconds;}
 internal bool Due(double now){if(now<Next)return false;Next=now+interval;return true;}

}
internal sealed partial class Controller {
 void CycleGarland(){garlandMode=(garlandMode+1)%GarlandModes.Count;lights=garlandMode!=0;garlandItem.Text=GarlandModes.Label(garlandMode);foreach(var s in windows.Values){s.LightMode=-1;s.Light.Show(false);}tracker.Dirty=true;scanAt=0;}
 void ConfigureGarland(Surface s,WindowInfo info,Rectangle screen){var band=WindowDecor.Band(info.Rect,WindowDecor.Caption(info.Handle),s.Scale);band=Rectangle.Intersect(band,screen);s.LightAllowed=!band.IsEmpty&&band.Width>40;s.LightX=band.X;s.LightY=band.Y;if(!s.LightAllowed){s.Light.Show(false);return;}
  if(s.LightFrames!=null&&s.LightWidth==band.Width&&s.LightHeight==band.Height&&s.LightMode==garlandMode)return;
  if(s.LightFrames!=null)s.LightFrames.Dispose();s.LightWidth=band.Width;s.LightHeight=band.Height;int mode=garlandMode,seed=sceneSeed^info.Handle.GetHashCode(),width=band.Width,height=band.Height;s.LightFrames=new FrameBank(GarlandModes.Frames,n=>WatercolorTheme.Garland(width,height,n,seed,mode),n=>n.ToString(),4,4194304);if(mode!=0){s.LightFrames.Request(0);s.LightFrames.Request(1);}s.LightMode=mode;s.LightPhase=-1;s.ClipHash=0;
 }
 void SpawnWindowSnowman(){if(snowmen.Count>=MaxSnowmen)return;var available=new List<Ledge>();foreach(var l in ledges){var screen=ScreenAt(l.Bounds.Left+l.Bounds.Width/2,l.Bounds.Top);if(screens.Exists(r=>r.Screen.DeviceName==screen.DeviceName&&r.FullScreen))continue;if(l.Bounds.Width<240*Scale(screen)||l.Bounds.Top<screen.Bounds.Top||l.Bounds.Top>screen.WorkingArea.Bottom-40)continue;available.Add(l);}
  while(available.Count>0){int index=random.Next(available.Count);var l=available[index];available.RemoveAt(index);for(int attempt=0;attempt<12;attempt++){double x=l.Bounds.Left+l.Bounds.Width*(.25+random.NextDouble()*.5);if(!l.Supports(x))continue;float scale=.598f*Scale(ScreenAt(x,l.Bounds.Top));double y=l.Bounds.Top-150*scale;bool blocked=false;foreach(var rect in l.Blockers)if(rect.IntersectsWith(new Rectangle((int)x-20,(int)y,40,(int)(150*scale+1))))blocked=true;if(blocked)continue;SpawnSnowmanAt(x,y,scale,true);return;}}
 }
}
}
