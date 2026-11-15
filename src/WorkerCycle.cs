using System;
namespace WinterDesk {
internal static class WorkerCycle {
 internal const double WalkSeconds=2.8,ThrowSeconds=1.25;
 internal static bool Moving(double time){return time%(WalkSeconds+ThrowSeconds)<WalkSeconds;}
 internal static int Frame(double time,double distance,float scale){double age=time%(WalkSeconds+ThrowSeconds);return age<WalkSeconds?(int)(distance/(64*scale)*128)%128:128+Math.Min(127,(int)((age-WalkSeconds)/ThrowSeconds*128));}
}
internal static class PenguinGait {
 // The painted sheet contains two step cycles, so one complete sheet spans four footfalls.
 internal static double Phase(double distance,float scale){double p=distance/(180*Math.Max(.1f,scale));return p-Math.Floor(p);}
}
}
