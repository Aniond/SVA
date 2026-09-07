namespace SolaceWeather.Core;

/// <summary>Requested reaction vocabulary with backward-compatible stored expression names.</summary>
public static class CharacterReactions
{
    public static bool UsesFive(string name) => name is "Abigail" or "Emily" or "Haley" or "Penny" or "Alex" or "Maru";
    public static IReadOnlyList<string> Names(string name) => UsesFive(name) ? EmilyExpression.Names : AbigailExpression.Names;
    public static string Normalize(string name, string? expression) => UsesFive(name) && expression != null && EmilyExpression.Names.Contains(expression)
        ? expression : AbigailExpression.Normalize(expression);
    public const string Prompt = "Use the structured expression field: delighted, thoughtful, concerned, stern or neutral, matching the dominant emotion. Never prefix spoken text with a reaction tag. Stern means a proportionate firm boundary, not punishment for declining. Existing portrait cells or neutral fallbacks are used; do not invent expression images. ";
}
