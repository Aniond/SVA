using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SolaceWeather.Controls;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Pathfinding;
using StardewValley.TerrainFeatures;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private void MovementChecks()
    {
        object info = Helper.ModRegistry.Get("David.SolaceWeather")!;
        object mod = info.GetType().GetProperty("Mod", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(info)!;
        var movement = (MovementControls)typeof(SolaceWeather.ModEntry).GetField("movement", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(mod)!;
        var results = new List<object>();
        void Check(string name, Func<bool> test)
        {
            bool passed = false; string? error = null;
            try { passed = test(); } catch (Exception ex) { error = ex.GetBaseException().Message; }
            results.Add(new { Name = name, Passed = passed, Error = error });
            Monitor.Log($"{(passed ? "PASS" : "FAIL")}: {name}", passed ? LogLevel.Info : LogLevel.Error);
        }
        Farmer player = Game1.player;
        GameLocation location = Game1.currentLocation;
        Vector2 position = player.Position;
        bool canMove = player.CanMove, fade = Game1.fadeToBlack;
        var oldMenu = Game1.activeClickableMenu;
        var originalController = player.controller;
        bool enabled = State.Enabled;
        var mouseProperty = Game1.input.GetType().GetProperty("MouseState")!;
        object originalMouse = mouseProperty.GetValue(Game1.input)!;
        Point start = default, goal = default;
        bool found = false;
        TerrainFeature? original = null;
        Vector2 targetTile = default;
        try
        {
            Game1.exitActiveMenu(); player.CanMove = true; Game1.fadeToBlack = false; State.Enabled = false;
            movement.EnsureBindings();
            Check("Arrow directions added alongside existing WASD", () =>
                Game1.options.moveUpButton.Any(b => b.key == Keys.Up) && Game1.options.moveUpButton.Any(b => b.key == Keys.W)
                && Game1.options.moveRightButton.Any(b => b.key == Keys.Right) && Game1.options.moveRightButton.Any(b => b.key == Keys.D)
                && Game1.options.moveDownButton.Any(b => b.key == Keys.Down) && Game1.options.moveDownButton.Any(b => b.key == Keys.S)
                && Game1.options.moveLeftButton.Any(b => b.key == Keys.Left) && Game1.options.moveLeftButton.Any(b => b.key == Keys.A));
            movement.EnsureBindings();
            Check("Arrow bindings remain unique on repeated setup", () => Game1.options.moveUpButton.Count(b => b.key == Keys.Up) == 1);
            var rightClick = new MouseState(100, 100, 120, ButtonState.Released, ButtonState.Released, ButtonState.Pressed, ButtonState.Released, ButtonState.Released);
            mouseProperty.SetValue(Game1.input, rightClick);
            Check("World left-click cannot enter native tool input", () =>
            {
                mouseProperty.SetValue(Game1.input, new MouseState(100, 100, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released));
                return Game1.input.GetMouseState().LeftButton == ButtonState.Released;
            });
            mouseProperty.SetValue(Game1.input, rightClick);
            Check("Right-click reaches native tool input without an action click", () =>
            {
                MouseState mapped = Game1.input.GetMouseState();
                return mapped.LeftButton == ButtonState.Pressed && mapped.RightButton == ButtonState.Released && mapped.ScrollWheelValue == 120;
            });
            mouseProperty.SetValue(Game1.input, new MouseState(100, 100, 120, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released));
            Check("Releasing right-click releases native tool input", () => Game1.input.GetMouseState().LeftButton == ButtonState.Released);
            mouseProperty.SetValue(Game1.input, rightClick);
            Game1.activeClickableMenu = new GameMenu();
            Check("Menus retain their original right mouse button", () => Game1.input.GetMouseState().RightButton == ButtonState.Pressed && Game1.input.GetMouseState().LeftButton == ButtonState.Released);
            Game1.activeClickableMenu = null;
            mouseProperty.SetValue(Game1.input, originalMouse);
            for (int y = 2; y < location.Map.Layers[0].LayerHeight - 2 && !found; y++)
                for (int x = 2; x < location.Map.Layers[0].LayerWidth - 4 && !found; x++)
                    if (Enumerable.Range(0, 3).All(dx => movement.IsGroundDestination(new Point(x + dx, y))))
                    { start = new(x, y); goal = new(x + 2, y); found = true; }
            if (!found) throw new InvalidOperationException("No open three-tile test corridor found.");
            player.Position = new Vector2(start.X * 64, start.Y * 64 - 32);
            targetTile = new(goal.X, goal.Y);
            location.terrainFeatures.TryGetValue(targetTile, out original);
            Check("Click route starts while weather is disabled", () => movement.TryWalkTo(goal) && movement.IsWalking);
            Check("Native farmer walks to the destination and stops", () =>
            {
                Vector2 before = player.Position;
                for (int frame = 0; frame < 500 && player.controller != null; frame++)
                    if (player.controller.update(new GameTime(TimeSpan.FromMilliseconds(frame * 16), TimeSpan.FromMilliseconds(16)))) player.controller = null;
                return player.Position != before && player.TilePoint == goal && player.controller == null;
            });
            player.Position = new Vector2(start.X * 64, start.Y * 64 - 32);
            Check("Cancel releases only the click-walk controller", () =>
            {
                movement.TryWalkTo(goal); movement.Cancel();
                return player.controller == null && !movement.IsWalking && !player.isMoving();
            });
            location.terrainFeatures[targetTile] = new HoeDirt(0, location);
            Check("Tilled soil retains normal tool clicks", () => !movement.IsGroundDestination(goal) && !movement.TryWalkTo(goal));
            if (original == null) location.terrainFeatures.Remove(targetTile); else location.terrainFeatures[targetTile] = original;
            Game1.activeClickableMenu = new GameMenu();
            Check("Menus block click-to-walk", () => !movement.TryWalkTo(goal));
            Game1.activeClickableMenu = null;
            Check("Off-map destinations do not move the farmer", () => !movement.TryWalkTo(new Point(-1, -1)) && player.controller == null);
            var scripted = new PathFindController(new Stack<Point>(), location, player, goal);
            player.controller = scripted;
            Check("Existing scripted movement is not replaced", () => !movement.TryWalkTo(goal) && player.controller == scripted);
        }
        finally
        {
            movement.Cancel();
            if (found) { if (original == null) location.terrainFeatures.Remove(targetTile); else location.terrainFeatures[targetTile] = original; }
            player.controller = originalController; player.Position = position; player.Halt(); player.CanMove = canMove;
            Game1.activeClickableMenu = oldMenu; Game1.fadeToBlack = fade; State.Enabled = enabled;
            mouseProperty.SetValue(Game1.input, originalMouse);
            Helper.Data.WriteJsonFile("movement-results.json", new { Checks = results, Restored = true });
        }
    }
}
