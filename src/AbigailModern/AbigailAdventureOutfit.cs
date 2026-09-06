using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Characters;

namespace AbigailModern;

/// <summary>The quest owns when Abigail gets dressed; native appearance selection owns her textures.</summary>
internal static class AbigailAdventureOutfit
{
    public const string FlagKey = "David.AbigailModern/AbigailAdventureOutfit";
    private const string AppearancePrefix = "David.AbigailModern/Adventure";

    public static void Initialize(IModHelper helper)
    {
        helper.Events.Content.AssetRequested += (_, e) =>
        {
            if (!e.NameWithoutLocale.IsEquivalentTo("Data/Characters")) return;
            e.Edit(asset =>
            {
                if (!asset.AsDictionary<string, CharacterData>().Data.TryGetValue("Abigail", out var abigail)) return;
                abigail.Appearance ??= new List<CharacterAppearanceData>();
                abigail.Appearance.RemoveAll(a => a.Id == AppearancePrefix || a.Id == AppearancePrefix + "Island");
                foreach (var islandAttire in new[] { false, true })
                    abigail.Appearance.Add(new CharacterAppearanceData
                    {
                        Id = AppearancePrefix + (islandAttire ? "Island" : ""),
                        Condition = "PLAYER_MOD_DATA Host " + FlagKey + " true",
                        Sprite = "Characters/Abigail_Adventure",
                        Portrait = "Portraits/Abigail_Adventure",
                        Indoors = true,
                        Outdoors = true,
                        IsIslandAttire = islandAttire,
                        Precedence = -1000,
                        Weight = 1
                    });
            });
        };
        // Re-evaluate after the host's saved quest flag is available.
        helper.Events.GameLoop.SaveLoaded += (_, _) => Game1.getCharacterFromName("Abigail")?.ChooseAppearance();
    }
}
