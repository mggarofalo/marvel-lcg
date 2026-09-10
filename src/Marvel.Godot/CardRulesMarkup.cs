using System.Net;
using System.Text;
using Godot;

namespace Marvel.Godot;

/// <summary>Translates the card dataset's bounded display markup to safe Godot BBCode.</summary>
internal static class CardRulesMarkup
{
    public const string ResourceFontSourcePath = "res://assets/fonts/ChampionsIcons.ttf";
    public const string ResourceFontPath =
        "res://assets/fonts/ChampionsIcons.runtime.tres";

    private const string ResourceFontManifestName =
        "Marvel.Godot.Assets.ChampionsIcons.ttf";
    private static FontFile? resourceFont;

    private static readonly Dictionary<string, string> Symbols =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["physical"] = "P",
            ["energy"] = "E",
            ["mental"] = "M",
            ["wild"] = "W",
            ["star"] = "✦",
            ["per_hero"] = "◆",
        };

    private static readonly HashSet<string> ResourceSymbols = new(
        ["physical", "mental", "energy", "wild"],
        StringComparer.OrdinalIgnoreCase);

    /// <summary>Loads the pinned font without relying on Godot's import scan.</summary>
    public static Font ResourceFont()
    {
        if (resourceFont is not null)
        {
            return resourceFont;
        }

        using Stream source = typeof(CardRulesMarkup).Assembly
            .GetManifestResourceStream(ResourceFontManifestName)
            ?? throw new InvalidOperationException(
                $"Embedded resource {ResourceFontManifestName} is unavailable.");
        var data = new byte[source.Length];
        source.ReadExactly(data);
        resourceFont = new FontFile { Data = data };
        resourceFont.TakeOverPath(ResourceFontPath);
        return resourceFont;
    }

    /// <summary>Preserves supported emphasis and symbols while escaping all other markup.</summary>
    public static string ToBbCode(
        string markup,
        string fallback,
        InterfaceScale scale = InterfaceScale.Standard)
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
                string symbolName = source[(index + 1)..symbolEnd];
                if (ResourceSymbols.Contains(symbolName))
                {
                    ResourceIconMetrics metrics = VisualSystem.ResourceIcon(symbol, scale);
                    result.Append("[font=")
                        .Append(ResourceFontPath)
                        .Append("][font_size=")
                        .Append(metrics.FontSize)
                        .Append(']')
                        .Append(symbol)
                        .Append("[/font_size][/font]");
                }
                else
                {
                    result.Append("[b]").Append(symbol).Append("[/b]");
                }
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
        return string.Join(' ', ResourceGlyphs(codes));
    }

    /// <summary>Returns normalized resource glyphs in printed order.</summary>
    public static string[] ResourceGlyphs(string codes) =>
        [.. Resources(codes).Select(resource => resource.Icon)];

    /// <summary>Returns readable names paired with their canonical font glyphs.</summary>
    public static (string Name, string Icon)[] ResourceTokens(string codes) =>
        [.. Resources(codes)];

    /// <summary>Expands printed resource codes into accessible resource names.</summary>
    public static string ResourceNames(string codes) =>
        string.Join(", ", Resources(codes).Select(resource => resource.Name));

    private static List<(string Name, string Icon)> Resources(string value)
    {
        string codes = value.Trim().ToUpperInvariant() switch
        {
            "PHYSICAL" => "R",
            "MENTAL" => "B",
            "ENERGY" => "Y",
            "WILD" => "W",
            _ => value.ToUpperInvariant(),
        };
        var resources = new List<(string Name, string Icon)>();
        foreach (char code in codes)
        {
            (string Name, string Icon)? resource = code switch
            {
                'R' or 'P' => ("Physical", "P"),
                'B' or 'M' => ("Mental", "M"),
                'Y' or 'E' => ("Energy", "E"),
                'W' or 'G' => ("Wild", "W"),
                _ => null,
            };
            if (resource is { } recognized)
            {
                resources.Add(recognized);
            }
        }
        return resources;
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
