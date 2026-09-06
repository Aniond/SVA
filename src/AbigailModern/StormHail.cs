using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;

namespace AbigailModern;

/// <summary>Occasional cosmetic hail. Reads local weather; never writes weather or save state.</summary>
internal static class StormHail
{
    internal const string AssetName = "Mods/David.AbigailModern/Weather/Hail";
    internal const float Scale = 1.5f;
    private static readonly PerScreen<ScreenState> Screens = new(() => new ScreenState());
    private static Texture2D? texture;
    private static bool initialized, failed;

    internal sealed class ScreenState
    {
        public readonly Particle[] Particles = new Particle[24];
        public GameLocation? Location;
        public int Day = -1;
        public uint Seed, Random;
        public double SpawnTime;
    }

    internal struct Particle
    {
        public bool Active;
        public float X, Y, TargetY, Speed, Drift;
        public double ImpactAge;
    }

    public static void Initialize(IModHelper helper, IMonitor monitor)
    {
        if (initialized) return;
        initialized = true;
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => Screens.Value = new ScreenState();
        helper.Events.Content.AssetsInvalidated += (_, e) =>
        {
            if (e.NamesWithoutLocale.Any(name => name.IsEquivalentTo(AssetName)))
            {
                texture = null;
                failed = false;
            }
        };
        helper.Events.GameLoop.UpdateTicked += (_, _) =>
        {
            try
            {
                var state = Screens.Value;
                if (!ApplyEligibility(state, !failed && EligibleNow())) return;
                var location = Game1.currentLocation;
                int day = (int)Game1.stats.DaysPlayed;
                if (!ReferenceEquals(state.Location, location) || state.Day != day)
                {
                    ApplyEligibility(state, false);
                    state.Location = location;
                    state.Day = day;
                    state.Seed = StableSeed((long)Game1.uniqueIDForThisGame, day, location.NameOrUniqueName);
                    state.Random = state.Seed;
                }
                double dt = Math.Clamp(Game1.currentGameTime.ElapsedGameTime.TotalSeconds, 0, .1);
                Advance(state, dt);
                double minute = Game1.timeOfDay / 100 * 60 + Game1.timeOfDay % 100
                    + Math.Clamp(Game1.gameTimeInterval * 10d / Math.Max(1, Game1.realMilliSecondsPerGameTenMinutes), 0, 9.999);
                if (!IsBurst(minute, state.Seed)) { state.SpawnTime = 0; return; }
                state.SpawnTime += dt;
                if (state.SpawnTime >= .16)
                {
                    state.SpawnTime %= .16;
                    Spawn(state, location);
                }
            }
            catch (Exception ex) { Fail(monitor, ex); }
        };
        helper.Events.Display.RenderedWorld += (_, e) =>
        {
            try
            {
                var state = Screens.Value;
                if (!ApplyEligibility(state, !failed && EligibleNow())) return;
                if (!ReferenceEquals(state.Location, Game1.currentLocation)) return;
                if (!state.Particles.Any(p => p.Active)) return;
                texture ??= helper.GameContent.Load<Texture2D>(AssetName);
                if (texture.Width != 64 || texture.Height != 16)
                    throw new InvalidOperationException("Hail asset must contain four 16x16 frames in a 64x16 sheet.");
                foreach (var p in state.Particles)
                    if (p.Active)
                        DrawParticle(e.SpriteBatch, texture, p.X - Game1.viewport.X, p.Y - Game1.viewport.Y,
                            FrameAt(p.ImpactAge), Game1.viewport.Width, Game1.viewport.Height);
            }
            catch (Exception ex) { Fail(monitor, ex); }
        };
    }

    private static void Fail(IMonitor monitor, Exception ex)
    {
        if (!failed) monitor.Log($"Cosmetic hail disabled until its asset reloads: {ex.Message}", LogLevel.Warn);
        failed = true;
        ApplyEligibility(Screens.Value, false);
    }

    private static bool EligibleNow()
    {
        if (!Context.IsWorldReady || Game1.currentLocation == null) return false;
        var location = Game1.currentLocation;
        // GetWeather is the native local route, including SolaceWeather's effective-weather patch.
        var weather = location.GetWeather();
        return IsEligible(true, location.IsOutdoors, weather.IsRaining, weather.IsLightning,
            weather.IsSnowing, weather.IsGreenRain, Game1.eventUp,
            Game1.activeClickableMenu != null || Game1.dialogueUp || Game1.currentMinigame != null,
            Game1.shouldTimePass(), Game1.game1.takingMapScreenshot, location is Summit);
    }

    internal static bool IsEligible(bool ready, bool outdoors, bool rain, bool lightning, bool snow,
        bool green, bool eventUp, bool menu, bool timePassing, bool screenshot, bool summit)
        => ready && outdoors && rain && lightning && !snow && !green && !eventUp && !menu
            && timePassing && !screenshot && !summit;

    internal static bool ApplyEligibility(ScreenState state, bool eligible)
    {
        if (!eligible)
        {
            Array.Clear(state.Particles, 0, state.Particles.Length);
            state.SpawnTime = 0;
        }
        return eligible;
    }

    internal static uint StableSeed(long saveId, int day, string location)
    {
        unchecked
        {
            uint seed = 2166136261;
            foreach (char c in location) seed = (seed ^ c) * 16777619;
            seed = (seed ^ (uint)saveId) * 16777619;
            seed = (seed ^ (uint)(saveId >> 32)) * 16777619;
            seed = (seed ^ (uint)day) * 16777619;
            return seed == 0 ? 1u : seed;
        }
    }

    internal static bool IsBurst(double minute, uint seed)
        => double.IsFinite(minute) && ((minute + seed % 120) % 120 + 120) % 120 < 12;

    internal static int FrameAt(double impactAge)
        => impactAge < 0 ? 0 : impactAge < .09 ? 1 : impactAge < .18 ? 2 : impactAge < .27 ? 3 : -1;

    internal static bool Visible(float x, float y, int width, int height, float scale)
        => x - 8 * scale >= 0 && y - 12 * scale >= 0 && x + 8 * scale <= width && y + 4 * scale <= height;

    internal static bool DrawParticle(SpriteBatch batch, Texture2D sheet, float x, float y, int frame, int width, int height)
    {
        if (frame < 0 || frame > 3 || !Visible(x, y, width, height, Scale)) return false;
        batch.Draw(sheet, new Vector2(x, y), new Rectangle(frame * 16, 0, 16, 16),
            Color.White * .85f, 0, new Vector2(8, 12), Scale, SpriteEffects.None, 1);
        return true;
    }

    internal static void Advance(ScreenState state, double dt)
    {
        dt = Math.Clamp(dt, 0, .1);
        for (int i = 0; i < state.Particles.Length; i++)
        {
            ref var p = ref state.Particles[i];
            if (!p.Active) continue;
            if (p.ImpactAge < 0)
            {
                p.Y += p.Speed * (float)dt;
                p.X += p.Drift * (float)dt;
                if (p.Y >= p.TargetY) { p.Y = p.TargetY; p.ImpactAge = 0; }
            }
            else p.ImpactAge += dt;
            if (FrameAt(p.ImpactAge) < 0) p.Active = false;
        }
    }

    private static float Next(ScreenState state)
    {
        uint n = state.Random;
        n ^= n << 13; n ^= n >> 17; n ^= n << 5;
        state.Random = n;
        return (n >> 8) / 16777216f;
    }

    private static void Spawn(ScreenState state, GameLocation location)
    {
        var view = Game1.viewport;
        if (view.Width < 64 || view.Height < 64) return;
        int index = Array.FindIndex(state.Particles, p => !p.Active);
        if (index < 0) return;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            float x = view.X + 32 + Next(state) * (view.Width - 64);
            float y = view.Y + 32 + Next(state) * (view.Height - 48);
            var tile = new Vector2((int)(x / 64), (int)(y / 64));
            if (!location.isTileOnMap(tile) || !location.hasTileAt(tile.ToPoint(), "Back")) continue;
            state.Particles[index] = new Particle { Active = true, X = x, Y = y - 80 - Next(state) * 80,
                TargetY = y, Speed = 240 + Next(state) * 100, Drift = -20 - Next(state) * 16, ImpactAge = -1 };
            break;
        }
    }
}
