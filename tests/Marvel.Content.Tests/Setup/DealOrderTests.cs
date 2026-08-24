using System.Text.Json;
using Marvel.Content.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

/// <summary>
/// The deal order against a recorded game.
/// </summary>
/// <remarks>
/// <para>
/// A card's <c>object_id</c> is its position in the sequence
/// <see cref="Dealer.DealOrder"/> produces, and <c>object_id</c> is on the wire
/// in every state digest. So the strongest available test is to hold the
/// sequence against a digest a real game produced:
/// <c>datasets/digest/vectors.json</c> names the card at every id for
/// <c>rhino / spider_man / 12345</c>.
/// </para>
/// <para>
/// This is the same claim <c>py_src/unit_test/test_setup_dataset.py</c> makes
/// about the Python mirror, against the same fixture. Two implementations held
/// to one recording is the whole shape of this port.
/// </para>
/// </remarks>
public sealed class DealOrderTests
{
    private static readonly SetupCatalog Catalog =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static JsonElement RecordedStepZero()
    {
        using var vectors = JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("digest", "vectors.json")));
        var board = vectors.RootElement.GetProperty("cases")[0];

        Assert.Equal("rhino", board.GetProperty("campaign").GetString());
        Assert.Equal(12345, board.GetProperty("seed").GetInt32());

        using var step = JsonDocument.Parse(board.GetProperty("step_digests")[0].GetString()!);
        return step.RootElement.Clone();
    }

    private static List<(int Id, string Card)> RecordedCards()
    {
        var cards = new List<(int, string)>();
        foreach (var card in RecordedStepZero().GetProperty("cards").EnumerateArray())
        {
            cards.Add((card.GetProperty("id").GetInt32(), card.GetProperty("card").GetString()!));
        }

        return cards;
    }

    [Fact]
    public void EveryRecordedIdIsDealtAtThatPosition()
    {
        var recorded = RecordedCards();
        var dealt = Dealer.DealOrder(Catalog, "rhino", ["spider_man"]);

        Assert.Equal(81, recorded.Count);
        Assert.Equal(recorded.Count, dealt.Count);

        for (int id = 0; id < recorded.Count; id++)
        {
            Assert.Equal(id, recorded[id].Id);
            Assert.Contains(recorded[id].Card, dealt[id].Faces);
        }
    }

    [Fact]
    public void TheOnlyCardNotShowingItsFirstFaceIsTheMainScheme()
    {
        // An identity needs no flip: `MoveBToFront` puts the alter-ego side
        // first. A main scheme is created `1A,1B` and turned to `1B` when it
        // enters play. A port that flipped both, or neither, would still pass
        // the membership test above — so the exception is pinned by name.
        var recorded = RecordedCards();
        var dealt = Dealer.DealOrder(Catalog, "rhino", ["spider_man"]);

        var flipped = recorded
            .Where((card, id) => card.Card != dealt[id].Faces[0])
            .Select(card => (card.Id, card.Card))
            .ToList();

        Assert.Equal([(48, "01097b")], flipped);
        Assert.Equal("01097a,01097b", dealt[48].Spec);
    }

    [Fact]
    public void SourcesRunInTheDocumentedOrder()
    {
        var seen = new List<CreationSource>();
        foreach (var creation in Dealer.DealOrder(Catalog, "rhino", ["spider_man"]))
        {
            if (seen.Count == 0 || seen[^1] != creation.Source)
            {
                seen.Add(creation.Source);
            }
        }

        Assert.Equal(
            [
                CreationSource.Rules, CreationSource.Identity, CreationSource.Obligation,
                CreationSource.Nemesis, CreationSource.HeroDeck, CreationSource.PlayerDeck,
                CreationSource.MainScheme, CreationSource.Villain, CreationSource.Encounter,
                CreationSource.EncounterSet,
            ],
            seen);
    }

    [Fact]
    public void EverySeatIsDealtBeforeTheScenario()
    {
        var seats = Dealer.DealOrder(Catalog, "klaw", ["captain_marvel", "she_hulk"])
            .Select(creation => creation.Player)
            .ToList();

        Assert.Equal(Creation.Scenario, seats[0]);          // the rules card
        Assert.Equal(1, seats.IndexOf(0));
        Assert.True(seats.LastIndexOf(0) < seats.IndexOf(1));
        Assert.All(seats[(seats.LastIndexOf(1) + 1)..], seat => Assert.Equal(Creation.Scenario, seat));
    }

    [Theory]
    [InlineData("01001a,01001b", "01001b,01001a")]
    [InlineData("01094", "01094")]
    [InlineData("29001a,29001b,29001c", "29001b,29001a,29001c")]
    public void MoveBToFrontMatchesTheEngine(string printed, string dealt) =>
        Assert.Equal(dealt, Dealer.MoveBToFront(printed));

    [Fact]
    public void AnUnknownNameIsAFailureNotAShortBoard()
    {
        Assert.Throws<KeyNotFoundException>(
            () => Dealer.DealOrder(Catalog, "no_such_scenario", ["spider_man"]));
        Assert.Throws<KeyNotFoundException>(
            () => Dealer.DealOrder(Catalog, "rhino", ["no_such_hero"]));
    }

    [Fact]
    public void ModularSetsAreJoinedOnDemandAndNotInTheDataset()
    {
        var rhino = Catalog.Campaign("rhino");

        Assert.Equal(["standard"], rhino.EncounterSets);
        Assert.Equal(["bomb_scare"], rhino.ModularSets);
        Assert.Equal(["standard", "bomb_scare"], Dealer.EncounterSetNames(rhino));
    }

    [Fact]
    public void SpiderDealsPeniParkerAndNotTheSuit()
    {
        // The one hero the engine does not deal from her own descriptor. The
        // declared identity is the SP//dr Suit; the engine substitutes Peni
        // Parker, whose `a` face is already the alter-ego side.
        var sp = Catalog.Hero("sp_dr");
        Assert.Equal(["31002a,31002b"], Dealer.IdentitySpecs(sp));
        Assert.NotEqual(sp.Hero, Dealer.IdentitySpecs(sp));

        var dealt = Dealer.DealOrder(Catalog, "rhino", ["sp_dr"]);
        var identity = dealt.First(creation => creation.Source == CreationSource.Identity);
        Assert.Equal("31002a", identity.Faces[0]);
    }

    [Fact]
    public void EveryOtherHeroIsDealtFromHerOwnDescriptor()
    {
        // The substitution is keyed on a printed id prefix, so the test that
        // matters is that it fires exactly once across the whole dataset.
        var substituted = Catalog.HeroNames
            .Where(name => !Dealer.IdentitySpecs(Catalog.Hero(name))
                .SequenceEqual(Catalog.Hero(name).Hero.Select(Dealer.MoveBToFront)))
            .ToList();

        Assert.Equal(["sp_dr"], substituted);
    }

    [Fact]
    public void TheCatalogHoldsTheWholeDataset()
    {
        Assert.Equal(135, Catalog.CampaignNames.Count);
        Assert.Equal(63, Catalog.HeroNames.Count);
        Assert.Equal(184, Catalog.EncounterSetNames.Count);
    }
}
