using SolaceWeather.Core;
using StardewValley;
using Microsoft.Xna.Framework;

namespace SolaceWeather.Relationships;

internal sealed partial class EmilyLifeService
{
    private NPC? movementNpc;
    private RomanceDateMenu? movementMenu;
    private int[] movementFrames = Array.Empty<int>();
    private int movementDay, movementStep, oldMovementFrame, oldMovementFacing;
    private bool oldMovementFollow, oldMovementIgnore, movementRecoveryUnlocked;
    private double movementStarted;
    private string movementLocation = "";
    private readonly HashSet<int> displayedMovementFrames = new();
    internal bool StartMovement()
    {
        if (!Ready || !BesideEmily() || !DesignBoundaries() || HasReservation || movementNpc != null
            || !state!.Tree.Unlocked.Contains("rhythm") || state.Tree.LastMovementDay >= Today
            || romance.State.Characters.GetValueOrDefault("Emily")?.InConflict == true) return false;
        var npc = Game1.getCharacterFromName("Emily");
        if (Game1.currentLocation.Name is not ("Town" or "HaleyHouse" or "Saloon") || npc.isSleeping.Value || npc.isMoving() || npc.controller != null
            || npc.temporaryController != null || npc.doingEndOfRouteAnimation.Value || npc.Sprite.CurrentAnimation != null
            || Game1.currentLocation.IsOutdoors && (Game1.isRaining || Game1.isGreenRain) || !romance.Dates.Available(npc, Today, Minute, "town-walk")) return false;
        try
        {
            var animations = helper.GameContent.Load<Dictionary<string, string>>("Data/animationDescriptions");
            if (!animations.TryGetValue("emily_exercise", out string? description)) return false;
            movementFrames = description.Split('/')[1].Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            if (movementFrames.Length is < 2 or > 64 || movementFrames.Any(f => f < 0 || f >= npc.Sprite.Texture.Width / 16 * (npc.Sprite.Texture.Height / 32))) return false;
        }
        catch { return false; }
        movementNpc = npc; movementDay = Today; movementStep = 0; movementLocation = Game1.currentLocation.Name;
        oldMovementFrame = npc.Sprite.CurrentFrame; oldMovementFacing = npc.FacingDirection;
        oldMovementFollow = npc.followSchedule; oldMovementIgnore = npc.ignoreScheduleToday;
        movementRecoveryUnlocked = state.Tree.Unlocked.Contains("light");
        npc.Halt(); npc.followSchedule = false; npc.ignoreScheduleToday = true;
        Game1.exitActiveMenu(); Game1.player.Halt(); Game1.player.CanMove = false;
        ShowMovement(); return true;
    }
    private bool MovementValid() => Ready && movementNpc != null && Today == movementDay && BesideEmily()
        && Game1.currentLocation.Name == movementLocation && (!Game1.currentLocation.IsOutdoors || !Game1.isRaining && !Game1.isGreenRain)
        && ReferenceEquals(Game1.activeClickableMenu, movementMenu);
    private void ShowMovement()
    {
        movementStarted = Game1.currentGameTime.TotalGameTime.TotalMilliseconds; displayedMovementFrames.Clear();
        movementMenu = new RomanceDateMenu(movementNpc, movementStep == 0
            ? "Find your rhythm\nNo perfect steps today. Follow the movement, and let it feel a little different each time."
            : "A little room to move\nThere you go. You can enjoy a rhythm without trying to impress anyone.",
            new[] { (movementStep == 0 ? "Follow the movement (10 minutes)" : "Finish together (10 minutes)", (Action)AdvanceMovement), ("Stop here", (Action)CleanupMovement) }, CleanupMovement);
        Game1.activeClickableMenu = movementMenu;
    }
    private void TickMovement()
    {
        if (movementNpc == null) return;
        if (!MovementValid()) { CleanupMovement(); return; }
        int frame = movementFrames[(int)((Game1.currentGameTime.TotalGameTime.TotalMilliseconds - movementStarted) / 140) % movementFrames.Length];
        movementNpc.Sprite.CurrentFrame = frame;
    }
    private void ObserveMovementFrame()
    {
        if (MovementValid() && movementNpc != null) displayedMovementFrames.Add(movementNpc.Sprite.CurrentFrame);
    }
    private void AdvanceMovement()
    {
        if (!MovementValid() || displayedMovementFrames.Count < 2) return;
        Game1.performTenMinuteClockUpdate();
        if (!MovementValid()) { CleanupMovement(); return; }
        if (movementStep++ == 0) { Game1.player.jump(); ShowMovement(); return; }
        bool completed = state!.Tree.RecordMovement(Today);
        if (completed)
        {
            RefreshTree();
            bool recovered = movementRecoveryUnlocked && Game1.player.Stamina < Game1.player.MaxStamina && state.Tree.ClaimRecovery(Today);
            if (recovered) Game1.player.Stamina = Math.Min(Game1.player.MaxStamina, Game1.player.Stamina + 30);
            getMemory()!.Experiences.Record("emily:movement:" + Today, "shared-time", Today,
                "The farmer explicitly followed and completed twenty minutes of movement with Emily. Her native exercise animation was visibly observed. "
                + (recovered ? "The farmer recovered a little energy. " : "No energy reward was given. ")
                + "No romantic commitment or supernatural effect was established.", "dance movement creative expression shared break");
        }
        CleanupMovement();
        if (completed) Game1.activeClickableMenu = new RomanceDateMenu(Game1.getCharacterFromName("Emily"), "That felt good. Sometimes a little movement gives an idea room to breathe.", Array.Empty<(string, Action)>(), Game1.exitActiveMenu);
    }
    private void CleanupMovement()
    {
        if (movementNpc == null) return;
        var npc = movementNpc; movementNpc = null;
        if (ReferenceEquals(Game1.activeClickableMenu, movementMenu)) Game1.exitActiveMenu();
        movementMenu = null; displayedMovementFrames.Clear();
        npc.Sprite.CurrentFrame = oldMovementFrame; npc.faceDirection(oldMovementFacing);
        npc.followSchedule = oldMovementFollow; npc.ignoreScheduleToday = oldMovementIgnore;
        if (!Game1.eventUp && oldMovementFollow) npc.checkSchedule(Game1.timeOfDay);
        if (!Game1.eventUp && Game1.currentMinigame == null && !Game1.player.isInBed.Value) Game1.player.CanMove = true;
    }
}
