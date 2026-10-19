using System.Text.RegularExpressions;

namespace Marvel.Client;

/// <summary>One AND clause with quoted or pipe-separated alternatives.</summary>
internal sealed record CardSearchCondition(string Field, char Operator, IReadOnlyList<string> Values)
{
    private static readonly Regex Token = new("""\G\s*(?:(?<field>[a-z]{1,2})(?<operator>[:!<>]))?(?<values>(?:"[^"]*"|[^\s|":!<>]+)(?:\|(?:"[^"]*"|[^\s|":!<>]+))*)(?=\s|$)""",
        RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex Value = new(@"""([^""]*)""|([^|]+)", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    internal static IReadOnlyList<CardSearchCondition> Parse(string query)
    {
        if (query.Length > 2048) throw new FormatException("Search is limited to 2048 characters.");
        var conditions = new List<CardSearchCondition>();
        int offset = 0;
        query = query.Trim();
        while (offset < query.Length)
        {
            var token = Token.Match(query, offset);
            if (!token.Success) throw new FormatException($"Check search syntax near character {offset + 1}. Quote phrases and separate filters with spaces.");
            string field = token.Groups["field"].Value;
            if (!CardSearch.Fields.ContainsKey(field)) throw new FormatException($"Unknown search field '{field}'.");
            char operation = token.Groups["operator"].Success ? token.Groups["operator"].Value[0] : ':';
            var values = Value.Matches(token.Groups["values"].Value).Select(value => value.Groups[1].Success ? value.Groups[1].Value : value.Groups[2].Value).ToArray();
            conditions.Add(new(field, operation, values));
            offset += token.Length;
        }
        return conditions;
    }
}
