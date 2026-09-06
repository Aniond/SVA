using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using xTile.Tiles;

namespace NpcArtAudit;

/// <summary>Read-only map/loaded-art contracts. CPU scene previews are produced by the artwork preparation.</summary>
internal static class TownBuildingsAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        int checks = 0;
        string? error = null;
        var textures = new List<object>();
        var doors = new List<object>();
        var selectorProof = new List<object>();
        var random = Game1.random;
        var location = Game1.currentLocation;
        var viewport = Game1.viewport;
        void Check(bool ok, string message) { checks++; if (!ok) throw new InvalidOperationException(message); }
        try
        {
            var map = helper.GameContent.Load<xTile.Map>("Maps/Town");
            var townSheet = map.GetTileSheet("Town");
            Check(townSheet != null && townSheet.SheetSize.Width == 32 && townSheet.TileSize.Width == 16 && townSheet.TileSize.Height == 16,
                "Town atlas tile geometry changed.");
            foreach (var (x, y, id, action) in new[]
            {
                (36,55,579,"LockedDoorWarp 10 19 Hospital 900 1500"),
                (43,56,618,"LockedDoorWarp 6 29 SeedShop 900 2100"),
                (44,56,619,"LockedDoorWarp 6 29 SeedShop 900 2100"),
                (45,70,596,"LockedDoorWarp 14 24 Saloon 1200 2400")
            })
            {
                var tile = map.GetLayer("Buildings").Tiles[x, y];
                Check(tile != null && tile.TileSheet.Id == "Town" && tile.TileIndex == id, "Door tile changed at " + x + "," + y);
                Check(tile!.Properties.TryGetValue("Action", out var actual) && actual.ToString() == action, "Door action changed at " + x + "," + y);
                doors.Add(new { X = x, Y = y, Tile = id, Action = action });
            }
            var selectors = new Dictionary<string, Dictionary<string, int>>();
            foreach (string property in new[] { "DayTiles", "NightTiles" })
            {
                Check(map.Properties.TryGetValue(property, out var value), "Missing " + property);
                var parts = value!.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                Check(parts.Length % 4 == 0, "Malformed " + property);
                var entries = new Dictionary<string, int>();
                for (int i = 0; i < parts.Length; i += 4)
                    entries.Add(parts[i] + ":" + parts[i + 1] + ":" + parts[i + 2], int.Parse(parts[i + 3]));
                selectors.Add(property, entries);
                Check(entries.Count == 14, "Native Town light selector count changed.");
                Check(entries["Buildings:41:69"] == (property == "DayTiles" ? 560 : 653), "Saloon day/night selector changed.");
                selectorProof.Add(new { Property = property, Count = entries.Count, Saloon = entries["Buildings:41:69"], ClinicStreetlight = entries["AlwaysFront:33:53"] });
            }
            Check(map.Properties.TryGetValue("Light", out var lights), "Missing map Light positions.");
            var lightParts = lights!.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            Check(lightParts.Length == 42, "Native Town light count changed.");
            Check(lights.ToString().Contains("41 69 4") && lights.ToString().Contains("33 53 4"), "Building-area light anchors changed.");
            var areas = new Dictionary<string, Rectangle[]>
            {
                ["PierreClinic"] = new[] { new Rectangle(16,176,224,32),new Rectangle(0,208,240,96),new Rectangle(80,304,160,16),new Rectangle(400,128,48,32),new Rectangle(400,160,112,16) },
                ["Saloon"] = new[] { new Rectangle(240,176,112,144),new Rectangle(208,320,16,16) }
            };
            foreach (string season in new[] { "spring", "summer", "fall", "winter" })
            {
                var texture = helper.GameContent.Load<Texture2D>("Maps/" + season + "_town");
                Check(texture.Width == 512 && texture.Height == 1152, "Seasonal Town atlas dimensions changed: " + season);
                var pixels = new Color[texture.Width * texture.Height]; texture.GetData(pixels);
                foreach (var group in areas)
                {
                    var occupancy = new List<object>();
                    foreach (var area in group.Value)
                    {
                        int count = 0;
                        for (int y = area.Y; y < area.Bottom; y++) for (int x = area.X; x < area.Right; x++)
                            if (pixels[y * texture.Width + x].A > 0) count++;
                        Check(count > 0, "Empty prepared building region: " + season + "/" + group.Key);
                        occupancy.Add(new { area.X, area.Y, area.Width, area.Height, OccupiedPixels = count });
                    }
                    textures.Add(new { Season = season, Building = group.Key, Areas = occupancy });
                }
                foreach (int index in new[] { 560, 653 })
                {
                    int count = 0, sx = index % 32 * 16, sy = index / 32 * 16;
                    for (int y = sy; y < sy + 16; y++) for (int x = sx; x < sx + 16; x++) if (pixels[y * 512 + x].A > 0) count++;
                    Check(count > 0, "Saloon day/night cell is empty: " + season + "/" + index);
                }
            }
            foreach (var (x,y,w,h) in new[] { (32,46,17,13),(39,62,10,12) })
                foreach (var layer in map.Layers)
                    for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++)
                    {
                        var tile = layer.Tiles[xx, yy];
                        if (tile?.TileSheet.Id == "Town") Check(tile is not AnimatedTile, "Unexpected Town-atlas building animation requires expanded art coverage.");
                    }
            Check(ReferenceEquals(random, Game1.random) && ReferenceEquals(location, Game1.currentLocation) && viewport.Equals(Game1.viewport), "Live global state changed.");
        }
        catch (Exception ex) { error = ex.ToString(); }
        bool unchanged = ReferenceEquals(random, Game1.random) && ReferenceEquals(location, Game1.currentLocation) && viewport.Equals(Game1.viewport);
        helper.Data.WriteJsonFile("town-buildings-checks.json", new
        {
            Passed = error == null && unchanged, Error = error, Checks = checks, GlobalsUnchanged = unchanged,
            Doors = doors, DayNightSelectors = selectorProof, LoadedTextures = textures,
            Scope = "Read-only native Town map/action/day-night/lighting contract and loaded seasonal building-area occupancy. No door action, location change, graphics draw, weather, clock or save mutation. Artwork identity uses root registry pixel verifier; CPU building scene previews provide visual review. Native sconce glow is unchanged."
        });
        monitor.Log(error == null && unchanged ? $"Town buildings audit passed: {checks} checks." : "Town buildings audit failed: " + error, error == null && unchanged ? LogLevel.Info : LogLevel.Error);
    }
}
