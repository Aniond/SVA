using System.Reflection;
using Microsoft.Xna.Framework;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewValley;
using xTile;
using xTile.Dimensions;
using xTile.Layers;
using xTile.Tiles;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private sealed class RomanceWitnessNpc : NPC
    {
        public override bool CanSocialize => true;
        public RomanceWitnessNpc(string name, GameLocation location, int x, int y)
            : base(new AnimatedSprite("Characters/Robin", 0, 16, 32), new Vector2(x * 64, y * 64), 2, name)
        {
            currentLocation = location;
        }
    }

    private static GameLocation WitnessLocation(string name)
    {
        var map = new Map();
        var back = new Layer("Back", map, new Size(32, 32), new Size(64, 64));
        var buildings = new Layer("Buildings", map, new Size(32, 32), new Size(64, 64));
        var front = new Layer("Front", map, new Size(32, 32), new Size(64, 64));
        map.AddLayer(back);
        map.AddLayer(buildings);
        map.AddLayer(front);
        var sheet = new TileSheet("fixture", map, "Maps/spring_outdoorsTileSheet", new Size(1, 1), new Size(64, 64));
        map.AddTileSheet(sheet);
        for (int x = 0; x < 32; x++)
            for (int y = 0; y < 32; y++) back.Tiles[x, y] = new StaticTile(back, sheet, BlendMode.Alpha, 0);
        var location = new GameLocation { map = map };
#pragma warning disable AvoidNetField // The Name property is read-only; this fixture needs no map loading.
        location.name.Value = name;
#pragma warning restore AvoidNetField
        return location;
    }

    private void RomanceWitnessChecks()
    {
        RequireWorld();
        object mod = AiMod(Helper);
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        object service = mod.GetType().GetField("romance", flags)!.GetValue(mod)!;
        Type type = service.GetType();
        FieldInfo farmField = type.GetField("farm", flags)!;
        object? originalFarm = farmField.GetValue(service);
        Type farmType = type.GetNestedType("FarmRelationships", flags)!;
        object fixtureFarm = Activator.CreateInstance(farmType)!;
        var state = new RomanceSaveState();
        farmType.GetProperty("FarmerId")!.SetValue(fixtureFarm, Game1.player.UniqueMultiplayerID);
        farmType.GetProperty("State")!.SetValue(fixtureFarm, state);
        var originalLocation = Game1.currentLocation;
        var originalPlayerLocation = Game1.player.currentLocation;
        Vector2 originalPosition = Game1.player.Position;
        var first = WitnessLocation("RomanceWitnessFirst");
        var other = WitnessLocation("RomanceWitnessOther");
        var near = new RomanceWitnessNpc("RomanceWitnessNear", first, 18, 10);
        var distant = new RomanceWitnessNpc("RomanceWitnessDistant", first, 19, 10);
        var elsewhere = new RomanceWitnessNpc("RomanceWitnessElsewhere", other, 10, 10);
        first.characters.Add(near);
        first.characters.Add(distant);
        other.characters.Add(elsewhere);
        var results = new List<object>();
        object? Invoke(string name, params object[] args)
        {
            MethodInfo method = type.GetMethod(name, flags)!;
            return method.Invoke(method.IsStatic ? null : service, args);
        }
        bool See(NPC npc, Vector2 target, int range = 8) => (bool)Invoke("CanSee", npc, target, range)!;
        void Check(string name, Func<bool> test)
        {
            bool passed = false;
            string? error = null;
            try { passed = test(); }
            catch (Exception ex) { error = ex.GetBaseException().Message; }
            results.Add(new { Name = name, Passed = passed, Error = error });
            Monitor.Log($"{(passed ? "PASS" : "FAIL")}: {name}", passed ? LogLevel.Info : LogLevel.Error);
        }
        try
        {
            farmField.SetValue(service, fixtureFarm);
            Game1.locations.Add(first);
            Game1.locations.Add(other);
            Game1.currentLocation = first;
            Game1.player.currentLocation = first;
            Game1.player.Position = new Vector2(10 * 64, 10 * 64);
            Check("Eight-tile unobstructed witness can see", () => See(near, Game1.player.Tile));
            Check("Nine-tile witness cannot see", () => !See(distant, Game1.player.Tile));
            Check("Sleeping witness cannot see", () =>
            {
                near.isSleeping.Value = true;
                try { return !See(near, Game1.player.Tile); }
                finally { near.isSleeping.Value = false; }
            });
            Check("Invisible witness cannot see", () =>
            {
                near.IsInvisible = true;
                try { return !See(near, Game1.player.Tile); }
                finally { near.IsInvisible = false; }
            });
            Check("A building tile blocks the actual sight path", () =>
            {
                Layer buildings = first.Map.GetLayer("Buildings");
                buildings.Tiles[14, 10] = new StaticTile(buildings, first.Map.TileSheets[0], BlendMode.Alpha, 0);
                try { return !See(near, Game1.player.Tile); }
                finally { buildings.Tiles[14, 10] = null; }
            });
            Check("Explicit signal informs only nearby witnesses in the same location", () =>
            {
                Invoke("Signal", "Abigail", "flirt");
                return state.Incidents.Count == 1 && state.Knowledge.Any(k => k.KnowerNpc == near.Name)
                    && !state.Knowledge.Any(k => k.KnowerNpc == distant.Name || k.KnowerNpc == elsewhere.Name);
            });
            // Move the previously distant listener into a real two-tile encounter.
            near.Position = new Vector2(12 * 64, 10 * 64);
            distant.Position = new Vector2(14 * 64, 10 * 64);
            Check("An actual encounter forwards the original eyewitness and increments hops", () =>
            {
                Invoke("SpreadNews");
                var report = state.Knowledge.FirstOrDefault(k => k.KnowerNpc == distant.Name);
                return report != null && report.Source == near.Name && report.OriginalEyewitness == near.Name
                    && report.Hops == 1 && !state.Knowledge.Any(k => k.KnowerNpc == elsewhere.Name);
            });
            Check("Repeated encounters do not duplicate knowledge", () =>
            {
                int count = state.Knowledge.Count;
                Invoke("SpreadNews");
                return state.Knowledge.Count == count && state.IsValid();
            });
        }
        finally
        {
            farmField.SetValue(service, originalFarm);
            Game1.currentLocation = originalLocation;
            Game1.player.currentLocation = originalPlayerLocation;
            Game1.player.Position = originalPosition;
            Game1.locations.Remove(first);
            Game1.locations.Remove(other);
            Helper.Data.WriteJsonFile("romance-witness-results.json", results);
        }
    }
}
