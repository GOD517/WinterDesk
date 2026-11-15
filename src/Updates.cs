using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinterDesk {
internal sealed class ReleaseInfo {
 internal Version Version; internal string Url, Hash; internal long Size;
}

internal static class UpdateTrust {
 internal const string Repository = "https://github.com/GOD517/WinterDesk";
 internal const string Feed = Repository + "/releases/latest/download/update.txt";
 internal static Version Current { get { return Assembly.GetExecutingAssembly().GetName().Version; } }
 internal static string VersionText { get { return Current.ToString(3); } }
 // The signature covers these exact four UTF-8 lines, including the final newline.
 internal static ReleaseInfo Parse(byte[] manifest) {
  if(manifest.Length>16384)throw new InvalidDataException("Слишком большой файл обновления.");
  string[] lines=new UTF8Encoding(false,true).GetString(manifest).Replace("\r\n","\n").TrimEnd('\n').Split('\n');
  if(lines.Length!=5)throw new InvalidDataException("Неверный формат обновления.");
  byte[] payload=Encoding.UTF8.GetBytes(string.Join("\n",lines,0,4)+"\n");
  string key;using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("WinterDesk.UpdatePublicKey"))using(var reader=new StreamReader(stream))key=reader.ReadToEnd();
  using(var rsa=new RSACryptoServiceProvider()){rsa.PersistKeyInCsp=false;rsa.FromXmlString(key);if(!rsa.VerifyData(payload,"SHA256",Convert.FromBase64String(lines[4])))throw new InvalidDataException("Подпись обновления не подтверждена.");}
  Version version;long size;
  if(!Version.TryParse(lines[0],out version)||version.Revision<0||!long.TryParse(lines[2],NumberStyles.None,CultureInfo.InvariantCulture,out size)||size<1024||size>200*1024*1024)throw new InvalidDataException("Неверные сведения о выпуске.");
  string expected=Repository+"/releases/download/v"+version.ToString(3)+"/WinterDesk.exe";
  if(lines[1]!=expected||lines[3].Length!=64)throw new InvalidDataException("Неверный адрес выпуска.");
  foreach(char c in lines[3])if(!Uri.IsHexDigit(c))throw new InvalidDataException("Неверная контрольная сумма.");
  return new ReleaseInfo{Version=version,Url=lines[1],Size=size,Hash=lines[3]};
 }
 internal static void VerifyFile(string path,ReleaseInfo release) {
  if(new FileInfo(path).Length!=release.Size)throw new InvalidDataException("Размер обновления не совпал.");
  using(var input=File.OpenRead(path))using(var sha=SHA256.Create())if(BitConverter.ToString(sha.ComputeHash(input)).Replace("-","").ToLowerInvariant()!=release.Hash.ToLowerInvariant())throw new InvalidDataException("Контрольная сумма обновления не совпала.");
  if(AssemblyName.GetAssemblyName(path).Version!=release.Version)throw new InvalidDataException("Версия файла не совпала.");
 }
 internal static byte[] Fetch(string url,int maximum) {
  using(var data=new MemoryStream()){Download(url,data,maximum,null);return data.ToArray();}
 }
 internal static void Download(string url,Stream output,long maximum,Action<long> progress) {
  ServicePointManager.SecurityProtocol|=(SecurityProtocolType)3072;
  var request=(HttpWebRequest)WebRequest.Create(url);request.UserAgent="WinterDesk/"+VersionText;request.Timeout=15000;request.ReadWriteTimeout=30000;
  using(var response=(HttpWebResponse)request.GetResponse()){
   if(response.ResponseUri.Scheme!="https"||response.ContentLength>maximum)throw new InvalidDataException("Недопустимый ответ сервера.");
   using(var input=response.GetResponseStream()){var buffer=new byte[65536];long total=0;int n;while((n=input.Read(buffer,0,buffer.Length))>0){total+=n;if(total>maximum)throw new InvalidDataException("Файл превышает допустимый размер.");output.Write(buffer,0,n);if(progress!=null)progress(total);}}
  }
 }
 internal static string Quote(string value){if(value.IndexOf('"')>=0)throw new ArgumentException("Invalid path");return "\""+value+"\"";}
}

internal sealed class UpdateClient : IDisposable {
 readonly Control ui;readonly Action exit;readonly System.Windows.Forms.Timer timer;bool disposed,busy;ReleaseInfo release;byte[] manifest;
 internal string Status="Проверяем версию…";internal bool Available;internal event Action Changed;
 internal bool Busy {get{return busy;}}
 internal UpdateClient(Control dispatch,Action close){ui=dispatch;exit=close;timer=new System.Windows.Forms.Timer{Interval=6000};timer.Tick+=delegate{timer.Interval=6*60*60*1000;Check(false);};timer.Start();}
 void Notify(){if(!disposed){var action=Changed;if(action!=null)action();}}
 void OnUI(Action action){if(disposed||ui.IsDisposed)return;try{ui.BeginInvoke((Action)delegate{if(!disposed)action();});}catch(InvalidOperationException){}}
 internal void Check(bool manual){
  if(disposed||busy)return;busy=true;Status="Проверяем версию…";Notify();
  Task.Factory.StartNew(delegate{
   try{byte[] bytes=UpdateTrust.Fetch(UpdateTrust.Feed,16384);var info=UpdateTrust.Parse(bytes);OnUI(delegate{manifest=bytes;release=info;Available=info.Version>UpdateTrust.Current;busy=false;Status=Available?"Доступно обновление "+info.Version.ToString(3):"Установлена последняя версия";Notify();});}
   catch(Exception e){RuntimeLog.Error("update check",e);OnUI(delegate{busy=false;Status=Available?"Доступно обновление "+release.Version.ToString(3):"Нет связи · нажмите «Обновить»";Notify();if(manual)MessageBox.Show("Не удалось проверить обновления. Проверьте подключение к интернету и попробуйте ещё раз.","WinterDesk",MessageBoxButtons.OK,MessageBoxIcon.Information);});}
  });
 }
 internal void Click(){if(busy)return;if(!Available){Check(true);return;}Install();}
 void Install(){
  string target=Application.ExecutablePath,folder=Path.GetDirectoryName(target);
  try{string probe=Path.Combine(folder,".winterdesk-write-"+Guid.NewGuid().ToString("N"));using(File.Create(probe)){}File.Delete(probe);}
  catch{MessageBox.Show("Нет права записи в папку программы. Перенесите WinterDesk в свою папку и повторите обновление.","WinterDesk",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
  busy=true;Status="Загрузка обновления…";Notify();ReleaseInfo info=release;byte[] signed=manifest;
  Task.Factory.StartNew(delegate{
   string stage=null;
   try{
    stage=Path.Combine(RuntimeLog.DirectoryPath,"Updates",Guid.NewGuid().ToString("N"));Directory.CreateDirectory(stage);string file=Path.Combine(stage,"WinterDesk.exe");File.WriteAllBytes(Path.Combine(stage,"update.txt"),signed);
    int previous=-1;using(var output=File.Create(file))UpdateTrust.Download(info.Url,output,info.Size,delegate(long count){if(disposed)throw new OperationCanceledException();int percent=(int)(count*100/info.Size);if(percent!=previous){previous=percent;OnUI(delegate{Status="Загрузка · "+percent+"%";Notify();});}});
    UpdateTrust.VerifyFile(file,info);string helper=Path.Combine(stage,"WinterDesk-updater.exe");File.Copy(target,helper);
    string arguments="--apply-update "+UpdateTrust.Quote(target)+" "+Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture)+" "+UpdateTrust.Quote(stage);
    string stagePath=stage;
    OnUI(delegate{try{Status="Устанавливаем обновление…";Notify();Process.Start(new ProcessStartInfo(helper,arguments){UseShellExecute=false,WorkingDirectory=stagePath});exit();}catch(Exception e){busy=false;Status="Не удалось запустить установку";RuntimeLog.Error("update helper",e);Notify();MessageBox.Show("Установка не запустилась. Текущая версия сохранена.","WinterDesk");}});
   }catch(Exception e){RuntimeLog.Error("update download",e);OnUI(delegate{busy=false;Status="Ошибка загрузки · повторить";Notify();MessageBox.Show("Обновление не установлено. Текущая версия сохранена. Попробуйте ещё раз.","WinterDesk",MessageBoxButtons.OK,MessageBoxIcon.Information);});}
  });
 }
 public void Dispose(){disposed=true;timer.Stop();timer.Dispose();}
}

internal static class UpdateInstaller {
 internal static bool Handle(string[] args){
  if(args.Length==0||args[0]!="--apply-update")return false;
  string target=null,backup=null;bool replaced=false;Process child=null;
  try{
   if(args.Length!=4)throw new ArgumentException("Invalid installer arguments");
   target=Path.GetFullPath(args[1]);int parent=int.Parse(args[2],CultureInfo.InvariantCulture);string stage=Path.GetFullPath(args[3]);
   if(!string.Equals(Path.GetDirectoryName(Application.ExecutablePath),stage,StringComparison.OrdinalIgnoreCase)||string.Equals(target,Application.ExecutablePath,StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Invalid installer location");
   var info=UpdateTrust.Parse(File.ReadAllBytes(Path.Combine(stage,"update.txt")));string incoming=Path.Combine(stage,"WinterDesk.exe");UpdateTrust.VerifyFile(incoming,info);
   if(AssemblyName.GetAssemblyName(target).Name!="WinterDesk")throw new InvalidDataException("Invalid installation target");
   try{using(var old=Process.GetProcessById(parent)){if(!old.WaitForExit(20000))throw new IOException("Программа не закрылась.");}}catch(ArgumentException){}
   Version installed=AssemblyName.GetAssemblyName(target).Version;if(info.Version<=installed)throw new InvalidDataException("Обновление должно быть новее установленной версии.");
   backup=target+".previous-"+Guid.NewGuid().ToString("N")+".bak";File.Replace(incoming,target,backup);replaced=true;
   string ready=Path.Combine(stage,"ready");child=Process.Start(new ProcessStartInfo(target,"--update-ready "+UpdateTrust.Quote(ready)){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(target)});
   bool acknowledged=false;var watch=Stopwatch.StartNew();while(watch.Elapsed.TotalSeconds<35){if(child.HasExited)break;if(File.Exists(ready)){acknowledged=true;break;}Thread.Sleep(200);}
   if(!acknowledged)throw new IOException("Новая версия не подтвердила запуск.");
   RuntimeLog.Write("update installed "+info.Version+"; backup retained");
  }catch(Exception e){
   RuntimeLog.Error("update install",e);
   if(replaced){try{if(child!=null&&!child.HasExited){child.Kill();child.WaitForExit(5000);}File.Replace(backup,target,target+".failed-"+Guid.NewGuid().ToString("N"));Process.Start(new ProcessStartInfo(target){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(target)});}catch(Exception rollback){RuntimeLog.Error("update rollback",rollback);}}
   else if(target!=null&&File.Exists(target)){try{Process.Start(new ProcessStartInfo(target){UseShellExecute=false,WorkingDirectory=Path.GetDirectoryName(target)});}catch{}}
   MessageBox.Show("Обновление не завершено. Предыдущая версия сохранена.\n\n"+e.Message,"WinterDesk",MessageBoxButtons.OK,MessageBoxIcon.Information);Environment.ExitCode=1;
  }finally{if(child!=null)child.Dispose();}return true;
 }
 internal static void Acknowledge(string[] args){
  if(args.Length!=2||args[0]!="--update-ready")return;
  string path=Path.GetFullPath(args[1]);string allowed=Path.GetFullPath(Path.Combine(RuntimeLog.DirectoryPath,"Updates"))+Path.DirectorySeparatorChar;
  if(!path.StartsWith(allowed,StringComparison.OrdinalIgnoreCase)||Path.GetFileName(path)!="ready")return;
  var timer=new System.Windows.Forms.Timer{Interval=5000};timer.Tick+=delegate{timer.Stop();timer.Dispose();try{File.WriteAllText(path,UpdateTrust.VersionText);}catch(Exception e){RuntimeLog.Error("update acknowledge",e);}};timer.Start();
 }
}
}
