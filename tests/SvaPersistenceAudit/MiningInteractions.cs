using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Tools;
using StardewValley.TerrainFeatures;

namespace SvaPersistenceAudit;
public sealed partial class ModEntry
{
    private void MiningInteractions()
    {
        RequireOwnedWorld();
        if(Game1.IsMultiplayer)throw new InvalidOperationException("Mining fixtures require the owned single-player session.");
        var originalPlayer=Game1.player;var originalLocation=Game1.currentLocation;var random=Game1.random;var multiplayerRandom=Game1.recentMultiplayerRandom;var menu=Game1.activeClickableMenu;
        bool dialogue=Game1.dialogueUp;
        var rumble=typeof(Rumble).GetFields(BindingFlags.Static|BindingFlags.NonPublic).Where(f=>!f.IsInitOnly).Select(f=>(Field:f,Value:f.GetValue(null))).ToArray();
        uint rocks=originalPlayer.stats.RocksCrushed;float stamina=originalPlayer.Stamina;int originalObjects=originalLocation.objects.Pairs.Count();
        var checks=new Dictionary<string,bool>();var cases=new List<object>();string? error=null;Farmer? farmer=null;
        try
        {
            using var content=Game1.content.CreateTemporary();
            var fixture=new GameLocation();fixture.map=content.Load<xTile.Map>("Maps/Mines/20");
            Helper.Reflection.GetField<Netcode.NetString>(fixture,"name").GetValue().Value="SvaMiningDisposable";
            farmer=new Farmer();farmer.UniqueMultiplayerID=originalPlayer.UniqueMultiplayerID;farmer.Position=new Vector2(5*64,5*64);farmer.currentLocation=fixture;
            typeof(Game1).GetField("_player",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,farmer);Game1.currentLocation=fixture;Game1.random=new Random(6172026);Game1.activeClickableMenu=null;
            var tool=ItemRegistry.Create<Pickaxe>("(T)Pickaxe");tool.UpgradeLevel=4;tool.PlayUseSounds=false;tool.lastUser=farmer;
            farmer.Items.Clear();farmer.Items.Add(tool);farmer.CurrentToolIndex=0;
            foreach(var spec in new[]{(Id:"343",Drop:""),(Id:"751",Drop:"378"),(Id:"290",Drop:"380"),(Id:"8",Drop:"66"),(Id:"14",Drop:"62")})
            {
                fixture.debris.Clear();fixture.temporarySprites.Clear();var tile=new Vector2(5,5);
                var node=ItemRegistry.Create<StardewValley.Object>("(O)"+spec.Id);node.TileLocation=tile;node.MinutesUntilReady=10;fixture.objects.Add(tile,node);
                float before=farmer.Stamina;uint count=farmer.stats.RocksCrushed;int hits=0;
                while(fixture.objects.ContainsKey(tile)&&hits<16){tool.swingTicker++;tool.DoFunction(fixture,5*64+32,5*64+32,0,farmer);hits++;}
                string[] drops=fixture.debris.Select(d=>d.item?.QualifiedItemId??d.itemId.Value??"").Where(s=>s.Length>0).ToArray();
                bool expected=spec.Drop.Length==0 || drops.Any(d=>d==spec.Drop||d=="(O)"+spec.Drop);
                checks[spec.Id+"-native-removed"]=!fixture.objects.ContainsKey(tile);
                checks[spec.Id+"-native-drop"]=expected;
                checks[spec.Id+"-native-stats-stamina"]=farmer.stats.RocksCrushed==count+1&&farmer.Stamina<before;
                cases.Add(new{spec.Id,Hits=hits,Drops=drops,ExpectedDrop=spec.Drop,Removed=!fixture.objects.ContainsKey(tile)});
            }
            fixture.debris.Clear();fixture.temporarySprites.Clear();
            var clump=new ResourceClump(672,2,2,new Vector2(8,5));clump.Location=fixture;fixture.resourceClumps.Add(clump);
            float health=clump.health.Value;tool.UpgradeLevel=0;tool.swingTicker++;
            tool.DoFunction(fixture,8*64+32,5*64+32,0,farmer);
            checks["Boulder-rejects-basic-pickaxe"]=clump.health.Value==health&&fixture.resourceClumps.Contains(clump);
            Game1.activeClickableMenu=null;tool.UpgradeLevel=4;int strikes=0;
            while(fixture.resourceClumps.Contains(clump)&&strikes<32){tool.swingTicker++;tool.DoFunction(fixture,8*64+32,5*64+32,0,farmer);strikes++;}
            string[] rockDrops=fixture.debris.Select(d=>d.item?.QualifiedItemId??d.itemId.Value??"").ToArray();
            checks["Boulder-native-removal"]=!fixture.resourceClumps.Contains(clump)&&clump.health.Value<=0;
            checks["Boulder-native-stone-drop"]=rockDrops.Any(s=>s=="390"||s=="(O)390")||fixture.debris.Any(d=>d.chunkType.Value==390);
            cases.Add(new{Id="ResourceClump672",Hits=strikes,Drops=rockDrops,InitialHealth=health,FinalHealth=clump.health.Value});
        }
        catch(Exception ex){error=ex.ToString();}
        finally
        {
            typeof(Game1).GetField("_player",BindingFlags.Static|BindingFlags.NonPublic)!.SetValue(null,originalPlayer);Game1.currentLocation=originalLocation;Game1.random=random;Game1.recentMultiplayerRandom=multiplayerRandom;Game1.activeClickableMenu=menu;
            Game1.dialogueUp=dialogue;foreach(var value in rumble)value.Field.SetValue(null,value.Value);
            checks["OriginalWorldUnchanged"]=originalPlayer.stats.RocksCrushed==rocks&&originalPlayer.Stamina==stamina&&originalLocation.objects.Pairs.Count()==originalObjects&&ReferenceEquals(Game1.player,originalPlayer)&&ReferenceEquals(Game1.currentLocation,originalLocation)&&ReferenceEquals(Game1.random,random);
            farmer?.FarmerRenderer.unload();
        }
        Helper.Data.WriteJsonFile("mining-interaction-checks.json",new{Passed=error==null&&checks.Values.All(v=>v),Error=error,Checks=checks,Cases=cases,Scope="Actual Pickaxe.DoFunction native stone/ore/gem removal and drops, native ResourceClump672 upgrade rejection/destruction through GameLocation.performToolAction. Disposable location with native mine20 map; fresh player; no map/save persistence.",Limitations="Generic GameLocation native drops, not generated MineShaft ladder/level-specific drop logic. Does not test every node/clump ID or multiplayer. Native clump sounds may play during this authorized interaction."});
    }
}
