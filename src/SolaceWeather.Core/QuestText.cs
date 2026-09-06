using System.Text.RegularExpressions;

namespace SolaceWeather.Core;

public readonly record struct ItemTextRange(int Start, int Length);

public static class QuestText
{
    public static List<ItemTextRange> FindItems(string text, IEnumerable<string> names)
    {
        var marked = new bool[text.Length];
        foreach (string name in names.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct(StringComparer.OrdinalIgnoreCase))
            foreach (Match match in Regex.Matches(text, @"(?<![\p{L}\p{N}_])" + Regex.Escape(name) + @"(?![\p{L}\p{N}_])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                Array.Fill(marked, true, match.Index, match.Length);
        var ranges = new List<ItemTextRange>();
        for (int i = 0; i < marked.Length; i++)
        {
            if (!marked[i]) continue;
            int start = i;
            while (i + 1 < marked.Length && marked[i + 1]) i++;
            ranges.Add(new ItemTextRange(start, i - start + 1));
        }
        return ranges;
    }
}
