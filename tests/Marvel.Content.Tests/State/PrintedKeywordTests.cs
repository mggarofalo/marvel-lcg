using System.Text.Json;
using Marvel.Content.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.State;

/// <summary>
/// Printed keywords reaching the fields that carry them.
/// </summary>
/// <remarks>
/// <para>
/// <b>The bug these exist for.</b> A card kind registers a field like
/// <c>hazard</c> or <c>crisis</c>, and <c>StateFields.PrintedFrom</c> decides
/// which printed attribute fills it. Everything not in that map reads zero
/// forever — so a side scheme printing <c>Hazard 1</c> reported no hazard, and
/// <c>rr:hazard-icon</c> was unimplementable rather than unimplemented.
/// </para>
/// <para>
/// <b>Why no fixture caught it.</b> The recorded <c>rhino / spider_man /
/// 12345</c> board contains six cards that print one of these attributes, and
/// every one of them sits out of play in the encounter deck, where printed
/// values are not filled at all. Of the two cards in that game that ever hold
/// their token pools, neither prints one. So the recording is silent here, and
/// the mapping is held to the pool instead.
/// </para>
/// </remarks>
public sealed class PrintedKeywordTests
{
    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    private static readonly string CardText =
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json"));

    /// <summary>Every printed attribute name the pool actually uses.</summary>
    private static readonly HashSet<string> PrintedAttributes = ReadAttributeNames();

    [Theory]
    // One card per keyword, each printing 1, each of a kind that registers the
    // field. These are the values the engine was reading as zero.
    [InlineData("01107", "hazard", 1)]          // Breakin' & Takin', a side scheme
    [InlineData("01108", "crisis", 1)]          // Goblin Glider
    [InlineData("01109", "acceleration_icon", 1)] // Bomb Scare
    [InlineData("01167", "quickstrike", 1)]     // a minion
    [InlineData("01102", "toughness", 1)]
    [InlineData("01121", "surge", 1)]           // Weapons Runner
    [InlineData("01101", "guard", 1)]           // measured in the recording
    [InlineData("01099", "boost_const", 2)]     // measured in the recording
    public void APrintedKeywordReachesItsField(string faceId, string field, long expected)
    {
        // In play and holding its pools, which is the only state in which
        // printed values are filled at all.
        var world = new World(Cards, players: 1);
        world.CreateSeat("p0");
        var area = world.AreaOf(DeckType.VillainArea);
        var card = world.CreateCard(faceId, area);

        var fields = StateFields.For(
            card, Cards, players: 1, inPlay: true, hasHeldPools: true,
            hasFirstPlayerToken: false, world);

        Assert.Equal(expected, fields[field]);
    }

    [Fact]
    public void EveryRegisteredFieldWithAPrintedAttributeOfTheSameNameIsMapped()
    {
        // The structural invariant, and the one that would have caught the
        // whole class of bug rather than the eight cases above. For every field
        // any kind registers, if the pool prints an attribute whose name is
        // that field in Pascal case, the map has to fill it from there.
        //
        // Not every mapping is mechanical -- `acceleration_icon` comes from
        // `Acceleration` and `boost_const` from `Boost`, and neither is a case
        // conversion -- so this cannot check them all. It checks the ones that
        // would otherwise be missed in silence.
        var missing = new List<string>();
        foreach (var kind in Enum.GetValues<CardKind>())
        {
            foreach (string field in StateFields.Keys(kind, hasHeldPools: true))
            {
                string pascal = Pascal(field);
                if (PrintedAttributes.Contains(pascal)
                    && !StateFields.FilledFrom.ContainsKey(field))
                {
                    missing.Add($"{field} <- {pascal}");
                }
            }
        }

        Assert.Empty(missing.Distinct().Order(StringComparer.Ordinal));
    }

    [Fact]
    public void EveryMappedAttributeIsOneThePoolPrints()
    {
        // The other direction. A mapping naming an attribute no card prints is
        // dead weight that reads zero and looks implemented, which is the state
        // this whole file exists to end.
        var unprinted = StateFields.FilledFrom.Values
            .Where(attribute => !PrintedAttributes.Contains(attribute))
            .Order(StringComparer.Ordinal);

        Assert.Empty(unprinted);
    }

    [Rule("rr:form-change-form.6")]
    [Fact]
    public void ThePrintedFormAttributeAgreesWithThePrintedText()
    {
        // The engine reads the structured `Form` attribute; this holds it
        // against the words on the card. They are two independent readings of
        // one fact, and a face whose attribute was missed when the dataset was
        // built would show up here rather than silently granting no form.
        var byAttribute = new List<string>();
        var byText = new List<string>();

        using var document = JsonDocument.Parse(CardText);
        foreach (var element in document.RootElement.GetProperty("cards").EnumerateArray())
        {
            string id = element.GetProperty("card_id").GetString()!;
            if (Cards.FormKeyword(id) is { } attribute)
            {
                byAttribute.Add($"{id}={attribute}");
            }

            if (element.TryGetProperty("engine", out var engine)
                && engine.ValueKind == JsonValueKind.Object
                && engine.TryGetProperty("text", out var text)
                && CardCatalog.FormOf(text.GetString()) is { } written)
            {
                byText.Add($"{id}={written}");
            }
        }

        Assert.Equal(byText, byAttribute);
        Assert.Equal(9, byAttribute.Count);
    }

    /// <summary><c>acceleration_icon</c> becomes <c>AccelerationIcon</c>.</summary>
    private static string Pascal(string field) =>
        string.Concat(field.Split('_').Select(
            part => part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..]));

    private static HashSet<string> ReadAttributeNames()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        using var document = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

        foreach (var element in document.RootElement.GetProperty("cards").EnumerateArray())
        {
            if (!element.TryGetProperty("engine", out var engine)
                || engine.ValueKind != JsonValueKind.Object
                || !engine.TryGetProperty("attributes", out var attributes)
                || attributes.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var attribute in attributes.EnumerateObject())
            {
                // A card carrying the key with an empty or zero value is not
                // printing it, and a mapping to it would still be dead.
                if (attribute.Value.GetString() is { Length: > 0 } value && value != "0")
                {
                    names.Add(attribute.Name);
                }
            }
        }

        return names;
    }
}
