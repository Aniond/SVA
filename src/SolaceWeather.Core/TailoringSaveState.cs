using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace SolaceWeather.Core;

public sealed class TailoringOrder
{
    public string Id { get; set; } = "";
    public string Slot { get; set; } = "shirt";
    public string Recipe { get; set; } = "plain";
    public string Design { get; set; } = "";
    public string Status { get; set; } = "draft";
    public string Error { get; set; } = "";
    public int CreatedDay { get; set; }
    public int? FulfilledDay { get; set; }
    public int Attempts { get; set; }
    public string Png { get; set; } = "";
    public string Sha256 { get; set; } = "";
    public string Color { get; set; } = "255 255 255";
    [System.Text.Json.Serialization.JsonIgnore]
    public string ItemId => "David.SolaceWeather_Custom_" + Id;
    [System.Text.Json.Serialization.JsonIgnore]
    public string QualifiedId => (Slot == "shirt" ? "(S)" : "(P)") + ItemId;
    [System.Text.Json.Serialization.JsonIgnore]
    public string DisplayName => Recipe == "mend" ? "Emily's Mend Jacket" : Recipe == "renew" ? "Emily's Renew Trousers" : Slot == "shirt" ? "Emily's Custom Jacket" : "Emily's Custom Trousers";
    public bool IsValid() => Id != null && Regex.IsMatch(Id, "^[a-f0-9]{32}$") && Slot is "shirt" or "pants"
        && (Recipe == "plain" || Recipe == "mend" && Slot == "shirt" || Recipe == "renew" && Slot == "pants")
        && Design != null && Design.Length is >= 1 and <= 300 && CreatedDay >= 0 && Attempts is >= 0 and <= 3
        && Status is "draft" or "generating" or "failed" or "ready" or "fulfilled" or "cancelled"
        && Error != null && Error.Length <= 80 && Png != null && Png.Length <= 350000 && Sha256 != null && Sha256.Length <= 64
        && Color != null && Color.Split(' ').Length == 3 && Color.Split(' ').All(c => byte.TryParse(c, out _))
        && (Status == "fulfilled" ? FulfilledDay >= CreatedDay : FulfilledDay == null);
    public bool HasValidImage()
    {
        try
        {
            if (!IsValid() || Png.Length < 100) return false;
            var bytes = Convert.FromBase64String(Png);
            if (bytes.Length > 256000 || Convert.ToHexString(SHA256.HashData(bytes)) != Sha256) return false;
            return bytes.Length > 24 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10})
                && System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16,4)) == (Slot == "shirt" ? 256 : 192)
                && System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20,4)) == (Slot == "shirt" ? 32 : 688);
        }
        catch (FormatException) { return false; }
    }
}

public sealed class TailoringSaveState
{
    public int Version { get; set; } = 1;
    public long FarmerId { get; set; }
    public List<TailoringOrder> Orders { get; set; } = new();
    public int LastCraftDay { get; set; } = -100;
    public TailoringRecovery Recovery { get; set; } = new();
    public bool IsValid(long farmer) => Version == 1 && FarmerId == farmer && Orders != null && Orders.Count <= 24
        && Orders.All(o => o != null && o.IsValid()) && Orders.Select(o => o.Id).Distinct().Count() == Orders.Count
        && Orders.Sum(o => (long)o.Png.Length) <= 2800000 && LastCraftDay >= -100 && Recovery?.IsValid() == true
        && Orders.Count(o => o.Status is not ("fulfilled" or "cancelled")) <= 1;
    [System.Text.Json.Serialization.JsonIgnore]
    public TailoringOrder? Pending => Orders.LastOrDefault(o => o.Status is not ("fulfilled" or "cancelled"));
    public TailoringOrder? Create(string slot, string recipe, string design, int day)
    {
        if (!IsValid(FarmerId) || Pending != null || Orders.Count >= 24 || day < 0 || day - LastCraftDay < 3 || string.IsNullOrWhiteSpace(design)) return null;
        var order = new TailoringOrder { Id = Guid.NewGuid().ToString("N"), Slot = slot, Recipe = recipe, Design = design.Trim(), CreatedDay = day };
        if (!order.IsValid()) return null; Orders.Add(order); return order;
    }
    public bool Begin(string id)
    {
        if (Pending is not { } o || o.Id != id || o.Status is not ("draft" or "failed") || o.Attempts >= 3) return false;
        o.Status = "generating"; o.Attempts++; o.Error = ""; return true;
    }
    public bool Fail(string id, string code)
    {
        if (Pending is not { } o || o.Id != id || o.Status != "generating") return false;
        o.Status = "failed"; o.Error = code.Length <= 80 ? code : "generation_failed"; return true;
    }
    public bool Ready(string id, string png, string sha256, string color)
    {
        if (Pending is not { } o || o.Id != id || o.Status != "generating") return false;
        o.Png = png; o.Sha256 = sha256; o.Color = color;
        if (!o.HasValidImage() || Orders.Sum(x => (long)x.Png.Length) > 2800000)
        { o.Png = o.Sha256 = ""; o.Color = "255 255 255"; return false; }
        o.Status = "ready"; return true;
    }
    public bool Fulfill(string id, int day)
    {
        if (Pending is not { } o || o.Id != id || o.Status != "ready" || !o.HasValidImage() || day < o.CreatedDay || day - LastCraftDay < 3) return false;
        o.Status = "fulfilled"; o.FulfilledDay = LastCraftDay = day; return true;
    }
    public bool Cancel(string id)
    {
        if (Pending is not { } o || o.Id != id) return false;
        o.Status = "cancelled"; o.Png = o.Sha256 = ""; return true;
    }
}
