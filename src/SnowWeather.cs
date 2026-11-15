using System;
namespace WinterDesk {internal static class SnowWeather {internal static int Size(double diameter,float scale,bool storm){return Math.Max(2,(int)(diameter*scale*.5));}}}
