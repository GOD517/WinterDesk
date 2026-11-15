using System.Collections.Generic;
using System.Drawing;
namespace WinterDesk {
internal static class SnowLanding {
 internal static bool Crosses(SnowModel model,Rectangle bounds,int baseY,IList<Rectangle> blockers,double x,double oldBottom,double bottom,out float top){top=baseY-model.LevelAt((int)x-bounds.Left);if(x<bounds.Left||x>=bounds.Right||oldBottom>top||bottom<top)return false;foreach(var block in blockers)if(block.Contains((int)x,baseY))return false;return true;}
}
}
