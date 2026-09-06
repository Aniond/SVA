using HarmonyLib;
using SolaceWeather.Core;
using SolaceWeather.Controls;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Characters;
using StardewValley.Menus;

namespace SolaceWeather.Relationships;

/// <summary>Keeps explicit relationship choices and Stardew's spouse lifecycle in agreement.</summary>
public sealed class RomanceNative
{
    private static RomanceNative? instance;
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly ModConfig config;
    private readonly Func<RomanceSaveState> getState;
    private readonly Action<NPC> openConversation;
    private bool managedSave;
    private bool patchesReady;

    public RomanceNative(IModHelper helper, IMonitor monitor, ModConfig config,
        Func<RomanceSaveState> getState, Action<NPC> openConversation)
    {
        this.helper = helper;
        this.monitor = monitor;
        this.config = config;
        this.getState = getState;
        this.openConversation = openConversation;
    }

    private bool Ready => managedSave && patchesReady && Context.IsWorldReady
        && !Context.IsMultiplayer && config.EnableAbigailMemory && getState().IsValid();

    public void Register(string modId)
    {
        if (Game1.version != "1.6.15" || Constants.ApiVersion.ToString() != "4.5.2")
        {
            monitor.Log("Romance's native bridge requires the verified Stardew 1.6.15 and SMAPI 4.5.2 versions.", LogLevel.Error);
            return;
        }
        instance = this;
        var harmony = new Harmony(modId + ".RomanceNative");
        try
        {
            harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.tryToReceiveActiveObject),
                new[] { typeof(Farmer), typeof(bool) }),
                prefix: new HarmonyMethod(typeof(RomanceNative), nameof(BeforeReceiveObject)));
            harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.checkAction),
                new[] { typeof(Farmer), typeof(GameLocation) }),
                prefix: new HarmonyMethod(typeof(RomanceNative), nameof(BeforeCheckAction)));
            harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.marriageDuties)),
                prefix: new HarmonyMethod(typeof(RomanceNative), nameof(BeforeMarriageDuties)));
            harmony.Patch(AccessTools.Method(typeof(NPC), nameof(NPC.canGetPregnant)),
                prefix: new HarmonyMethod(typeof(RomanceNative), nameof(BeforePregnancyQuestion)));
            patchesReady = true;
            helper.Events.Content.AssetRequested += OnAssetRequested;
        }
        catch (Exception ex)
        {
            harmony.UnpatchAll(harmony.Id);
            patchesReady = false;
            monitor.Log($"Romance's native bridge could not start: {ex.Message}", LogLevel.Error);
        }
    }

    public void OnReturnedToTitle()
    {
        managedSave = false;
        helper.GameContent.InvalidateCache("Data/Characters");
    }

    public void ImportNative(int day)
    {
        if (!Context.IsWorldReady || Context.IsMultiplayer || !patchesReady || !getState().IsValid()) return;
        RomanceSaveState state = getState();
        foreach (string name in RomanceRules.Candidates)
        {
            if (!Game1.player.friendshipData.TryGetValue(name, out Friendship friendship)) continue;
            bool dating = friendship.IsDating();
            bool engaged = friendship.IsEngaged();
            bool married = friendship.IsMarried() && !friendship.RoommateMarriage;
            if (!dating && !engaged && !married)
            {
                // Native divorce or another mod may end a relationship outside this menu.
                // Observe that transition without inventing a romantic violation or altering native status.
                if (state.Characters.TryGetValue(name, out var previous) && previous.IsDating)
                {
                    previous.InConflict = true;
                    previous.PendingTransition = previous.IsMarried ? "divorce" : "breakup";
                    RomanceRules.CompleteTransition(state, name, day);
                }
                continue;
            }
            if (!state.Characters.TryGetValue(name, out var character) || !character.IsDating)
            {
                if (!RomanceRules.ImportPartner(state, name, day, married)) continue;
                character = state.Characters[name];
            }
            // Native dates remain authoritative, but we never fabricate historical courtship evidence.
            character.IsDating = true;
            character.IsMarried = married;
            character.IsEngaged = engaged && !married;
            character.FriendshipOnly = false;
        }
        managedSave = true;
        helper.GameContent.InvalidateCache("Data/Characters");
    }

    public bool ApplyDating(NPC npc)
    {
        if (!Ready || !RomanceRules.IsCandidate(npc.Name) || !Game1.player.friendshipData.TryGetValue(npc.Name, out var friendship)) return false;
        var state = getState();
        // The caller may have already recorded the accepted choice in the core.
        if (!state.Characters.TryGetValue(npc.Name, out var character) || !character.IsDating)
        {
            if (!RomanceRules.StartDating(state, npc.Name, Game1.Date.TotalDays)) return false;
            character = state.Characters[npc.Name];
        }
        if (character.NeedsReaffirmation || character.InConflict || character.PendingTransition != null
            || character.IsMarried || character.IsEngaged || friendship.IsMarried() || friendship.IsEngaged()) return false;
        friendship.Status = FriendshipStatus.Dating;
        Game1.player.autoGenerateActiveDialogueEvent("dating_" + npc.Name);
        Game1.player.autoGenerateActiveDialogueEvent("dating");
        return true;
    }

    public bool TryPropose(NPC npc, out string reason)
    {
        reason = "This relationship is not ready for a proposal yet.";
        if (!Ready || Game1.eventUp || !RomanceRules.IsCandidate(npc.Name) || !getState().GetJourney(npc.Name, Game1.Date.TotalDays).CanPropose) return false;
        if (Game1.player.spouse != null || Game1.player.isEngaged() || Game1.player.isMarriedOrRoommates()
            || npc.isMarriedOrEngaged())
        {
            reason = "An existing marriage or engagement must be resolved first.";
            return false;
        }
        if (Game1.player.HouseUpgradeLevel < 1)
        {
            reason = "Upgrade your farmhouse before proposing.";
            return false;
        }
        int pendantSlot = FindPendantSlot(Game1.player);
        if (pendantSlot < 0)
        {
            reason = "Bring an unprotected Mermaid's Pendant in your backpack, then choose to propose here.";
            return false;
        }
        if (!Game1.player.friendshipData.TryGetValue(npc.Name, out var friendship) || !friendship.IsDating()) return false;
        var method = AccessTools.Method(typeof(NPC), "engagementResponse", new[] { typeof(Farmer), typeof(bool) });
        if (method == null)
        {
            reason = "The game's wedding service is unavailable.";
            return false;
        }
        // Stardew schedules an eligible wedding day and consumes the verified pendant itself.
        if (!TryApplyProposal(Game1.player, npc, friendship, pendantSlot,
            () => method.Invoke(npc, new object[] { Game1.player, false })))
        {
            reason = "The wedding could not be scheduled. Your relationship and pendant have been restored.";
            return false;
        }
        getState().Characters[npc.Name].IsEngaged = true;
        reason = "";
        return true;
    }

    private bool TryApplyProposal(Farmer farmer, NPC npc, Friendship friendship, int pendantSlot, Action accept)
    {
        var inventory = farmer.Items.ToArray();
        var stacks = inventory.Select(item => item?.Stack ?? 0).ToArray();
        string? spouse = farmer.spouse;
        FriendshipStatus status = friendship.Status;
        WorldDate? wedding = friendship.WeddingDate;
        bool roommate = friendship.RoommateMarriage;
        int points = friendship.Points;
        var events = farmer.activeDialogueEvents.Keys.ToDictionary(key => key, key => farmer.activeDialogueEvents[key]);
        var previousEvents = farmer.previousActiveDialogueEvents.Keys.ToDictionary(key => key, key => farmer.previousActiveDialogueEvents[key]);
        var dialogue = npc.CurrentDialogue.ToArray();
        var menu = Game1.activeClickableMenu;
        bool dialogueUp = Game1.dialogueUp;
        NPC? speaker = Game1.currentSpeaker;
        bool canMove = farmer.CanMove;
        try
        {
            WithPendantSelected(farmer, pendantSlot, accept);
            if (!friendship.IsEngaged() || farmer.spouse != npc.Name || friendship.WeddingDate == null)
                throw new InvalidOperationException("Native proposal did not produce an engagement and wedding date.");
            // The conversation controller owns the acknowledgement. Leaving a native
            // DialogueBox behind lets its legacy flags outlive the NPC's daily dialogue stack.
            if (Game1.activeClickableMenu is DialogueBox native && native.characterDialogue?.speaker == npc)
            {
                Action? finish = native.characterDialogue.onFinish;
                native.characterDialogue.onFinish = null;
                native.closeDialogue();
                finish?.Invoke();
            }
            Game1.dialogueUp = false;
            Game1.currentSpeaker = null;
            farmer.CanMove = true;
            Game1.activeClickableMenu = menu;
            npc.CurrentDialogue.Clear();
            for (int index = dialogue.Length - 1; index >= 0; index--) npc.CurrentDialogue.Push(dialogue[index]);
            return true;
        }
        catch (Exception ex)
        {
            farmer.spouse = spouse;
            friendship.Status = status;
            friendship.WeddingDate = wedding;
            friendship.RoommateMarriage = roommate;
            friendship.Points = points;
            farmer.Items.Clear();
            for (int index = 0; index < inventory.Length; index++)
            {
                if (inventory[index] != null) inventory[index].Stack = stacks[index];
                farmer.Items.Add(inventory[index]);
            }
            farmer.activeDialogueEvents.Clear();
            foreach (var pair in events) farmer.activeDialogueEvents[pair.Key] = pair.Value;
            farmer.previousActiveDialogueEvents.Clear();
            foreach (var pair in previousEvents) farmer.previousActiveDialogueEvents[pair.Key] = pair.Value;
            npc.CurrentDialogue.Clear();
            for (int index = dialogue.Length - 1; index >= 0; index--) npc.CurrentDialogue.Push(dialogue[index]);
            Game1.activeClickableMenu = menu;
            Game1.dialogueUp = dialogueUp;
            Game1.currentSpeaker = speaker;
            farmer.CanMove = canMove;
            monitor.Log($"Proposal was rolled back: {ex.GetBaseException().Message}", LogLevel.Warn);
            return false;
        }
    }

    private static int FindPendantSlot(Farmer farmer)
    {
        for (int index = 0; index < farmer.Items.Count; index++)
            if (farmer.Items[index] is StardewValley.Object item && item.QualifiedItemId == "(O)460"
                && item.Stack > 0 && !item.questItem.Value && !item.modData.ContainsKey(QuickStack.ProtectedKey))
                return index;
        return -1;
    }

    private static void WithPendantSelected(Farmer farmer, int slot, Action accept)
    {
        int previousSlot = farmer.CurrentToolIndex;
        try
        {
            farmer.CurrentToolIndex = slot;
            accept();
        }
        finally
        {
            farmer.CurrentToolIndex = previousSlot;
        }
    }

    public bool CancelEngagement(NPC npc)
    {
        if (!Ready || !Game1.player.friendshipData.TryGetValue(npc.Name, out var friendship)
            || !friendship.IsEngaged() || !getState().Characters.TryGetValue(npc.Name, out var character)) return false;
        friendship.Status = FriendshipStatus.Dating;
        friendship.WeddingDate = null;
        if (Game1.player.spouse == npc.Name) Game1.player.spouse = null;
        character.IsEngaged = false;
        character.IsDating = true;
        return true;
    }

    public bool IsCommitmentBlocked(NPC npc)
    {
        if (!Ready || !getState().Characters.TryGetValue(npc.Name, out var character)) return false;
        return character.SeparationUntilDay.HasValue || character.PendingTransition != null;
    }

    public void OnDayEnding()
    {
        if (!Ready) return;
        RomanceSaveState state = getState();
        foreach (var entry in state.Characters.ToArray())
        {
            if (!Game1.player.friendshipData.TryGetValue(entry.Key, out var friendship)) continue;
            if (friendship.IsEngaged() && (entry.Value.SeparationUntilDay.HasValue || entry.Value.PendingTransition != null))
            {
                NPC? fiance = Game1.getCharacterFromName(entry.Key);
                if (fiance != null) CancelEngagement(fiance);
            }
            if (entry.Value.PendingTransition == null) continue;
            // Existing birth/adoption events keep their spouse and home until they have completed.
            if (friendship.NextBirthingDate != null) continue;
            bool divorce = entry.Value.PendingTransition == "divorce";
            if (divorce && friendship.IsMarried())
            {
                if (Game1.player.spouse != entry.Key || Game1.getCharacterFromName(entry.Key) == null) continue;
                Game1.player.doDivorce();
                // Vanilla splits farmer cleanup and the former spouse's move home. Both are required.
                Game1.getCharacterFromName(entry.Key).PerformDivorce();
            }
            else
            {
                if (Game1.player.spouse == entry.Key) Game1.player.spouse = null;
                Game1.player.removeDatingActiveDialogueEvents(entry.Key);
            }
            // The native cleanup has already updated the farmhouse, patio and spouse assignment.
            friendship.Status = divorce ? FriendshipStatus.Divorced : FriendshipStatus.Friendly;
            friendship.WeddingDate = null;
            friendship.RoommateMarriage = false;
            Game1.player.removeMarriageActiveDialogueEvents(entry.Key);
            NPC? npc = Game1.getCharacterFromName(entry.Key);
            if (npc != null) npc.divorcedFromFarmer = null;
            RomanceRules.CompleteTransition(state, entry.Key, Game1.Date.TotalDays);
        }
    }

    private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
    {
        if (!Ready || !e.NameWithoutLocale.IsEquivalentTo("Data/Characters")) return;
        e.Edit(asset =>
        {
            foreach (var entry in asset.AsDictionary<string, CharacterData>().Data)
                if (RomanceRules.IsCandidate(entry.Key)) entry.Value.SpouseGiftJealousy = "FALSE";
        }, AssetEditPriority.Late);
    }

    private static bool BeforeReceiveObject(NPC __instance, Farmer who, bool probe, ref bool __result)
    {
        if (instance?.Ready != true || who != Game1.player || !RomanceRules.IsCandidate(__instance.Name)
            || who.ActiveObject?.QualifiedItemId is not ("(O)458" or "(O)460" or "(O)277")) return true;
        __result = true;
        if (!probe) instance.openConversation(__instance);
        return false;
    }

    private static bool BeforeCheckAction(NPC __instance, Farmer who, ref bool __result)
    {
        if (who != Game1.player || instance?.IsCommitmentBlocked(__instance) != true) return true;
        __result = true;
        instance.openConversation(__instance);
        return false;
    }

    private static bool BeforeMarriageDuties(NPC __instance) => instance?.IsCommitmentBlocked(__instance) != true;

    private static bool BeforePregnancyQuestion(NPC __instance, ref bool __result)
    {
        if (instance?.IsCommitmentBlocked(__instance) != true) return true;
        __result = false;
        return false;
    }
}
