using System.Security.Cryptography;
using SolaceWeather.Controls;
using SolaceWeather.Core;
using SolaceWeather.Relationships;
using StardewValley;
using StardewValley.Menus;

namespace SolaceWeather.Tailoring;

internal sealed partial class TailoringService
{
    private bool Beside() => Ready && canCraft() && !Game1.eventUp && Game1.currentMinigame == null
        && Game1.getCharacterFromName("Emily") is { } npc && npc.currentLocation == Game1.currentLocation
        && Microsoft.Xna.Framework.Vector2.Distance(npc.Tile, Game1.player.Tile) <= 3;
    internal bool Open()
    {
        if (!Beside()) return false;
        if (state!.Pending is { } order) { ShowOrder(order); return true; }
        var choices = new List<(string, Action)> { ("Custom jacket: 3 Cloth + 20 Fiber", () => Describe("shirt", "plain")), ("Custom trousers: 3 Cloth + 20 Fiber", () => Describe("pants", "plain")) };
        if (canRecover()) { choices.Add(("Mend jacket: also 1 Jade (health recovery)", () => Describe("shirt", "mend"))); choices.Add(("Renew trousers: also 1 Aquamarine (energy recovery)", () => Describe("pants", "renew"))); }
        choices.Add(("Not today", Close));
        Show("Made for you\nCreate a new fabric pattern for a fixed jacket or trousers shape. One garment every three days. Materials are used only after you approve the finished preview.", choices.ToArray()); return true;
    }
    private static void Close() => Game1.exitActiveMenu();
    private static void Show(string text, params (string, Action)[] choices) => Game1.activeClickableMenu = new RomanceDateMenu(Game1.getCharacterFromName("Emily"), text, choices, Close);
    private void Describe(string slot, string recipe)
    {
        if (!Beside() || recipe != "plain" && !canRecover()) { Close(); return; }
        Game1.activeClickableMenu = new FabricInput(value =>
        {
            if (Game1.activeClickableMenu is FabricInput input) input.Release();
            if (!Beside()) { Close(); return; }
            var order = state!.Create(slot, recipe, value, Today);
            if (order == null) { Show("Let's leave a little time between projects.\nWait three days after your last garment. There is room for 24 saved orders per farm.", ("Close", Close)); return; }
            Persist(); ShowOrder(order);
        });
    }
    private sealed class FabricInput : NamingMenu
    {
        internal FabricInput(Action<string> accept) : base(accept.Invoke, "Describe your fabric: colors and a simple pattern", "")
        { FilterInput = false; textBox.textLimit = 300; textBox.limitWidth = false; randomButton.bounds.X = -1000; }
        internal void Release() { textBox.Selected = false; if (ReferenceEquals(Game1.keyboardDispatcher.Subscriber, textBox)) Game1.keyboardDispatcher.Subscriber = null; }
        protected override void cleanupBeforeExit() { Release(); base.cleanupBeforeExit(); }
    }
    private void ShowOrder(TailoringOrder order)
    {
        if (!Beside()) { Close(); return; }
        string details = order.DisplayName + "\n" + CostLabel(order);
        if (order.Status == "ready" && !damaged.Contains(order.Id))
        {
            Game1.activeClickableMenu = new TailoringPreview(ItemRegistry.Create(order.QualifiedId), details + "\n" + Description(order), () => Craft(order.Id), () => Cancel(order.Id));
        }
        else if (order.Status == "generating") Show(details + "\nThe fabric image is being created. You can close this and check back; no materials have been used.", ("Close and check later", Close), ("Cancel order", () => Cancel(order.Id)));
        else
        {
            var choices = new List<(string, Action)>();
            if (order.Status is "draft" or "failed" && order.Attempts < 3) choices.Add((order.Attempts == 0 ? "Generate preview (uses Gemini credits)" : "Try generation again (uses Gemini credits)", () => Begin(order.Id)));
            choices.Add(("Cancel order", () => Cancel(order.Id)));
            Show(details + (order.Status == "failed" || damaged.Contains(order.Id) ? "\nThe image is unavailable. No materials were used." : "\nA new image request uses Gemini credits. Review the wearable preview before crafting."), choices.ToArray());
        }
    }
    private void Begin(string id)
    {
        if (!Beside() || request != null || state!.Pending?.Id != id || !state.Begin(id)) return;
        Persist(); requestId = id; cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        string? key = Environment.GetEnvironmentVariable("GEMINI_API_KEY", EnvironmentVariableTarget.User);
        if (string.IsNullOrWhiteSpace(key)) key = Environment.GetEnvironmentVariable("GEMINI_API_KEY") ?? Environment.GetEnvironmentVariable("GOOGLE_API_KEY");
        // The swatch is a local visual reference, never player/farm data or credentials.
        try { request = new GeminiTailoringImage(Http).Generate(key ?? "", File.ReadAllBytes(Path.Combine(helper.DirectoryPath, "assets/tailoring/reference.png")), state.Pending!.Design, cancellation.Token); }
        catch { state.Fail(id, "reference_unavailable"); cancellation.Dispose(); cancellation = null; Persist(); }
        ShowOrder(state.Pending);
    }
    private void CompleteRequest()
    {
        if (request?.IsCompleted != true) return;
        var completed = request; request = null; cancellation?.Dispose(); cancellation = null;
        var order = state!.Pending; if (order?.Id != requestId || order.Status != "generating") { _ = completed.Exception; return; }
        try
        {
            var converted = FabricConverter.Convert(completed.GetAwaiter().GetResult(), Template(order.Slot), order.Slot);
            if (!state.Ready(order.Id, Convert.ToBase64String(converted.Png), Convert.ToHexString(SHA256.HashData(converted.Png)), converted.Color)) throw new InvalidDataException();
            Register(order); Publish();
        }
        catch { state.Fail(order.Id, "generation_unavailable"); }
        Persist(); Game1.addHUDMessage(new HUDMessage(order.Status == "ready" ? "Emily's fabric preview is ready. Talk to her to review it." : "Emily's fabric image was unavailable. No materials were used."));
    }
    private void Cancel(string id)
    {
        if (!Ready || state!.Pending?.Id != id) return;
        cancellation?.Cancel(); if (request != null) _ = request.ContinueWith(t => { _ = t.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
        request = null; cancellation?.Dispose(); cancellation = null;
        var order = state.Pending; state.Cancel(id); catalog.Remove(order.ItemId); images.Remove(Route(order)); damaged.Remove(id);
        helper.GameContent.InvalidateCache(Route(order)); Publish(); Persist(); Close();
    }
    private static string CostLabel(TailoringOrder order) => "3 Cloth + 20 Fiber" + (order.Recipe == "mend" ? " + 1 Jade" : order.Recipe == "renew" ? " + 1 Aquamarine" : "");
    private static Dictionary<string, int> Costs(TailoringOrder order)
    {
        var costs = new Dictionary<string, int> { ["(O)428"] = 3, ["(O)771"] = 20 };
        if (order.Recipe == "mend") costs["(O)70"] = 1; if (order.Recipe == "renew") costs["(O)62"] = 1; return costs;
    }
    internal bool Craft(string id)
    {
        if (!Beside() || state!.Pending is not { } order || order.Id != id || order.Status != "ready" || damaged.Contains(id)
            || !order.HasValidImage() || Today - state.LastCraftDay < 3 || order.Recipe != "plain" && !canRecover()) return false;
        int empty = -1; for (int i = 0; i < Game1.player.Items.Count; i++) if (Game1.player.Items[i] == null) { empty = i; break; }
        var available = Game1.player.Items.OfType<StardewValley.Object>().Where(o => !o.questItem.Value && !o.modData.ContainsKey(QuickStack.ProtectedKey) && o.Stack > 0).ToArray();
        var cost = Costs(order);
        if (empty < 0 || cost.Any(c => available.Where(o => o.QualifiedItemId == c.Key).Sum(o => o.Stack) < c.Value))
        { Show("Bring the materials and leave one empty inventory slot.\nProtected and quest items will stay with you.", ("Back to preview", () => ShowOrder(order))); return false; }
        Item garment = ItemRegistry.Create(order.QualifiedId); if (garment is not StardewValley.Objects.Clothing) return false;
        var paid = new List<(StardewValley.Object Item, int Index, int Stack)>();
        int oldCraftDay = state.LastCraftDay;
        try
        {
            foreach (var c in cost)
            {
                int remaining = c.Value;
                foreach (var item in available.Where(o => o.QualifiedItemId == c.Key).OrderBy(o => o.Quality))
                {
                    int take = Math.Min(remaining, item.Stack); if (take == 0) break;
                    int index = Game1.player.Items.IndexOf(item); paid.Add((item, index, item.Stack));
                    item.Stack -= take; if (item.Stack == 0) Game1.player.Items[index] = null; remaining -= take;
                }
            }
            if (!state.Fulfill(id, Today)) throw new InvalidOperationException();
            Game1.player.Items[empty] = garment; Persist();
        }
        catch
        {
            Game1.player.Items[empty] = null;
            foreach (var part in paid) { part.Item.Stack = part.Stack; Game1.player.Items[part.Index] = part.Item; }
            state.LastCraftDay = oldCraftDay; order.Status = "ready"; order.FulfilledDay = null; Persist(); return false;
        }
        crafted(Today, order.DisplayName); Show("Made for you\nYour garment is in your inventory. Try it on whenever you like.", ("Thank you, Emily", Close)); return true;
    }
}
