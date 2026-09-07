using System.Reflection;
using StardewValley;
using StardewValley.Locations;

namespace SvaPersistenceAudit;

public sealed partial class ModEntry
{
    private void RunDoorwayChecks()
    {
        RequireOwnedWorld(); Game1.exitActiveMenu();
        object movement = Member(Mod("David.SolaceWeather"), "movement") ?? throw new InvalidOperationException("Movement unavailable.");
        var tests = Assembly.LoadFrom(Path.Combine(Helper.DirectoryPath, "SolaceWeather.GameTests.dll"));
        tests.GetType("SolaceWeather.GameTests.DoorwayChecks", true)!.GetMethod("Run")!.Invoke(null, new[] { movement });
        Helper.Data.WriteJsonFile("doorway-checks.json", new { Passed = true, Location = Game1.currentLocation.Name, Cases = new[] { "visible entry route", "native trigger destination", "cancel without warp", "scripted controller preserved", "blocked entry", "adjacent floor rejected" } });
    }
    private void StartDoorwayExit()
    {
        RequireOwnedWorld(); Game1.exitActiveMenu();
        if (Game1.currentLocation is not FarmHouse house) throw new InvalidOperationException("Native exit requires farmhouse.");
        object movement = Member(Mod("David.SolaceWeather"), "movement")!;
        bool handled = (bool)movement.GetType().GetMethod("TryInteract")!.Invoke(movement, new object[] { house.getEntryLocation() })!;
        Helper.Data.WriteJsonFile("doorway-exit-start.json", new { Handled = handled, Location = house.Name, Entry = house.getEntryLocation().ToString(), Walking = Member(movement, "IsWalking") });
        QueueSnapshot("after-native-doorway-walk", 300);
    }
}
