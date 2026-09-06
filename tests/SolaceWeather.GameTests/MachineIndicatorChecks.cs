using Microsoft.Xna.Framework;
using SolaceWeather.Controls;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void MachineIndicatorChecks()
    {
        var results = new List<object>();
        void Check(string name, Func<bool> test)
        {
            bool passed = false; string? error = null;
            try { passed = test(); } catch (Exception ex) { error = ex.GetBaseException().Message; }
            results.Add(new { Name = name, Passed = passed, Error = error });
            Monitor.Log($"{(passed ? "PASS" : "FAIL")}: {name}", passed ? LogLevel.Info : LogLevel.Error);
        }
        var furnace = ItemRegistry.Create<StardewValley.Object>("(BC)13");
        Check("Idle furnace reports waiting for ingredients", () => MachineIndicators.Describe(furnace)?.Text == "Idle: waiting for ingredients");
        furnace.heldObject.Value = ItemRegistry.Create<StardewValley.Object>("(O)334"); furnace.MinutesUntilReady = 80;
        Check("Working machine reports game-time countdown", () => MachineIndicators.Describe(furnace)?.Text == "Working: about 1h 20m of game time left");
        Check("Reading status does not advance timer or collect output", () =>
        {
            var output = furnace.heldObject.Value;
            for (int i = 0; i < 20; i++) MachineIndicators.Describe(furnace);
            return furnace.MinutesUntilReady == 80 && ReferenceEquals(output, furnace.heldObject.Value);
        });
        furnace.MinutesUntilReady = 0;
        Check("Unready output is not labelled collectable", () => MachineIndicators.Describe(furnace)?.State == MachineState.Working);
        furnace.readyForHarvest.Value = true;
        Check("Ready output reports item name", () => MachineIndicators.Describe(furnace)?.Text == "Ready: " + furnace.heldObject.Value.DisplayName);
        Check("Chests have no production indicator", () => MachineIndicators.Describe(new Chest(true)) == null);
        Check("Ordinary resources have no production indicator", () => MachineIndicators.Describe(ItemRegistry.Create<StardewValley.Object>("(O)388")) == null);
        furnace.isTemporarilyInvisible = true;
        Check("Hidden machines do not reveal status", () => MachineIndicators.Describe(furnace) == null);
        Helper.Data.WriteJsonFile("machine-indicator-results.json", results);
    }

    private void StageMachineIndicators()
    {
        for (int i = 0; i < 3; i++)
        {
            var furnace = ItemRegistry.Create<StardewValley.Object>("(BC)13");
            furnace.TileLocation = new Vector2(3 + i * 2, 6);
            if (i > 0) furnace.heldObject.Value = ItemRegistry.Create<StardewValley.Object>("(O)334");
            furnace.MinutesUntilReady = i == 1 ? 80 : 0;
            furnace.readyForHarvest.Value = i == 2;
            Game1.currentLocation.objects[furnace.TileLocation] = furnace;
        }
        Game1.setMousePosition((int)((5 * 64 + 32 - Game1.viewport.X) * Game1.options.zoomLevel),
            (int)((6 * 64 + 32 - Game1.viewport.Y) * Game1.options.zoomLevel));
        captureRequested = true; captureDelay = 4;
    }
}
