using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Thor's attack events finish their continuations after an opaque defeat or recovery window.</summary>
public sealed class ThorLokiRecoveryContinuationTests
{
    private const string Treachery = "01186";
    private const string Minion = "01101";

    // Get Over Here!: "Deal 1 damage to a minion. If you have the Aerial trait, engage that
    // enemy." Loki: "Forced Interrupt: When Loki would be defeated, discard the top card of
    // the encounter deck. If that card is a treachery, heal all damage from Loki instead."
    // The board is Marvel.Sim's Absorbing Man expert game with Thor and Hawkeye, seed 2, at the
    // Get Over Here! target choice: Loki has 1 hit point left and Family Feud is in play.
    [Theory]
    [InlineData(Treachery, 0, false, 0)]
    [InlineData(Treachery, 0, true, 0)]
    [InlineData(Treachery, 1, false, 1)]
    [InlineData(Treachery, 1, true, 0)]
    [InlineData(Minion, 0, true, -1)]
    [InlineData(Minion, 1, true, -1)]
    [Rule("rr:ability.4")]
    [Rule("rr:in-play-and-out-of-play.3")]
    [Rule("rr:in-play-and-out-of-play.9")]
    [Rule("rr:leaves-play.1")]
    public void GetOverHereEngagesLokiOnlyWhenItsRecoveryKeepsItInPlay(
        string top, int engaged, bool aerial, int engagedAfter)
    {
        // rr:ability.4: "resolve the ability one sentence at a time", so Loki's interrupt
        // finishes before the engagement test reads the board. A healed Loki never left play
        // and is the chosen incarnation; rr:in-play-and-out-of-play.3: "A card enters play
        // when it moves from an out-of-play area to a play area", so engaging it is a move
        // between play areas. A defeated Loki is in the encounter discard pile, which
        // rr:in-play-and-out-of-play.9 puts out of play, and rr:leaves-play.1 makes it a new
        // copy, so it is never engaged.
        var world = Board("04077", top);
        world.CreateCard("06027", world.AreaOf(DeckType.SideSchemesArea));
        world.CreateCard("06019", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        if (aerial) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: Traits.Granted + "AERIAL", Affects: world.Seats[0].IdentityCard.ObjectId));
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(engaged)));
        loki.TakeDamage(3);
        int incarnation = loki.Incarnation;
        var card = world.CreateCard("06014", world.Seats[0].Hand);

        Play(world, card, loki, []);

        Assert.Single(world.AreaOf(DeckType.EncounterDiscardPile).Cards, c => c.FaceId == top);
        if (engagedAfter < 0)
        {
            Assert.Equal(DeckType.EncounterDiscardPile, loki.Area.Type);
        }
        else
        {
            Assert.Equal(DeckType.EngagedEnemiesArea, loki.Area.Type);
            Assert.Equal(PlayArea.Of(engagedAfter), loki.Area.PlayArea);
            Assert.Equal(incarnation, loki.Incarnation);
            Assert.Equal(0, loki.Damage);
        }
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    // Hammer Throw: "deal 8 damage to an enemy and return Mjolnir to your hand. This attack
    // gains overkill." The boards are Marvel.Sim's Absorbing Man standard games with Thor and
    // Spider-Man, seed 29, and with Thor and Hulk, seed 22, at the Hammer Throw target choice.
    // Absorbing Man: "Absorbing Man gains the trait of each environment in play."
    [Theory]
    [InlineData("06008,06009", Treachery, true)]
    [InlineData("06008,06009", Minion, true)]
    [InlineData("06008,06009", Minion, false)]
    [InlineData("06009,06010,06019", Treachery, true)]
    [InlineData("06009,06010,06019", Minion, true)]
    [Rule("rr:overkill.1")]
    [Rule("rr:in-play-and-out-of-play.9")]
    public void MjolnirReturnsAfterLokisRecoveryUnderAbsorbingMansTraitConstant(
        string upgrades, string top, bool tough)
    {
        // rr:in-play-and-out-of-play.9: "Cards in the encounter deck, encounter discard pile,
        // ... are out of play", so Loki's discard moves no environment and Absorbing Man's
        // copied traits, which set no hit points, stay as they were. rr:overkill.1: "If this
        // attack defeats a minion, deal the damage beyond its hit points to the villain"; a
        // healed Loki is not defeated, so nothing passes to the villain.
        var world = Board("04076", top);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        if (tough) Statuses.Give(world, villain, Statuses.Tough);
        var placed = upgrades.Split(',').Select(code => world.CreateCard(code,
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0))).ToArray();
        var hammer = Assert.Single(placed, upgrade => upgrade.FaceId == "06009");
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var card = world.CreateCard("06005", world.Seats[0].Hand);
        var first = world.CreateCard("01090", world.Seats[0].Hand);
        var second = world.CreateCard("01003", world.Seats[0].Hand);

        Play(world, card, loki, [first.ObjectId, second.ObjectId]);

        bool recovered = top == Treachery;
        Assert.Same(world.Seats[0].Hand, hammer.Area);
        Assert.Equal(recovered ? DeckType.EngagedEnemiesArea : DeckType.EncounterDiscardPile, loki.Area.Type);
        Assert.Equal(0, loki.Damage);
        Assert.Equal(recovered || tough ? 0 : 4, villain.Damage);
        Assert.Equal(recovered && tough, Statuses.Has(world, villain, Statuses.Tough));
        Assert.Empty(world.Agenda.Outstanding);
    }

    // Spider-Tracer: "Attach to a minion. Forced Interrupt: When attached minion is defeated,
    // remove 3 threat from a scheme." The board is Marvel.Sim's Absorbing Man expert game with
    // Thor and Spider-Man, seed 18, at the Get Over Here! target choice: a Hydra Regular with
    // 1 hit point left carries Spider-Man's Spider-Tracer and is engaged with Spider-Man.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:leaves-play.1")]
    public void GetOverHereFindsTheTracedMinionGoneAfterTheTracerRemovesThreat(bool aerial)
    {
        // The tracer's interrupt resolves while the minion is defeated, before the engagement
        // test; the defeated minion is a new copy (rr:leaves-play.1) and is never engaged.
        var world = Board("04077", Minion);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        main.PlaceTokens("k_threat", 6);
        if (aerial) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: Traits.Granted + "AERIAL", Affects: world.Seats[0].IdentityCard.ObjectId));
        var regular = world.CreateCard("04152", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        regular.TakeDamage(1);
        var tracer = world.CreateCard("01007",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), regular.ObjectId, cardOwner: 1));
        var card = world.CreateCard("06014", world.Seats[0].Hand);

        Play(world, card, regular, []);

        Assert.Equal(DeckType.EncounterDiscardPile, regular.Area.Type);
        Assert.Equal(DeckType.DiscardPile, tracer.Area.Type);
        Assert.Equal(3, main.Tokens["k_threat"]);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static World Board(string villain, string top)
    {
        var world = ThorActualFixture.Board("06001a", players: 2, villain: villain);
        world.CreateCard(top, world.AreaOf(DeckType.EncounterDeck));
        return world;
    }

    private static void Play(World world, Card card, Card target, int[] resources)
    {
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, resources, []));
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(target.ObjectId), []);
        // Spider-Tracer's scheme choice takes the main scheme; optional responses are declined.
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        for (var next = Sequence.Work(world, world.Facts, runner, []); next is not null;
            next = Sequence.Work(world, world.Facts, runner, []))
            Sequence.Answer(world, world.Facts, runner, next,
                next.Affordances.Any(a => a.AnchorId == main.ObjectId) ? Decision.Take(main.ObjectId) : Decision.Decline, []);
    }
}
