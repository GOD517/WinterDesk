using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;

namespace WinterDesk {
internal static class Native {
 [StructLayout(LayoutKind.Sequential)] internal struct BITMAPINFO {internal uint Size;internal int Width,Height;internal ushort Planes,Bits;internal uint Compression,ImageSize;internal int XPels,YPels;internal uint Used,Important;}
 [DllImport("gdi32.dll")] internal static extern IntPtr CreateDIBSection(IntPtr dc,ref BITMAPINFO info,uint usage,out IntPtr bits,IntPtr section,uint offset);
 [DllImport("msvcrt.dll",CallingConvention=CallingConvention.Cdecl)] internal static extern IntPtr memset(IntPtr target,int value,UIntPtr length);
 [DllImport("msimg32.dll")] internal static extern bool AlphaBlend(IntPtr dst,int x,int y,int w,int h,IntPtr src,int sx,int sy,int sw,int sh,BLEND blend);

 [StructLayout(LayoutKind.Sequential)] internal struct POINT { public int X,Y; public POINT(int x,int y){X=x;Y=y;} }
 [StructLayout(LayoutKind.Sequential)] internal struct SIZE { public int W,H; public SIZE(int w,int h){W=w;H=h;} }
 [StructLayout(LayoutKind.Sequential)] internal struct RECT { public int L,T,R,B; }
 [StructLayout(LayoutKind.Sequential, Pack=1)] internal struct BLEND { public byte Op,Flags,Alpha,Format; }
 [DllImport("user32.dll")] internal static extern IntPtr GetDC(IntPtr h);
 [DllImport("user32.dll")] internal static extern int ReleaseDC(IntPtr h,IntPtr dc);
 [DllImport("gdi32.dll")] internal static extern IntPtr CreateCompatibleDC(IntPtr dc);
 [DllImport("gdi32.dll")] internal static extern IntPtr SelectObject(IntPtr dc,IntPtr obj);
 [DllImport("gdi32.dll")] internal static extern bool DeleteDC(IntPtr dc);
 [DllImport("gdi32.dll")] internal static extern bool DeleteObject(IntPtr obj);
 [DllImport("user32.dll",SetLastError=true)] internal static extern bool UpdateLayeredWindow(IntPtr hwnd,IntPtr dst,ref POINT pos,ref SIZE size,IntPtr src,ref POINT origin,int key,ref BLEND blend,int flags);
 [DllImport("user32.dll")] internal static extern bool SetWindowPos(IntPtr h,IntPtr after,int x,int y,int w,int height,uint flags);
 [DllImport("user32.dll")] internal static extern bool ShowWindow(IntPtr h,int how);
 [DllImport("user32.dll")] internal static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr h,out RECT rect);
 [DllImport("dwmapi.dll")] internal static extern int DwmGetWindowAttribute(IntPtr h,int attr,out RECT rect,int size);
 [DllImport("user32.dll")] internal static extern bool IsIconic(IntPtr h);
 [DllImport("user32.dll")] internal static extern int GetClassName(IntPtr h,StringBuilder text,int count);
 [DllImport("user32.dll")] internal static extern bool SetProcessDpiAwarenessContext(IntPtr value);
 [DllImport("user32.dll")] internal static extern bool SetProcessDPIAware();
 [DllImport("user32.dll")] internal static extern bool RegisterHotKey(IntPtr h,int id,uint modifiers,uint key);
 [DllImport("user32.dll")] internal static extern bool UnregisterHotKey(IntPtr h,int id);
 [DllImport("user32.dll")] internal static extern IntPtr BeginDeferWindowPos(int count);
 [DllImport("user32.dll")] internal static extern IntPtr DeferWindowPos(IntPtr batch,IntPtr h,IntPtr after,int x,int y,int w,int height,uint flags);
 [DllImport("user32.dll")] internal static extern bool EndDeferWindowPos(IntPtr batch);
 internal delegate bool EnumProc(IntPtr h,IntPtr arg);
 [DllImport("user32.dll")] internal static extern bool EnumWindows(EnumProc callback,IntPtr arg);
 [DllImport("user32.dll")] internal static extern bool IsWindowVisible(IntPtr h);
 [DllImport("user32.dll")] internal static extern bool IsWindow(IntPtr h);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)] internal static extern int GetWindowText(IntPtr h,StringBuilder text,int count);
 [DllImport("user32.dll")] internal static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("user32.dll",EntryPoint="GetWindowLongW")] internal static extern int GetStyle(IntPtr h,int index);
 [DllImport("user32.dll",EntryPoint="SetWindowLongW")] internal static extern int SetStyle(IntPtr h,int index,int value);
 [DllImport("user32.dll")] internal static extern IntPtr SetCapture(IntPtr h);
 [DllImport("user32.dll")] internal static extern bool ReleaseCapture();
 [DllImport("user32.dll")] internal static extern IntPtr SendMessage(IntPtr h,int message,IntPtr w,IntPtr l);
 [DllImport("dwmapi.dll",EntryPoint="DwmGetWindowAttribute")] internal static extern int GetCloaked(IntPtr h,int attr,out int value,int size);
 [DllImport("gdi32.dll")] internal static extern IntPtr CreateRectRgn(int l,int t,int r,int b);
 [DllImport("gdi32.dll")] internal static extern int CombineRgn(IntPtr dst,IntPtr a,IntPtr b,int mode);
 [DllImport("user32.dll")] internal static extern int SetWindowRgn(IntPtr h,IntPtr region,bool redraw);
 [DllImport("user32.dll")] internal static extern int GetWindowRgn(IntPtr h,IntPtr region);
 [DllImport("gdi32.dll")] internal static extern bool PtInRegion(IntPtr region,int x,int y);
 [DllImport("user32.dll")] internal static extern bool GetCursorPos(out POINT pos);
 [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
 [DllImport("user32.dll")] internal static extern uint GetGuiResources(IntPtr h,int flag);
 internal delegate void EventProc(IntPtr hook,uint evt,IntPtr h,int obj,int child,uint thread,uint timestamp);
 [DllImport("user32.dll")] internal static extern IntPtr SetWinEventHook(uint min,uint max,IntPtr module,EventProc callback,uint process,uint thread,uint flags);
 [DllImport("user32.dll")] internal static extern bool UnhookWinEvent(IntPtr hook);
 internal static string Class(IntPtr h){var s=new StringBuilder(128);GetClassName(h,s,128);return s.ToString();}
 internal static Rectangle Bounds(IntPtr h){RECT r;if(DwmGetWindowAttribute(h,9,out r,16)!=0) GetWindowRect(h,out r);return Rectangle.FromLTRB(r.L,r.T,r.R,r.B);}
}
// Each sprite owns its premultiplied bitmap and DC. Reused across animation frames.
internal sealed class Sprite : IDisposable {
 internal IntPtr DC; IntPtr bitmap,old,bits; internal int Width,Height;
 internal Sprite(int width,int height){Width=width;Height=height;DC=Native.CreateCompatibleDC(IntPtr.Zero);var info=new Native.BITMAPINFO{Size=40,Width=width,Height=-height,Planes=1,Bits=32};bitmap=Native.CreateDIBSection(DC,ref info,0,out bits,IntPtr.Zero,0);if(bitmap==IntPtr.Zero){Native.DeleteDC(DC);DC=IntPtr.Zero;throw new System.ComponentModel.Win32Exception();}old=Native.SelectObject(DC,bitmap);}
 internal void Clear(){Native.memset(bits,0,new UIntPtr((uint)(Width*Height*4)));}
 internal unsafe void Blit(Sprite source,int x,int y){int sx=Math.Max(0,-x),sy=Math.Max(0,-y),w=Math.Min(source.Width-sx,Width-Math.Max(0,x)),h=Math.Min(source.Height-sy,Height-Math.Max(0,y));if(w<=0||h<=0)return;uint* src=(uint*)source.bits.ToPointer()+sy*source.Width+sx;uint* dst=(uint*)bits.ToPointer()+Math.Max(0,y)*Width+Math.Max(0,x);for(int yy=0;yy<h;yy++){for(int xx=0;xx<w;xx++){uint c=src[xx],alpha=c>>24;if(alpha==0)continue;if(alpha==255){dst[xx]=c;continue;}uint d=dst[xx],inv=256-alpha;dst[xx]=unchecked(c+(((d&0x00FF00FF)*inv>>8)&0x00FF00FF)+((((d>>8)&0x00FF00FF)*inv)&0xFF00FF00));}src+=source.Width;dst+=Width;}}
 internal Sprite(Bitmap image):this(image.Width,image.Height){try{Upload(image);}catch{Dispose();throw;}}
 internal void Upload(Bitmap image){var data=image.LockBits(new Rectangle(0,0,Width,Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppPArgb);try{var bytes=new byte[Width*4];for(int y=0;y<Height;y++){Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),bytes,0,bytes.Length);Marshal.Copy(bytes,0,IntPtr.Add(bits,y*Width*4),bytes.Length);}}finally{image.UnlockBits(data);}}
 internal int Pixel(int x,int y){return Marshal.ReadInt32(bits,(y*Width+x)*4);}

 public void Dispose(){if(DC==IntPtr.Zero)return;Native.SelectObject(DC,old);Native.DeleteObject(bitmap);Native.DeleteDC(DC);DC=IntPtr.Zero;}
}
internal sealed class Layer : NativeWindow, IDisposable {
 internal int X,Y; internal bool Visible;int regionHash;bool regionReady,input,dragging;internal Func<int,int,bool> Hit;internal Action<int,int> MouseDown,MouseDrag,MouseUp;internal static readonly HashSet<IntPtr> Handles=new HashSet<IntPtr>();
 internal void SetInput(bool enabled){input=enabled;int style=Native.GetStyle(Handle,-20);Native.SetStyle(Handle,-20,enabled?style&~0x20:style|0x20);if(!enabled&&dragging){dragging=false;Native.ReleaseCapture();if(MouseUp!=null)MouseUp(0,0);}}
 internal Layer(){CreateHandle(new CreateParams {Caption="WinterDesk decoration",ClassStyle=0,Style=unchecked((int)0x80000000),ExStyle=0x80000|0x20|0x80|0x08000000});Handles.Add(Handle);}
 internal void Clip(Rectangle bounds,IList<Rectangle> blockers){int hash=bounds.Width*397^bounds.Height;foreach(var b in blockers){Rectangle cut=Rectangle.Intersect(bounds,b);if(cut.IsEmpty)continue;cut.Offset(-bounds.Left,-bounds.Top);hash=unchecked(hash*31+cut.GetHashCode());}if(regionReady&&hash==regionHash)return;regionHash=hash;regionReady=true;IntPtr region=Native.CreateRectRgn(0,0,bounds.Width,bounds.Height);try{foreach(var b in blockers){Rectangle cut=Rectangle.Intersect(bounds,b);if(cut.IsEmpty)continue;IntPtr other=Native.CreateRectRgn(cut.Left-bounds.Left,cut.Top-bounds.Top,cut.Right-bounds.Left,cut.Bottom-bounds.Top);Native.CombineRgn(region,region,other,4);Native.DeleteObject(other);}if(Native.SetWindowRgn(Handle,region,true)!=0)region=IntPtr.Zero;}finally{if(region!=IntPtr.Zero)Native.DeleteObject(region);}}
 internal void Paint(Sprite s,int x,int y){X=x;Y=y;var p=new Native.POINT(x,y);var o=new Native.POINT(0,0);var z=new Native.SIZE(s.Width,s.Height);var b=new Native.BLEND{Op=0,Alpha=255,Format=1};if(!Native.UpdateLayeredWindow(Handle,IntPtr.Zero,ref p,ref z,s.DC,ref o,0,ref b,2))throw new System.ComponentModel.Win32Exception();}
 internal void Move(int x,int y){if(X==x&&Y==y)return;X=x;Y=y;Native.SetWindowPos(Handle,IntPtr.Zero,x,y,0,0,0x1|0x10|0x4);}
 internal void Show(bool show){if(Visible==show)return;if(!show&&dragging){dragging=false;Native.ReleaseCapture();if(MouseUp!=null)MouseUp(0,0);}Visible=show;Native.ShowWindow(Handle,show?4:0);if(show)Native.SetWindowPos(Handle,new IntPtr(-1),0,0,0,0,0x1|0x2|0x10);}
 protected override void WndProc(ref Message m){if(m.Msg==0x84){int x=(short)(m.LParam.ToInt64()&65535)-X,y=(short)((m.LParam.ToInt64()>>16)&65535)-Y;m.Result=new IntPtr(input&&(dragging||Hit!=null&&Hit(x,y))?1:-1);return;}if(m.Msg==0x21){m.Result=new IntPtr(3);return;}
  int lx=(short)(m.LParam.ToInt64()&65535),ly=(short)((m.LParam.ToInt64()>>16)&65535);
  if(input&&m.Msg==0x201){dragging=true;Native.SetCapture(Handle);if(MouseDown!=null)MouseDown(lx,ly);m.Result=IntPtr.Zero;return;}
  if(dragging&&m.Msg==0x200){if(MouseDrag!=null)MouseDrag(lx,ly);m.Result=IntPtr.Zero;return;}
  if(dragging&&(m.Msg==0x202||m.Msg==0x215)){dragging=false;if(m.Msg==0x202)Native.ReleaseCapture();if(MouseUp!=null)MouseUp(lx,ly);m.Result=IntPtr.Zero;return;}
  base.WndProc(ref m);}
 public void Dispose(){if(Handle!=IntPtr.Zero){Handles.Remove(Handle);DestroyHandle();}}
}
}
