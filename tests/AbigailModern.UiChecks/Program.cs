using System.Reflection;
using Microsoft.Xna.Framework;
var type = Assembly.GetExecutingAssembly().GetType("AbigailModern.BlueUiText");
void Check(bool ok, string message) { if (!ok) throw new Exception("FAIL: " + message); Console.WriteLine("PASS: " + message); }
Check(type != null, "UI text policy exists");
object? Call(string name, params object?[] args) => type!.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)!.Invoke(null, args);
Color Ink(Color c) => (Color)Call("MapInk", c)!;
var dark = new Color(34,17,34);
Check(Ink(dark) == dark, "world text remains unchanged outside menu scope");
object?[] outer = {0}; Call("EnterScope", outer);
var light = Ink(dark);
Check(light.R > 200 && light.G > 200 && light.B > 200, "native dark menu ink becomes light");
Check(Ink(Color.White) == Color.White, "already light ink remains unchanged");
var red = Ink(Color.DarkRed); Check(red.R > red.G && red.R > red.B && red.G > 70, "warning red remains red and gains contrast");
var green = Ink(Color.DarkGreen); Check(green.G > green.R && green.G > green.B, "success green retains meaning");
var faded = Ink(dark * .5f); Check(faded.A == 127 && faded.R <= 127 && faded.R > 90, "premultiplied opacity is retained");
Check(Ink(Color.Transparent) == Color.Transparent, "transparent ink stays transparent");
object?[] inner = {0}; Call("EnterScope", inner);
Call("ExitScope", inner[0], new InvalidOperationException("fixture"));
Check(Ink(dark).R > 200, "nested exception restores enclosing menu scope");
Call("ExitScope", outer[0], null);
Check(Ink(dark) == dark, "outer exception-safe cleanup restores world ink");
Call("EnterScope", outer);
Check(Ink(Color.Blue).R > 70 && Ink(Color.Blue).B > Ink(Color.Blue).R, "saturated blue gains contrast without losing hue");
Check(Ink(Color.Gray).R < Ink(dark).R && Ink(Color.Gray).R > 140, "disabled gray stays visibly muted");
object?[] shadow = { dark, 0 }; Call("ShadowInk", shadow);
Check(((Color)shadow[0]!).R > 200, "shadow helper foreground becomes light");
object?[] black = { Color.Black }; Call("DrawStringInk", black);
Check((Color)black[0]! == Color.Black, "shadow helper shadow ink stays dark");
Call("ExitShadow", shadow[1], new Exception("fixture"));
object?[] bitmap = { null, false, -1, 0 }; Call("BitmapInk", bitmap);
Check(bitmap[0] is Color bitmapColor && bitmapColor.R > 200, "bitmap glyphs select the tintable native palette");
Call("ExitScope", bitmap[3], null);
Call("ExitScope", outer[0], null);
Call("EnterScope", outer);
var brown = Ink(new Color(86,22,12));
Check(brown.R > 200 && brown.G > 200 && brown.B >= brown.R, "native brown body text uses pale neutral ink");
Call("ExitScope", outer[0], null);
Check(type!.GetMethod("PortraitDrawMethods", BindingFlags.NonPublic | BindingFlags.Static) != null, "custom portrait text scope discovery exists");
var production = Assembly.LoadFrom(Path.GetFullPath("src/AbigailModern/bin/Debug/net6.0/AbigailModern.dll"));
var portraitMethods = ((IEnumerable<MethodInfo>)Call("PortraitDrawMethods", production)!).ToArray();
Check(new[]{"PortraitPanel", "ChildPortraits", "TrashBearPortrait"}.All(name => portraitMethods.Any(m => m.DeclaringType!.Name == name)), "all existing portrait name-label helpers receive scopes");
Check(portraitMethods.All(m => m.Name == "Draw" && m.DeclaringType!.Name != "StormHail"), "portrait scope discovery excludes world drawing");
Check(typeof(StardewValley.Menus.IClickableMenu).IsAssignableFrom(typeof(StardewValley.Menus.DayTimeMoneyBox)), "native clock and money HUD is covered by menu inheritance");
var defaultShadow = new Color(206,156,95);
object?[] outsideShadow = { defaultShadow }; Call("DrawStringInk", outsideShadow);
Check((Color)outsideShadow[0]! == defaultShadow, "native shadow outside menu remains unchanged");
Call("EnterScope", outer);
Check(type!.GetMethod("DefaultShadowInk", BindingFlags.NonPublic | BindingFlags.Static) != null, "default shadow helper has scoped navy handling");
object?[] defaultHelper = { dark, 0 }; Call("DefaultShadowInk", defaultHelper);
object?[] menuShadow = { defaultShadow }; Call("DrawStringInk", menuShadow);
Check((Color)menuShadow[0]! == new Color(8,22,35), "native orange helper shadow becomes navy in menus");
object?[] fadedShadow = { new Color(221,148,84) * .5f }; Call("DrawStringInk", fadedShadow);
var fadedNavy = (Color)fadedShadow[0]!;
Check(fadedNavy.A == 127 && fadedNavy.R <= 4 && fadedNavy.G <= 11 && fadedNavy.B is >= 16 and <= 18, "default darker shadow retains faded opacity");
Call("ExitDefaultShadow", defaultHelper[1], new Exception("fixture"));
object?[] coloredHelper = { dark, 0 }; Call("ShadowInk", coloredHelper);
object?[] deliberateShadow = { defaultShadow }; Call("DrawStringInk", deliberateShadow);
Check((Color)deliberateShadow[0]! == defaultShadow, "intentional colored shadows retain their colors");
Call("ExitShadow", coloredHelper[1], null);
Call("ExitScope", outer[0], null);

Check(type!.GetMethod("MapSurface", BindingFlags.NonPublic | BindingFlags.Static) != null, "custom widget surface policy exists");
Color Surface(Color c, string? context, bool stamina = true, bool fade = false) => (Color)Call("MapSurface", c, context, stamina, fade)!;
var cream = new Color(250,222,171);
Check(Surface(cream, null, false, true) == cream, "world fill palette remains unchanged outside exact widget scope");
Check(Surface(cream, "QuestTextRenderer", false, false) == cream, "unrelated textures retain their colors inside widget scopes");
var button = Surface(cream, "QuestTextRenderer", false, true);
var hover = Surface(new Color(255,238,203), "QuestTextRenderer", false, true);
Check(button.B > button.R && button.A < 255 && hover.R > button.R && hover.G > button.G, "custom blue button has translucent fill and distinct hover");
var dim = Color.Black * .75f;
Check(Surface(dim, "ServiceChoicesMenu") == dim, "fullscreen black dim remains unchanged");
Check(Surface(Color.Gold, "ClickFeedback") == Color.Gold && Surface(Color.LightGreen, "ClickFeedback") == Color.LightGreen, "world target and machine semantic colors remain unchanged");
Check(Surface(Color.Black * .85f, "ClickFeedback").B > 40, "click feedback panel receives blue fill");
Check(Surface(new Color(104,63,145), "RelationshipSocialEntry").G > 180, "social relationship symbol becomes readable pale blue");
Check(type!.GetMethod("MapMoneyInk", BindingFlags.NonPublic | BindingFlags.Static) != null, "native money glyph tint policy exists");
var digit = new Rectangle(286,502,5,8);
Color Money(Color c, bool cursor, Rectangle? rect) => (Color)Call("MapMoneyInk", c, cursor, rect)!;
Check(Money(Color.Maroon, true, digit) == Color.Maroon, "money-colored world glyphs stay unchanged outside UI");
Call("EnterScope", outer);
var moneyInk = Money(Color.Maroon, true, digit);
Check(moneyInk.R > 220 && moneyInk.G > 200, "native money digits gain readable light tint");
Check(Money(Color.Maroon, false, digit) == Color.Maroon && Money(Color.Maroon, true, new Rectangle(285,502,5,8)) == Color.Maroon, "money recoloring requires exact native texture and digit source");
Call("ExitScope", outer[0], null);
var solaceAssembly = Assembly.LoadFrom(Path.GetFullPath("src/SolaceWeather/bin/Release/net6.0/SolaceWeather.dll"));
var customMethods = ((IEnumerable<MethodInfo>)Call("CustomWidgetMethods", solaceAssembly)!).ToArray();
Check(customMethods.Length == 6 && customMethods.Any(m => m.Name == "DrawHud") && customMethods.Any(m => m.DeclaringType!.Name == "ServiceChoicesMenu"), "all six exact custom widget helpers are discoverable");
var buttonMethod = customMethods.Single(m => m.Name == "DrawButton");
object?[] widgetScope = { buttonMethod, null }; Call("EnterCustomScope", widgetScope);
Check(Ink(dark).R > 200, "post-menu custom button text receives light ink");
Call("ExitCustomScope", widgetScope[1], new Exception("fixture"));
Check(Ink(dark) == dark && type!.GetField("surfaceContext", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null) == null, "custom widget exceptions restore both text and surface scopes");
Check(type!.GetMethod("ToolbarShortcutPosition", BindingFlags.NonPublic | BindingFlags.Static) != null, "toolbar shortcut centering policy exists");
Vector2 Shortcut(Vector2 at, Vector2 size) => (Vector2)Call("ToolbarShortcutPosition", at, size)!;
foreach(int viewportWidth in new[]{960,1280,1920}) foreach(int viewportHeight in new[]{720,1080}) foreach(bool topDock in new[]{false,true}) foreach(float uiScale in new[]{.75f,1f,1.25f,1.5f})
{
    float slotY=(topDock?112:viewportHeight)-88;
    for(int slot=0;slot<12;slot++)
    {
        float slotX=viewportWidth/2f-384+slot*64;
        var nativeAt=new Vector2(slotX+4,slotY-8);
        var labelSize=new Vector2(slot==5?7:6,20);
        var centered=Shortcut(nativeAt,labelSize);
        if(Math.Abs(centered.X+labelSize.X/2-(slotX+32))>.51f || centered.Y!=nativeAt.Y+2
            || (centered.X-slotX)*uiScale<0 || (centered.X+labelSize.X-slotX)*uiScale>64*uiScale)
            throw new Exception("Toolbar label no longer centered/in native top band");
    }
}
Check(true, "all twelve shortcut positions fit top/bottom slots at 75/100/125/150 percent UI scales");
var nativeToolbar=typeof(StardewValley.Menus.Toolbar).GetMethod("draw", new[] { typeof(Microsoft.Xna.Framework.Graphics.SpriteBatch) })!;
AppDomain.CurrentDomain.AssemblyResolve += (_, args) => new AssemblyName(args.Name).Name == "MonoMod.Common"
    ? Assembly.LoadFrom(Path.Combine(AppContext.BaseDirectory, "MonoMod.Common.dll")) : null;
var instructions=HarmonyLib.PatchProcessor.GetOriginalInstructions(nativeToolbar).ToList();
var rewritten=((IEnumerable<HarmonyLib.CodeInstruction>)Call("ToolbarShortcutTranspiler",instructions)!).ToList();
Check(rewritten.Count==instructions.Count && rewritten.Zip(instructions).Count(pair=>!Equals(pair.First.operand,pair.Second.operand))==1,
    "native toolbar patch changes only its shortcut text call");
Check(rewritten.Any(i=>i.operand is MethodInfo m && m.Name=="DrawToolbarShortcut"), "toolbar routes only shortcut labels through dedicated renderer");

