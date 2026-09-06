using System.Reflection;
using SolaceWeather.Controls;
using SolaceWeather.Core;
using SolaceWeather.Relationships;
using StardewModdingAPI;
using StardewValley;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void RomanceNativeChecks()
    {
        RequireWorld();
        Farmer player = Game1.player;
        NPC npc = Game1.getCharacterFromName("Abigail");
        int day = Game1.Date.TotalDays;
        var inventory = player.Items.ToArray();
        int selected = player.CurrentToolIndex;
        string? spouse = player.spouse;
        bool hadFriendship = player.friendshipData.TryGetValue(npc.Name, out var originalFriendship);
        var dialogueEvents = player.activeDialogueEvents.Keys.ToDictionary(key => key, key => player.activeDialogueEvents[key]);
        var previousDialogueEvents = player.previousActiveDialogueEvents.Keys.ToDictionary(key => key, key => player.previousActiveDialogueEvents[key]);
        bool? divorcedCache = npc.divorcedFromFarmer;
        var originalMenu = Game1.activeClickableMenu;
        bool originalDialogueUp = Game1.dialogueUp;
        NPC? originalSpeaker = Game1.currentSpeaker;
        bool originalCanMove = player.CanMove;
        var originalDialogue = npc.CurrentDialogue.ToArray();
        int originalFacing = npc.FacingDirection;
        Type type = typeof(RomanceNative);
        const BindingFlags flags = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo instanceField = type.GetField("instance", flags)!;
        object? originalInstance = instanceField.GetValue(null);
        var results = new List<object>();
        RomanceSaveState state = new();
        int conversations = 0;
        var bridge = new RomanceNative(Helper, Monitor, new ModConfig(), () => state, _ => conversations++);
        object? Invoke(string name, params object?[] args)
        {
            MethodInfo method = type.GetMethod(name, flags)!;
            return method.Invoke(method.IsStatic ? null : bridge, args);
        }
        void Check(string name, Func<bool> test)
        {
            bool passed = false;
            string? error = null;
            try { passed = test(); }
            catch (Exception ex) { error = ex.GetBaseException().Message; }
            results.Add(new { Name = name, Passed = passed, Error = error });
            Monitor.Log($"{(passed ? "PASS" : "FAIL")}: {name}", passed ? LogLevel.Info : LogLevel.Error);
        }
        try
        {
            // This fixture never registers patches or borrows the user's friendship object.
            type.GetField("managedSave", flags)!.SetValue(bridge, true);
            type.GetField("patchesReady", flags)!.SetValue(bridge, true);
            instanceField.SetValue(null, bridge);
            player.Items.Clear();
            var axe = new StardewValley.Tools.Axe();
            var protectedPendant = ItemRegistry.Create<StardewValley.Object>("(O)460");
            protectedPendant.modData[QuickStack.ProtectedKey] = "true";
            var questPendant = ItemRegistry.Create<StardewValley.Object>("(O)460");
            questPendant.questItem.Value = true;
            var usablePendant = ItemRegistry.Create<StardewValley.Object>("(O)460", 2);
            player.Items.Add(axe);
            player.Items.Add(protectedPendant);
            player.Items.Add(questPendant);
            player.Items.Add(usablePendant);
            player.CurrentToolIndex = 0;
            Check("Backpack selection skips protected and quest pendants", () => (int)Invoke("FindPendantSlot", player)! == 3);
            Check("Native consumption selects only the eligible pendant and restores the held tool", () =>
            {
                Invoke("WithPendantSelected", player, 3, (Action)(() => player.reduceActiveItemByOne()));
                return usablePendant.Stack == 1 && protectedPendant.Stack == 1 && questPendant.Stack == 1
                    && ReferenceEquals(player.Items[0], axe) && player.CurrentToolIndex == 0;
            });
            Check("Held tool is restored even when proposal code throws", () =>
            {
                try { Invoke("WithPendantSelected", player, 3, (Action)(() => throw new InvalidOperationException("fixture"))); }
                catch (TargetInvocationException) { }
                return player.CurrentToolIndex == 0;
            });
            player.Items[3] = null;
            Check("No eligible pendant means no proposal item", () => (int)Invoke("FindPendantSlot", player)! == -1);
            player.CurrentToolIndex = 1;
            Check("Native commitment probe does not consume or open dialogue", () =>
            {
                object?[] args = { npc, player, true, false };
                bool runNative = (bool)Invoke("BeforeReceiveObject", args)!;
                return !runNative && (bool)args[3]! && conversations == 0 && protectedPendant.Stack == 1;
            });
            Check("Native commitment gift redirects to explicit conversation without consuming", () =>
            {
                object?[] args = { npc, player, false, false };
                return !(bool)Invoke("BeforeReceiveObject", args)! && conversations == 1 && protectedPendant.Stack == 1;
            });

            var fixtureFriendship = new Friendship(2500)
            {
                Status = FriendshipStatus.Engaged,
                WeddingDate = new WorldDate(Game1.Date) { TotalDays = day + 3 }
            };
            player.friendshipData[npc.Name] = fixtureFriendship;
            player.spouse = npc.Name;
            WorldDate wedding = fixtureFriendship.WeddingDate;
            Check("Existing engagement imports without invented courtship dates", () =>
            {
                bridge.ImportNative(day);
                return fixtureFriendship.Status == FriendshipStatus.Engaged && ReferenceEquals(wedding, fixtureFriendship.WeddingDate)
                    && state.Characters[npc.Name].IsEngaged && state.Characters[npc.Name].NeedsReaffirmation
                    && state.Characters[npc.Name].DatingSinceDay == null;
            });
            Check("Explicit cancellation clears native wedding and spouse assignment", () =>
                bridge.CancelEngagement(npc) && fixtureFriendship.Status == FriendshipStatus.Dating
                    && fixtureFriendship.WeddingDate == null && player.spouse == null && !state.Characters[npc.Name].IsEngaged);

            player.Items[3] = ItemRegistry.Create("(O)460", 2);
            Check("Proposal failure before consumption restores partial spouse assignment", () =>
            {
                bool accepted = (bool)Invoke("TryApplyProposal", player, npc, fixtureFriendship, 3, (Action)(() =>
                {
                    player.spouse = npc.Name;
                    throw new InvalidOperationException("Injected failure before consumption");
                }))!;
                return !accepted && player.spouse == null && fixtureFriendship.Status == FriendshipStatus.Dating
                    && player.Items[3].Stack == 2 && player.CurrentToolIndex == 1;
            });
            Check("Proposal failure after consumption restores item, relationship and dialogue memories", () =>
            {
                int points = fixtureFriendship.Points;
                bool accepted = (bool)Invoke("TryApplyProposal", player, npc, fixtureFriendship, 3, (Action)(() =>
                {
                    player.spouse = npc.Name;
                    fixtureFriendship.Status = FriendshipStatus.Engaged;
                    fixtureFriendship.WeddingDate = new WorldDate(Game1.Date) { TotalDays = day + 3 };
                    fixtureFriendship.RoommateMarriage = true;
                    fixtureFriendship.Points++;
                    player.activeDialogueEvents["romance_fixture_rollback"] = 1;
                    player.previousActiveDialogueEvents["romance_fixture_rollback"] = 1;
                    player.reduceActiveItemByOne();
                    throw new InvalidOperationException("Injected failure after consumption");
                }))!;
                return !accepted && player.spouse == null && fixtureFriendship.Status == FriendshipStatus.Dating
                    && fixtureFriendship.WeddingDate == null && !fixtureFriendship.RoommateMarriage
                    && fixtureFriendship.Points == points && player.Items[3].Stack == 2 && player.CurrentToolIndex == 1
                    && !player.activeDialogueEvents.ContainsKey("romance_fixture_rollback")
                    && !player.previousActiveDialogueEvents.ContainsKey("romance_fixture_rollback");
            });
            Check("Successful proposal closes native dialogue flags before the authored acknowledgement", () =>
            {
                var previousMenu = Game1.activeClickableMenu;
                bool accepted = (bool)Invoke("TryApplyProposal", player, npc, fixtureFriendship, 3, (Action)(() =>
                {
                    player.spouse = npc.Name;
                    fixtureFriendship.Status = FriendshipStatus.Engaged;
                    fixtureFriendship.WeddingDate = new WorldDate(Game1.Date) { TotalDays = day + 3 };
                    player.reduceActiveItemByOne();
                    npc.CurrentDialogue.Clear();
                    npc.CurrentDialogue.Push(new Dialogue(npc, null, "We are engaged."));
                    Game1.drawDialogue(npc);
                }))!;
                // Day-start can clear this stack: no legacy speaker may remain afterward.
                npc.CurrentDialogue.Clear();
                return accepted && !Game1.dialogueUp && Game1.currentSpeaker == null && player.CanMove
                    && ReferenceEquals(previousMenu, Game1.activeClickableMenu) && player.CurrentToolIndex == 1;
            });

            // Only the deferral branch is exercised; no native divorce or home rebuild is invoked.
            fixtureFriendship.Status = FriendshipStatus.Married;
            var birth = new WorldDate(Game1.Date) { TotalDays = day + 1 };
            fixtureFriendship.NextBirthingDate = birth;
            player.spouse = npc.Name;
            var character = state.Characters[npc.Name];
            character.IsMarried = true;
            character.InConflict = true;
            character.SeparationUntilDay = day + 14;
            character.PendingTransition = "divorce";
            state.Booking = new ActivityBooking { Npc = npc.Name, Day = day, StartMinute = 1200, Type = "town-walk", Arrived = true, ArrivedMinute = 1200 };
            ActivityBooking booking = state.Booking;
            Check("Pending birth or adoption defers divorce and preserves arrived appointment", () =>
            {
                bridge.OnDayEnding();
                return ReferenceEquals(birth, fixtureFriendship.NextBirthingDate) && player.spouse == npc.Name
                    && fixtureFriendship.IsMarried() && character.PendingTransition == "divorce" && ReferenceEquals(state.Booking, booking);
            });
            Check("Separation pauses assistance without clearing birth or arrival state", () =>
                !(bool)Invoke("BeforeMarriageDuties", npc)! && ReferenceEquals(birth, fixtureFriendship.NextBirthingDate)
                    && ReferenceEquals(booking, state.Booking));
            Check("Separation prevents new pregnancy questions without touching an existing birth", () =>
            {
                object?[] args = { npc, true };
                return !(bool)Invoke("BeforePregnancyQuestion", args)! && !(bool)args[1]!
                    && ReferenceEquals(birth, fixtureFriendship.NextBirthingDate);
            });
            Check("External native divorce clears a previously managed marriage without inventing betrayal", () =>
            {
                fixtureFriendship.Status = FriendshipStatus.Divorced;
                fixtureFriendship.NextBirthingDate = null;
                player.spouse = null;
                int incidents = state.Incidents.Count;
                bridge.ImportNative(day);
                return fixtureFriendship.IsDivorced() && !state.Characters[npc.Name].IsDating
                    && !state.Characters[npc.Name].IsMarried && !state.Characters[npc.Name].InConflict
                    && state.Incidents.Count == incidents && state.IsValid();
            });
            Check("Existing native divorced status is preserved on import", () =>
            {
                state = new RomanceSaveState();
                fixtureFriendship.Status = FriendshipStatus.Divorced;
                fixtureFriendship.NextBirthingDate = null;
                player.spouse = null;
                bridge.ImportNative(day);
                return fixtureFriendship.IsDivorced() && !state.Characters.ContainsKey(npc.Name);
            });
        }
        finally
        {
            instanceField.SetValue(null, originalInstance);
            player.Items.Clear();
            foreach (Item? item in inventory) player.Items.Add(item);
            player.CurrentToolIndex = selected;
            player.spouse = spouse;
            if (hadFriendship) player.friendshipData[npc.Name] = originalFriendship!;
            else player.friendshipData.Remove(npc.Name);
            player.activeDialogueEvents.Clear();
            foreach (var pair in dialogueEvents) player.activeDialogueEvents[pair.Key] = pair.Value;
            player.previousActiveDialogueEvents.Clear();
            foreach (var pair in previousDialogueEvents) player.previousActiveDialogueEvents[pair.Key] = pair.Value;
            npc.divorcedFromFarmer = divorcedCache;
            Game1.activeClickableMenu = originalMenu;
            Game1.dialogueUp = originalDialogueUp;
            Game1.currentSpeaker = originalSpeaker;
            player.CanMove = originalCanMove;
            npc.CurrentDialogue.Clear();
            for (int index = originalDialogue.Length - 1; index >= 0; index--) npc.CurrentDialogue.Push(originalDialogue[index]);
            npc.faceDirection(originalFacing);
            Helper.GameContent.InvalidateCache("Data/Characters");
            Helper.Data.WriteJsonFile("romance-native-results.json", results);
        }
    }
}
