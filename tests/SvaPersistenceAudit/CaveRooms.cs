using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
namespace SvaPersistenceAudit;
public sealed partial class ModEntry
{
    private int caveTargetLevel, caveSettledTicks;
    private string? caveCaptureName;
    private bool? cavePreviousEnabled;
    private string? namedCapture;
    private void EnterCaveRoom(int level)
    {
        RequireOwnedWorld();if(Game1.IsMultiplayer)throw new InvalidOperationException("Cave room capture requires owned single-player world.");
        if(level is not (15 or 20 or 60 or 100))throw new InvalidOperationException("Unsupported fixture floor.");
        Game1.activeClickableMenu=null;Game1.enterMine(level);
        caveTargetLevel=level;caveSettledTicks=0;caveCaptureName=$"cave-room-{level}-native.png";
        Record("CaveRoomRequested:"+level);
    }
    private object CaveSettings()
    {
        var type=Mod("David.AbigailModern").GetType().Assembly.GetType("AbigailModern.Visuals.CaveAtmosphereController")!;
        var owner=type.GetField("audioOwner",BindingFlags.Static|BindingFlags.NonPublic)!.GetValue(null)??throw new InvalidOperationException("Cave controller audio owner is not initialized; cannot claim native runtime integration.");
        return type.GetField("settings",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(owner)!;
    }
    private void SetCaveEffects(bool enabled)
    {
        RequireOwnedWorld();if(Game1.currentLocation is not MineShaft mine)throw new InvalidOperationException("Enter a native cave room first.");
        var settings=CaveSettings();var property=settings.GetType().GetProperty("Enabled")!;
        cavePreviousEnabled??=(bool)property.GetValue(settings)!;property.SetValue(settings,enabled);
        caveTargetLevel=mine.mineLevel;caveSettledTicks=0;caveCaptureName=$"cave-room-{mine.mineLevel}-effects-{(enabled?"on":"off")}.png";
        Record("CaveEffects:"+enabled);
    }
    private void WatchCaveRoom()
    {
        if(caveCaptureName==null)return;
        if(!Context.IsWorldReady || Game1.currentLocation is not MineShaft mine || mine.mineLevel!=caveTargetLevel || Game1.fadeToBlackAlpha>0){caveSettledTicks=0;return;}
        if(++caveSettledTicks<120)return;
        namedCapture=caveCaptureName;capturePending=true;caveCaptureName=null;
        var settings=CaveSettings();
        bool dripHook=Harmony.GetPatchInfo(typeof(MineShaft).GetMethod(nameof(MineShaft.UpdateWhenCurrentLocation),new[]{typeof(Microsoft.Xna.Framework.GameTime)})!)?.Transpilers.Any(p=>p.owner=="David.AbigailModern.CaveDrip")==true;
        WriteProfile($"cave-room-{mine.mineLevel}-state-{sequence:D4}.json",new {mine.mineLevel,Location=mine.NameOrUniqueName,PlayerTile=new {X=Game1.player.TilePoint.X,Y=Game1.player.TilePoint.Y},Viewport=new {Game1.viewport.X,Game1.viewport.Y,Game1.viewport.Width,Game1.viewport.Height},Ambient=new {Game1.ambientLight.R,Game1.ambientLight.G,Game1.ambientLight.B,Game1.ambientLight.A},NativeLights=Game1.currentLightSources.Count,Objects=mine.objects.Pairs.Count(),Clumps=mine.resourceClumps.Count,DripHookInstalled=dripHook,ControllerOwnerReady=true,EffectsEnabled=settings.GetType().GetProperty("Enabled")!.GetValue(settings),LocalizedDrips=settings.GetType().GetProperty("LocalizedDrips")!.GetValue(settings),Strength=settings.GetType().GetProperty("Strength")!.GetValue(settings),Game1.options.soundVolumeLevel,Game1.options.ambientVolumeLevel,Game1.options.musicVolumeLevel,SoundMasterVolume=Microsoft.Xna.Framework.Audio.SoundEffect.MasterVolume,Capture=namedCapture,Scope="Actual native generated mine and full backbuffer; no screenshot mode, no save command, no synthetic sound generation. Human audible verification still pending."});
    }
    private void LeaveCaveRoom()
    {
        RequireOwnedWorld();caveCaptureName=null;
        if(cavePreviousEnabled.HasValue){var settings=CaveSettings();settings.GetType().GetProperty("Enabled")!.SetValue(settings,cavePreviousEnabled.Value);cavePreviousEnabled=null;}
        Game1.warpFarmer(Utility.getHomeOfFarmer(Game1.player).NameOrUniqueName,5,9,2);
        Record("CaveRoomReturnedHome");
    }
}
