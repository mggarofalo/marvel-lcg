using System.Net;
using System.Text;

namespace Marvel.Godot;

/// <summary>Translates the card dataset's bounded display markup to safe Godot BBCode.</summary>
internal static class CardRulesMarkup
{
    private static readonly Dictionary<string, string> Symbols =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["physical"] = "●",
            ["energy"] = "⚡",
            ["mental"] = "◉",
            ["wild"] = "★",
            ["star"] = "✦",
            ["per_hero"] = "◆",
        };

    /// <summary>Preserves supported emphasis and symbols while escaping all other markup.</summary>
    public static string ToBbCode(string markup, string fallback)
    {
        string source = string.IsNullOrWhiteSpace(markup) ? fallback : markup;
        var result = new StringBuilder(source.Length + 32);
        var literal = new StringBuilder();

        void Flush()
        {
            if (literal.Length == 0)
            {
                return;
            }
            result.Append(WebUtility.HtmlDecode(literal.ToString())
                .Replace("[", "[lb]", StringComparison.Ordinal));
            literal.Clear();
        }

        for (int index = 0; index < source.Length;)
        {
            if (Token(source, index, "<b>", out int consumed)
                || Token(source, index, "<b/>", out consumed))
            {
                Flush();
                result.Append("[b]");
                index += consumed;
            }
            else if (Token(source, index, "</b>", out consumed))
            {
                Flush();
                result.Append("[/b]");
                index += consumed;
            }
            else if (Token(source, index, "<i>", out consumed)
                     || Token(source, index, "<em>", out consumed))
            {
                Flush();
                result.Append("[i]");
                index += consumed;
            }
            else if (Token(source, index, "</i>", out consumed)
                     || Token(source, index, "</em>", out consumed))
            {
                Flush();
                result.Append("[/i]");
                index += consumed;
            }
            else if (Token(source, index, "<hr />", out consumed)
                     || Token(source, index, "<hr/>", out consumed))
            {
                Flush();
                result.Append("\n────────────\n");
                index += consumed;
            }
            else if (source.AsSpan(index).StartsWith("[[", StringComparison.Ordinal)
                     && source.IndexOf("]]", index + 2, StringComparison.Ordinal) is int end
                     && end >= 0)
            {
                Flush();
                AppendLiteral(result, source[(index + 2)..end], italic: true);
                index = end + 2;
            }
            else if (source[index] == '['
                     && source.IndexOf(']', index + 1) is int symbolEnd
                     && symbolEnd >= 0
                     && Symbols.TryGetValue(source[(index + 1)..symbolEnd], out string? symbol))
            {
                Flush();
                result.Append("[b]").Append(symbol).Append("[/b]");
                index = symbolEnd + 1;
            }
            else
            {
                literal.Append(source[index]);
                index++;
            }
        }

        Flush();
        return result.ToString();
    }

    /// <summary>Turns printed resource codes into stable, readable glyphs.</summary>
    public static string ResourceIcons(string codes)
    {
        var result = new StringBuilder(codes.Length * 2);
        foreach (char code in codes)
        {
            result.Append(code switch
            {
                'R' => "●",
                'Y' => "⚡",
                'B' => "◉",
                'G' => "★",
                _ => code,
            });
            result.Append(' ');
        }
        return result.ToString().TrimEnd();
    }

    private static bool Token(string source, int index, string token, out int consumed)
    {
        consumed = token.Length;
        return source.AsSpan(index).StartsWith(token, StringComparison.OrdinalIgnoreCase);
    }

    private static void AppendLiteral(StringBuilder into, string text, bool italic)
    {
        if (italic)
        {
            into.Append("[i]");
        }
        into.Append(WebUtility.HtmlDecode(text)
            .Replace("[", "[lb]", StringComparison.Ordinal));
        if (italic)
        {
            into.Append("[/i]");
        }
    }
}
