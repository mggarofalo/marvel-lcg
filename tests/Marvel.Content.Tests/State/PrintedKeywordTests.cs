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

            if (element.TryGetProperty("text_plain", out var text)
                && CardCatalog.FormOf(text.GetString()) is { } written)
            {
                byText.Add($"{id}={written}");
            }
        }

        Assert.Equal(byText, byAttribute);
        Assert.Equal(11, byAttribute.Count);
    }

    [Rule("rr:consequential-damage")]
    [Theory]
    // **The star means two different things and the card kind tells them
    // apart.** On an ally's ATK or THW it is a consequential damage icon; the
    // number before it is the value, whatever the table size.
    [InlineData("01002", "THW", 1, 1)]    // Black Cat, THW "1*"
    [InlineData("01011", "ATK", 2, 1)]    // Spider-Woman, ATK "2*"
    [InlineData("01011", "THW", 2, 1)]
    public void AnAllysStarIsConsequentialDamageAndNotPerPlayer(
        string faceId, string power, long value, long icons)
    {
        // At four players, because at one the two readings agree -- `1*` is 1
        // either way, and every recorded game has one player. That is exactly
        // why no fixture could catch this.
        Assert.Equal(value, Cards.PrintedValue(faceId, power, players: 4));
        Assert.Equal(icons, Cards.ConsequentialDamage(faceId, power));
    }

    [Rule("rr:hit-points")]
    [Theory]
    // And everywhere else it still is the per-player icon.
    [InlineData("01094", "HP", 56)]           // Rhino, HP "14*", four players
    [InlineData("01097b", "TargetThreat", 28)] // The Break-In!, "7*"
    public void EverywhereElseTheStarIsStillPerPlayer(
        string faceId, string attribute, long expected)
    {
        Assert.Equal(expected, Cards.PrintedValue(faceId, attribute, players: 4));
        Assert.Equal(0, Cards.ConsequentialDamage(faceId, attribute));
    }

    [Rule("rr:consequential-damage")]
    [Fact]
    public void AnAllyInPlayReportsItsConsequentialDamageOnTheWire()
    {
        // `attack_consequential_damage` and `thwart_consequential_damage` are
        // registered fields on every ally and read zero until now, because they
        // are not an attribute of their own to map from -- the icons are stars
        // printed inside `ATK` and `THW`.
        //
        // `01011` Spider-Woman prints ATK 2 and THW 2, each with one icon.
        var world = new World(Cards, players: 1);
        world.CreateSeat("p0");
        var ally = world.CreateCard(
            "01011", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));

        var fields = StateFields.For(
            ally, Cards, players: 1, inPlay: true, hasHeldPools: true,
            hasFirstPlayerToken: false, world);

        Assert.Equal(2, fields["attack"]);
        Assert.Equal(2, fields["thwart"]);
        Assert.Equal(1, fields["attack_consequential_damage"]);
        Assert.Equal(1, fields["thwart_consequential_damage"]);
    }

    [Rule("rr:consequential-damage")]
    [Fact]
    public void TheWholePoolAgreesAboutWhatAnAllysStarMeans()
    {
        // The reading, held against the whole pool rather than three cards:
        // for every ally ATK/THW value, the number before the star is
        // MarvelSDB's printed value and the star count is its consequential
        // damage. 640 of 644 agree; the four that do not are two cards
        // upstream records with a base of -1, where the star count still does.
        //
        // **Held against the vendored snapshot and not against the dataset.**
        // `datasets/cards/` is generated from `datasets/marvelsdb/`, so
        // comparing it with itself would say only that the generator is
        // consistent. Upstream's own fields are the independent reading, and
        // they are what this walks.
        int agree = 0, differ = 0;
        foreach (var element in Upstream())
        {
            string id = element.GetProperty("code").GetString()!;
            var stats = element;

            // `26002` is one upstream record for a double-sided upgrade and
            // the dataset carries its two faces instead -- see
            // `datasets/cards/supplement.json`. It is not an ally either way.
            if (!Cards.Has(id) || Cards.Kind(id) != CardKind.Ally)
            {
                continue;
            }

            foreach (var (power, printed, icons) in new[]
            {
                ("ATK", "attack", "attack_cost"), ("THW", "thwart", "thwart_cost"),
            })
            {
                if (!Cards.Attributes(id).ContainsKey(power)
                    || !stats.TryGetProperty(printed, out var value))
                {
                    continue;
                }

                long want = value.GetInt64();
                long cost = stats.TryGetProperty(icons, out var found) ? found.GetInt64() : 0;
                bool ok = Cards.PrintedValue(id, power, players: 3) == want
                    && Cards.ConsequentialDamage(id, power) == cost;
                if (ok)
                {
                    agree++;
                }
                else
                {
                    // The star count agrees even where the base does not.
                    Assert.Equal(cost, Cards.ConsequentialDamage(id, power));
                    differ++;
                }
            }
        }

        Assert.Equal(640, agree);
        Assert.Equal(4, differ);
    }

    /// <summary>Every card in the vendored MarvelSDB snapshot.</summary>
    private static IEnumerable<JsonElement> Upstream()
    {
        foreach (string path in Directory
            .EnumerateFiles(RepositoryPaths.Dataset("marvelsdb", "pack"), "*.json")
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            foreach (var card in document.RootElement.EnumerateArray())
            {
                yield return card.Clone();
            }
        }
    }

    [Rule("rr:villain-defeat.3")]
    [Rule("rr:identity.2")]
    [Theory]
    // `rr:villain-defeat.3` decides whether a defeated stage's attachments and
    // tokens carry over by whether "the new stage of the villain has the **same
    // title**", so a title is rules data and not decoration. Rhino's three
    // stages share one.
    [InlineData("01094", "Rhino")]
    [InlineData("01095", "Rhino")]
    [InlineData("01096", "Rhino")]
    // And `rr:identity.2`: "if a card refers to a hero or alter-ego by title,
    // it refers only to the identity with that title." Angel's third face is a
    // different character by name.
    [InlineData("42001a", "Angel")]
    [InlineData("42001c", "Archangel")]
    public void APrintedTitleIsRulesDataAndNotDecoration(string faceId, string title)
    {
        Assert.Equal(title, Cards.Title(faceId));
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
            if (!element.TryGetProperty("attributes", out var attributes)
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
    [Rule("rr:team-up")]
    [Rule("rr:unique-icon.1.2")]
    [Fact]
    public void EveryNameATeamUpCardPrintsIsACharacterThatExists()
    {
        // A team-up card names two characters, and if a name matches nothing
        // the card is unplayable for ever — silently, because "no such
        // character is in play" and "no such character exists" look identical
        // from inside the restriction.
        //
        // Two of the thirty-four names the pool prints are the reason this is
        // a test rather than an assumption: `Black Panther/T'Challa` and
        // `Black Panther/Shuri` are titles no card carries. They are read as
        // two halves, which is what makes them resolve.
        var names = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var card in Every())
        {
            if (Cards.Attributes(card).TryGetValue("TeamUp", out string? printed))
            {
                names.UnionWith(printed.Split(';', StringSplitOptions.RemoveEmptyEntries));
            }
        }

        Assert.NotEmpty(names);
        foreach (string name in names)
        {
            Assert.True(
                name.Split('/', StringSplitOptions.RemoveEmptyEntries).All(Exists),
                $"the team-up name '{name}' matches no card's title or subtitle");
        }
    }

    /// <summary>Whether any card in the pool is titled or subtitled this.</summary>
    private static bool Exists(string name) => Every().Any(card =>
        string.Equals(Cards.Title(card), name, StringComparison.Ordinal)
        || string.Equals(Cards.Subtitle(card), name, StringComparison.Ordinal));

    /// <summary>Every printed card id in the pool.</summary>
    private static IEnumerable<string> Every()
    {
        using var document = JsonDocument.Parse(CardText);
        foreach (var card in document.RootElement.GetProperty("cards").EnumerateArray())
        {
            if (card.TryGetProperty("card_id", out var id) && id.GetString() is { } faceId)
            {
                yield return faceId;
            }
        }
    }

}
