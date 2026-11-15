using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
namespace WinterDesk {
internal static class DrawnArt {

 internal static readonly object Sync=new object();
 static readonly Dictionary<string,Bitmap> frames=new Dictionary<string,Bitmap>();static readonly LinkedList<string> recent=new LinkedList<string>();static Dictionary<string,string> aliases;static ZipArchive archive;static Stream stream;static long sourceBytes;
 static void Open(){if(archive!=null)return;stream=typeof(DrawnArt).Assembly.GetManifestResourceStream("WinterDesk.DrawnPack");archive=new ZipArchive(stream,ZipArchiveMode.Read);aliases=new Dictionary<string,string>();using(var reader=new StreamReader(archive.GetEntry("frames.txt").Open())){string line;while((line=reader.ReadLine())!=null){int at=line.IndexOf('=');aliases.Add(line.Substring(0,at),line.Substring(at+1));}}}
 internal static string Identity(string group,int n){lock(Sync){Open();return aliases[group+"/"+n+".png"];}}
 static Bitmap Frame(string group,int n){string key=Identity(group,n);Bitmap image;if(frames.TryGetValue(key,out image)){recent.Remove(key);recent.AddLast(key);return image;}using(var source=archive.GetEntry(key).Open())using(var raw=new Bitmap(source))image=raw.Clone(new Rectangle(0,0,raw.Width,raw.Height),PixelFormat.Format32bppPArgb);frames.Add(key,image);recent.AddLast(key);sourceBytes+=(long)image.Width*image.Height*4;TrimSources(key);return image;}
 static void TrimSources(string keep){int attempts=recent.Count;while(sourceBytes>33554432&&recent.Count>1&&attempts-->0){string key=recent.First.Value;recent.RemoveFirst();if(key==keep||Pinned(key)){recent.AddLast(key);continue;}var b=frames[key];sourceBytes-=(long)b.Width*b.Height*4;frames.Remove(key);b.Dispose();}}
 static bool Pinned(string key){if(aliases==null)return false;if(key==aliases["snowBank/0.png"])return true;for(int i=0;i<8;i++)if(key==aliases["snowBall/"+i+".png"])return true;return false;}
 internal static string PetIdentity(int index){int action=index/32,n=index%32;if(action<=1)return Identity("flap",n*2%64);if(action==2)return Identity("recover",n/2);if(action==3)return Identity("recover",16+n);if(action==4)return Identity("recover",48+n*47/31);return Identity("walk",0);}
 static void Setup(Graphics g){g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;}
 // Penguin actions use intact authored cels; no optical-flow distortion of face or limbs.
 // No grafted wings, rotated standing cutouts, or shear transforms are used for character actions.
 static Bitmap Character(string group,int frame,int width,int height,float scale,int direction=1){lock(Sync){return CharacterImage(Frame(group,frame),width,height,scale,direction);}}
 static Bitmap CharacterImage(Bitmap image,int width,int height,float scale,int direction=1){var b=Artwork.Canvas((int)(width*scale),(int)(height*scale));using(var g=Graphics.FromImage(b)){Setup(g);g.DrawImage(image,new Rectangle(0,0,b.Width,b.Height));}if(direction<0)b.RotateFlip(RotateFlipType.RotateNoneFlipX);return b;}
 internal static Bitmap Worker(int frame,float scale,int omit=0){if((omit&2)!=0)return Artwork.Canvas((int)(252*scale),(int)(205*scale));return Character("shovel",frame%256,252,205,scale);}
 internal static Bitmap BankTexture(){lock(Sync){return Frame("snowBank",0);}}
 internal static Bitmap BallTexture(int variant){lock(Sync){return Frame("snowBall",((variant%8)+8)%8);}}
 internal static Bitmap Runner(int frame,float scale){return Character("walk",frame%128,208,205,scale);}
 internal static Bitmap Pet(int index,float scale){int action=index/32,n=index%32;string group;int frame;if(action<=1){group="flap";frame=(n*2)%64;}else if(action==2){group="recover";frame=n/2;}else if(action==3){group="recover";frame=16+n;}else if(action==4){group="recover";frame=48+n*47/31;}else{group="walk";frame=0;}return Character(group,frame,208,205,scale);}
 internal static Bitmap Slide(int frame,float scale){return Character("flap",(frame*2)%64,208,205,scale);}
 internal static Bitmap Squirrel(int frame,float scale,int direction=1){return frame>=136?Character("squirrelMount",Math.Min(7,frame-136),220,320,scale,direction):frame>=128?Character("squirrelClimb",(frame-128)%8,220,320,scale,direction):Character("squirrel",frame%128,220,170,scale,direction);}
 internal static unsafe void TintScarf(Bitmap bitmap,int scarf){Color[] palette={Color.FromArgb(165,51,55),Color.FromArgb(65,130,169),Color.FromArgb(76,137,97),Color.FromArgb(147,91,172),Color.FromArgb(195,118,46),Color.FromArgb(55,152,151)};var tint=palette[scarf%6];var data=bitmap.LockBits(new Rectangle(0,0,bitmap.Width,bitmap.Height),ImageLockMode.ReadWrite,PixelFormat.Format32bppPArgb);try{for(int y=0;y<bitmap.Height;y++){byte* row=(byte*)data.Scan0+y*data.Stride;for(int x=0;x<bitmap.Width;x++){byte* p=row+x*4;int a=p[3],blue=p[0],green=p[1],red=p[2];if(a<15||red*255<55*a||red-green<15*a/255.0||red<blue*1.4||red<green*1.45||green>blue*1.65||blue*255>150*a)continue;double lum=(red*.55+green*.32+blue*.13)/a,k=.45+lum*.95;p[0]=(byte)Math.Min(a,tint.B*k*a/255);p[1]=(byte)Math.Min(a,tint.G*k*a/255);p[2]=(byte)Math.Min(a,tint.R*k*a/255);}}}finally{bitmap.UnlockBits(data);}}
 static void SnowmanSource(int pose,out string group,out int frame){group="snowIdle";frame=0;
  if(pose>=464){group="snowLand";frame=Math.Min(15,pose-464);}else if(pose>=400){group="snowWalk";frame=(pose-400)%64;}else if(pose>=364){group="snowPlace";frame=(pose-364)%32;}else if(pose>=300){group="snowCarry";frame=(pose-300)%64;}else if(pose>=200){group="snowRoll";frame=(pose-200)%64;}else if(pose>=100){group="snowAir";frame=(pose-100)%64;}else if(pose<8){group="snowWalk";frame=(pose%4)*16;}else if(pose>=16&&pose<20){group="snowRoll";frame=(pose-16)*16;}else if(pose>=20){group="snowCarry";frame=((pose-20)*12)%64;}else if(pose==12){group="snowAir";}else if(pose==14){group="snowPlace";frame=28;}
 }
 internal static string SnowmanIdentity(int pose){string group;int frame;SnowmanSource(pose,out group,out frame);return Identity(group,frame);}
 internal static Bitmap Snowman(int pose,float scale,int scarf=0,int direction=1){string group="snowIdle";int frame=0;
  SnowmanSource(pose,out group,out frame);
  lock(Sync){using(var image=(Bitmap)Frame(group,frame).Clone()){if(scarf%6!=0)TintScarf(image,scarf);return CharacterImage(image,280,205,scale,direction);}}
 }
 // One prepainted whole-caravan image per exposure, including every harness connection.
 internal static Bitmap Santa(int frame,float scale){return Character("santaTeam",frame%96,1060,275,scale);}
 internal static void Release(){lock(Sync){foreach(var b in frames.Values)b.Dispose();frames.Clear();recent.Clear();sourceBytes=0;if(archive!=null)archive.Dispose();archive=null;if(stream!=null)stream.Dispose();stream=null;aliases=null;}}
}
}
