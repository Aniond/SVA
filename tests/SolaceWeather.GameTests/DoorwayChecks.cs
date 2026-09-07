using Microsoft.Xna.Framework;
using SolaceWeather.Controls;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Pathfinding;

namespace SolaceWeather.GameTests;

/// <summary>Run in an isolated, loaded farmhouse; never advances time or performs a warp.</summary>
public static class DoorwayChecks
{
    public static void Run(MovementControls movement)
    {
        if (Game1.currentLocation is not FarmHouse house) throw new InvalidOperationException("Doorway checks need a farmhouse.");
        Farmer player = Game1.player;
        Point entry = house.getEntryLocation();
        Vector2 position = player.Position, key = new(entry.X, entry.Y);
        var originalController = player.controller;
        house.objects.TryGetValue(key, out var originalObject);
        void Check(bool result, string message) { if (!result) throw new InvalidOperationException(message); }
        try
        {
            movement.Cancel(); player.controller = null;
            player.Position = new Vector2(entry.X * 64, (entry.Y - 1) * 64 - 32);
            Check(movement.TryUseFarmhouseExit(entry) && movement.IsWalking, "Visible doorway should start an exit route.");
            Check(movement.WalkingDestination == new Point(entry.X, entry.Y + 1), "Route must end at the native trigger.");
            Vector2 beforeCancel = player.Position;
            movement.Cancel();
            Check(player.controller == null && !movement.IsWalking && player.Position == beforeCancel && Game1.currentLocation == house,
                "Cancelling must leave the farmer in the house without a pending warp.");
            var scripted = new PathFindController(new Stack<Point>(), house, player, entry);
            player.controller = scripted;
            movement.TryUseFarmhouseExit(entry);
            Check(player.controller == scripted, "Doorway click must not replace scripted movement.");
            player.controller = null;
            house.objects[key] = new StardewValley.Objects.Chest(true);
            movement.TryUseFarmhouseExit(entry);
            Check(!movement.IsWalking && player.controller == null && Game1.currentLocation == house, "Blocked entry must not start or warp.");
            Check(!movement.TryUseFarmhouseExit(new Point(entry.X + 1, entry.Y)), "Adjacent floor must not select the exit.");
        }
        finally
        {
            movement.Cancel();
            if (originalObject == null) house.objects.Remove(key); else house.objects[key] = originalObject;
            player.Position = position; player.Halt(); player.controller = originalController;
        }
    }
}
