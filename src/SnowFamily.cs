namespace WinterDesk {
internal static class SnowFamily {
 internal static int NextScarf(int parent,int variation,int partner=-1){int color=(parent+1+(variation%5+5)%5)%6;while(color==parent||color==partner)color=(color+1)%6;return color;}
}
}
