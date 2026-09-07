using SolaceWeather.Core;
using System.Reflection;

var type = Assembly.Load("SolaceWeather").GetType("SolaceWeather.Controls.FarmhouseDoorwayPath");
if (type == null) throw new Exception("FAIL: visible farmhouse doorway has no exit route planner.");
var method = type.GetMethod("Find")!;
IReadOnlyList<WalkTile>? Find(WalkTile click, WalkTile entry, WalkTile warp, Func<WalkTile, bool>? passable = null)
    => (IReadOnlyList<WalkTile>?)method.Invoke(null, new object[] { new WalkTile(3, 8), click, entry, warp, 12, 12, passable ?? (_ => true) });
void Check(string name, bool result) { if (!result) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); }
var entry = new WalkTile(3, 11); var warp = new WalkTile(3, 12);
var route = Find(entry, entry, warp);
Check("Visible doorway reaches the native off-map trigger", route != null && route[^2] == entry && route[^1] == warp);
Check("Exact trigger click reaches the same doorway", Find(warp, entry, warp)?.SequenceEqual(route!) == true);
Check("Neighboring floor is not an exit click", Find(new(2, 11), entry, warp) == null);
Check("Far off-map click is not an exit click", Find(new(3, 13), entry, warp) == null);
Check("Unrelated warp cannot be substituted", Find(entry, entry, new(4, 12)) == null);
Check("Blocked doorway does not append a warp", Find(entry, entry, warp, p => p != entry) == null);
Check("Blocked corridor cannot teleport to exit", Find(entry, entry, warp, p => p.Y != 10) == null);
