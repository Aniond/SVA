using HarmonyLib;
using StardewModdingAPI;
using StardewValley;

namespace AbigailModern;

/// <summary>Opt-in local preference: mute music without altering ambient/sound channels or save files.</summary>
internal static class MusicPreference
{
    private sealed class Settings { public bool MuteMusic { get; set; } }
    private static bool muted;
    public static void Initialize(IModHelper helper,IMonitor monitor,string id)
    {
        try {muted=helper.Data.ReadJsonFile<Settings>("audio-preferences.json")?.MuteMusic??false;}
        catch(Exception ex){monitor.Log("Could not read audio-preferences.json: "+ex.Message,LogLevel.Warn);return;}
        if(!muted)return;
        var harmony=new Harmony(id+".MusicPreference");
        foreach(var method in AccessTools.GetDeclaredMethods(typeof(Game1)).Where(m=>m.Name is "updateMusic" or "changeMusicTrack"))
            harmony.Patch(method,prefix:new HarmonyMethod(typeof(MusicPreference),nameof(Apply)));
        helper.Events.GameLoop.GameLaunched+=(_,_)=>Apply();
        void Report(string phase)
        {
            helper.Data.WriteJsonFile("audio-runtime-status.json",new {Phase=phase,CheckedAt=DateTime.UtcNow,
                MusicVolume=Game1.options.musicVolumeLevel,MusicPlayerVolume=Game1.musicPlayerVolume,
                SoundVolume=Game1.options.soundVolumeLevel,AmbientVolume=Game1.options.ambientVolumeLevel});
            monitor.Log($"Music mute verified ({phase}): music {Game1.options.musicVolumeLevel}, sound {Game1.options.soundVolumeLevel}, ambience {Game1.options.ambientVolumeLevel}.",LogLevel.Info);
        }
        helper.Events.GameLoop.SaveLoaded+=(_,_)=>{Apply();Report("save-loaded");};
        bool reported=false;
        helper.Events.GameLoop.UpdateTicked+=(_,e)=>
        {
            Apply();
            if(!reported&&e.Ticks>=120&&Game1.activeClickableMenu is StardewValley.Menus.TitleMenu)
            {reported=true;Report("title");}
        };
        monitor.Log("Local music-only mute enabled; sound effects and ambience retain their settings.",LogLevel.Info);
    }
    private static void Apply()
    {
        if(!muted||Game1.options==null)return;
        Game1.options.musicVolumeLevel=0;
        Game1.musicPlayerVolume=0;
        if(Game1.soundBank!=null)Game1.musicCategory.SetVolume(0);
    }
}
