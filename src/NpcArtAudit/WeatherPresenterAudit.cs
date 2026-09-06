using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Network;
using StardewValley.Objects;

namespace NpcArtAudit;

// Integration draft only. Invoke synchronously on the game thread in an isolated,
// single-player audit session. Never schedule a save or advance the game clock here.
internal static class WeatherPresenterAudit
{
    public static void Run(IModHelper helper, IMonitor monitor)
    {
        if (Game1.IsMultiplayer || Context.IsMultiplayer)
        {
            helper.Data.WriteJsonFile("weather-presenter-checks.json", new { Passed = false, Error = "This temporary weather fixture must run outside multiplayer." });
            return;
        }

        var menuField = typeof(Game1).GetField("_activeClickableMenu", BindingFlags.Static | BindingFlags.NonPublic)!;
        var oldMenu = Game1.activeClickableMenu;
        var oldLocation = Game1.currentLocation;
        var oldAfter = Game1.afterDialogues;
        var oldDialogue = Game1.dialogueUp;
        var farmer = Game1.player;
        var oldMove = farmer.CanMove;
        var oldCursor = Game1.mouseCursorTransparency;
        var oldRandom = Game1.random;
        var oldDay = Game1.dayOfMonth;
        var oldSeason = Game1.season;
        var oldYear = Game1.year;
        var oldDaysPlayed = Game1.stats.DaysPlayed;
        var oldTomorrow = Game1.weatherForTomorrow;
        var world = Game1.netWorldState.Value;
        var oldNetTomorrow = world.WeatherForTomorrow;
        var contexts = world.LocationWeather;
        bool hadDefault = contexts.TryGetValue("Default", out var oldDefault);
        bool hadIsland = contexts.TryGetValue("Island", out var oldIsland);
        bool hadIslandMail = farmer.mailReceived.Contains("Visited_Island");
        // Use a separate light dictionary so even a fixture light ID collision
        // cannot overwrite or remove the player's existing light source.
        var oldLights = Game1.currentLightSources;
        var cases = new List<object>();
        string? error = null;
        int scheduledGreenDay = 0;
        bool fixtureStarted = false;

        try
        {
            fixtureStarted = true;
            Game1.random = new Random(210501);
            Game1.currentLightSources = new Dictionary<string, LightSource>();
            var defaultWeather = new LocationWeather { Weather = "Sun", WeatherForTomorrow = "Sun" };
            var islandWeather = new LocationWeather { Weather = "Sun", WeatherForTomorrow = "Rain" };
            contexts["Default"] = defaultWeather;
            contexts["Island"] = islandWeather;
            // The currentLocation setter raises LocationChanged and mutates
            // visited-location/event state. This isolated fixture needs neither.
            Game1.game1.instanceGameLocation = new GameLocation("Maps/FarmHouse", "FarmHouse");
            Require(Game1.currentLocation.GetLocationContextId() == "Default", "Fixture map no longer uses Default weather context");
            Game1.season = Season.Spring;
            Game1.dayOfMonth = 6;
            Game1.stats.DaysPlayed = 100;
            Game1.weatherForTomorrow = world.WeatherForTomorrow = "Sun";
            if (!hadIslandMail) farmer.mailReceived.Add("Visited_Island");

            var panelType = AppDomain.CurrentDomain.GetAssemblies().Single(a => a.GetName().Name == "AbigailModern")
                .GetType("AbigailModern.PortraitPanel")!;
            var table = panelType.GetField("Panels", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            // Load only after the first native menu exists, so the pre-hook red
            // result identifies missing portrait routing rather than a missing asset.
            Texture2D? expectedPortrait = null;
            var callbackMethod = typeof(TV).GetMethod(nameof(TV.proceedToNextScene))!;

            TemporaryAnimatedSprite? Screen(TV tv) => helper.Reflection.GetField<TemporaryAnimatedSprite?>(tv, "screen").GetValue();
            TemporaryAnimatedSprite? Overlay(TV tv) => helper.Reflection.GetField<TemporaryAnimatedSprite?>(tv, "screenOverlay").GetValue();
            int Channel(TV tv) => helper.Reflection.GetField<int>(tv, "currentChannel").GetValue();
            void ClearFixtureMenu()
            {
                // Avoid emergencyShutDown on the previous fixture box, which
                // would be a second, unintended dialogue transition.
                menuField.SetValue(null, null);
                Game1.dialogueUp = false;
            }
            void CheckCallback(TV tv)
            {
                var callback = Game1.afterDialogues;
                Require(callback != null && callback.GetInvocationList().Length == 1 && ReferenceEquals(callback.Target, tv)
                    && callback.Method == callbackMethod, "Original TV proceedToNextScene callback changed");
            }
            void Advance(TV tv)
            {
                CheckCallback(tv);
                var callback = Game1.afterDialogues!;
                ClearFixtureMenu();
                callback();
            }
            string NativeText(TV tv, string method) => (string)typeof(TV)
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null)!.Invoke(tv, null)!;
            string ExpectedForecast(TV tv, string method, int seed)
            {
                Game1.random = new Random(seed);
                string expected = Game1.parseText(NativeText(tv, method));
                // The next native callback receives the exact same starting RNG.
                Game1.random = new Random(seed);
                return expected;
            }
            DialogueBox CheckDialogue(TV tv, string expected, int? expression)
            {
                var box = Game1.activeClickableMenu as DialogueBox ?? throw new Exception("Native object dialogue missing");
                Require(box.characterDialogue == null && !box.isQuestion, "Weather text was replaced with NPC/question dialogue");
                // DialogueBox(string) uses Split('#'); compare every page exactly,
                // including punctuation and the native parseText line wrapping.
                Require(box.dialogues.SequenceEqual(expected.Split('#')), "Native weather message/pages changed");
                Require(Game1.dialogueUp && !farmer.CanMove, "Native dialogue/movement flags changed");
                CheckCallback(tv);
                var args = new object?[] { box, null };
                bool found = (bool)table.GetType().GetMethod("TryGetValue")!.Invoke(table, args)!;
                if (expression == null)
                    Require(!found, "Static channel 9999 must not have a presenter portrait");
                else
                {
                    Require(found, "Missing Weather Report portrait");
                    var panel = args[1]!;
                    Require((string)panel.GetType().GetProperty("Name")!.GetValue(panel)! == "Weather Report", "Presenter display name changed");
                    Require((int)panel.GetType().GetProperty("Emotion")!.GetValue(panel)! == expression.Value, "Wrong weather portrait expression");
                    var texture = (Texture2D)panel.GetType().GetProperty("Texture")!.GetValue(panel)!;
                    expectedPortrait ??= helper.GameContent.Load<Texture2D>("Portraits/WeatherPresenter");
                    Require(texture.Width == 128 && texture.Height == 128 && expectedPortrait.Width == 128 && expectedPortrait.Height == 128,
                        "Expected four 64px portrait cells in a 128x128 atlas");
                    var actual = new Color[128 * 128];
                    var expectedPixels = new Color[actual.Length];
                    texture.GetData(actual);
                    expectedPortrait.GetData(expectedPixels);
                    Require(actual.SequenceEqual(expectedPixels), "Portrait pixels differ from Portraits/WeatherPresenter");
                }
                return box;
            }
            void CheckSprite(TemporaryAnimatedSprite? sprite, TV tv, string texture, Rectangle rect, float interval, int frames, bool overlay = false)
            {
                Require(sprite != null, "Native screen/overlay missing");
                Require(sprite!.textureName.Replace('\\', '/') == texture && sprite.sourceRect == rect
                    && sprite.sourceRectStartingPos == new Vector2(rect.X, rect.Y)
                    && sprite.interval == interval && sprite.animationLength == frames && sprite.totalNumberOfLoops == 999999,
                    "Native screen/overlay texture, rectangle or animation changed");
                Require(sprite.scale == tv.getScreenSizeModifier()
                    && sprite.position == tv.getScreenPosition() + (overlay ? new Vector2(3, 3) * tv.getScreenSizeModifier() : Vector2.Zero),
                    "Native screen/overlay placement or scale changed");
                Require(sprite.color == Color.White && !sprite.flipped && sprite.rotation == 0 && sprite.rotationChange == 0
                    && sprite.alphaFade == 0 && sprite.scaleChange == 0, "Native screen/overlay tint or transform changed");
            }
            void Record(string route, TV tv, DialogueBox box, int? expression)
            {
                var screen = Screen(tv)!;
                var overlay = Overlay(tv);
                string file = "weather-presenter-" + route + "-runtime-preview.png";
                Capture(helper, panelType, box, screen, overlay, file);
                cases.Add(new
                {
                    Route = route, Channel = Channel(tv), Portrait = expression, NativePages = box.dialogues.ToArray(),
                    NativeCallback = nameof(TV.proceedToNextScene), ScreenTexture = screen.textureName,
                    ScreenRectangle = new[] { screen.sourceRect.X, screen.sourceRect.Y, screen.sourceRect.Width, screen.sourceRect.Height },
                    ScreenId = screen.id, ScreenInterval = screen.interval, ScreenFrames = screen.animationLength,
                    OverlayTexture = overlay?.textureName,
                    OverlayRectangle = overlay == null ? null : new[] { overlay.sourceRect.X, overlay.sourceRect.Y, overlay.sourceRect.Width, overlay.sourceRect.Height },
                    OverlayInterval = overlay?.interval, OverlayFrames = overlay?.animationLength,
                    Preview = file
                });
            }
            TV Open(string route = "opening")
            {
                ClearFixtureMenu();
                var tv = (TV)ItemRegistry.Create("(F)1468");
                tv.Location = Game1.currentLocation;
                tv.selectChannel(farmer, "Weather");
                Require(Channel(tv) == 2, "Expected native Weather channel 2");
                var box = CheckDialogue(tv, Game1.parseText(NativeText(tv, "getWeatherChannelOpening")), 1);
                CheckSprite(Screen(tv), tv, "LooseSprites/Cursors", new Rectangle(413, 305, 42, 28), 150, 2);
                Require(Overlay(tv) == null, "Opening unexpectedly contains a forecast overlay");
                Require(Game1.currentLightSources.Count == 1, "Native TV screen light missing");
                Record(route, tv, box, 1);
                return tv;
            }
            void TurnOff(TV tv)
            {
                Advance(tv);
                Require(Channel(tv) == 0 && Screen(tv) == null && Overlay(tv) == null, "Native TV turn-off changed");
                Require(Game1.currentLightSources.Count == 0, "Native TV turn-off left its screen light behind");
                Require(Game1.activeClickableMenu == null, "Turn-off unexpectedly opened another dialogue");
            }
            void RequireTomorrow(string weather)
            {
                var tomorrow = new WorldDate(Game1.Date);
                tomorrow.TotalDays++;
                Require(Game1.getWeatherModificationsForDate(tomorrow, Game1.IsMasterGame ? Game1.weatherForTomorrow : world.WeatherForTomorrow) == weather,
                    "Fixture forecast was overridden by date/festival/weather data");
            }

            // Ordinary forecast uses native random Sun wording. Island Rain is
            // deliberately different so an accidental mainland overlay is caught.
            Require(!Game1.IsGreenRainingHere(), "Fixture unexpectedly has current green rain");
            RequireTomorrow("Sun");
            var ordinary = Open();
            string expected = ExpectedForecast(ordinary, "getWeatherForecast", 210502);
            Advance(ordinary);
            var ordinaryBox = CheckDialogue(ordinary, expected, 0);
            Require(Channel(ordinary) == 2 && Screen(ordinary)!.id == 777, "Ordinary forecast lost native id 777");
            CheckSprite(Screen(ordinary), ordinary, "LooseSprites/Cursors", new Rectangle(497, 305, 42, 28), 9999, 1);
            CheckSprite(Overlay(ordinary), ordinary, "LooseSprites/Cursors", new Rectangle(413, 333, 13, 13), 100, 4, true);
            Record("ordinary", ordinary, ordinaryBox, 0);

            expected = ExpectedForecast(ordinary, "getIslandWeatherForecast", 210503);
            Advance(ordinary);
            var islandBox = CheckDialogue(ordinary, expected, 0);
            Require(Channel(ordinary) == 2 && Screen(ordinary)!.id == 0, "Island forecast route/id changed");
            CheckSprite(Screen(ordinary), ordinary, "LooseSprites/Cursors2", new Rectangle(148, 62, 42, 28), 9999, 1);
            CheckSprite(Overlay(ordinary), ordinary, "LooseSprites/Cursors", new Rectangle(465, 333, 13, 13), 70, 4, true);
            Record("island", ordinary, islandBox, 0);
            TurnOff(ordinary);

            // Keep the real year/save ID; derive its scheduled summer date.
            Game1.season = Season.Summer;
            scheduledGreenDay = Enumerable.Range(1, 28).Single(day => Utility.isGreenRainDay(day, Season.Summer));
            Game1.dayOfMonth = scheduledGreenDay - 1;
            RequireTomorrow("GreenRain");
            var green = Open("before-greenrain-opening");
            expected = ExpectedForecast(green, "getWeatherForecast", 210504);
            Require(expected == Game1.parseText(Game1.content.LoadString("Strings\\1_6_Strings:GreenRainForecast")), "Native green-rain wording differs");
            Advance(green);
            var greenBox = CheckDialogue(green, expected, 3);
            Require(Channel(green) == 2 && Screen(green)!.id == 776, "Green-rain forecast lost native id 776");
            CheckSprite(Screen(green), green, "LooseSprites/Cursors_1_6", new Rectangle(213, 335, 43, 28), 9999, 1);
            CheckSprite(Overlay(green), green, "LooseSprites/Cursors_1_6", new Rectangle(178, 363, 13, 13), 80, 6, true);
            Record("tomorrow-greenrain", green, greenBox, 3);
            // Visited_Island remains true: id 776 must bypass the island segment.
            TurnOff(green);

            Game1.dayOfMonth = scheduledGreenDay;
            defaultWeather.Weather = "GreenRain";
            defaultWeather.IsGreenRain = true; // Native setter also sets IsRaining.
            Require(defaultWeather.IsRaining && Game1.IsGreenRainingHere(), "Current-day green-rain context was not established");
            ClearFixtureMenu();
            var staticTv = (TV)ItemRegistry.Create("(F)1468");
            staticTv.Location = Game1.currentLocation;
            staticTv.selectChannel(farmer, "Weather");
            Require(Channel(staticTv) == 9999, "Current green rain must override requested Weather with static channel 9999");
            var staticBox = CheckDialogue(staticTv, "...................", null);
            CheckSprite(Screen(staticTv), staticTv, "LooseSprites/Cursors_1_6", new Rectangle(386, 334, 42, 28), 40, 3);
            Require(Overlay(staticTv) == null, "Static channel unexpectedly has a weather overlay");
            Record("current-day-static", staticTv, staticBox, null);
            TurnOff(staticTv);
        }
        catch (Exception ex)
        {
            error = ex.ToString();
            monitor.Log("Weather presenter audit failed: " + ex, LogLevel.Error);
        }
        finally
        {
            if (fixtureStarted)
            {
                if (hadDefault) contexts["Default"] = oldDefault!; else contexts.Remove("Default");
                if (hadIsland) contexts["Island"] = oldIsland!; else contexts.Remove("Island");
                if (!hadIslandMail) farmer.mailReceived.Remove("Visited_Island");
                Game1.weatherForTomorrow = oldTomorrow;
                world.WeatherForTomorrow = oldNetTomorrow;
                Game1.year = oldYear;
                Game1.season = oldSeason;
                Game1.dayOfMonth = oldDay;
                Game1.stats.DaysPlayed = oldDaysPlayed;
                Game1.currentLightSources = oldLights;
                menuField.SetValue(null, oldMenu);
                Game1.game1.instanceGameLocation = oldLocation;
                Game1.afterDialogues = oldAfter;
                Game1.dialogueUp = oldDialogue;
                farmer.CanMove = oldMove;
                Game1.mouseCursorTransparency = oldCursor;
                Game1.random = oldRandom;
            }
        }

        bool restored = ReferenceEquals(Game1.random, oldRandom) && ReferenceEquals(Game1.currentLightSources, oldLights)
            && ReferenceEquals(Game1.activeClickableMenu, oldMenu) && ReferenceEquals(Game1.currentLocation, oldLocation)
            && Game1.afterDialogues == oldAfter && Game1.dialogueUp == oldDialogue && farmer.CanMove == oldMove
            && Game1.mouseCursorTransparency == oldCursor && Game1.dayOfMonth == oldDay && Game1.season == oldSeason
            && Game1.year == oldYear && Game1.stats.DaysPlayed == oldDaysPlayed && Game1.weatherForTomorrow == oldTomorrow
            && world.WeatherForTomorrow == oldNetTomorrow && farmer.mailReceived.Contains("Visited_Island") == hadIslandMail
            && (hadDefault ? ReferenceEquals(contexts["Default"], oldDefault) : !contexts.ContainsKey("Default"))
            && (hadIsland ? ReferenceEquals(contexts["Island"], oldIsland) : !contexts.ContainsKey("Island"));
        helper.Data.WriteJsonFile("weather-presenter-checks.json", new
        {
            Passed = error == null && restored, Error = error, StateRestored = restored, Cases = cases,
            ScheduledSummerGreenRainDay = scheduledGreenDay, ForecastRandomness = "Fresh seeded generator before native expectation and callback; original generator restored",
            FarmLoaded = false, SaveWritten = false, FullFurnitureScene = false, TimedAnimationPlayback = false,
            NativeCallbackInvokedDirectly = true, RealInputDismissalTested = false
        });
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Capture(IModHelper helper, Type panelType, DialogueBox box, TemporaryAnimatedSprite screen,
        TemporaryAnimatedSprite? overlay, string file)
    {
        var device = Game1.graphics.GraphicsDevice;
        var previousTargets = device.GetRenderTargets();
        using var target = new RenderTarget2D(device, Game1.uiViewport.Width, Game1.uiViewport.Height);
        using var batch = new SpriteBatch(device);
        bool begun = false;
        try
        {
            device.SetRenderTarget(target);
            device.Clear(new Color(35, 45, 58));
            batch.Begin(samplerState: SamplerState.PointClamp);
            begun = true;
            batch.Draw(screen.texture, new Rectangle(80, 60, screen.sourceRect.Width * 6, screen.sourceRect.Height * 6), screen.sourceRect, Color.White);
            if (overlay != null)
            {
                var offset = (overlay.position - screen.position) / screen.scale * 6;
                batch.Draw(overlay.texture, new Rectangle(80 + (int)offset.X, 60 + (int)offset.Y,
                    overlay.sourceRect.Width * 6, overlay.sourceRect.Height * 6), overlay.sourceRect, Color.White);
            }
            box.transitioning = false;
            box.transitionInitialized = true;
            box.characterIndexInDialogue = box.getCurrentString().Length;
            box.draw(batch);
            panelType.GetMethod("Render")!.Invoke(null, new object[] { batch, box });
            batch.End();
            begun = false;
            device.SetRenderTargets(previousTargets);
            using var output = File.Create(Path.Combine(helper.DirectoryPath, file));
            target.SaveAsPng(output, target.Width, target.Height);
        }
        finally
        {
            try { if (begun) batch.End(); }
            finally { device.SetRenderTargets(previousTargets); }
        }
    }
}
