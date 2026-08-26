using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

/// <summary>
/// Expert mode — <c>rr:modes-of-play</c>.
/// </summary>
/// <remarks>
/// <para>
/// "Expert Mode is a modification of standard mode for advanced players who
/// seek a greater challenge", and <c>.2</c> says what it changes: the listed
/// expert villain stages, and the Expert encounter set added to the deck. Both
/// are the dealer's business and it has always done them — the <c>_expert</c>
/// campaigns list different stages and sets.
/// </para>
/// <para>
/// What was missing is that <b>86 cards in the pool read the mode</b>, 59 of
/// them main schemes, and a board that did not carry it could not answer them.
/// The Unus scenario's own main scheme is one: "<b>Setup:</b> Reveal the Gene
/// Pool side scheme. <i>In expert mode, deal each player a facedown encounter
/// card.</i>"
/// </para>
/// </remarks>
public sealed class ExpertModeTests
{
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:modes-of-play.2")]
    [Theory]
    [InlineData("unus", false)]
    [InlineData("unus_expert", true)]
    [InlineData("rhino", false)]
    [InlineData("rhino_expert", true)]
    public void TheBoardCarriesWhichModeItWasDealtFor(string campaign, bool expert)
    {
        // The flag itself, held against the campaign it came from — so a
        // scenario whose expert variant is spelled some other way in the data
        // fails here rather than quietly playing on standard.
        Assert.Equal(expert, Deal(campaign).Expert);
    }

    [Rule("rr:modes-of-play.2")]
    [Rule("rr:appendix-ii-setup.step.12.a")]
    [Theory]
    [InlineData("unus", 0)]
    [InlineData("unus_expert", 1)]
    public void TheMainSchemesExpertClauseDealsACardOnlyInExpertMode(
        string campaign, int extra)
    {
        // "**Setup:** [...] In expert mode, deal each player a facedown
        // encounter card." Both halves asserted, because a clause that always
        // fired and a clause that never did would each pass one of them.
        //
        // `rr:deal` leaves the card facedown in front of the player: step 4 of
        // the villain phase is what turns it over, and it drains the queue
        // however the cards got there. So this counts what is waiting, not what
        // has happened.
        var world = Deal(campaign);

        Assert.Equal(
            extra,
            world.Cards.Count(card => card.Area.Type == DeckType.DealtEncounterCardsDeck));
    }

    [Rule("rr:in-player-order")]
    [Fact]
    public void EachPlayerMeansEachPlayerAndNotTheFirstOne()
    {
        // "Deal **each player** a facedown encounter card." One seat cannot
        // tell that apart from "deal the first player one", so the table has
        // two — and `rr:in-player-order` is why the loop goes round in order
        // rather than in seat order, since the deck can empty part-way.
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(
                Dealer.DealOrder(Setup, "unus_expert", ["spider_man", "spider_man"]), Cards),
            ["Spider-Man", "Spider-Woman"],
            Seed,
            AuthoredCards.Runner(),
            expert: true);

        Assert.Equal(
            2,
            world.Cards.Count(card => card.Area.Type == DeckType.DealtEncounterCardsDeck));
    }

    [Rule("rr:modes-of-play")]
    [Fact]
    public void ACardAskingAboutAnotherModeSaysSoRatherThanReadingExpert()
    {
        // `rr:modes-of-play` names four and lets them combine. Only expert is
        // modelled, and heroic is the reason the others are not a set: `.4`
        // gives it a level number rather than a flag. A card reaching for one
        // of the other three must not quietly read this one.
        var world = Deal("rhino");
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            """
            { "cards": [ { "card": "01097b", "abilities": [ {
                "trigger": { "event": "WhenThreatPlaced", "timing": "ForcedResponse" },
                "effect": { "if": {
                    "test": { "inExpertMode": "heroic" },
                    "then": { "placeThreat": { "scheme": "this", "amount": 1 } } } }
            } ] } ] }
            """));

        var refused = Assert.Throws<RulesNotImplementedException>(() => runner.Resolve(
            world,
            new Occurrence(0, ["WhenThreatPlaced"], Subject: scheme.ObjectId),
            new PendingAbility(scheme.ObjectId, AbilityType.ForcedResponse, -1)));

        Assert.Contains("'heroic' mode", refused.Message, StringComparison.Ordinal);
    }

    [Rule("rr:villain-phase.step.1")]
    [Fact]
    public void ThreatOnTheMainSchemeFeedsGenePool()
    {
        // "[star] **Forced Response:** After resolving step one of the villain
        // phase, place 1 threat on Gene Pool." Step one is where threat goes on
        // the main scheme, and the star puts the ability on the scheme itself.
        //
        // Gene Pool is on the table because it prints the setup keyword and
        // `rr:appendix-ii-setup.step.11` put it there — the two halves of this
        // scenario's engine work meeting on one board.
        var world = Deal("unus");
        var pool = Assert.Single(world.Cards, card => card.FaceId == AuthoredCards.GenePool);
        long before = pool.Tokens.GetValueOrDefault("k_threat");

        world.Agenda.Add(new PhaseStep(Steps.PlaceThreat, 1, 1));
        Agendas.Finish(world, Cards, AuthoredCards.Runner());

        Assert.Equal(before + 1, pool.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:appendix-ii-setup")]
    [Theory]
    [InlineData("unus")]
    [InlineData("unus_expert")]
    public void TheUnusBoardDeals(string campaign)
    {
        // The scenario the constant-ability work was for. Its villain, its main
        // scheme and its one setup card are read, so the deal completes — which
        // is what the twelve encounter cards still to author will be held
        // against.
        var world = Deal(campaign);

        Assert.Equal("Unus", Cards.Title(world.TheCardIn(DeckType.VillainArea)!.FaceId));
        Assert.Equal(
            DeckType.SideSchemesArea,
            Assert.Single(world.Cards, card => card.FaceId == AuthoredCards.GenePool).Area.Type);
    }

    private static World Deal(string campaign) => WorldSetup.Deal(
        Cards,
        Blueprints.From(Dealer.DealOrder(Setup, campaign, ["spider_man"]), Cards),
        ["Spider-Man"],
        Seed,
        AuthoredCards.Runner(),
        expert: Setup.Campaign(campaign).Expert);
}
