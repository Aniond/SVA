using HarmonyLib;
using StardewValley.Menus;
using StardewValley.Minigames;

namespace SolaceWeather.PlayerPortraits;

public static class PlayerPortraitCreationHooks
{
    private static PlayerPortraitService? service;
    public static void Install(string uniqueId, PlayerPortraitService portraits)
    {
        service = portraits;
        var harmony = new Harmony(uniqueId + ".PlayerPortraitCreation");
        var postfix = new HarmonyMethod(typeof(PlayerPortraitCreationHooks), nameof(Confirmed));
        harmony.Patch(AccessTools.Method(typeof(TitleMenu), nameof(TitleMenu.createdNewCharacter)), postfix: postfix);
        harmony.Patch(AccessTools.Method(typeof(Intro), nameof(Intro.doneCreatingCharacter)), postfix: postfix);
    }
    private static void Confirmed() => service?.MarkCreationConfirmed();
}
