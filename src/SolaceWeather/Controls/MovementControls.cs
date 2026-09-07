using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SolaceWeather.Core;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Pathfinding;

namespace SolaceWeather.Controls;

/// <summary>Player controls, independent of each farm's weather opt-in.</summary>
public sealed class MovementControls
{
    private readonly IModHelper helper;
    private readonly IMonitor monitor;
    private readonly ModConfig config;
    private readonly Func<int, int, bool> overWeatherButton;
    private PathFindController? walk;
    private Farmer? walker;
    private bool consumedClick;
    private Action? afterWalk;
    private Point? afterStanding;
    public bool IsWalking => walk != null && walker?.controller == walk;
    public Point? WalkingDestination => IsWalking ? walk!.endPoint : null;

    public MovementControls(IModHelper helper, IMonitor monitor, ModConfig config, Func<int, int, bool> overWeatherButton)
    {
        this.helper = helper; this.monitor = monitor; this.config = config; this.overWeatherButton = overWeatherButton;
        helper.Events.GameLoop.SaveLoaded += (_, _) => EnsureBindings();
        helper.Events.GameLoop.DayStarted += (_, _) => EnsureBindings();
        helper.Events.GameLoop.ReturnedToTitle += (_, _) => { Cancel(); consumedClick = false; };
        helper.Events.Player.Warped += (_, e) => { if (e.IsLocalPlayer) Cancel(); };
        helper.Events.Display.MenuChanged += (_, _) => { Cancel(); EnsureBindings(); };
        helper.Events.Input.ButtonPressed += OnButton;
        helper.Events.Input.ButtonReleased += (_, e) => { if (e.Button == SButton.MouseLeft) consumedClick = false; };
        helper.Events.GameLoop.UpdateTicking += (_, _) =>
        {
            // The native game repeats tool use while a mouse button is held.
            if (consumedClick && helper.Input.IsDown(SButton.MouseLeft)) helper.Input.Suppress(SButton.MouseLeft);
            if (afterStanding.HasValue && !Game1.player.IsSitting())
            {
                Point target = afterStanding.Value; afterStanding = null;
                if (CanWalkNow() && !ManualMovementHeld())
                {
                    try { if (!TryInteract(target)) TryWalkTo(target); }
                    catch (Exception ex) { Cancel(); monitor.Log($"Stand-and-walk cancelled: {ex.Message}", LogLevel.Warn); }
                }
            }
            if (walk == null) return;
            if (!IsWalking)
            {
                CompleteWalk(); return;
            }
            if (!CanWalkNow() || ManualMovementHeld() || helper.Input.IsDown(SButton.LeftThumbstickUp)
                || helper.Input.IsDown(SButton.LeftThumbstickDown) || helper.Input.IsDown(SButton.LeftThumbstickLeft)
                || helper.Input.IsDown(SButton.LeftThumbstickRight)) Cancel();
        };
    }

    public void EnsureBindings()
    {
        if (!config.EnableArrowKeys || !Context.IsWorldReady) return;
        static InputButton[] Add(InputButton[] existing, Keys key) => existing.Any(b => b.key == key)
            ? existing : existing.Append(new InputButton(key)).ToArray();
        Game1.options.moveUpButton = Add(Game1.options.moveUpButton, Keys.Up);
        Game1.options.moveRightButton = Add(Game1.options.moveRightButton, Keys.Right);
        Game1.options.moveDownButton = Add(Game1.options.moveDownButton, Keys.Down);
        Game1.options.moveLeftButton = Add(Game1.options.moveLeftButton, Keys.Left);
    }

    private bool ManualMovementHeld() => Game1.options.moveUpButton.Concat(Game1.options.moveRightButton)
        .Concat(Game1.options.moveDownButton).Concat(Game1.options.moveLeftButton).Any(b => helper.Input.IsDown(b.ToSButton()));

    private static bool CanWalkNow(bool allowSitting = false) => Context.IsWorldReady && !Context.IsMultiplayer && !Game1.eventUp
        && Game1.activeClickableMenu == null && Game1.currentMinigame == null && !Game1.dialogueUp
        && Game1.player.CanMove && !Game1.player.UsingTool && !Game1.player.isRidingHorse()
        && (allowSitting || !Game1.player.IsSitting()) && !Game1.player.swimming.Value && !Game1.fadeToBlack && !Game1.isWarping
        && !(Game1.chatBox?.chatBox.Selected ?? false);

    private void OnButton(object? sender, ButtonPressedEventArgs e)
    {
        if (e.Button != SButton.MouseLeft) { Cancel(); return; }
        if (e.IsSuppressed()) return;
        Cancel();
        if (!config.EnableClickToMove || !CanWalkNow(allowSitting: true) || helper.Input.IsDown(SButton.LeftShift)
            || helper.Input.IsDown(SButton.RightShift) || ManualMovementHeld()) return;
        int x = Game1.getMouseX(true), y = Game1.getMouseY(true);
        if (overWeatherButton(x, y) || Game1.onScreenMenus.Any(m => m.isWithinBounds(x, y))) return;
        Point tile = new((int)e.Cursor.Tile.X, (int)e.Cursor.Tile.Y);
        // A world left-click belongs to navigation/actions, even if nothing is reachable.
        // Never let an unrecognized target fall through to the native tool binding.
        consumedClick = true;
        helper.Input.Suppress(SButton.MouseLeft);
        try
        {
            if (TryLeaveSeat(tile)) return;
            if (TryUseFarmhouseExit(tile)) return;
            if (config.EnableClickToInteract && TryInteract(tile))
            {
                consumedClick = true;
                helper.Input.Suppress(SButton.MouseLeft);
                return;
            }
            // Walkable soil can be approached without swinging the selected tool.
            if (!IsGroundDestination(tile)) return;
            consumedClick = true;
            helper.Input.Suppress(SButton.MouseLeft);
            if (!TryWalkTo(tile)) Game1.showRedMessage("Can't walk to that spot.");
        }
        catch (Exception ex) { Cancel(); monitor.Log($"Click-to-walk cancelled: {ex.Message}", LogLevel.Warn); }
    }

    public bool IsGroundDestination(Point tile)
    {
        GameLocation location = Game1.currentLocation;
        Vector2 position = new(tile.X, tile.Y);
        if (location.objects.ContainsKey(position) || location.terrainFeatures.ContainsKey(position)) return false;
        if (location.doesTileHaveProperty(tile.X, tile.Y, "Action", "Buildings") != null
            || location.doesTileHaveProperty(tile.X, tile.Y, "TouchAction", "Back") != null) return false;
        var bounds = new Rectangle(tile.X * 64, tile.Y * 64, 64, 64);
        if (location.characters.Any(c => c.GetBoundingBox().Intersects(bounds))) return false;
        return CanCross(new WalkTile(tile.X, tile.Y));
    }

    public bool TryLeaveSeat(Point tile)
    {
        if (!config.EnableClickToMove || !CanWalkNow(allowSitting: true) || !Game1.player.IsSitting()) return false;
        var seat = Game1.player.sittingFurniture;
        bool sameSeat = seat?.GetSeatBounds().Contains(tile) == true;
        if (seat is StardewValley.Objects.Furniture f)
            sameSeat |= new Rectangle(f.boundingBox.X, f.boundingBox.Y - Math.Max(0, f.sourceRect.Height * 4 - f.boundingBox.Height),
                f.boundingBox.Width, Math.Max(f.boundingBox.Height, f.sourceRect.Height * 4)).Contains(tile.X * 64 + 32, tile.Y * 64 + 32);
        afterStanding = sameSeat ? null : tile;
        if (!Game1.player.isStopSitting) Game1.player.StopSitting();
        return true;
    }

    private static bool CanCross(WalkTile tile)
    {
        GameLocation location = Game1.currentLocation;
        if (tile.X < 0 || tile.Y < 0 || tile.X >= location.Map.Layers[0].LayerWidth || tile.Y >= location.Map.Layers[0].LayerHeight) return false;
        if (location.warps.Any(w => w.X == tile.X && w.Y == tile.Y)
            || location.doesTileHaveProperty(tile.X, tile.Y, "TouchAction", "Back") != null) return false;
        return !location.isCollidingPosition(new Rectangle(tile.X * 64 + 1, tile.Y * 64 + 1, 62, 62),
            Game1.viewport, true, 0, false, Game1.player, true, skipCollisionEffects: true);
    }

    public bool TryWalkTo(Point target)
    {
        Cancel();
        if (!config.EnableClickToMove || !CanWalkNow() || Game1.player.controller != null || !IsGroundDestination(target)) return false;
        GameLocation location = Game1.currentLocation;
        Point start = Game1.player.TilePoint;
        var route = WalkingPath.Find(new(start.X, start.Y), new(target.X, target.Y),
            location.Map.Layers[0].LayerWidth, location.Map.Layers[0].LayerHeight, CanCross);
        if (route == null) return false;
        return StartRoute(route, null);
    }

    /// <summary>Recognize only the house's visible entrance and its adjacent native exit trigger.</summary>
    public bool TryUseFarmhouseExit(Point tile)
    {
        if (!config.EnableClickToMove || !CanWalkNow() || ManualMovementHeld()
            || Game1.currentLocation is not StardewValley.Locations.FarmHouse house) return false;
        Point entry = house.getEntryLocation();
        var exit = house.warps.FirstOrDefault(w => !w.npcOnly.Value && w.TargetName == "Farm"
            && w.X == entry.X && w.Y == entry.Y + 1);
        if (exit == null || (tile != entry && tile != new Point(exit.X, exit.Y))) return false;
        if (Game1.player.controller != null && !IsWalking) return true;
        Cancel();
        Point start = Game1.player.TilePoint;
        var route = FarmhouseDoorwayPath.Find(new(start.X, start.Y), new(tile.X, tile.Y),
            new(entry.X, entry.Y), new(exit.X, exit.Y), house.Map.Layers[0].LayerWidth,
            house.Map.Layers[0].LayerHeight, CanCross);
        if (route == null) { Game1.showRedMessage("Can't reach that from here."); return true; }
        // The final step deliberately crosses the map edge. Farmer.MovePositionImpl checks
        // the live native warp before collision, preserving its destination and normal transition.
        StartRoute(route, null);
        return true;
    }

    private bool StartRoute(IReadOnlyList<WalkTile> route, Action? completed)
    {
        if (route.Count == 1) { completed?.Invoke(); return true; }
        var end = route[^1];
        Point target = new(end.X, end.Y);
        afterWalk = completed;
        walker = Game1.player;
        walk = new PathFindController(new Stack<Point>(route.Reverse().Select(p => new Point(p.X, p.Y))), Game1.currentLocation, walker, target)
        { finalFacingDirection = -1 };
        walker.controller = walk;
        return true;
    }

    public bool TryApproachTool(Point target, Action completed)
    {
        Cancel();
        if (!config.EnableClickToMove || !CanWalkNow() || ManualMovementHeld() || Game1.player.controller != null) return false;
        var location = Game1.currentLocation;
        int width = location.Map.Layers[0].LayerWidth, height = location.Map.Layers[0].LayerHeight;
        if (target.X < 0 || target.Y < 0 || target.X >= width || target.Y >= height) return false;
        Point start = Game1.player.TilePoint;
        IReadOnlyList<WalkTile>? best = null;
        // Cardinal approach ensures the native tool faces the intended tile.
        foreach (Point offset in new[] { new Point(0, 1), new Point(1, 0), new Point(0, -1), new Point(-1, 0) })
        {
            var goal = new WalkTile(target.X + offset.X, target.Y + offset.Y);
            var route = WalkingPath.Find(new(start.X, start.Y), goal, width, height,
                p => (p.X != target.X || p.Y != target.Y) && CanCross(p));
            if (route != null && (best == null || route.Count < best.Count)) best = route;
        }
        return best != null && StartRoute(best, () =>
        {
            if (Game1.currentLocation == location && Utility.tileWithinRadiusOfPlayer(target.X, target.Y, 1, Game1.player)) completed();
        });
    }

    private void CompleteWalk()
    {
        if (walk == null || IsWalking) return;
        // Timeouts and controllers taken over by another system must not activate the target.
        Action? completed = walker?.controller == null && walk.pathToEndPoint?.Count == 0 ? afterWalk : null;
        if (walker?.controller == null) walker?.Halt();
        walk = null; walker = null; afterWalk = null;
        afterStanding = null;
        if (CanWalkNow() && !ManualMovementHeld())
        {
            try { completed?.Invoke(); }
            catch (Exception ex) { Cancel(); monitor.Log($"Click interaction cancelled: {ex.Message}", LogLevel.Warn); }
        }
    }

    /// <summary>Returns true for a recognized action, including one which cannot be reached.</summary>
    public bool TryInteract(Point tile)
    {
        if (TryUseFarmhouseExit(tile)) return true;
        if (!config.EnableClickToInteract || !CanWalkNow()) return false;
        var location = Game1.currentLocation;
        if (tile.X < 0 || tile.Y < 0 || tile.X >= location.Map.Layers[0].LayerWidth || tile.Y >= location.Map.Layers[0].LayerHeight) return false;
        // Tall chests and machines draw above their occupied ground tile.
        if (!location.objects.ContainsKey(new Vector2(tile.X, tile.Y))
            && location.objects.TryGetValue(new Vector2(tile.X, tile.Y + 1), out var below)
            && (below is StardewValley.Objects.Chest || below.bigCraftable.Value)) tile = new Point(tile.X, tile.Y + 1);
        var actor = location.characters.FirstOrDefault(c => !c.IsMonster && c.GetBoundingBox().Intersects(new Rectangle(tile.X * 64, tile.Y * 64, 64, 64)));
        bool exit = location.warps.Any(w => w.X == tile.X && w.Y == tile.Y);
        var furnishing = location.furniture.FirstOrDefault(f => f.boundingBox.Value.Contains(tile.X * 64 + 32, tile.Y * 64 + 32));
        if (furnishing == null)
            furnishing = location.furniture.LastOrDefault(f => new Rectangle(f.boundingBox.X,
                f.boundingBox.Y - Math.Max(0, f.sourceRect.Height * 4 - f.boundingBox.Height),
                f.boundingBox.Width, Math.Max(f.boundingBox.Height, f.sourceRect.Height * 4)).Contains(tile.X * 64 + 32, tile.Y * 64 + 32));
        if (furnishing != null)
            tile = new Point(Math.Clamp(tile.X, furnishing.boundingBox.X / 64, (furnishing.boundingBox.Right - 1) / 64),
                Math.Clamp(tile.Y, furnishing.boundingBox.Y / 64, (furnishing.boundingBox.Bottom - 1) / 64));
        bool machine = location.objects.TryGetValue(new Vector2(tile.X, tile.Y), out var placed) && placed.GetMachineData() != null;
        if (actor == null && !exit && !machine && furnishing == null && !location.isActionableTile(tile.X, tile.Y, Game1.player)) return false;
        if (Game1.player.controller != null && !IsWalking) return true;
        Cancel();
        Vector2 key = new(tile.X, tile.Y);
        location.objects.TryGetValue(key, out var originalObject);
        string? originalAction = location.doesTileHaveProperty(tile.X, tile.Y, "Action", "Buildings");
        var originalBuilding = location.getBuildingAt(key);
        Rectangle? originalFurnitureBounds = furnishing?.boundingBox.Value;
        int attempts = 0;
        Point actionTile = tile;
        void Approach()
        {
            if (!CanWalkNow() || Game1.currentLocation != location) return;
            Point target = actor?.TilePoint ?? actionTile;
            if (furnishing != null && (!location.furniture.Contains(furnishing) || furnishing.boundingBox.Value != originalFurnitureBounds)) return;
            if (actor != null && !location.characters.Contains(actor)) return;
            if (actor == null && !exit)
            {
                location.objects.TryGetValue(key, out var currentObject);
                if (!ReferenceEquals(originalObject, currentObject) || originalAction != location.doesTileHaveProperty(tile.X, tile.Y, "Action", "Buildings")
                    || !ReferenceEquals(originalBuilding, location.getBuildingAt(key))) return;
            }
            if (!exit && Utility.tileWithinRadiusOfPlayer(target.X, target.Y, 1, Game1.player))
            {
                MouseToolInput.Activate(target);
                if (actor != null) AfterNpcInteraction?.Invoke(actor);
                return;
            }
            if (++attempts > 3) return;
            Point start = Game1.player.TilePoint;
            IReadOnlyList<WalkTile>? best = null;
            var targets = furnishing == null ? new[] { target } :
                (from fy in Enumerable.Range(furnishing.boundingBox.Y / 64, (furnishing.boundingBox.Height + 63) / 64)
                 from fx in Enumerable.Range(furnishing.boundingBox.X / 64, (furnishing.boundingBox.Width + 63) / 64)
                 select new Point(fx, fy)).ToArray();
            foreach (Point useTile in targets)
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (exit ? dx != 0 || dy != 0 : dx == 0 && dy == 0) continue;
                    var goal = new WalkTile(useTile.X + dx, useTile.Y + dy);
                    bool Passable(WalkTile p) => exit && p == goal || CanCross(p);
                    var route = WalkingPath.Find(new(start.X, start.Y), goal, location.Map.Layers[0].LayerWidth,
                        location.Map.Layers[0].LayerHeight, Passable);
                    if (route != null && (best == null || route.Count < best.Count)) { best = route; actionTile = useTile; }
                }
            if (best == null) { Game1.showRedMessage("Can't reach that from here."); return; }
            StartRoute(best, exit ? null : Approach);
        }
        Approach();
        return true;
    }

    public void Cancel()
    {
        if (walk != null && walker?.controller == walk) { walker.controller = null; walker.Halt(); }
        walk = null; walker = null; afterWalk = null;
        afterStanding = null;
    }

    public Action<NPC>? AfterNpcInteraction { get; set; }
}

/// <summary>Routes to the visible house doorway before taking one step onto its native warp.</summary>
public static class FarmhouseDoorwayPath
{
    public static IReadOnlyList<WalkTile>? Find(WalkTile start, WalkTile click, WalkTile entry, WalkTile warp,
        int width, int height, Func<WalkTile, bool> canWalk)
    {
        if ((click != entry && click != warp) || warp.X != entry.X || warp.Y != entry.Y + 1) return null;
        var approach = WalkingPath.Find(start, entry, width, height, canWalk);
        return approach?.Append(warp).ToArray();
    }
}
