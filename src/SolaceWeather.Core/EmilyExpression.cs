namespace SolaceWeather.Core;

public static class EmilyExpression
{
    public static readonly IReadOnlyList<string> Names = Array.AsReadOnly(new[] { "delighted", "thoughtful", "concerned", "stern", "neutral" });
    public static string Normalize(string? value) => value != null && Names.Contains(value) ? value : "neutral";
    // Native candidate cells: happy=1, sad=2, neutral=0. No dedicated thoughtful/stern artwork is claimed.
    public static int PortraitIndex(string? value) => value switch {
        "delighted" or "happy" or "warm" => 1, "concerned" or "sad" => 2,
        "angry" => 5, // Preserve historical replies saved before Emily's authored vocabulary.
        _ => 0 };
}
