using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Play;

/// <summary>
/// The main scheme deck advancing, and the encounter deck running out.
/// </summary>
/// <remarks>
/// The Rhino scenario's main scheme deck holds one card, so the recorded game
/// completes it and the villain wins — the advance never happens. These boards
/// give it two.
/// </remarks>
public sealed class MainSchemeTests
{
    [Rule("rr:main-scheme-main-scheme-deck.2")]
    [Rule("rr:main-scheme-main-scheme-deck.step.1")]
    [Fact]
    public void CompletingAStageRemovesItAndPutsTheNextIntoPlay()
    {
        var printed = new Printed()
            .With("scheme", ("TargetThreat", "3"), ("EscalationThreat", "3"))
            .With("scheme2b", ("StartingThreat", "2"), ("TargetThreat", "9"));
        var world = Board(printed, stages: 2);
        var first = world.TheCardIn(DeckType.MainSchemesArea)!;
        var events = new List<GameEvent>();

        Run(world, printed, events);

        // `.step.1` -- "remove the top main scheme card from the game."
        Assert.Equal(DeckType.RemovedArea, first.Area.Type);
        Assert.Equal(1, first.Tokens["is_completed"]);

        var second = world.TheCardIn(DeckType.MainSchemesArea)!;
        Assert.NotSame(first, second);

        // `.step.3` -- flipped to its B side, with its starting threat.
        Assert.Equal("scheme2b", second.FaceId);
        Assert.Equal(2, second.Tokens["k_threat"]);
        Assert.Equal(Outcome.Unfinished, world.Result);
    }

    [Rule("rr:main-scheme-main-scheme-deck.4")]
    [Fact]
    public void ExcessThreatDoesNotCarryOver()
    {
        // "When the main scheme deck advances, excess threat from the previous
        // stage does not carry over to the new stage." Nine threat against a
        // target of three, and the new stage starts at its own starting value.
        var printed = new Printed()
            .With("scheme", ("TargetThreat", "3"), ("EscalationThreat", "9"))
            .With("scheme2b", ("StartingThreat", "1"), ("TargetThreat", "9"));
        var world = Board(printed, stages: 2);

        Run(world, printed, []);

        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
    }

    [Rule("rr:main-scheme-main-scheme-deck.5")]
    [Rule("rr:acceleration-token.2.1")]
    [Fact]
    public void AccelerationTokensCarryOverAndThreatDoesNot()
    {
        // "Acceleration tokens on it carry over to the new stage", and
        // `rr:acceleration-token.2.1` says why: "unlike other tokens, when a
        // main scheme card leaves play, the acceleration token does not get
        // discarded."
        var printed = new Printed()
            .With("scheme", ("TargetThreat", "3"), ("EscalationThreat", "3"))
            .With("scheme2b", ("StartingThreat", "0"), ("TargetThreat", "9"));
        var world = Board(printed, stages: 2);
        world.TheCardIn(DeckType.MainSchemesArea)!
            .PlaceTokens(EncounterDeck.AccelerationToken, 2);

        Run(world, printed, []);

        var second = world.TheCardIn(DeckType.MainSchemesArea)!;
        Assert.Equal(2, second.Tokens[EncounterDeck.AccelerationToken]);
    }

    [Rule("rr:main-scheme-main-scheme-deck.step.1")]
    [Fact]
    public void CardsAttachedToTheOldStageAreDiscarded()
    {
        // "Discard each card attached to it." Discarded rather than removed
        // with it, so a client is told the card left play.
        var printed = new Printed()
            .With("scheme", ("TargetThreat", "3"), ("EscalationThreat", "3"))
            .With("scheme2b", ("StartingThreat", "0"), ("TargetThreat", "9"));
        var world = Board(printed, stages: 2);
        var first = world.TheCardIn(DeckType.MainSchemesArea)!;
        var attached = world.CreateCard(
            "attachment",
            world.AreaOf(DeckType.UpgradesArea, first.Area.PlayArea, first.ObjectId));

        Run(world, printed, []);

        Assert.Equal(DeckType.EncounterDiscardPile, attached.Area.Type);
    }

    [Rule("rr:main-scheme-main-scheme-deck.step.2")]
    [Rule("rr:main-scheme-main-scheme-deck.step.3")]
    [Fact]
    public void BothSidesOfTheNewStageGetAWhenRevealedWindow()
    {
        // Step 2 resolves the **A** side's ability and step 3 the **B** side's.
        // They are different abilities on different faces, so a reader that
        // went straight to B would silently drop every A-side ability in the
        // pool.
        var printed = new Printed()
            .With("scheme", ("TargetThreat", "3"), ("EscalationThreat", "3"))
            .With("scheme2b", ("StartingThreat", "0"), ("TargetThreat", "9"));
        var world = Board(printed, stages: 2);
        var seen = new Revealing();

        VillainPhase.Schedule(world.Agenda, round: 1);
        Sequence.Finish(world, printed, seen, []);

        Assert.Equal(["scheme2a", "scheme2b"], seen.Faces);
    }

    [Rule("rr:main-scheme-main-scheme-deck.2.1")]
    [Fact]
    public void CompletingTheFinalStageIsTheVillainWinning()
    {
        var printed = new Printed()
            .With("scheme", ("TargetThreat", "3"), ("EscalationThreat", "3"));
        var world = Board(printed, stages: 1);

        Run(world, printed, []);

        Assert.Equal(Outcome.VillainWins, world.Result);
    }

    [Rule("rr:villain-phase.step.1")]
    [Rule("rr:acceleration-icon")]
    [Theory]
    [InlineData(0, 0, 1)]
    // "If any acceleration icons or tokens are active, additional threat equal
    // to the number of such icons and tokens is also placed at this time."
    [InlineData(2, 0, 3)]
    [InlineData(0, 3, 4)]
    [InlineData(1, 1, 3)]
    public void AccelerationIconsAndTokensBothAddToStepOne(int icons, int tokens, int expected)
    {
        var printed = new Printed()
            .With("scheme", ("EscalationThreat", "1"), ("TargetThreat", "99"))
            .With("sideScheme", ("Acceleration", icons.ToString()));
        var world = Board(printed, stages: 1);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        world.CreateCard("sideScheme", world.AreaOf(DeckType.SideSchemesArea));
        if (tokens > 0)
        {
            scheme.PlaceTokens(EncounterDeck.AccelerationToken, tokens);
        }

        VillainPhase.Schedule(world.Agenda, round: 1);
        Sequence.Finish(world, printed, new Silent(), []);

        Assert.Equal(expected, scheme.Tokens["k_threat"]);
    }

    [Rule("rr:encounter-deck.1")]
    [Fact]
    public void AnEmptyEncounterDeckIsRebuiltAndCostsAnAccelerationToken()
    {
        // "If the encounter deck is empty, the encounter discard pile is
        // immediately shuffled to create a new encounter deck. When this
        // occurs, **place an acceleration token next to the main scheme
        // deck.**"
        var printed = new Printed().With("scheme", ("TargetThreat", "99"));
        var world = Board(printed, stages: 1, encounter: 0);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var pile = world.AreaOf(DeckType.EncounterDiscardPile);
        for (int card = 0; card < 5; card++)
        {
            world.CreateCard($"e{card}", pile);
        }

        var events = new List<GameEvent>();
        long before = world.Random.Generator.WordsConsumed;

        Assert.True(EncounterDeck.Reset(world, "test", events));

        Assert.Equal(5, world.AreaOf(DeckType.EncounterDeck).Cards.Count);
        Assert.Empty(pile.Cards);
        Assert.Equal(1, scheme.Tokens[EncounterDeck.AccelerationToken]);
        Assert.True(
            world.Random.Generator.WordsConsumed > before,
            "the new encounter deck must be shuffled from the game's one stream");
    }

    [Rule("rr:encounter-deck.4")]
    [Fact]
    public void AnEncounterDeckAndDiscardBothEmptyLosesTheGame()
    {
        // "If there are no cards in both the encounter deck and the encounter
        // discard pile simultaneously, an infinite loop occurs with an infinite
        // number of acceleration tokens being placed next to the main scheme
        // deck. **If this happens, the players lose.**"
        //
        // Not a quiet no-op the way an empty *player* deck is: the rules work
        // through what the loop would do and then name the result.
        var printed = new Printed().With("scheme", ("TargetThreat", "99"));
        var world = Board(printed, stages: 1, encounter: 0);

        Assert.False(EncounterDeck.Reset(world, "test", []));
        Assert.Equal(Outcome.PlayersLose, world.Result);
    }

    /// <summary>Runs one villain phase, which places threat at step 1.</summary>
    private static void Run(World world, Printed printed, List<GameEvent> events)
    {
        VillainPhase.Schedule(world.Agenda, round: 1);
        Sequence.Finish(world, printed, new Silent(), events);
    }

    /// <summary>A villain, one identity, and a main scheme deck of N stages.</summary>
    private static World Board(Printed printed, int stages, int encounter = 6)
    {
        var world = new World(printed, players: 1);
        world.CreateSeat("p0");
        world.Seats[0].IdentityCard = world.CreateCard("identity", world.Seats[0].Hero);
        world.CreateCard("villain", world.AreaOf(DeckType.VillainArea));

        var scheme = world.CreateCard("schemea,scheme", world.AreaOf(DeckType.MainSchemesArea));
        scheme.TurnTo("scheme");
        for (int stage = 2; stage <= stages; stage++)
        {
            world.CreateCard(
                $"scheme{stage}a,scheme{stage}b", world.AreaOf(DeckType.MainSchemesDeck));
        }

        // Enough encounter cards for the villain's boost card and step 3's
        // deal, unless a test is about the deck running out.
        for (int card = 0; card < encounter; card++)
        {
            world.CreateCard($"e{card}", world.AreaOf(DeckType.EncounterDeck));
        }

        return world;
    }

    /// <summary>Nothing has an ability.</summary>
    private sealed class Silent : NoCardAbilities
    {


        public override IReadOnlyList<GameEvent> Resolve(
            World world, Occurrence occurrence, PendingAbility ability,
            IReadOnlyList<int> paying) => [];

    }

    /// <summary>Records which faces were offered a "When Revealed" window.</summary>
    private sealed class Revealing : NoCardAbilities
    {
        public List<string> Faces { get; } = [];

        public override IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player)
        {
            // Only the scheme stages; the encounter cards this board deals are
            // not what the test is about.
            if (card.FaceId.StartsWith("scheme", StringComparison.Ordinal))
            {
                Faces.Add(card.FaceId);
            }

            return [];
        }


        public override IReadOnlyList<GameEvent> Resolve(
            World world, Occurrence occurrence, PendingAbility ability,
            IReadOnlyList<int> paying) => [];

    }

    /// <summary>Printed data for a handful of made-up cards.</summary>
    private sealed class Printed : ICardFacts
    {
        private readonly Dictionary<string, Dictionary<string, string>> attributes =
            new(StringComparer.Ordinal);

        public Printed With(string faceId, params (string Key, string Value)[] values)
        {
            var table = attributes.TryGetValue(faceId, out var found)
                ? found
                : attributes[faceId] = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in values)
            {
                table[key] = value;
            }

            return this;
        }

        public CardKind Kind(string faceId) => faceId switch
        {
            "identity" => CardKind.AlterEgo,
            "villain" => CardKind.EncounterVillain,
            "attachment" => CardKind.Attachment,
            "sideScheme" => CardKind.EncounterSideScheme,
            _ when faceId.StartsWith("scheme", StringComparison.Ordinal) => CardKind.MainScheme,
            _ => CardKind.Treachery,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];

        public IReadOnlyDictionary<string, string> Attributes(string faceId) =>
            attributes.TryGetValue(faceId, out var found)
                ? found
                : new Dictionary<string, string>(StringComparer.Ordinal);

        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            Attributes(faceId).TryGetValue(attribute, out string? value)
            && long.TryParse(value, out long number)
                ? number
                : fallback;
    }
}
