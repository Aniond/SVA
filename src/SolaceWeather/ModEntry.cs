using SolaceWeather.Core;
using SolaceWeather.Festivals;
using SolaceWeather.Integration;
using SolaceWeather.Presentation;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace SolaceWeather;

public sealed class ModEntry : Mod
{
    private WeatherRuntime runtime = null!;
    private WeatherUi ui = null!;
    private ModConfig config = null!;
    private Controls.MovementControls? movement;
    private Controls.SmartToolSelection? smartTools;
    private Relationships.AbigailRelationship? abigail;
    private Relationships.NpcConversation? abigailConversation;
    private Relationships.RomanceService? romance;

    public override void Entry(IModHelper helper)
    {
        config = helper.ReadConfig<ModConfig>();
        abigail = new Relationships.AbigailRelationship(helper, Monitor, config);
        _ = new Relationships.AbigailDeliveryQuest(helper, config, abigail);
        var tree = new Relationships.AbigailTreeService(helper, Monitor, abigail);
        romance = new Relationships.RomanceService(helper, Monitor, config, abigail, ModManifest.UniqueID);
        Relationships.RelationshipSocialEntry.Install(ModManifest.UniqueID, Monitor, () => romance.Ready, romance.OpenJournal,
            name => romance.State.GetJourney(name, Game1.Date.TotalDays).BondStage);
        abigailConversation = new Relationships.NpcConversation(helper, Monitor, config, abigail, romance);
        romance.OpenConversation = abigailConversation.Start;
        ClimateSettings climate;
        try
        {
            climate = helper.Data.ReadJsonFile<ClimateSettings>("assets/climate.json") ?? new ClimateSettings();
            _ = new WeatherEngine(0, climate);
        }
        catch (Exception ex)
        {
            Monitor.Log($"Invalid climate settings; using built-in defaults. {ex.Message}", LogLevel.Warn);
            climate = new ClimateSettings();
        }
        runtime = new WeatherRuntime(helper, Monitor, config, climate);
        ui = new WeatherUi(runtime, helper.Translation);
        var festivals = new EggFestivalController(helper, Monitor, () => runtime.Enabled, () => runtime.GetCurrent(Region.Town));
        runtime.Compatible = GamePatches.Install(ModManifest.UniqueID, runtime, ui, festivals, Monitor);
        helper.Events.GameLoop.SaveLoaded += (_, _) => Safe(() =>
        {
            runtime.Load();
            if (runtime.Enabled)
                Utility.ForEachCharacter(npc => { if (npc.IsVillager) npc.TryLoadSchedule(); return true; });
        });
        helper.Events.GameLoop.DayStarted += (_, _) => Safe(() => { if (runtime.State == null) runtime.Load(); runtime.StartDay(); });
        helper.Events.GameLoop.DayEnding += (_, _) => Safe(runtime.EndDay);
        helper.Events.GameLoop.Saving += (_, _) => Safe(runtime.Save);
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => runtime.Reset();
        helper.Events.GameLoop.TimeChanged += (_, _) => Safe(() => { runtime.Refresh(); runtime.WaterThrough(runtime.Minute); });
        helper.Events.GameLoop.UpdateTicked += (_, e) =>
        {
            if (e.IsMultipleOf(60) && Context.IsWorldReady && Game1.shouldTimePass()) Safe(runtime.Refresh);
        };
        helper.Events.Player.Warped += (_, _) => Safe(runtime.Refresh);
        helper.Events.Display.RenderedHud += (_, e) =>
        {
            if (Context.IsWorldReady && runtime.Engine != null && !Context.IsMultiplayer && !Game1.eventUp) ui.DrawHud(e.SpriteBatch);
        };
        helper.Events.Display.RenderedWorld += (_, e) =>
        {
            if (!runtime.Enabled || !Context.IsWorldReady || !Game1.currentLocation.IsOutdoors) return;
            // A translucent layer uses the game's own solid texture, providing cloud
            // shading without changing map assets or accumulating lighting changes.
            var sample = runtime.GetCurrent(runtime.ActiveRegion);
            if (runtime.Protection(runtime.Day, runtime.ActiveRegion) == null && sample.CloudCover > .35)
                e.SpriteBatch.Draw(Game1.fadeToBlackRect, new Microsoft.Xna.Framework.Rectangle(0, 0, Game1.viewport.Width, Game1.viewport.Height), Microsoft.Xna.Framework.Color.DarkSlateGray * (float)(sample.CloudCover * .12));
        };
        helper.Events.Input.ButtonPressed += OnButton;
        if (Game1.version == "1.6.15" && Constants.ApiVersion.ToString() == "4.5.2") _ = new Controls.QuickStack(helper, Monitor, config);
        if (Game1.version == "1.6.15") movement = new Controls.MovementControls(helper, Monitor, config, ui.HitTestHud);
        if (movement != null) movement.AfterNpcInteraction = abigailConversation.AfterInteraction;
        Controls.MouseToolInput.Install(ModManifest.UniqueID, config, Monitor);
        Controls.CropProtection.Install(ModManifest.UniqueID, helper, config, Monitor);
        Controls.NearbyCrafting.Install(ModManifest.UniqueID, config, Monitor);
        if (Game1.version == "1.6.15" && Constants.ApiVersion.ToString() == "4.5.2")
            smartTools = new Controls.SmartToolSelection(helper, Monitor, config, ui.HitTestHud, movement);
        if (movement != null) _ = new Controls.ClickFeedback(helper, config, movement, ui.HitTestHud);
        if (movement != null) _ = new Controls.MachineIndicators(helper, config);
        helper.ConsoleCommands.Add("solace_weather", "Weather development controls (DeveloperMode required): status | force <region> <kind|auto> | journal | enable <true|false>", OnCommand);
    }

    private void OnButton(object? sender, ButtonPressedEventArgs e)
    {
        if (!Context.IsWorldReady || runtime.Engine == null || Context.IsMultiplayer || Game1.activeClickableMenu != null || Game1.eventUp) return;
        if (e.Button == config.JournalKey || (e.Button == SButton.MouseLeft && ui.HitTestHud(Game1.getMouseX(true), Game1.getMouseY(true))))
        { Helper.Input.Suppress(e.Button); ui.OpenJournal(); }
    }

    private void OnCommand(string name, string[] args)
    {
        if (!config.DeveloperMode) { Monitor.Log("Set DeveloperMode to true in config.json and restart to use test controls.", LogLevel.Info); return; }
        if (!Context.IsWorldReady || runtime.Engine == null) { Monitor.Log("Load the test farm first.", LogLevel.Info); return; }
        Safe(() =>
        {
            switch (args.FirstOrDefault()?.ToLowerInvariant())
            {
                case "journal": ui.OpenJournal(); break;
                case "enable" when args.Length == 2 && bool.TryParse(args[1], out var enabled):
                    runtime.RequestEnabled(enabled); Monitor.Log("Weather change queued for next morning.", LogLevel.Info); break;
                case "force" when args.Length == 3 && Enum.TryParse<Region>(args[1], true, out var region) && Enum.IsDefined(typeof(Region), region):
                    if (args[2].Equals("auto", StringComparison.OrdinalIgnoreCase)) runtime.Force(region, null);
                    else if (Enum.TryParse<WeatherKind>(args[2], true, out var kind) && Enum.IsDefined(typeof(WeatherKind), kind)) runtime.Force(region, kind);
                    else { Monitor.Log("Unknown weather kind.", LogLevel.Warn); break; }
                    Monitor.Log("Override updated. Protected special days remain protected. Overrides expire next morning.", LogLevel.Info); break;
                default:
                    Monitor.Log($"Farm={Game1.player.farmName.Value}; enabled={runtime.Enabled}; pending={runtime.PendingEnabled}; day={runtime.Day}; time={Game1.timeOfDay}; region={runtime.ActiveRegion}", LogLevel.Info);
                    foreach (Region area in Enum.GetValues<Region>())
                    {
                        var sample = runtime.GetCurrent(area);
                        Monitor.Log($"{area}: {sample.Kind}, {sample.TemperatureC:F1}C, intensity={sample.RainIntensity:F2}, rain total={runtime.Rainfall(runtime.Day, runtime.Minute, area):F1}, tomorrow={runtime.GetForecast(area, 1).MainKind}", LogLevel.Info);
                    }
                    break;
            }
        });
    }
    private void Safe(Action action)
    { try { action(); } catch (Exception ex) { runtime.DisableForSession(ex); } }
}
