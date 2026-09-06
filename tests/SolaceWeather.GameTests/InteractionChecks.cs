using System.Reflection;
using Microsoft.Xna.Framework;
using SolaceWeather.Controls;
using StardewModdingAPI;
using StardewValley;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private sealed class InteractionProbe : StardewValley.Object
    {
        public int Activations;
        public override bool checkForAction(Farmer who, bool justCheckingForActivity = false)
        {
            if (!justCheckingForActivity) Activations++;
            return true;
        }
    }

    private void InteractionChecks()
    {
        object info = Helper.ModRegistry.Get("David.SolaceWeather")!;
        object mod = info.GetType().GetProperty("Mod", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(info)!;
        var movement = (MovementControls)typeof(SolaceWeather.ModEntry).GetField("movement", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(mod)!;
        var method = movement.GetType().GetMethod("TryInteract");
        var feedback = new ClickFeedback(Helper, new SolaceWeather.ModConfig { EnableClickFeedback = false }, movement, (_, _) => false);
        var results = new List<object>();
        void Check(string name, Func<bool> test)
        {
            bool passed = false; string? error = null;
            try { passed = test(); } catch (Exception ex) { error = ex.GetBaseException().Message; }
            results.Add(new { Name = name, Passed = passed, Error = error });
            Monitor.Log($"{(passed ? "PASS" : "FAIL")}: {name}", passed ? LogLevel.Info : LogLevel.Error);
        }
        Check("Click activation is installed", () => method != null);
        if (method == null) { Helper.Data.WriteJsonFile("interaction-results.json", results); return; }
        var player = Game1.player;
        var location = Game1.currentLocation;
        var position = player.Position;
        bool canMove = player.CanMove, fade = Game1.fadeToBlack;
        var inventory = player.Items.ToArray();
        int selectedSlot = player.CurrentToolIndex;
        var lastClick = player.lastClick;
        int freezePause = player.freezePause;
        var originalSeat = player.sittingFurniture;
        bool originalUsingTool = player.UsingTool;
        Point start = default; bool found = false;
        for (int y = 2; y < location.Map.Layers[0].LayerHeight - 2 && !found; y++)
            for (int x = 2; x < location.Map.Layers[0].LayerWidth - 6 && !found; x++)
                if (Enumerable.Range(0, 5).All(dx => movement.IsGroundDestination(new Point(x + dx, y))))
                { start = new(x, y); found = true; }
        if (!found) throw new InvalidOperationException("No test corridor.");
        Point target = new(start.X + 4, start.Y);
        Vector2 key = new(target.X, target.Y);
        var probe = new InteractionProbe { TileLocation = key, ItemId = "130", Type = "interactive" };
        StardewValley.Objects.Furniture? testChair = null;
        bool Activate() => (bool)method.Invoke(movement, new object[] { target })!;
        void Finish()
        {
            for (int frame = 0; frame < 700 && player.controller != null; frame++)
                if (player.controller.update(new GameTime(TimeSpan.FromMilliseconds(frame * 16), TimeSpan.FromMilliseconds(16)))) player.controller = null;
            movement.GetType().GetMethod("CompleteWalk", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(movement, null);
        }
        try
        {
            if (player.IsSitting()) player.StopSitting(false);
            player.isStopSitting = false; player.UsingTool = false; player.freezePause = 0;
            player.CanMove = true; Game1.fadeToBlack = false;
            player.Position = new Vector2(start.X * 64, start.Y * 64 - 32);
            location.objects[key] = probe;
            Check("Distant action starts walking without activating", () => Activate() && movement.IsWalking && probe.Activations == 0);
            Check("Native arrival activates exactly once", () => { Finish(); Finish(); return probe.Activations == 1 && !movement.IsWalking; });
            player.Position = new Vector2(start.X * 64, start.Y * 64 - 32);
            Check("Cancelling prevents activation", () => { Activate(); movement.Cancel(); Finish(); return probe.Activations == 1; });
            Check("Removed target is not activated", () => { Activate(); location.objects.Remove(key); Finish(); return probe.Activations == 1; });
            location.objects[key] = probe;
            Check("Adjacent target activates without walking", () => Activate() && probe.Activations == 2 && !movement.IsWalking);
            Check("Empty ground is not an interaction", () => !(bool)method.Invoke(movement, new object[] { start })!);
            player.Items.Clear(); player.Items.Add(new StardewValley.Tools.Axe()); player.CurrentToolIndex = 0;
            var chest = new StardewValley.Objects.Chest(true) { TileLocation = key };
            location.objects[key] = chest;
            Check("Chest lid feedback resolves to its ground tile", () =>
            {
                var hint = feedback.Describe(new Point(target.X, target.Y - 1));
                return hint?.Left == "Open chest" && hint.Bounds.Y == target.Y * 64 && chest.frameCounter.Value <= 0;
            });
            player.Position = new Vector2(start.X * 64, start.Y * 64 - 32);
            Check("Clicking chest lid approaches the chest", () => movement.TryInteract(new Point(target.X, target.Y - 1)) && movement.IsWalking);
            Check("Arrival requests the native chest opening", () => { Finish(); return chest.GetMutex().IsLockHeld() || chest.frameCounter.Value > 0; });
            chest.GetMutex().ReleaseLock();
            player.freezePause = 0;
            var furnace = ItemRegistry.Create<StardewValley.Object>("(BC)13"); furnace.TileLocation = key;
            location.objects[key] = furnace;
            Check("Hover feedback does not load or activate a machine", () => feedback.Describe(target)?.Left == "Use machine" && furnace.heldObject.Value == null);
            player.Items.Clear(); player.Items.Add(ItemRegistry.Create("(O)378", 5)); player.Items.Add(ItemRegistry.Create("(O)382", 1));
            player.CurrentToolIndex = 0; player.Position = new Vector2(start.X * 64, start.Y * 64 - 32);
            Check("Idle machine accepts a distant approach with input selected", () => Activate() && movement.IsWalking && furnace.heldObject.Value == null);
            Check("Arrival loads furnace through normal game rules", () => { Finish(); return furnace.heldObject.Value != null && furnace.MinutesUntilReady > 0; });
            furnace.MinutesUntilReady = 0; furnace.readyForHarvest.Value = true;
            player.Items.Clear(); player.Items.Add(new StardewValley.Tools.Axe()); player.CurrentToolIndex = 0;
            while (player.Items.Count < player.MaxItems) player.Items.Add(null);
            player.Position = new Vector2(start.X * 64, start.Y * 64 - 32);
            Check("Finished machine output is collected after approach", () =>
            {
                Activate(); Finish();
                if (furnace.heldObject.Value != null || !player.Items.Any(item => item?.QualifiedItemId == "(O)334"))
                    throw new InvalidOperationException($"Output={furnace.heldObject.Value?.QualifiedItemId}; inventory={string.Join(",", player.Items.Where(i => i != null).Select(i => i.QualifiedItemId))}; moving={movement.IsWalking}");
                return true;
            });
            location.objects.Remove(key);
            testChair = ItemRegistry.Create<StardewValley.Objects.Furniture>("(F)0");
            testChair.TileLocation = key;
            testChair.boundingBox.Value = new Rectangle(target.X * 64, target.Y * 64, 64, 64);
            location.furniture.Add(testChair);
            Check("Chair feedback describes sitting", () => feedback.Describe(target)?.Left == "Sit" && !player.IsSitting());
            player.Position = new Vector2(start.X * 64, start.Y * 64 - 32);
            Check("Furniture click approaches the chair", () => Activate() && movement.IsWalking);
            Check("Arrival sits using the native chair action", () => { Finish(); return player.IsSitting() && testChair.IsSittingHere(player); });
            Check("Seated feedback offers standing up", () => feedback.Describe(target)?.Left == "Stand up");
            Check("Clicking the occupied chair starts native standing", () => movement.TryLeaveSeat(target) && player.isStopSitting
                && movement.GetType().GetField("afterStanding", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(movement) == null);
            Check("Clicking elsewhere queues an approach after standing", () => movement.TryLeaveSeat(start)
                && (Point?)movement.GetType().GetField("afterStanding", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(movement) == start);
            movement.Cancel();
            player.isStopSitting = false;
            player.StopSitting(false); player.CanMove = true;
            location.furniture.Remove(testChair); testChair = null;
            player.Items.Clear(); player.Items.Add(new StardewValley.Tools.Hoe()); player.CurrentToolIndex = 0;
            player.Position = new Vector2(start.X * 64, start.Y * 64 - 32);
            var smart = (SmartToolSelection)typeof(SolaceWeather.ModEntry).GetField("smartTools", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(mod)!;
            Check("Distant tool click walks before swinging", () => smart.TryWalkAndUse(target, true) && movement.IsWalking && !player.UsingTool);
            Check("Walking exposes a destination marker", () => movement.WalkingDestination.HasValue);
            Check("Cancelling tool approach prevents swing", () => { movement.Cancel(); Finish(); return !player.UsingTool; });
            Check("Cancel clears the destination marker", () => movement.WalkingDestination == null);
            Check("Arrival starts native tool use at the stored click", () =>
            {
                smart.TryWalkAndUse(target, true); Finish();
                return !movement.IsWalking && player.UsingTool && player.lastClick == key * 64 + new Vector2(32, 32);
            });
        }
        finally
        {
            movement.Cancel(); location.objects.Remove(key); player.Position = position; player.Halt();
            if (testChair != null) { player.StopSitting(false); location.furniture.Remove(testChair); }
            player.UsingTool = false; player.canReleaseTool = false; player.FarmerSprite.StopAnimation();
            player.Items.Clear(); foreach (var item in inventory) player.Items.Add(item);
            player.CurrentToolIndex = selectedSlot; player.lastClick = lastClick;
            if (originalSeat != null) player.BeginSitting(originalSeat);
            player.UsingTool = originalUsingTool;
            player.freezePause = freezePause;
            player.CanMove = canMove; Game1.fadeToBlack = fade;
            Helper.Data.WriteJsonFile("interaction-results.json", results);
        }
    }
}
