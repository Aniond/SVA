namespace SolaceWeather.Core;

/// <summary>One fish-delivery pilot per farmer. Only game code grants items or confirms delivery.</summary>
public sealed class DeliveryRequest
{
    public string Status { get; set; } = "none";
    public int? CreatedDay { get; set; }
    public int? CompletedDay { get; set; }
    public bool TestFishPending { get; set; }
    public bool TestFishGranted { get; set; }
    public string DeliveredItemId { get; set; } = "";
    public string DeliveredItemName { get; set; } = "";

    public bool IsValid() => DeliveredItemId != null && DeliveredItemId.Length <= 100
        && DeliveredItemName != null && DeliveredItemName.Length <= 200
        && !(TestFishPending && TestFishGranted)
        && (Status switch {
            "none" => CreatedDay == null && CompletedDay == null && !TestFishPending && !TestFishGranted
                && DeliveredItemId == "" && DeliveredItemName == "",
            "active" => CreatedDay >= 0 && CompletedDay == null && DeliveredItemId == "" && DeliveredItemName == "",
            "completed" => CreatedDay >= 0 && CompletedDay >= CreatedDay && !TestFishPending
                && DeliveredItemId.Length > 0 && DeliveredItemName.Length > 0,
            _ => false
        });

    public bool Offer(string request, int day, bool provideTestFish)
    {
        if (!IsValid() || Status != "none" || request != "fish" || day < 0) return false;
        Status = "active";
        CreatedDay = day;
        TestFishPending = provideTestFish;
        return true;
    }

    public void MarkTestFishGranted()
    {
        if (Status != "active" || !TestFishPending) return;
        TestFishPending = false;
        TestFishGranted = true;
    }

    public bool Complete(string itemId, string itemName, int day)
    {
        if (!IsValid() || Status != "active" || day < CreatedDay || string.IsNullOrWhiteSpace(itemId)
            || itemId.Length > 100 || string.IsNullOrWhiteSpace(itemName) || itemName.Length > 200) return false;
        Status = "completed";
        CompletedDay = day;
        DeliveredItemId = itemId;
        DeliveredItemName = itemName;
        TestFishPending = false;
        return true;
    }
}
