using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using xTile.Tiles;

namespace NpcArtAudit;

/// <summary>Seasonal home coverage and native upgrade contracts on detached maps.</summary>
internal static class TownHousesAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        int checks = 0;
        string? error = null;
        var doors = new List<object>();
        var seasons = new List<object>();
        var upgradeCases = new List<object>();
        var random = Game1.random;
        var location = Game1.currentLocation;
        var player = Game1.player;
        var viewport = Game1.viewport;
        int time = Game1.timeOfDay;
        void Check(bool ok, string message) { checks++; if (!ok) throw new InvalidOperationException(message); }
        try
        {
            var map = helper.GameContent.Load<xTile.Map>("Maps/Town");
            foreach (var (home, x, y, id, action) in new[]
            {
                ("Emily",20,88,257,"LockedDoorWarp 2 24 HaleyHouse 900 2000"),
                ("Jodi",10,85,331,"LockedDoorWarp 4 23 SamHouse 900 2000"),
                ("George",57,63,339,"LockedDoorWarp 9 24 JoshHouse 800 2000"),
                ("Lewis",58,85,728,"LockedDoorWarp 4 11 ManorHouse 830 2200"),
                ("Lewis",59,85,729,"LockedDoorWarp 5 11 ManorHouse 830 2200"),
                ("Pam",72,68,753,"LockedDoorWarp 12 9 Trailer 900 2000")
            })
            {
                var tile = map.GetLayer("Buildings").Tiles[x,y];
                Check(tile?.TileSheet.Id == "Town" && tile.TileIndex == id, "Door source changed: " + home);
                Check(tile!.Properties.TryGetValue("Action", out var actual) && actual.ToString() == action, "Door action changed: " + home);
                doors.Add(new { Home=home, X=x, Y=y, Tile=id, Action=action });
            }
            foreach (var (property, expected) in new[] { ("DayTiles",720), ("NightTiles",726) })
            {
                var parts = map.Properties[property].ToString().Split(' ',StringSplitOptions.RemoveEmptyEntries);
                Check(parts.Length == 56,"Town light selector count changed: " + property);
                bool found = false;
                for (int i=0; i<parts.Length; i+=4)
                    if (parts[i] == "Buildings" && parts[i+1] == "71" && parts[i+2] == "67")
                    { found=true; Check(int.Parse(parts[i+3]) == expected,"Pam window selector changed: " + property); }
                Check(found,"Missing Pam window selector: " + property);
            }
            Check(map.Properties["Light"].ToString().Contains("71 67 4"),"Pam native light anchor changed.");
            var areas = new Dictionary<string,Rectangle>
            {
                ["Emily"] = new(0,0,128,144), ["Jodi"] = new(128,48,128,128),
                ["George"] = new(256,32,144,144), ["Lewis"] = new(368,176,128,208),
                ["PamTrailer"] = new(224,320,128,64), ["PamRebuilt"] = new(384,656,128,144),
                ["PamNight"] = new(352,352,16,16)
            };
            foreach (string season in new[] { "spring","summer","fall","winter" })
            {
                var texture=helper.GameContent.Load<Texture2D>("Maps/"+season+"_town");
                Check(texture.Width == 512 && texture.Height == 1152,"Seasonal house atlas size changed: "+season);
                var pixels=new Color[texture.Width*texture.Height]; texture.GetData(pixels);
                foreach (var (home,area) in areas)
                {
                    int occupied=0;
                    for(int y=area.Y; y<area.Bottom; y++) for(int x=area.X; x<area.Right; x++)
                        if(pixels[y*texture.Width+x].A>0) occupied++;
                    Check(occupied>0,"Missing loaded house material: "+season+"/"+home);
                    seasons.Add(new { Season=season,Home=home,OccupiedPixels=occupied });
                }
            }
            // A separate content manager guarantees the native upgrade never edits
            // the game-content cache or a location belonging to the player.
            foreach(int startingWindow in new[] {720,726})
            {
                using var content=new ContentManager(new GameServiceContainer(),Path.Combine(AppContext.BaseDirectory,"Content"));
                var detachedMap=content.Load<xTile.Map>("Maps/Town");
                Check(!ReferenceEquals(map,detachedMap),"Upgrade map was not detached.");
                detachedMap.GetLayer("Buildings").Tiles[71,67].TileIndex=startingWindow;
                var town=new Town { map=detachedMap };
                town.showImprovedPamHouse();
                var upgradeWindow=detachedMap.GetLayer("Buildings").Tiles[71,67];
                var upgradeDoor=detachedMap.GetLayer("Buildings").Tiles[72,68];
                Check(upgradeWindow.TileIndex==1562,"Rebuilt house selected the wrong window from "+startingWindow);
                Check(upgradeDoor.TileIndex==1595,"Rebuilt house selected the wrong door.");
                Check(upgradeDoor.Properties["Action"].ToString()=="LockedDoorWarp 12 9 Trailer 900 2000","Upgrade changed the entrance action.");
                Check(detachedMap.GetLayer("AlwaysFront").Tiles[69,60].TileIndex==1336,"Rebuilt roof anchor changed.");
                town.showImprovedPamHouse();
                Check(upgradeWindow.TileIndex==1562 && upgradeDoor.TileIndex==1595,"Repeated upgrade changed the source again.");
                upgradeCases.Add(new { StartingWindow=startingWindow, RebuiltWindow=upgradeWindow.TileIndex, RebuiltDoor=upgradeDoor.TileIndex });
            }
            Check(map.GetLayer("Buildings").Tiles[71,67].TileIndex==720,"Shared game map was modified by detached upgrade checks.");
            foreach(var (x,y,w,h) in new[] {(18,79,10,12),(5,75,12,13),(53,53,11,12),(55,73,12,16),(68,59,11,12)})
                foreach(var layer in map.Layers)
                    for(int yy=y;yy<y+h;yy++)for(int xx=x;xx<x+w;xx++)
                        if(layer.Tiles[xx,yy]?.TileSheet.Id=="Town")
                            Check(layer.Tiles[xx,yy] is not AnimatedTile,"Unexpected animated house atlas tile.");
        }
        catch(Exception ex) { error=ex.ToString(); }
        bool globalsUnchanged=ReferenceEquals(random,Game1.random) && ReferenceEquals(location,Game1.currentLocation)
            && ReferenceEquals(player,Game1.player) && viewport.Equals(Game1.viewport) && time==Game1.timeOfDay;
        helper.Data.WriteJsonFile("town-houses-checks.json",new
        {
            Passed=error==null && globalsUnchanged,Error=error,Checks=checks,GlobalsUnchanged=globalsUnchanged,
            Doors=doors,LoadedSeasonalAreas=seasons,DetachedNativeUpgradeCases=upgradeCases,
            Scope="Six native door tiles/actions, Pam day/night selection, four loaded house sheets, detached native rebuilt-house selection from day and night. No farm loaded, save written, door entered, world/mail/clock/lighting modified. Exact artwork identity is covered by the registered loader and source preservation checks."
        });
        monitor.Log(error==null && globalsUnchanged ? $"Town houses audit passed: {checks} checks." : "Town houses audit failed: "+error,error==null && globalsUnchanged?LogLevel.Info:LogLevel.Error);
    }
}
