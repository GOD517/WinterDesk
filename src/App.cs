using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;
namespace WinterDesk {
internal sealed class Flake : IDisposable {internal Sprite Image;internal double X,Y,Speed,Drift,Phase;internal int Size,Height,Count,Screen,FlowSeed;internal double FlowX,FlowY;public void Dispose(){}}
internal sealed class ScreenRig : IDisposable {
 internal Screen Screen;internal float Scale;internal Surface Ground;internal bool FullScreen;internal Layer FallingLayer=new Layer();internal Sprite FallingCanvas;
 public void Dispose(){Ground.Dispose();FallingLayer.Dispose();if(FallingCanvas!=null)FallingCanvas.Dispose();}
}
internal sealed class Atlas:IDisposable {
 internal readonly FrameBank Workers,Santas,PetImages,Runners;readonly FrameBank workerLeft,petMirrors,slides;internal readonly float Scale;internal double LastUsed;
 internal Atlas(float dpi){Scale=Math.Max(.8f,dpi*1.6f);Workers=new FrameBank(256,i=>DrawnArt.Worker(i,WorkerScale),i=>DrawnArt.Identity("shovel",i),48);workerLeft=new FrameBank(256,i=>{var b=DrawnArt.Worker(i,WorkerScale);b.RotateFlip(RotateFlipType.RotateNoneFlipX);return b;},i=>DrawnArt.Identity("shovel",i),48);Santas=new FrameBank(96,i=>DrawnArt.Santa(i,Scale/1.6f),i=>DrawnArt.Identity("santaTeam",i),16,16777216);PetImages=new FrameBank(192,i=>DrawnArt.Pet(i,PetScale),DrawnArt.PetIdentity,64);Runners=new FrameBank(128,i=>DrawnArt.Runner(i,PetScale),i=>DrawnArt.Identity("walk",i),24);slides=new FrameBank(32,i=>DrawnArt.Slide(i,PetScale),i=>DrawnArt.Identity("flap",i*2%64),8);petMirrors=new FrameBank(352,i=>{var b=i>=320?DrawnArt.Slide(i-320,PetScale):i>=192?DrawnArt.Runner(i-192,PetScale):DrawnArt.Pet(i,PetScale);b.RotateFlip(RotateFlipType.RotateNoneFlipX);return b;},i=>i>=320?DrawnArt.Identity("flap",(i-320)*2%64):i>=192?DrawnArt.Identity("walk",i-192):DrawnArt.PetIdentity(i),96);}
 internal float WorkerScale {get{return Scale*.4f;}}
 internal float PetScale {get{return WorkerScale*(31f/30f);}}
 internal void PrepareWorker(){if(WorkerReady)return;Workers.Warm();workerLeft.Warm();}
 internal bool WorkerReady {get{return Workers.Ready&&workerLeft.Ready;}}
 internal Sprite Worker(int frame,int mask,int direction=1){return (mask&2)!=0?null:(direction>0?Workers[frame]:workerLeft[frame]);}
 internal Sprite PetSprite(int frame,int direction){return direction<0?petMirrors[frame]:frame>=320?slides[frame-320]:frame>=192?Runners[frame-192]:PetImages[frame];}
 internal void PrepareSanta(){Santas.Request(0);Santas.Request(1);}
 internal void PreparePets(){if(PetImages.Ready&&Runners.Ready&&petMirrors.Ready&&slides.Ready)return;PetImages.Warm();Runners.Warm();slides.Warm();petMirrors.Warm();}
 internal void ReleaseSanta(){} // The bounded cache keeps a few recent frames for the next visit.
 public void Dispose(){Workers.Dispose();Santas.Dispose();PetImages.Dispose();Runners.Dispose();workerLeft.Dispose();petMirrors.Dispose();slides.Dispose();}
}
internal sealed class Actor : IDisposable {internal Layer Window=new Layer();internal int Direction=1;internal Surface Surface;internal ScreenRig Rig;internal Atlas Atlas;internal bool Santa;internal double X,Y,Progress,WorkTime;internal int Missing;internal Pet Holding;internal int Frame=-1;public void Dispose(){Window.Dispose();}}
internal sealed partial class Controller : ApplicationContext {
 readonly Control dispatch=new Control();readonly NotifyIcon tray;readonly FrameClock clock;readonly Stopwatch watch=Stopwatch.StartNew();readonly Random random=new Random();readonly int sceneSeed=Artwork.NewSeed();readonly WindowTracker tracker=new WindowTracker();
 readonly List<Flake> flakes=new List<Flake>();readonly List<Sprite> flakeArt=new List<Sprite>();readonly List<ScreenRig> screens=new List<ScreenRig>();readonly Dictionary<string,Surface> windows=new Dictionary<string,Surface>();readonly List<Actor> actors=new List<Actor>();readonly List<Pet> pets=new List<Pet>();readonly List<Ledge> ledges=new List<Ledge>();readonly Dictionary<int,Atlas> atlases=new Dictionary<int,Atlas>();readonly HourlySchedule schedule;
 readonly Hotkeys keys;ToolStripMenuItem pauseItem,statusItem,stormItem,garlandItem;int garlandMode=2;readonly WindowSnowmanSchedule snowmanSchedule=new WindowSnowmanSchedule();
 bool paused,locked,disposed,falling=true,banks=true,lights=true,penguins=true,santa=true,storm=false,raking=true;double lastTime,scanAt,eventScanAt,settleAt,statusAt,snowTime,lastStatusTime;long lastStatusFrames;int visibleWindows,sceneRevision;
 internal int DecoratedCount {get{return windows.Count;}}internal int WindowSnowCount {get{int n=0;foreach(var w in windows.Values)if(w.Model!=null)n++;return n;}}internal int SnowCount {get{int n=0;foreach(var f in flakes)n+=f.Count;return n;}}internal bool Storm {get{return storm;}}internal int ActorCount {get{return actors.Count;}}
 internal bool HasTarget(IntPtr h){foreach(var s in windows.Values)if(s.Target==h)return true;return false;}
 [DllImport("shcore.dll")] static extern int GetDpiForMonitor(IntPtr monitor,int type,out uint x,out uint y);
 [DllImport("user32.dll")] static extern IntPtr MonitorFromPoint(Native.POINT point,uint flags);
 internal static float Scale(Screen s){try{uint x,y;if(GetDpiForMonitor(MonitorFromPoint(new Native.POINT(s.Bounds.X+10,s.Bounds.Y+10),2),0,out x,out y)==0)return Math.Max(1f,x/96f);}catch{}return 1;}
 internal Controller(){
  var handle=dispatch.Handle;schedule=new HourlySchedule(random,0);clock=new FrameClock(dispatch){Interval=8};clock.Tick+=Tick;
  tray=new NotifyIcon{Icon=WatercolorTheme.AppIcon,Text="WinterDesk "+UpdateTrust.VersionText+" — snow & penguins",Visible=true};var menu=new ContextMenuStrip();menu.Items.Add("WinterDesk "+UpdateTrust.VersionText+" • Все окна").Enabled=false;
  pauseItem=new ToolStripMenuItem("Пауза • Ctrl+Alt+Shift+S");pauseItem.Click+=delegate{TogglePause();};menu.Items.Add(new ToolStripSeparator());
  AddToggle(menu,"Снег по всему рабочему столу",falling,delegate(bool v){falling=v;RefreshFlakes();});
  garlandItem=new ToolStripMenuItem(GarlandModes.Label(garlandMode));garlandItem.Click+=delegate{CycleGarland();};menu.Items.Add(garlandItem);
  stormItem=AddToggle(menu,"Вьюга • в 8 раз больше снега",storm,delegate(bool v){SetStorm(v);});
  menu.Items.Add(new ToolStripSeparator());
  menu.Items.Add("Левая кнопка — катать шар; Shift — сгребать").Enabled=false;
  statusItem=new ToolStripMenuItem("Готово"){Enabled=false};menu.Items.Add("Выход • Ctrl+Alt+Shift+Q",null,delegate{ExitThread();});menu.Items.Add("Панель управления",null,delegate{TogglePanel();});WatercolorTheme.StyleMenu(menu);tray.ContextMenuStrip=menu;tray.DoubleClick+=delegate{TogglePanel();};keys=new Hotkeys(TogglePause,delegate{ExitThread();},LaunchSanta,Blizzard,delegate{SetStorm(!storm);});

  SystemEvents.DisplaySettingsChanged+=DisplayChanged;SystemEvents.UserPreferenceChanged+=PreferencesChanged;SystemEvents.SessionSwitch+=SessionChanged;
  ResetScreens();InitPanel(menu);lastTime=watch.Elapsed.TotalSeconds;clock.Start();
 }
 ToolStripMenuItem AddToggle(ContextMenuStrip m,string title,bool initial,Action<bool> action){var i=new ToolStripMenuItem(title){Checked=initial,CheckOnClick=true};i.CheckedChanged+=delegate{action(i.Checked);};m.Items.Add(i);return i;}
 void TogglePause(){paused=!paused;pauseItem.Checked=paused;clock.Interval=paused?500:FrameInterval;if(paused)HideAll();else tracker.Dirty=true;}
 int FrameInterval {get{return 8;}}
 void OnUI(Action a){if(disposed)return;if(dispatch.InvokeRequired)dispatch.BeginInvoke(a);else a();}
 void DisplayChanged(object s,EventArgs e){OnUI(ResetScreens);}void PreferencesChanged(object s,UserPreferenceChangedEventArgs e){OnUI(ResetScreens);}
 void SessionChanged(object s,SessionSwitchEventArgs e){OnUI(delegate{locked=e.Reason==SessionSwitchReason.SessionLock||e.Reason==SessionSwitchReason.SessionLogoff;if(locked)HideAll();else tracker.Dirty=true;});}
 void ResetScreens(){sceneRevision++;
  ClearAvalanches();ClearSquirrel();ClearSnowmen();ClearSnowToys();foreach(var p in pets)p.Dispose();pets.Clear();foreach(var a in actors)a.Dispose();actors.Clear();foreach(var f in flakes)f.Dispose();flakes.Clear();foreach(var s in flakeArt)s.Dispose();flakeArt.Clear();foreach(var r in screens)r.Dispose();screens.Clear();foreach(var w in windows.Values)w.Dispose();windows.Clear();
  var all=Screen.AllScreens;for(int i=0;i<all.Length;i++){Screen s=all[i];float scale=Scale(s);var area=s.WorkingArea;
   var rig=new ScreenRig{Screen=s,Scale=scale,Ground=new Surface("desktop:"+i,IntPtr.Zero,area.Width,scale,true)};rig.Ground.Bounds=area;rig.Ground.BaseY=area.Bottom;rig.Ground.Visible=true;
   rig.FallingCanvas=new Sprite(area.Width,area.Height);ConfigureRake(rig.Ground);screens.Add(rig);var atlas=GetAtlas(scale);atlas.PrepareWorker();atlas.PreparePets();
  }RefreshFlakes();if(panel!=null)panel.EnsureVisible();tracker.Dirty=true;scanAt=0;
 }
 void ConfigureRake(Surface surface){surface.Snow.Hit=delegate(int x,int y){x-=surface.SnowPadding;return x>=0&&x<surface.Width&&y>=surface.Model.Height-surface.Model.LevelAt(x)&&y<surface.Model.Height;};
  surface.Snow.MouseDown=delegate(int x,int y){x-=surface.SnowPadding;surface.Dragging=true;surface.DragX=x;surface.Model.UntilWorker=watch.Elapsed.TotalSeconds+4;if((Native.GetAsyncKeyState(0x10)&0x8000)==0)StartBall(surface,x,y);};
  surface.Snow.MouseDrag=delegate(int x,int y){x-=surface.SnowPadding;if(activeBall!=null)RollBall(surface,x,y);else surface.Model.Pile(surface.DragX,x,(int)(18*surface.Scale));surface.DragX=x;surface.Model.UntilWorker=watch.Elapsed.TotalSeconds+4;};
  surface.Snow.MouseUp=delegate(int x,int y){surface.Dragging=false;if(activeBall!=null)ReleaseBall();surface.Model.SettleAfterDrag();surface.LastPaint=0;};surface.Snow.SetInput(raking);
 }
 internal void SetStorm(bool enabled){if(storm==enabled)return;storm=enabled;if(stormItem!=null&&stormItem.Checked!=enabled)stormItem.Checked=enabled;RefreshFlakes();}
 void RefreshFlakes(){foreach(var f in flakes)f.Dispose();flakes.Clear();foreach(var s in flakeArt)s.Dispose();flakeArt.Clear();if(!falling)return;
  int total=storm?1152:144;
  var usedSeeds=new HashSet<int>();for(int i=0;i<screens.Count;i++){var rig=screens[i];float scale=rig.Scale;var area=rig.Screen.WorkingArea;
   int poolStart=flakeArt.Count;int n=total/screens.Count+(i<total%screens.Count?1:0);for(int k=0;k<n;k++){int seed;do{seed=random.Next();}while(!usedSeeds.Add(seed));int size=SnowWeather.Size(10+random.NextDouble()*14,scale,storm);Sprite image;if(k<48){using(var b=VolumeSnow.Flake(size,seed))image=new Sprite(b);flakeArt.Add(image);}else image=flakeArt[poolStart+k%48];size=image.Width;var f=new Flake{Screen=i,Size=size,Height=size,Count=1,X=area.Left+random.NextDouble()*area.Width,Y=area.Top+random.NextDouble()*area.Height,Speed=(storm?85+random.NextDouble()*90:27+random.NextDouble()*40)*scale,Drift=(storm?90:10)*scale,Phase=random.NextDouble()*6.28,FlowSeed=seed};f.Image=image;flakes.Add(f);}
  }
 }

 Atlas GetAtlas(float scale){int key=Math.Max(1,(int)Math.Round(scale*10)*10);Atlas a;if(!atlases.TryGetValue(key,out a)){a=new Atlas(key/100f);atlases.Add(key,a);}a.LastUsed=watch.Elapsed.TotalSeconds;return a;}
 void TrimAtlases(double now){var remove=new List<int>();foreach(var pair in atlases){var atlas=pair.Value;if(now-atlas.LastUsed<30||actors.Exists(a=>a.Atlas==atlas)||pets.Exists(p=>p.Atlas==atlas))continue;remove.Add(pair.Key);}foreach(int key in remove){atlases[key].Dispose();atlases.Remove(key);}}
 void ProcessWindowDepartures(){var targets=tracker.TakeDepartures();if(targets.Count==0)return;var remove=new List<string>();foreach(var pair in windows)if(targets.Contains(pair.Value.Target)){ReleaseWindowSnow(pair.Value);CancelWorker(pair.Value);pair.Value.Dispose();remove.Add(pair.Key);}foreach(var key in remove)windows.Remove(key);}
 void ScanWindows(double now){
  ProcessWindowDepartures();var found=tracker.Snapshot();sceneRevision++;ledges.Clear();foreach(var info in found)ledges.Add(new Ledge{Handle=info.Handle,Bounds=info.Rect,Blockers=info.Blockers});var keep=new HashSet<string>();visibleWindows=0;
  foreach(var info in found){for(int i=0;i<screens.Count;i++){var rig=screens[i];var screen=rig.Screen.Bounds;var r=info.Rect;if(r.Top<screen.Top-6||r.Top>screen.Bottom-30)continue;Rectangle part=Rectangle.Intersect(r,screen);if(part.Width<120)continue;
   string key=info.Handle.ToInt64()+":"+i;Surface surface;float scale=rig.Scale;int width=part.Width;
   if(windows.TryGetValue(key,out surface)&&(surface.Width!=width||surface.Scale!=scale)){var previous=surface;surface=new Surface(key,info.Handle,width,scale,false);SnowModel.Transfer(previous.Model,surface.Model);windows[key]=surface;ConfigureRake(surface);CancelWorker(previous);previous.Dispose();}
   if(surface==null){surface=new Surface(key,info.Handle,width,scale,false);windows.Add(key,surface);ConfigureRake(surface);}
   surface.Bounds=new Rectangle(part.Left,r.Top,width,r.Height);surface.BaseY=Math.Max(screen.Top,r.Top)+3;ConfigureGarland(surface,info,screen);surface.Blockers=info.Blockers;surface.Visible=!rig.FullScreen;
   Rectangle band=new Rectangle(part.Left,Math.Max(screen.Top,r.Top-30),width,(int)(80*scale));foreach(var b in info.Blockers)if(b.Contains(band)){surface.Visible=false;break;}
   if(surface.Visible)visibleWindows++;keep.Add(key);
  }}
  var gone=new List<string>();foreach(var pair in windows)if(!keep.Contains(pair.Key))gone.Add(pair.Key);foreach(var key in gone){var lost=windows[key];bool exists=Native.IsWindow(lost.Target),minimized=exists&&Native.IsIconic(lost.Target),visible=exists&&Native.IsWindowVisible(lost.Target);if(!exists||minimized||!visible){if(WindowSnowRelease.ShouldRelease(exists,minimized,visible,false,lost.Model.Mass*lost.Model.Cell))ReleaseWindowSnow(lost);CancelWorker(lost);lost.Dispose();windows.Remove(key);}else lost.Visible=false;}scanAt=now+1.2;eventScanAt=now+.12;
 }
 void CancelWorker(Surface s){for(int i=actors.Count-1;i>=0;i--)if(actors[i].Surface==s){actors[i].Dispose();actors.RemoveAt(i);}}
 void HideAll(){foreach(var impact in snowImpacts)impact.Window.Show(false);foreach(var b in snowBalls)b.Window.Show(false);foreach(var c in avalanches)c.Window.Show(false);if(squirrel!=null)squirrel.Window.Show(false);foreach(var r in screens){r.Ground.Hide();r.FallingLayer.Show(false);}foreach(var p in pets)p.Window.Show(false);foreach(var w in windows.Values)w.Hide();foreach(var a in actors){a.Window.Show(false);}foreach(var a in snowmen){a.Window.Show(false);a.Construction.Show(false);}}
 void CheckFullscreen(){IntPtr h=Native.GetForegroundWindow();string cls=Native.Class(h);Rectangle r=Native.Bounds(h);bool shell=cls=="Progman"||cls=="WorkerW"||cls=="Shell_TrayWnd";foreach(var rig in screens){Rectangle s=rig.Screen.Bounds;bool full=!shell&&r.Contains(s);if(rig.FullScreen!=full)tracker.Dirty=true;rig.FullScreen=full;rig.Ground.Visible=!full;}}
 internal double TotalSnowMass {get{double total=0;foreach(var r in screens)total+=r.Ground.Model.Mass;return total;}}
 List<Surface> AllSurfaces(){var list=new List<Surface>();foreach(var rig in screens)if(!rig.FullScreen)list.Add(rig.Ground);foreach(var w in windows.Values)if(w.Visible)list.Add(w);return list;}
 internal void Blizzard(){if(disposed)return;banks=true;penguins=true;foreach(var s in AllSurfaces())s.Model.Blizzard();LaunchWorkers(watch.Elapsed.TotalSeconds);}
 void LaunchWorkers(double now){if(!penguins||!banks)return;int workers=0;foreach(var a in actors)if(!a.Santa)workers++;foreach(var s in AllSurfaces()){
  if(workers>=1)break;if(!s.IsDesktop)continue;if(s.Model.Peak<16*s.Scale||s.Model.Width<240*s.Scale||s.Dragging||now<s.Model.UntilWorker)continue;bool exists=false;foreach(var a in actors)if(a.Surface==s)exists=true;if(exists)continue;
  var atlas=GetAtlas(s.Scale);atlas.PrepareWorker();atlas.PreparePets();if(!atlas.WorkerReady||!atlas.PetImages.Ready)continue;var actor=new Actor{Surface=s,Atlas=atlas,X=s.Bounds.Left-atlas.Workers[0].Width,Y=s.BaseY-atlas.Workers[0].Height,Santa=false};actor.Direction=random.Next(2)==0?-1:1;float effective=atlas.WorkerScale;actor.Progress=0;actor.X=actor.Direction>0?s.Bounds.Left-85*effective:s.Bounds.Right-atlas.Workers[0].Width+85*effective;int first=(int)(actor.X-s.Bounds.Left+(actor.Direction>0?171*effective:atlas.Workers[0].Width-171*effective));s.Model.Sweep(actor.Direction>0?0:first,actor.Direction>0?first:s.Model.Width);ConfigurePick(actor);actors.Add(actor);s.Model.UntilWorker=now+8;workers++;
 }}
 internal void LaunchSanta(){if(disposed||!santa)return;foreach(var a in actors)if(a.Santa)return;ScreenRig rig=null;Native.POINT p;Native.GetCursorPos(out p);foreach(var s in screens)if(!s.FullScreen&&s.Screen.Bounds.Contains(p.X,p.Y))rig=s;if(rig==null)foreach(var s in screens)if(!s.FullScreen){rig=s;break;}if(rig==null)return;
  var atlas=GetAtlas(rig.Scale);atlas.PrepareSanta();if(!atlas.Santas.Has(0))return;Rectangle area=rig.Screen.WorkingArea;actors.Add(new Actor{Santa=true,Rig=rig,Atlas=atlas,X=area.Left-atlas.Santas[0].Width,Y=area.Top+Math.Max(60,area.Height*.18)});schedule.Schedule(watch.Elapsed.TotalSeconds);
 }
 int nativeFailures;bool resetAfterFault;double recoveryAt;
 void Tick(object sender,EventArgs e){var timing=Stopwatch.StartNew();try{RuntimeLog.Stage("frame preparation");if(resetAfterFault){if(watch.Elapsed.TotalSeconds<recoveryAt)return;resetAfterFault=false;ResetScreens();}FramePreparation.Pump();TickScene(sender,e);}catch(System.ComponentModel.Win32Exception error){RuntimeLog.Error("native frame",error);HideAll();if(++nativeFailures<3){resetAfterFault=true;recoveryAt=watch.Elapsed.TotalSeconds+.5;}else{paused=true;tray.ShowBalloonTip(5000,"WinterDesk","Оформление приостановлено после ошибки графики. Подробности сохранены в журнал.",ToolTipIcon.Warning);}}finally{RuntimeLog.Frame(timing.Elapsed.TotalMilliseconds);}}
 void TickScene(object sender,EventArgs e){
  double now=watch.Elapsed.TotalSeconds,dt=Math.Min(.15,now-lastTime);lastTime=now;if(paused||locked){UpdatePanel(now);return;}
  RuntimeLog.Stage("window scan");ProcessWindowDepartures();if(now>=scanAt||tracker.Dirty&&now>=eventScanAt){CheckFullscreen();ScanWindows(now);}
  bool any=false;foreach(var r in screens)if(!r.FullScreen)any=true;if(!any){HideAll();clock.Interval=250;return;}clock.Interval=FrameInterval;
  Native.POINT cursor;Native.GetCursorPos(out cursor);
  var surfaces=AllSurfaces();if(now>=settleAt){double seconds=settleAt==0?0:Math.Min(2,now-(settleAt-1));settleAt=now+1;if(falling&&banks)foreach(var s in surfaces)s.Model.Accumulate(seconds,storm);LaunchWorkers(now);}
  RuntimeLog.Stage("falling snow");AnimateFlakes(Math.Min(.2,now-snowTime),now,cursor,surfaces);snowTime=now;
  RuntimeLog.Stage("snowbanks and lights");int phase=GarlandModes.Phase(now,garlandMode);foreach(var rig in screens){if(rig.FullScreen){rig.Ground.Hide();continue;}if(banks){rig.Ground.PaintSnow(now);rig.Ground.Snow.Show(true);}else rig.Ground.Hide();}
  foreach(var w in windows.Values){if(!w.Visible){w.Hide();continue;}
   if(lights&&w.LightAllowed){var light=w.LightFrames[phase];w.LightFrames.Request((phase+1)%GarlandModes.Frames);if(light!=null&&w.LightPhase!=phase){w.Light.Paint(light,w.LightX,w.LightY);w.LightPhase=phase;}else if(w.Light.X!=w.LightX||w.Light.Y!=w.LightY)w.Light.Move(w.LightX,w.LightY);w.Light.Show(w.LightPhase>=0);}else w.Light.Show(false);if(banks){w.PaintSnow(now);w.Snow.Show(true);}else w.Snow.Show(false);w.ClipLayers();}
  if(snowmanSchedule.Due(now))SpawnWindowSnowman();if(squirrelSchedule.Due(now))SpawnSquirrel();RuntimeLog.Stage("squirrel and falling banks");AnimateSquirrel(dt,now);AnimateAvalanches(dt,now);RuntimeLog.Stage("worker and Santa");AnimateActors(dt,now,cursor);RuntimeLog.Stage("penguins");AnimatePets(dt,now);RuntimeLog.Stage("snowballs");AnimateSnowToys(dt,now);RuntimeLog.Stage("snowmen");AnimateSnowmen(dt,now);UpdatePanel(now);if(santa&&schedule.IsDue(now))LaunchSanta();
  if(now>=statusAt){TrimAtlases(now);statusAt=now+5;int minutes=(int)Math.Ceiling(Math.Max(0,schedule.Next-now)/60);statusItem.Text=string.Format("Окон: {0} • Санта: ~{1} мин • {2} снежинок"+(storm?" • ВЬЮГА":""),visibleWindows,minutes,SnowCount);long delivered=clock.Delivered;double fps=lastStatusTime==0?0:(delivered-lastStatusFrames)/Math.Max(.01,now-lastStatusTime);statusItem.Text+=" • цель "+clock.TargetFps+" • сейчас "+fps.ToString("F0")+" FPS";lastStatusFrames=delivered;lastStatusTime=now;}
 }
 void AnimateFlakes(double dt,double now,Native.POINT cursor,List<Surface> surfaces){foreach(var rig in screens)rig.FallingCanvas.Clear();foreach(var f in flakes){var rig=screens[f.Screen];if(rig.FullScreen)continue;var area=rig.Screen.WorkingArea;double eddy=(Artwork.Noise((float)(now*.65+f.Y/(170*rig.Scale)),f.FlowSeed)-.5)*2;double wind=(storm?135:0)*rig.Scale+eddy*(storm?125:42)*rig.Scale+Math.Sin(now*1.7+f.Phase)*f.Drift;double oldBottom=f.Y+f.Height;f.FlowX+=(wind-f.FlowX)*Math.Min(1,dt*2.8);double fall=f.Speed*(1+.32*Math.Sin(now*1.65+f.Phase))-eddy*(storm?28:10)*rig.Scale;f.FlowY+=(fall-f.FlowY)*(1-Math.Exp(-dt*2.2));CursorSnow.Push(ref f.FlowX,ref f.FlowY,f.X+f.Size*.5,f.Y+f.Height*.5,cursor.X,cursor.Y,rig.Scale,dt);f.X+=f.FlowX*dt;f.Y+=f.FlowY*dt;
  if(f.Y<area.Top-f.Height*2){f.Y=area.Top-f.Height*2;f.FlowY=Math.Max(0,f.FlowY);}
  Surface landing=rig.Ground;float top=landing.BaseY-(banks?landing.Model.LevelAt((int)f.X-landing.Bounds.Left):0);
  if(banks)foreach(var surface in surfaces){if(surface.IsDesktop||!surface.Visible||f.X<surface.Bounds.Left||f.X>=surface.Bounds.Right)continue;float candidate;if(SnowLanding.Crosses(surface.Model,surface.Bounds,surface.BaseY,surface.Blockers,f.X,oldBottom,f.Y+f.Height,out candidate)&&candidate<top){landing=surface;top=candidate;}}
  if(f.FlowY>=0&&f.Y+f.Height>=top){if(banks)landing.Model.Deposit((int)f.X-landing.Bounds.Left,.12f*f.Count);f.Y=area.Top-f.Height;f.X=area.Left+random.NextDouble()*area.Width;f.FlowY=f.Speed;}
  if(f.X>area.Right)f.X=area.Left-f.Size;if(f.X<area.Left-f.Size)f.X=area.Right;
  rig.FallingCanvas.Blit(f.Image,(int)f.X-area.Left,(int)f.Y-area.Top);
 }foreach(var rig in screens){if(rig.FullScreen||!falling){rig.FallingLayer.Show(false);continue;}var area=rig.Screen.WorkingArea;rig.FallingLayer.Paint(rig.FallingCanvas,area.Left,area.Top);rig.FallingLayer.Show(true);}}

 void AnimateActors(double dt,double now,Native.POINT mouse){for(int i=actors.Count-1;i>=0;i--){var a=actors[i];bool done=false;int frame=0;Sprite image;
  if(a.Santa){if(a.Rig.FullScreen){a.Window.Show(false);continue;}var area=a.Rig.Screen.WorkingArea;frame=((int)(now*96))%96;image=a.Atlas.Santas[frame];a.Atlas.Santas.Request((frame+1)%96);a.Atlas.Santas.Request((frame+2)%96);if(image==null)continue;a.X+=Math.Max(100*a.Rig.Scale,(area.Width+image.Width)/19.0)*dt;double y=a.Y+Math.Sin(now*2)*3*a.Rig.Scale;done=a.X>area.Right+10;if(!done){if(frame!=a.Frame){a.Window.Paint(image,(int)a.X,(int)y);a.Frame=frame;}else a.Window.Move((int)a.X,(int)y);a.Window.Show(true);}}
  else{if((a.Missing&2)!=0){a.Window.Show(false);if(WorkerActivity.RetireAfterCapture(a.Missing,a.Holding!=null)){a.Dispose();actors.RemoveAt(i);}continue;}var s=a.Surface;if(!s.Visible||!banks){a.Window.Show(false);continue;}int size=a.Atlas.Workers[0].Width;float effective=a.Atlas.WorkerScale;double speed=Math.Max(38*effective,s.Model.Width/65.0);double intake=a.Direction>0?171*effective:size-171*effective,oldFront=a.X+intake;if(WorkerActivity.CanClean(a.Missing,a.Holding!=null)&&!s.Dragging){a.WorkTime+=dt;double gain=WorkerCycle.Moving(a.WorkTime)?1:0;a.Progress+=speed*dt*gain;a.X+=a.Direction*speed*dt*gain;}a.Y=s.BaseY-a.Atlas.Workers[0].Height+7*effective;double newFront=a.X+intake;if(WorkerActivity.CanClean(a.Missing,a.Holding!=null)&&!s.Dragging){s.Model.Sweep((int)(Math.Min(oldFront,newFront)-s.Bounds.Left-12*effective),(int)(Math.Max(oldFront,newFront)-s.Bounds.Left+12*effective));}
   if(s.Model.Dirty&&now-s.LastPaint>.2)s.LastPaint=now-1;
   Rectangle area=new Rectangle((int)a.X,(int)a.Y,size,a.Atlas.Workers[0].Height);frame=WorkerCycle.Frame(a.WorkTime,a.Progress,effective);image=a.Atlas.Worker(frame,a.Missing,a.Direction);if(image==null)continue;done=((a.Direction>0?a.X>s.Bounds.Right:a.X+size<s.Bounds.Left)&&a.Holding==null);if(!done){if(frame!=a.Frame){a.Window.Paint(image,(int)a.X,(int)a.Y);a.Frame=frame;}else a.Window.Move((int)a.X,(int)a.Y);a.Window.Clip(area,s.Blockers);a.Window.Show(true);}
  }if(done){a.Dispose();actors.RemoveAt(i);if(a.Santa)a.Atlas.ReleaseSanta();}}
 }
 double FloorAt(double x,double y){var near=Screen.FromPoint(new Point((int)x,(int)y));foreach(var r in screens)if(r.Screen.DeviceName==near.DeviceName)return r.Ground.BaseY-(banks?r.Ground.Model.LevelAt((int)x-r.Ground.Bounds.Left):0);return near.WorkingArea.Bottom;}
 void ConfigurePick(Actor a){a.Window.Hit=delegate(int x,int y){float scale=a.Atlas.WorkerScale;double local=a.Direction>0?x/scale:252-x/scale;return local>=12&&local<238&&y>30*scale&&y<205*scale&&(a.Missing&2)==0;};a.Window.SetInput(true);
  a.Window.MouseDown=delegate(int x,int y){if(disposed||pets.Count>=24||(a.Missing&2)!=0)return;float scale=a.Atlas.WorkerScale;var p=CreatePet(a.X+(a.Direction>0?104:148)*scale,a.Y+198*scale,scale/.64f);p.Motion.Direction=a.Direction;p.Origin=a;a.Surface.Model.UntilWorker=watch.Elapsed.TotalSeconds+20;a.Missing|=2;a.Holding=p;a.Frame=-1;p.Motion.Grab(a.Window.X+x,a.Window.Y+y);};
  a.Window.MouseDrag=delegate(int x,int y){if(a.Holding!=null)a.Holding.Motion.Drag(a.Window.X+x,a.Window.Y+y);};
  a.Window.MouseUp=delegate(int x,int y){if(a.Holding!=null){if(a.Holding.Motion.State==PetState.Held)a.Holding.Motion.Drop(ledges);a.Holding=null;}};
 }
 Pet CreatePet(double x,double y,float scale){var atlas=GetAtlas(scale);atlas.PreparePets();var p=new Pet{Atlas=atlas,Motion=new PetMotion(x,y,atlas.PetScale){Direction=random.Next(2)==0?-1:1}};p.Window.Hit=delegate(int xx,int yy){return xx>=0&&yy>=0&&xx<(int)(208*atlas.PetScale)&&yy<(int)(205*atlas.PetScale);};p.Window.SetInput(true);
  p.Window.MouseDown=delegate(int xx,int yy){if(!p.Disposed){p.Frame=-1;p.Motion.Grab(p.Window.X+xx,p.Window.Y+yy);}};p.Window.MouseDrag=delegate(int xx,int yy){if(!p.Disposed&&p.Motion.State==PetState.Held)p.Motion.Drag(p.Window.X+xx,p.Window.Y+yy);};p.Window.MouseUp=delegate(int xx,int yy){if(!p.Disposed&&p.Motion.State==PetState.Held)p.Motion.Drop(ledges);};pets.Add(p);return p;
 }
 internal void SpawnPet(){if(pets.Count>=24||screens.Count==0)return;Native.POINT cursor;Native.GetCursorPos(out cursor);var screen=Screen.FromPoint(new Point(cursor.X,cursor.Y));float scale=Scale(screen);var p=CreatePet(cursor.X,cursor.Y+100*scale,scale);p.Motion.State=PetState.Falling;p.Motion.FallStart=p.Motion.Y;}

 void AnimatePets(double dt,double now){for(int index=pets.Count-1;index>=0;index--){var p=pets[index];if(p.Disposed)continue;if(p.Motion.State==PetState.Held){Native.POINT cursor;Native.GetCursorPos(out cursor);if((Native.GetAsyncKeyState(1)&0x8000)!=0)p.Motion.Drag(cursor.X,cursor.Y);else{p.Motion.Drop(ledges);Native.ReleaseCapture();if(p.Origin!=null)p.Origin.Holding=null;}}if(screens.Exists(r=>r.FullScreen&&r.Screen.Bounds.Contains((int)p.Motion.X,(int)p.Motion.Y))){p.Window.Show(false);continue;}p.Motion.Tick(dt,ledges,xx=>FloorAt(xx,p.Motion.Y));if(p.Motion.State==PetState.Running&&(p.Motion.X>SystemInformation.VirtualScreen.Right+110*p.Motion.Scale||p.Motion.X<SystemInformation.VirtualScreen.Left-110*p.Motion.Scale)){p.Dispose();pets.RemoveAt(index);continue;}if(p.Motion.State!=PetState.Held&&p.Motion.State!=PetState.Running){var extent=SystemInformation.VirtualScreen;p.Motion.X=Math.Max(extent.Left+25*p.Motion.Scale,Math.Min(extent.Right-25*p.Motion.Scale,p.Motion.X));}int frame=PetFrame(p.Motion);Sprite sprite=p.Atlas.PetSprite(frame,p.Motion.Direction);if(sprite==null)continue;int x=(int)(p.Motion.X-sprite.Width/2),y=(int)(p.Motion.Y-sprite.Height+7*p.Motion.Scale);
   if(frame!=p.Frame){p.Window.Paint(sprite,x,y);p.Frame=frame;}else p.Window.Move(x,y);var cuts=new List<Rectangle>();if(p.Motion.Support!=IntPtr.Zero){var l=ledges.Find(a=>a.Handle==p.Motion.Support);if(l!=null)cuts=l.Blockers;}p.Window.Clip(new Rectangle(x,y,sprite.Width,sprite.Height),cuts);p.Window.Show(true);
  }}
 internal int PetCount {get{return pets.Count;}}internal long FramesDone {get{return clock.Delivered;}}internal bool HighResolutionClock {get{return clock.HighResolution;}}
 internal bool PetUsesSupport(IntPtr h){return pets.Exists(p=>p.Motion.Support==h);}
 protected override void Dispose(bool disposing){if(disposed)return;disposed=true;clock.Stop();clock.Dispose();if(updates!=null)updates.Dispose();if(panel!=null)panel.Dispose();if(quickKeys!=null)quickKeys.Dispose();ClearAvalanches();ClearSquirrel();ClearSnowmen();ClearSnowToys();SystemEvents.DisplaySettingsChanged-=DisplayChanged;SystemEvents.UserPreferenceChanged-=PreferencesChanged;SystemEvents.SessionSwitch-=SessionChanged;tracker.Dispose();tray.Visible=false;tray.Dispose();keys.Dispose();foreach(var p in pets)p.Dispose();foreach(var a in actors)a.Dispose();foreach(var f in flakes)f.Dispose();foreach(var s in flakeArt)s.Dispose();foreach(var s in screens)s.Dispose();foreach(var w in windows.Values)w.Dispose();foreach(var a in atlases.Values)a.Dispose();FramePreparation.Stop();DrawnArt.Release();WatercolorTheme.Release();dispatch.Dispose();base.Dispose(disposing);}
}
internal sealed class Hotkeys : NativeWindow,IDisposable {
 readonly Action pause,exit,santa,snow,storm;internal Hotkeys(Action p,Action e,Action s,Action n,Action w){pause=p;exit=e;santa=s;snow=n;storm=w;CreateHandle(new CreateParams{Caption="WinterDesk controls",Parent=new IntPtr(-3)});Native.RegisterHotKey(Handle,1,0x4007,0x53);Native.RegisterHotKey(Handle,2,0x4007,0x51);Native.RegisterHotKey(Handle,3,0x4007,0x52);Native.RegisterHotKey(Handle,4,0x4007,0x50);Native.RegisterHotKey(Handle,5,0x4007,0x57);}
 protected override void WndProc(ref Message m){if(m.Msg==0x312){switch(m.WParam.ToInt32()){case 1:pause();break;case 2:exit();break;case 3:santa();break;case 4:snow();break;case 5:storm();break;}return;}base.WndProc(ref m);}
 public void Dispose(){for(int i=1;i<=5;i++)Native.UnregisterHotKey(Handle,i);DestroyHandle();}
}
internal static class Program {
 [STAThread] static void Main(string[] args){if(UpdateInstaller.Handle(args))return;try{try{Native.SetProcessDpiAwarenessContext(new IntPtr(-4));}catch{Native.SetProcessDPIAware();}
  Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
  bool created;using(var mutex=new Mutex(true,"Local\\WinterDesk.AllWindows",out created)){if(!created)return;using(var app=new Controller()){
   UpdateInstaller.Acknowledge(args);Application.Run(app);}

  }
 }catch(Exception e){RuntimeLog.Error("fatal",e);Environment.ExitCode=1;try{File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"WinterDesk-error.txt"),e.ToString());}catch{}if(args.Length==0)MessageBox.Show(e.Message,"WinterDesk",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
}
}
