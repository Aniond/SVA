using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace AbigailModern;

public sealed class ModEntry : Mod
{
    internal static bool EntryCompleted { get; private set; }
    private readonly Dictionary<string, IRawTextureData> artworkPixels = new(StringComparer.OrdinalIgnoreCase);

    private IRawTextureData GetArtworkPixels(string file)
    {
        if (!artworkPixels.TryGetValue(file, out var data))
        {
            data = Helper.ModContent.Load<IRawTextureData>(file);
            artworkPixels.Add(file, data);
        }
        return data;
    }
    public sealed class ArtAsset
    {
        public string Name { get; set; } = "";
        public string File { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public Rectangle? PatchArea { get; set; }
        public Rectangle[]? PatchAreas { get; set; }
        public IEnumerable<Rectangle> Areas => PatchAreas ?? (PatchArea is Rectangle area ? new[] { area } : new[] { new Rectangle(0, 0, Width, Height) });
        public bool IsPatch => PatchArea.HasValue || PatchAreas != null;
    }

    public override void Entry(IModHelper helper)
    {
        MusicPreference.Initialize(helper,Monitor,ModManifest.UniqueID);
        var artwork = helper.Data.ReadJsonFile<ArtAsset[]>("artwork.json")
            ?? throw new InvalidOperationException("Missing artwork.json.");
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var asset in artwork)
        {
            if (string.IsNullOrWhiteSpace(asset.Name) || !names.Add(asset.Name.Replace('\\', '/')))
                throw new InvalidOperationException($"Missing or duplicate asset name: {asset.Name}");
            if (asset.Width <= 0 || asset.Height <= 0 || Path.IsPathRooted(asset.File)
                || asset.File.Replace('\\', '/').Split('/').Contains("..") || !asset.File.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Invalid artwork entry: {asset.Name}");
            if (asset.PatchArea.HasValue && asset.PatchAreas != null || asset.PatchAreas?.Length == 0)
                throw new InvalidOperationException($"Ambiguous or empty patch regions: {asset.Name}");
            foreach (var area in asset.Areas)
                if (area.X < 0 || area.Y < 0 || area.Width <= 0 || area.Height <= 0 || area.Right > asset.Width || area.Bottom > asset.Height)
                    throw new InvalidOperationException($"Invalid patch bounds: {asset.Name}");
        }

        var artworkByName = artwork.ToDictionary(a => a.Name.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);
        helper.Events.Content.AssetRequested += (_, e) =>
        {
            // Prefer the prepared locale sheet so embedded labels retain their native language.
            if (artworkByName.TryGetValue(e.Name.Name.Replace('\\', '/'), out var asset)
                || artworkByName.TryGetValue(e.NameWithoutLocale.Name.Replace('\\', '/'), out asset))
            {
                if (asset.IsPatch)
                    e.Edit(target =>
                    {
                        var pixels = GetArtworkPixels(asset.File);
                        var image = target.AsImage();
                        foreach (var area in asset.Areas)
                            image.PatchImage(pixels, sourceArea: area, targetArea: area);
                    });
                else
                    e.LoadFromModFile<Texture2D>(asset.File, AssetLoadPriority.Medium);
                return;
            }
        };
        BlueUiText.Initialize(helper, Monitor, ModManifest.UniqueID);
        ModernClothingLoader.Initialize(helper, Monitor);
        PlayerHdRenderer.Initialize(helper, Monitor, ModManifest.UniqueID);
        PlayerCreatorPreview.Initialize(helper, Monitor, ModManifest.UniqueID);
        helper.Events.GameLoop.GameLaunched += (_, _) =>
        {
            var verified = 0;
            foreach (var asset in artwork)
            {
                try
                {
                    CheckAsset(asset);
                    var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(Helper.DirectoryPath, asset.File))));
                    Monitor.Log($"Verified {asset.Name}: {asset.Width}x{asset.Height} pixels, {(asset.IsPatch ? "patched region" : "full texture")} matches packaged art. SHA256 {hash}", LogLevel.Info);
                    verified++;
                }
                catch (Exception ex)
                {
                    Monitor.Log($"{asset.Name} artwork validation failed: {ex}", LogLevel.Error);
                }
            }
            Monitor.Log($"Registered asset verification: {verified}/{artwork.Length} passed. Full NPC coverage is tracked separately.", verified == artwork.Length ? LogLevel.Info : LogLevel.Error);
        };
        PortraitPanel.Initialize(helper, Monitor, ModManifest.UniqueID);
        KelPortraits.Initialize(helper, Monitor, ModManifest.UniqueID);
        GourmandPortraits.Initialize(helper, Monitor, ModManifest.UniqueID);
        RaccoonPortraits.Initialize(helper, Monitor, ModManifest.UniqueID);
        TrashBearPortrait.Initialize(helper, ModManifest.UniqueID);
        MermaidPortrait.Initialize(helper);
        JunimoPortraits.Initialize(helper);
        ChildPortraits.Initialize(helper);
        FishingContestantPortraits.Initialize(helper);
        IslandParrotPortraits.Initialize(helper, Monitor, ModManifest.UniqueID);
        WelwickPortraits.Initialize(helper, Monitor, ModManifest.UniqueID);
        TvHostPortraits.Initialize(helper, Monitor, ModManifest.UniqueID);
        WinterMysteryPortrait.Initialize(helper, Monitor, ModManifest.UniqueID);
        AbigailAdventureOutfit.Initialize(helper);
        RobotPortraits.Initialize(helper, Monitor, ModManifest.UniqueID);
        StormHail.Initialize(helper, Monitor);
        _ = new Visuals.VisualEffectsController(helper, Monitor);
        _ = new Visuals.CaveAtmosphereController(helper, Monitor);
        EntryCompleted = true;
    }

    private void CheckAsset(ArtAsset asset)
    {
        var actual = Helper.GameContent.Load<Texture2D>(asset.Name);
        var expected = GetArtworkPixels(asset.File);
        if (actual.Width != asset.Width || actual.Height != asset.Height || expected.Width != asset.Width || expected.Height != asset.Height)
            throw new InvalidOperationException($"Unexpected dimensions for {asset.Name}: game {actual.Width}x{actual.Height}, file {expected.Width}x{expected.Height}.");
        var pixels = new Color[asset.Width * asset.Height];
        var reference = expected.Data;
        actual.GetData(pixels);
        foreach (var area in asset.Areas)
        for (int y = area.Top; y < area.Bottom; y++)
            for (int x = area.Left; x < area.Right; x++)
                if (pixels[y * asset.Width + x] != reference[y * asset.Width + x])
                    throw new InvalidOperationException($"Loaded pixels differ for {asset.Name} at {x},{y}; another mod may override this artwork.");
    }
}

