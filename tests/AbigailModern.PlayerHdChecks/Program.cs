using AbigailModern;
using Microsoft.Xna.Framework;
void Check(bool value, string label) { if (!value) throw new Exception(label); Console.WriteLine("PASS: " + label); }
var native = new[] { new Color(12, 44, 220, 255), new Color(34, 17, 90, 0), new Color(220, 90, 7, 128), new Color(250, 2, 128, 255) };
var shade = Enumerable.Repeat(new Color(128,128,128,255),16).ToArray(); var output = new Color[16];
PlayerHdRenderer.ApplyShade(native,2,2,shade,output);
Check(Enumerable.Range(0,16).All(i => output[i] == native[(i/4/2)*2+(i%4/2)]),"neutral shade exactly preserves native RGBA including hidden RGB and partial alpha");
shade[0] = new Color(148,148,148); shade[15] = new Color(108,108,108);
PlayerHdRenderer.ApplyShade(native,2,2,shade,output);
Check(output[0] == new Color(32,64,240,255) && output[15] == new Color(230,0,108,255),"positive/negative subpixel detail clamps channels correctly");
Check(output[1] == native[0],"new detail can differ within one original pixel");
native[0] = new Color(60,100,140,255); PlayerHdRenderer.ApplyShade(native,2,2,shade,output);
Check(output[0] == new Color(80,120,160,255),"native dye changes propagate without changing authored detail");
shade[0] = new Color(1,2,3); bool rejected=false;
try { PlayerHdRenderer.ApplyShade(native,2,2,shade,output); } catch (InvalidDataException) { rejected=true; }
Check(rejected,"colored shade masks rejected");
Console.WriteLine("5 player HD CPU checks passed; no game or graphics device created.");
foreach(var portrait in new[] {new Rectangle(100,100,128,192),new Rectangle(720,240,128,192)})
{
    var button=PlayerCreatorPreview.ButtonBounds(portrait);
    Check(portrait.Contains(button) && button.Bottom <= portrait.Top+32 && button.Width>=24 && button.Height>=24,"preview target fits portrait header clear of composed farmer");
}
foreach(var size in new[] {new Point(1280,720),new Point(1024,576),new Point(854,480),new Point(640,360)})
{
    var layout=PlayerCreatorPreview.Layout(size.X,size.Y);
    Check(new Rectangle(0,0,size.X,size.Y).Contains(layout.Panel) && layout.Panel.Contains(layout.Picture) && layout.Panel.Contains(layout.Close) && layout.Panel.Contains(layout.Left) && layout.Panel.Contains(layout.Right),"preview and controls fit "+size);
    Check(layout.Picture.Bottom<layout.Left.Top && !layout.Left.Intersects(layout.Right) && !layout.Right.Intersects(layout.Close) && layout.Picture.Width*3==layout.Picture.Height*2,"preview aspect and control separation preserved "+size);
}
