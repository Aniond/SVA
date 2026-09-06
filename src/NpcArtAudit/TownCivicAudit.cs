using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using xTile.Tiles;

namespace NpcArtAudit;

/// <summary>Validate material source assumptions and export native civic appearance states.</summary>
internal static class TownCivicAudit
{
    private static readonly Dictionary<string, Rectangle> Windows = new()
    {
        ["CommunityCenter"] = new(46,10,15,13), ["Blacksmith"] = new(90,73,9,12),
        ["Museum"] = new(98,82,12,12), ["JojaMart"] = new(89,41,14,13)
    };

    private static object Capture(xTile.Map map, string state, Rectangle window)
    {
        var tiles = new List<object>();
        foreach (var layer in map.Layers)
            for (int y=window.Y; y<window.Bottom; y++)
                for (int x=window.X; x<window.Right; x++)
                {
                    var tile=layer.Tiles[x,y];
                    if (tile==null) continue;
                    var frames=tile is AnimatedTile animated ? animated.TileFrames : new[] {(StaticTile)tile};
                    for(int frame=0; frame<frames.Length; frame++)
                    {
                        var t=frames[frame]; var bounds=t.TileSheet.GetTileImageBounds(t.TileIndex);
                        tiles.Add(new { layer=layer.Id,x,y,index=t.TileIndex,sheet=t.TileSheet.Id,
                            asset=t.TileSheet.ImageSource.Replace('\\','/'),rect=new[] {bounds.X,bounds.Y,16,16},
                            frame,frames=frames.Length,properties=tile.Properties.ToDictionary(p=>p.Key,p=>p.Value.ToString()) });
                    }
                }
        return new { state,window=new[] {window.X,window.Y,window.Width,window.Height},tiles };
    }

    public static void Run(IModHelper helper, IMonitor monitor)
    {
        int checks=0; string? error=null;
        var states=new List<object>(); var loaded=new List<object>();
        var oldPlayer=Game1.player; var oldLocation=Game1.currentLocation; var oldRandom=Game1.random;
        var oldViewport=Game1.viewport; int oldTime=Game1.timeOfDay;
        void Check(bool ok,string message) { checks++; if(!ok) throw new InvalidOperationException(message); }
        ContentManager Native() => new(new GameServiceContainer(),Path.Combine(AppContext.BaseDirectory,"Content"));
        void Invoke(Town town,string method) => typeof(Town).GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic)!.Invoke(town,null);
        try
        {
            var shared=helper.GameContent.Load<xTile.Map>("Maps/Town");
            string fingerprint=JsonSerializer.Serialize(Windows.Select(w=>Capture(shared,w.Key,w.Value)));
            foreach(var (x,y,id,action) in new[]
            {
                (52,19,1541,"WarpCommunityCenter"),(53,19,1542,"WarpCommunityCenter"),
                (94,81,252,"LockedDoorWarp 5 19 Blacksmith 900 1600"),
                (101,89,871,"LockedDoorWarp 3 14 ArchaeologyHouse 800 1800"),
                (95,50,1925,"LockedDoorWarp 13 29 JojaMart 900 2300"),
                (96,50,1926,"LockedDoorWarp 14 29 JojaMart 900 2300")
            })
            {
                var tile=shared.GetLayer("Buildings").Tiles[x,y];
                Check(tile?.TileSheet.Id=="Town"&&tile.TileIndex==id,"Native civic entrance source differs.");
                Check(tile!.Properties["Action"].ToString()==action,"Native civic entrance action differs.");
            }
            foreach(var entry in Windows) states.Add(Capture(shared,entry.Key,entry.Value));
            Check(shared.Properties["DayTiles"].ToString().Contains("Buildings 92 79 186"),"Blacksmith day window selector differs.");
            Check(shared.Properties["NightTiles"].ToString().Contains("Buildings 92 79 190"),"Blacksmith night window selector differs.");
            using(var content=Native())
            {
                var map=content.Load<xTile.Map>("Maps/Town");
                map.GetLayer("Buildings").Tiles[92,79].TileIndex=190;
                states.Add(Capture(map,"BlacksmithNight",Windows["Blacksmith"]));
            }
            using(var content=Native())
            {
                var map=content.Load<xTile.Map>("Maps/Town"); var town=new Town {map=map};
                Check(!ReferenceEquals(map,shared),"Community Center check must use a detached map.");
                Invoke(town,"refurbishCommunityCenter");
                Check(map.GetLayer("Buildings").Tiles[52,19].TileIndex==1553,"Restored Community Center left door source differs.");
                Check(map.GetLayer("Buildings").Tiles[53,19].TileIndex==1554,"Restored Community Center right door source differs.");
                Check(map.GetLayer("Buildings").Tiles[52,19].Properties["Action"].ToString()=="WarpCommunityCenter","Restoration changed the entrance action.");
                var restored=Capture(map,"CommunityCenterRestored",Windows["CommunityCenter"]);
                states.Add(restored); Invoke(town,"refurbishCommunityCenter");
                Check(JsonSerializer.Serialize(restored)==JsonSerializer.Serialize(Capture(map,"CommunityCenterRestored",Windows["CommunityCenter"])),"Repeated restoration changed sources again.");
            }
            using(var content=Native())
            {
                var map=content.Load<xTile.Map>("Maps/Town"); var town=new Town {map=map};
                Invoke(town,"showDestroyedJoja");
                Check(map.GetLayer("Buildings").Tiles[95,50].TileIndex==1945,"Abandoned Joja left door source differs.");
                Check(map.GetLayer("Buildings").Tiles[96,50].TileIndex==1946,"Abandoned Joja right door source differs.");
                var abandoned=Capture(map,"JojaAbandoned",Windows["JojaMart"]);states.Add(abandoned);
                Invoke(town,"showDestroyedJoja");
                Check(JsonSerializer.Serialize(abandoned)==JsonSerializer.Serialize(Capture(map,"JojaAbandoned",Windows["JojaMart"])),"Repeated abandonment changed sources again.");
                town.crackOpenAbandonedJojaMartDoor();
                foreach(var (x,y,id) in new[] {(95,49,2000),(96,49,2001),(95,50,2032),(96,50,2033)})
                    Check(map.GetLayer("Buildings").Tiles[x,y].TileIndex==id,"Accessible Joja door source differs.");
                states.Add(Capture(map,"JojaAbandonedOpen",Windows["JojaMart"]));
            }
            foreach(var (name,state,window,offset) in new[]
            {
                ("Town-Theater","TheaterJoja",new Rectangle(84,41,27,15),Point.Zero),
                ("Town-TheaterCC","TheaterCC",new Rectangle(46,11,15,17),new Point(-43,-31)),
                ("Town-TheaterCC-Halloween2","TheaterCCHalloween",new Rectangle(46,11,15,17),new Point(-43,-31))
            })
            {
                using var content=Native();
                var map=content.Load<xTile.Map>(state=="TheaterCCHalloween"?"Maps/Town-Halloween2":"Maps/Town");
                var overlay=content.Load<xTile.Map>("Maps/"+name);
                var town=new Town {map=map};
                Check(!ReferenceEquals(map,shared),"Theater override must use a detached base map.");
                town.ApplyMapOverride(overlay,name,window,window);
                Check(map.GetLayer("Back").Tiles[95+offset.X,50+offset.Y]!=null,"Theater override lost the ground beneath its entrance.");
                if(state!="TheaterCCHalloween")
                    foreach(var (x,y,id,action) in new[] {(95,50,2245,"Theater_Entrance"),(96,50,2246,"Theater_Entrance"),(98,51,2280,"Theater_BoxOffice")})
                    {
                        var tile=map.GetLayer("Buildings").Tiles[x+offset.X,y+offset.Y];
                        Check(tile.TileIndex==id&&tile.Properties["Action"].ToString()==action,"Theater interaction source differs: "+name);
                    }
                states.Add(Capture(map,state,window));
            }
            var regions=new Dictionary<string,Rectangle>
            {
                ["CommunityCenter"]=new(0,640,192,160),["CommunityCenterRestored"]=new(192,640,192,160),
                ["Blacksmith"]=new(400,0,80,128),["Museum"]=new(80,320,144,128),
                ["JojaMart"]=new(0,832,192,160),["JojaAbandoned"]=new(320,832,192,160),["Theater"]=new(0,992,192,160)
            };
            foreach(string season in new[] {"spring","summer","fall","winter"})
            {
                var texture=helper.GameContent.Load<Texture2D>("Maps/"+season+"_town");
                Check(texture.Width==512&&texture.Height==1152,"Civic seasonal atlas dimensions differ.");
                var pixels=new Color[texture.Width*texture.Height];texture.GetData(pixels);
                foreach(var (name,rect) in regions)
                {
                    int occupied=0;for(int y=rect.Y;y<rect.Bottom;y++)for(int x=rect.X;x<rect.Right;x++)if(pixels[y*texture.Width+x].A>0)occupied++;
                    Check(occupied>100,"Empty loaded civic appearance: "+name+"/"+season);
                    loaded.Add(new {season,appearance=name,occupied});
                }
            }
            Check(Town.jojaFacadeTop==new Rectangle(424,1275,174,50)&&Town.jojaFacadeBottom==new Rectangle(424,1325,174,51),"Warehouse facade source bounds differ.");
            Check(Town.jojaFacadeWinterOverlay==new Rectangle(66,1678,174,25),"Warehouse winter overlay source differs.");
            var cursors=helper.GameContent.Load<Texture2D>("LooseSprites/Cursors");
            Check(cursors.Width>=598&&cursors.Height>=1703,"Warehouse facade atlas is too small.");
            Check(fingerprint==JsonSerializer.Serialize(Windows.Select(w=>Capture(shared,w.Key,w.Value))),"Native appearance audit modified the shared map.");
            helper.Data.WriteJsonFile("town-civic-native-states.json",states);
        }
        catch(Exception ex) {error=ex.ToString();}
        bool unchanged=ReferenceEquals(oldPlayer,Game1.player)&&ReferenceEquals(oldLocation,Game1.currentLocation)&&ReferenceEquals(oldRandom,Game1.random)&&oldViewport.Equals(Game1.viewport)&&oldTime==Game1.timeOfDay;
        helper.Data.WriteJsonFile("town-civic-checks.json",new {Passed=error==null&&unchanged,Error=error,Checks=checks,GlobalsUnchanged=unchanged,LoadedAreas=loaded,NativeStates=states.Count,
            Scope="Native entrance/light sources, detached restored/abandoned/open-door states, theater replacement maps and loaded seasonal materials. No farm, save, player, mail or lighting changes. Warehouse overlay bounds checked; dynamic clock, posters and live lighting remain hands-on checks. Exact material identity is verified by the registered loader and offline preservation checks."});
        monitor.Log(error==null&&unchanged?$"Town civic audit passed: {checks} checks.":"Town civic audit failed: "+error,error==null&&unchanged?LogLevel.Info:LogLevel.Error);
    }
}
