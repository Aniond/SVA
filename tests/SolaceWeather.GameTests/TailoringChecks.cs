using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SolaceWeather.Core;
using StardewValley;
using StardewValley.Objects;

namespace SolaceWeather.GameTests;

public sealed partial class ModEntry
{
    private static readonly List<string> tailoringLoadEvents = new();
    private static void BeforeFarmerRestore(Farmer target)
    {
        if (target.Name != "SvaAudit" || !Program.GetSavesFolder().Contains("phone", StringComparison.OrdinalIgnoreCase)) return;
        if (!target.modData.TryGetValue("David.SolaceWeather/TailoringV1", out string raw)) return;
        var saved = JsonSerializer.Deserialize<TailoringSaveState>(raw)!;
        lock (tailoringLoadEvents)
        {
            foreach (var order in saved.Orders.Where(o => o.Status == "fulfilled"))
            {
                bool exists = order.Slot == "shirt" ? Game1.shirtData.ContainsKey(order.ItemId) : Game1.pantsData.ContainsKey(order.ItemId);
                tailoringLoadEvents.Add("BeforeFarmerRestore:" + order.Id + ":" + exists + ":" + ItemRegistry.GetData(order.QualifiedId)?.TextureName);
            }
        }
    }
    private void TailoringSnapshot()
    {
        var service = Tailoring(); var state = PhoneGet<TailoringSaveState>(service, "State");
        var checks = new Dictionary<string, bool>();
        foreach (var order in state.Orders.Where(o => o.Status == "fulfilled"))
        {
            var data = ItemRegistry.GetData(order.QualifiedId); checks[order.Id + "NativeId"] = data != null && data.TextureName == "David.SolaceWeather/Tailoring/" + order.Id;
            if (data != null)
            {
                var texture = data.GetTexture(); using var png = new MemoryStream(); texture.SaveAsPng(png, texture.Width, texture.Height);
                checks[order.Id + "NativePixels"] = Convert.ToHexString(SHA256.HashData(png.ToArray())) == order.Sha256;
            }
        }
        Helper.Data.WriteJsonFile("tailoring-state.json", new { Passed = checks.Values.All(v => v), Checks = checks, LoadEvents = tailoringLoadEvents.ToArray(),
            Orders = state.Orders.Select(o => new { o.Id, o.QualifiedId, o.Status, o.Sha256, o.Slot, o.Recipe, o.FulfilledDay, o.Color }), state.Recovery,
            Shirt = Game1.player.shirtItem.Value?.QualifiedItemId, ShirtColor = Game1.player.shirtItem.Value?.clothesColor.Value.ToString(),
            Pants = Game1.player.pantsItem.Value?.QualifiedItemId, PantsColor = Game1.player.pantsItem.Value?.clothesColor.Value.ToString(),
            Items = Game1.player.Items.Where(i => i?.ItemId.StartsWith("David.SolaceWeather_Custom_") == true).Select(i => new { i.QualifiedItemId, i.Stack }),
            SavedPayload = Game1.player.modData.GetValueOrDefault("David.SolaceWeather/TailoringV1")?.Length });
    }
    private void TailoringGenerateFixture()
    {
        var service = Tailoring(); var state = PhoneGet<TailoringSaveState>(service, "State");
        string slot = File.ReadAllText(Path.Combine(Helper.DirectoryPath, "tailoring-slot.txt")).Trim();
        string recipePath = Path.Combine(Helper.DirectoryPath, "tailoring-recipe.txt");
        string recipe = File.Exists(recipePath) ? File.ReadAllText(recipePath).Trim() : "plain";
        var order = state.Create(slot, recipe, "Emerald green, cream stars and gold stitches", Game1.Date.TotalDays) ?? throw new InvalidOperationException("Order blocked.");
        state.Begin(order.Id); PhoneSet(service, "requestId", order.Id);
        PhoneSet(service, "request", Task.FromResult(File.ReadAllBytes(Path.Combine(Helper.DirectoryPath, "generated-fabric.jpg"))));
        Call(service, "CompleteRequest"); Call(service, "Open"); TailoringSnapshot(); captureRequested = true; captureDelay = 12;
    }
    private void TailoringCraftChecks()
    {
        var service = Tailoring(); var state = PhoneGet<TailoringSaveState>(service, "State"); var order = state.Pending ?? throw new InvalidOperationException("Preview missing.");
        Game1.player.MaxItems = 36;
        while (Game1.player.Items.Count < Game1.player.MaxItems) Game1.player.Items.Add(null);
        var checks = new Dictionary<string, bool>();
        int Count(string id) => Game1.player.Items.Where(i => i?.QualifiedItemId == id).Sum(i => i.Stack);
        if (Count("(O)428") < 3) Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)428", 3));
        if (Count("(O)771") < 20) Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)771", 20));
        if (order.Recipe == "mend") Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)70"));
        if (order.Recipe == "renew") Game1.player.addItemToInventoryBool(ItemRegistry.Create("(O)62"));
        var cloth = Game1.player.Items.OfType<StardewValley.Object>().Where(i => i.QualifiedItemId == "(O)428").ToArray();
        var prior = cloth.Select(i => i.questItem.Value).ToArray(); int c = Count("(O)428"), f = Count("(O)771");
        try { foreach (var item in cloth) item.questItem.Value = true; checks["ProtectedClothRejected"] = !(bool)Call(service, "Craft", order.Id)! && Count("(O)428") == c && Count("(O)771") == f; }
        finally { for (int i = 0; i < cloth.Length; i++) cloth[i].questItem.Value = prior[i]; }
        checks["CraftConsumesExactMaterials"] = (bool)Call(service, "Craft", order.Id)! && Count("(O)428") == c - 3 && Count("(O)771") == f - 20;
        checks["DuplicateCraftRejected"] = !(bool)Call(service, "Craft", order.Id)!;
        checks["SharedCooldown"] = state.Create("shirt", "plain", "Another idea", Game1.Date.TotalDays) == null;
        var garment = Game1.player.Items.OfType<Clothing>().Single(i => i.ItemId == order.ItemId);
        if (order.Slot == "shirt") Game1.player.shirtItem.Value = (Clothing)garment.getOne(); else Game1.player.pantsItem.Value = (Clothing)garment.getOne();
        if (order.Slot == "shirt") Game1.player.shirtItem.Value.clothesColor.Value = Color.CornflowerBlue; else Game1.player.pantsItem.Value.clothesColor.Value = Color.DarkRed;
        Helper.Data.WriteJsonFile("tailoring-craft-" + order.Slot + ".json", new { Passed = checks.Values.All(v => v), Checks = checks, Fixture = "Guarded materials and duplicate equip fixture; production craft path, previously generated live donor, no new provider call" });
        TailoringSnapshot();
    }
    private void TailoringNextDay()
    {
        Tailoring(); var date = new WorldDate(Game1.Date) { TotalDays = Game1.Date.TotalDays + 3 };
        Game1.year = date.Year; Game1.season = date.Season; Game1.dayOfMonth = date.DayOfMonth; EmilyStage();
    }
    private void TailoringRender()
    {
        Tailoring(); Game1.exitActiveMenu(); Game1.player.faceDirection((Game1.player.FacingDirection + 1) % 4);
        captureRequested = true; captureDelay = 10;
    }
    private void TailoringRecoveryChecks()
    {
        var service = Tailoring(); var state = PhoneGet<TailoringSaveState>(service, "State");
        var checks = new Dictionary<string, bool>(); var originalMeter = state.Recovery;
        var oldTime = Game1.currentGameTime; int health = Game1.player.health; float energy = Game1.player.Stamina;
        var shirt = Game1.player.shirtItem.Value; var pants = Game1.player.pantsItem.Value;
        try
        {
            Game1.exitActiveMenu(); Game1.player.CanMove = true; state.Recovery = new();
            var mend = state.Orders.Single(o => o.Recipe == "mend" && o.Status == "fulfilled");
            var renew = state.Orders.Single(o => o.Recipe == "renew" && o.Status == "fulfilled");
            Game1.player.shirtItem.Value = (Clothing)ItemRegistry.Create(mend.QualifiedId); Game1.player.pantsItem.Value = (Clothing)ItemRegistry.Create(renew.QualifiedId);
            Game1.player.health = 20; Game1.player.Stamina = 20; PhoneSet(service, "lastHealth", 20);
            double clock = 10000;
            void Tick(int times) { for (int i = 0; i < times; i++) { Game1.currentGameTime = new GameTime(TimeSpan.FromSeconds(clock++), TimeSpan.FromSeconds(1)); Call(service, "Recover"); } }
            Tick(59); checks["NoEarlyRecovery"] = Game1.player.health == 20 && Game1.player.Stamina == 20;
            Tick(1); checks["EquippedRecovery"] = Game1.player.health == 21 && Game1.player.Stamina == 22;
            Game1.player.health--; Tick(10); checks["DamageCooldown"] = Game1.player.health == 20;
            Game1.activeClickableMenu = new StardewValley.Menus.InventoryPage(0, 0, 800, 600); Tick(120);
            checks["NoMenuRecovery"] = Game1.player.health == 20 && Game1.player.Stamina == 22;
            Game1.exitActiveMenu(); Game1.player.shirtItem.Value = null; Game1.player.pantsItem.Value = null; Tick(120);
            checks["NoUnequippedRecovery"] = Game1.player.health == 20 && Game1.player.Stamina == 22;
            Game1.player.shirtItem.Value = (Clothing)ItemRegistry.Create(mend.QualifiedId); Game1.player.pantsItem.Value = (Clothing)ItemRegistry.Create(renew.QualifiedId);
            Tick(1400); checks["DailyCaps"] = state.Recovery.HealthToday == 20 && state.Recovery.EnergyToday == 40;
            var restored = JsonSerializer.Deserialize<TailoringRecovery>(JsonSerializer.Serialize(state.Recovery))!;
            checks["CapsPersist"] = restored.HealthToday == 20 && restored.EnergyToday == 40;
        }
        finally { state.Recovery = originalMeter; Game1.currentGameTime = oldTime; Game1.player.health = health; Game1.player.Stamina = energy; Game1.player.shirtItem.Value = shirt; Game1.player.pantsItem.Value = pants; PhoneSet(service, "lastHealth", health); PhoneSet(service, "damageUntil", 0d); }
        Helper.Data.WriteJsonFile("tailoring-recovery-checks.json", new { Passed = checks.Values.All(v => v), Checks = checks, Fixture = "Accelerated active-time ticks through production native recovery; original health, energy, equipment and cap record restored" });
    }
    private void TailoringIsolationChecks()
    {
        var service = Tailoring(); var state = PhoneGet<TailoringSaveState>(service, "State");
        var original = JsonSerializer.Serialize(state); var checks = new Dictionary<string, bool>();
        var orders = state.Orders.Where(o => o.Status == "fulfilled").ToArray();
        try
        {
            var other = new Farmer(); other.UniqueMultiplayerID = Game1.player.UniqueMultiplayerID + 1;
            Call(service, "Restore", other);
            checks["OtherFarmerHasNoOrders"] = PhoneGet<TailoringSaveState>(service, "State").Orders.Count == 0;
            checks["PreviousIdsUnpublished"] = orders.All(o => ItemRegistry.GetData(o.QualifiedId) == null);
            // Warmed misses must be invalidated by production publication.
            Game1.player.modData["David.SolaceWeather/TailoringV1"] = original; Call(service, "Restore", Game1.player);
            checks["PreviousIdsRepublishedAfterCachedMiss"] = orders.All(o => ItemRegistry.GetData(o.QualifiedId)?.TextureName == "David.SolaceWeather/Tailoring/" + o.Id);
            var corrupt = JsonSerializer.Deserialize<TailoringSaveState>(original)!; corrupt.Orders.First(o => o.Status == "fulfilled").Png = "damaged";
            Game1.player.modData["David.SolaceWeather/TailoringV1"] = JsonSerializer.Serialize(corrupt); Call(service, "Restore", Game1.player);
            checks["DamagedArtRetainsIds"] = orders.All(o => ItemRegistry.GetData(o.QualifiedId) != null);
            checks["DamagedArtNoProviderCall"] = PhoneGet<Task<byte[]>?>(service, "request") == null;
        }
        finally { Game1.player.modData["David.SolaceWeather/TailoringV1"] = original; Call(service, "Restore", Game1.player); }
        foreach (var order in orders)
        {
            var texture = ItemRegistry.GetData(order.QualifiedId)!.GetTexture();
            using var restoredPng = new MemoryStream(); texture.SaveAsPng(restoredPng, texture.Width, texture.Height);
            checks[order.Id + "RestoredPixelsAfterCorruption"] = Convert.ToHexString(SHA256.HashData(restoredPng.ToArray())) == order.Sha256;
        }
        Helper.Data.WriteJsonFile("tailoring-isolation-checks.json", new { Passed = checks.Values.All(v => v), Checks = checks, Fixture = "Alternate farmer and corrupt payload restored in memory through production loader, original payload restored before saving" });
    }
}
