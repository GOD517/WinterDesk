using System;
using System.Windows.Forms;
namespace WinterDesk {
// Explicit play mode registers only S/P/Escape. No keyboard hook or keystroke recording.
internal interface IQuickKeys {bool Enabled {get;}string Status {get;}void SetEnabled(bool enabled);}
internal sealed class QuickKeys:NativeWindow,IDisposable,IQuickKeys {
 readonly Action snowman,penguin,togglePanel;public bool Enabled {get;private set;}public string Status {get;private set;}internal Action Changed;
 internal QuickKeys(Action s,Action p,Action panel){Status="S/P выключены — можно печатать";snowman=s;penguin=p;togglePanel=panel;CreateHandle(new CreateParams{Caption="WinterDesk quick keys",Parent=new IntPtr(-3)});Native.RegisterHotKey(Handle,20,0x4007,0x48);Native.RegisterHotKey(Handle,21,0x4007,0x4B);Native.RegisterHotKey(Handle,22,0x4003,0x53);Native.RegisterHotKey(Handle,23,0x4003,0x50);}
 public void SetEnabled(bool enabled){foreach(int id in new[]{24,25,26})Native.UnregisterHotKey(Handle,id);Enabled=false;if(!enabled){Status="S/P выключены — можно печатать";if(Changed!=null)Changed();return;}bool s=Native.RegisterHotKey(Handle,24,0,0x53),p=Native.RegisterHotKey(Handle,25,0,0x50),escape=Native.RegisterHotKey(Handle,26,0x4000,0x1B);if(!s||!p||!escape){foreach(int id in new[]{24,25,26})Native.UnregisterHotKey(Handle,id);Status="S/P/Esc заняты: доступны Ctrl+Alt+S и Ctrl+Alt+P";if(Changed!=null)Changed();return;}Enabled=true;Status="ИГРОВОЙ РЕЖИМ: S / P • Esc выключает";if(Changed!=null)Changed();}
 protected override void WndProc(ref Message m){if(m.Msg==0x312){switch(m.WParam.ToInt32()){case 20:togglePanel();break;case 21:SetEnabled(!Enabled);break;case 22:case 24:snowman();break;case 23:case 25:penguin();break;case 26:SetEnabled(false);break;}return;}base.WndProc(ref m);}
 public void Dispose(){for(int id=20;id<=26;id++)Native.UnregisterHotKey(Handle,id);DestroyHandle();}
}
}
