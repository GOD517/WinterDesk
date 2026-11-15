using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading;
namespace WinterDesk {
internal static class RuntimeLog {
 static readonly Stopwatch time=Stopwatch.StartNew();static readonly ConcurrentQueue<string> lines=new ConcurrentQueue<string>();static int writing;static double nextReport;static long count,slow;static double sum,max,stageAt,stageMax;static string stage,slowestStage;
 internal static double Seconds {get{return time.Elapsed.TotalSeconds;}}
 internal static string DirectoryPath {get{return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"WinterDesk");}}
 internal static void Error(string where,Exception error){Write(where+": "+error);}
 internal static void Stage(string name){double now=Seconds;if(stage!=null&&(now-stageAt)*1000>stageMax){stageMax=(now-stageAt)*1000;slowestStage=stage;}stage=name;stageAt=now;}
 internal static void Frame(double ms){Stage(null);count++;sum+=ms;max=Math.Max(max,ms);if(ms>50)slow++;if(Seconds<nextReport)return;nextReport=Seconds+10;Write(string.Format("frames={0} average_ms={1:F2} max_ms={2:F2} over50ms={3} managed_MB={4:F1} slowest_stage={5} stage_ms={6:F2}",count,sum/Math.Max(1,count),max,slow,GC.GetTotalMemory(false)/1048576.0,slowestStage,stageMax));count=slow=0;sum=max=stageMax=0;}
 internal static void Write(string message){if(lines.Count>128)return;lines.Enqueue(DateTime.UtcNow.ToString("o")+" "+message);if(Interlocked.CompareExchange(ref writing,1,0)==0)ThreadPool.QueueUserWorkItem(_=>Flush());}
 static void Flush(){try{Directory.CreateDirectory(DirectoryPath);string file=Path.Combine(DirectoryPath,"runtime.log");if(File.Exists(file)&&new FileInfo(file).Length>1048576){string old=file+".1";if(File.Exists(old))File.Delete(old);File.Move(file,old);}using(var writer=new StreamWriter(file,true)){string line;while(lines.TryDequeue(out line))writer.WriteLine(line);}}catch{}finally{Interlocked.Exchange(ref writing,0);}}
}
}
