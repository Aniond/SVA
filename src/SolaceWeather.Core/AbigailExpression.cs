namespace SolaceWeather.Core;

/// <summary>Expression order shared by Abigail's native sheet and the installed Abigail Modern artwork.</summary>
public static class AbigailExpression
{
    public static readonly IReadOnlyList<string> Names = Array.AsReadOnly(new[] { "neutral", "happy", "sad", "angry", "thoughtful", "serious", "surprised", "warm" });
    public static string Normalize(string? expression) => expression != null && Names.Contains(expression) ? expression : "neutral";
    public static int PortraitIndex(string? expression) => expression switch {
        "happy" or "delighted" => 1, "sad" or "concerned" => 2, "angry" => 3, "thoughtful" => 4,
        "serious" or "stern" => 6, "surprised" => 7, "warm" => 9, _ => 0
    };
}
