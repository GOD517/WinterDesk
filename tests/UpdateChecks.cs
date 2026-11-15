using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
namespace WinterDesk {
internal static class UpdateChecks {
 static void Check(bool value,string message){if(!value)throw new Exception(message);}
 static byte[] Sign(string payload,RSACryptoServiceProvider rsa){return Encoding.UTF8.GetBytes(payload+Convert.ToBase64String(rsa.SignData(Encoding.UTF8.GetBytes(payload),"SHA256"))+"\n");}
 static void Reject(Action action,string message){bool failed=false;try{action();}catch{failed=true;}Check(failed,message);}
 static void Main(string[] args){
  using(var rsa=new RSACryptoServiceProvider()){
   rsa.PersistKeyInCsp=false;rsa.FromXmlString(File.ReadAllText(args[0]));
   string exe=args[1],hash;using(var sha=SHA256.Create())using(var stream=File.OpenRead(exe))hash=BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
   string payload="2.4.0.0\n"+UpdateTrust.Repository+"/releases/download/v2.4.0/WinterDesk.exe\n"+new FileInfo(exe).Length+"\n"+hash+"\n";
   var signed=Sign(payload,rsa);var release=UpdateTrust.Parse(signed);Check(release.Version==new Version(2,4,0,0),"signed version");UpdateTrust.VerifyFile(exe,release);
   var tampered=(byte[])signed.Clone();tampered[0]=(byte)'3';Reject(()=>UpdateTrust.Parse(tampered),"tampered signed metadata rejected");
   Reject(()=>UpdateTrust.Parse(Sign(payload.Replace("https://github.com/GOD517/WinterDesk","https://example.com/evil"),rsa)),"foreign release origin rejected");
   Reject(()=>UpdateTrust.Parse(Sign(payload.Replace("/v2.4.0/","/v2.5.0/"),rsa)),"mismatched URL version rejected");
   Reject(()=>UpdateTrust.Parse(Sign(payload.Replace(new FileInfo(exe).Length.ToString(),"999999999999"),rsa)),"oversized release rejected");
   Reject(()=>UpdateTrust.Parse(new byte[17000]),"oversized manifest rejected");
   string copy=Path.Combine(Path.GetDirectoryName(args[0]),"tampered.exe");File.Copy(exe,copy,true);using(var stream=File.OpenWrite(copy)){stream.Seek(-1,SeekOrigin.End);stream.WriteByte(0x7f);}Reject(()=>UpdateTrust.VerifyFile(copy,release),"tampered EXE rejected");
   release.Size++;Reject(()=>UpdateTrust.VerifyFile(exe,release),"wrong size rejected");release.Size--;release.Version=new Version(9,0,0,0);Reject(()=>UpdateTrust.VerifyFile(exe,release),"wrong assembly version rejected");
   Console.WriteLine("PASS: signed metadata, native EXE integrity, corrupted metadata/EXE, foreign URLs, version mismatch and size limits.");
  }
 }
}
}
