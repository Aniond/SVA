namespace SolaceWeather.Core;

/// <summary>Expression order shared by Abigail's native sheet and the installed Abigail Modern artwork.</summary>
public static class AbigailExpression
{
    public static readonly IReadOnlyList<string> Names = Array.AsReadOnly(new[] { "neutral", "happy", "sad", "angry", "thoughtful", "serious", "surprised", "warm" });
    public static string Normalize(string? expression) => expression != null && Names.Contains(expression) ? expression : "neutral";
    public static int PortraitIndex(string? expression) => expression switch {
        "happy" => 1, "sad" => 2, "angry" => 3, "thoughtful" => 4,
        "serious" => 6, "surprised" => 7, "warm" => 9, _ => 0
    };
}
