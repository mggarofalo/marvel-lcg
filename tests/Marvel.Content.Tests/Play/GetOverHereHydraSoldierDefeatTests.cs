using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.HammerThrowEncounterDealStageTests;

namespace Marvel.Content.Tests.Play;

/// <summary>Get Over Here! (06014) defeats Hydra Soldier (04153), whose When Defeated deals an encounter card.</summary>
public sealed class GetOverHereHydraSoldierDefeatTests
{
    // Get Over Here!: "Deal 1 damage to a minion. If you have the Aerial trait, engage that
    // enemy." Hydra Soldier: "When Defeated: Deal the engaged player an encounter card."
    // The board is Marvel.Sim's Absorbing Man standard game with Thor and Spider-Man, seed 12,
    // at the Get Over Here! target choice: two Hydra Soldiers are engaged with Thor, one with
    // 1 hit point left, and a Hydra Regular is engaged with Spider-Man.
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:deal-deal-an-encounter-card")]
    [Rule("rr:encounter-deck.1")]
    [Rule("rr:in-play-and-out-of-play.9")]
    [Rule("rr:leaves-play.1")]
    public void TheSoldiersFacedownDealResolvesBeforeTheEngagementTestFindsItGone(bool aerial, bool lastEncounterCard)
    {
        // rr:when-defeated-abilities.2.1: "A defeated card leaves play after its 'When
        // Defeated' ability is resolved". rr:deal-deal-an-encounter-card: "the player takes
        // the top card of the encounter deck and places it facedown in front of them. This
        // card is not revealed at this time." rr:encounter-deck.1: "If the encounter deck is
        // empty, the encounter discard pile is immediately shuffled to create a new encounter
        // deck. When this occurs, place an acceleration token". Ruling 2c98d8b065c9e8b0 deals,
        // reshuffles and discards the soldier before the next instruction.
        // rr:in-play-and-out-of-play.9: cards in the encounter deck and discard pile "are out
        // of play", and rr:leaves-play.1 makes a card that left play a new copy, so the
        // in-play test fails and the soldier is not engaged, with or without AERIAL.
        var game = Board(aerial);
        var deck = game.World.AreaOf(DeckType.EncounterDeck);
        var discard = game.World.AreaOf(DeckType.EncounterDiscardPile);
        if (lastEncounterCard)
            while (deck.Cards.Count > 1) World.MoveToTop(deck.Cards[0], discard);
        int pool = deck.Cards.Count + discard.Cards.Count;
        var main = game.World.TheCardIn(DeckType.MainSchemesArea)!;

        Choose(game);
        Sequence.Finish(game.World, game.World.Facts, game.Runner, []);

        var dealt = Assert.Single(game.World.Cards, card => card.Area.Type == DeckType.DealtEncounterCardsDeck);
        Assert.False(dealt.FaceUp);
        Assert.Equal(PlayArea.Of(0), dealt.Area.PlayArea);
        Assert.Equal([game.Soldier], discard.Cards);
        Assert.Equal(pool - 1, deck.Cards.Count);
        Assert.Equal(lastEncounterCard ? 1 : 0, main.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.Equal(PlayArea.Of(0), game.Guard.Area.PlayArea);
        Assert.Equal(DeckType.EngagedEnemiesArea, game.Guard.Area.Type);
        Assert.Equal(0, game.Guard.Damage);
        Assert.Equal(PlayArea.Of(1), game.Regular.Area.PlayArea);
        Assert.Equal(DeckType.DiscardPile, game.Event.Area.Type);
        Assert.Empty(game.World.Agenda.Outstanding);
    }

    // Synthetic continuations replace "if you have AERIAL and the chosen card is in play, put
    // it into play engaged with you". Each one either puts a card into play that no in-play
    // test of that card guards or reads the board after the engagement, so the power refuses
    // before the soldier is damaged.
    [Theory]
    [InlineData("""{"if":{"test":{"hasTrait":{"card":"you","trait":"AERIAL"}},"then":{"putIntoPlay":{"card":"chosen","where":"engagedWithYou"}}}}""")]
    [InlineData("""{"if":{"test":{"and":[{"hasTrait":{"card":"you","trait":"AERIAL"}},{"isInPlay":"yourHero"}]},"then":{"putIntoPlay":{"card":"chosen","where":"engagedWithYou"}}}}""")]
    [InlineData("""{"if":{"test":{"isInPlay":"chosen"},"then":{"putIntoPlay":{"card":"chosen","where":"engagedWithYou"}},"else":{"draw":{"player":"you","count":1}}}}""")]
    [InlineData("""{"if":{"test":{"isInPlay":{"query":"villain"}},"then":{"putIntoPlay":{"card":{"query":"villain"},"where":"engagedWithYou"}}}}""")]
    [Rule("rr:deal-deal-an-encounter-card")]
    public void AnUnguardedPutIntoPlayAfterTheDealStillRaisesBeforeTheSoldierIsDamaged(string continuation)
    {
        var runner = Runner(continuation);
        AssertRefused(Board(aerial: true, runner), runner, "defeat-triggered encounter deal");
    }

    [Fact]
    [Rule("rr:deal-deal-an-encounter-card")]
    public void AReadAfterTheGuardedEngagementStillRaisesBeforeTheSoldierIsDamaged()
    {
        var runner = Runner(later: """
            {"if":{"test":{"exists":{"cardsIn":{"area":"encounterDeck"}}},
              "then":{"draw":{"player":"you","count":1}}}}
            """);
        AssertRefused(Board(aerial: true, runner), runner, "resolves on the live board");
    }

    [Theory]
    [InlineData("""{"isInPlay":"chosen"}""", true)]
    [InlineData("""{"and":[{"hasTrait":{"card":"you","trait":"AERIAL"}},{"isInPlay":"chosen"}]}""", true)]
    [InlineData("""{"and":[{"isInPlay":"chosen"},{"isInPlay":"yourHero"}]}""", true)]
    [InlineData("""{"hasTrait":{"card":"you","trait":"AERIAL"}}""", false)]
    [InlineData("""{"isInPlay":"yourHero"}""", false)]
    [InlineData("""{"or":[{"isInPlay":"chosen"},{"isInPlay":"yourHero"}]}""", false)]
    [InlineData("""{"not":{"isInPlay":"chosen"}}""", false)]
    public void OnlyAnInPlayTestOfThePutCardGuardsTheLivePutIntoPlay(string test, bool resolves)
    {
        var node = Lowered("""{"if":{"test":""" + test
            + ""","then":{"putIntoPlay":{"card":"chosen","where":"engagedWithYou"}}}}""");

        Assert.Equal(resolves, AbilityLiveGuardedPutIntoPlay.Resolves(node));
    }

    private static AbilityEffect Lowered(string continuation)
    {
        var program = AbilityLowering.Book(AbilityCatalog.Parse(Book(continuation, null).ToJsonString()));
        return Find(program.On("06014").Single().Effect);

        static AbilityEffect Find(AbilityEffect effect) => effect switch
        {
            AbilityEffect.Conditional => effect,
            AbilityEffect.ChooseCard choose => Find(choose.Effect),
            AbilityEffect.Power power => Find(power.Effect),
            AbilityEffect.Sequence sequence => Find(sequence.Effects[^1]),
            _ => throw new InvalidOperationException(effect.GetType().FullName),
        };
    }

    private static void AssertRefused(Fixture game, AbilityRunner runner, string reason)
    {
        var action = Assert.Single(runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
        runner.ExecuteAction(game.World, new PlayerAction(action, [], []));
        var target = Sequence.Work(game.World, game.World.Facts, runner, [])!;
        var before = Snapshot(game.World);

        var error = Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(
            game.World, game.World.Facts, runner, target, Decision.Take(game.Soldier.ObjectId), []));

        Assert.Contains(reason, error.Message);
        Assert.Equal(before, Snapshot(game.World));
        Assert.Equal(3, game.Soldier.Damage);
    }

    private static void Choose(Fixture game)
    {
        var runner = game.Runner;
        var action = Assert.Single(runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
        runner.ExecuteAction(game.World, new PlayerAction(action, [], []));
        var target = Sequence.Work(game.World, game.World.Facts, runner, [])!;
        Sequence.Answer(game.World, game.World.Facts, runner, target, Decision.Take(game.Soldier.ObjectId), []);
    }

    private static AbilityRunner Runner(string? continuation = null, string? later = null) =>
        new(AbilityCatalog.Parse(Book(continuation, later).ToJsonString()));

    private static JsonNode Book(string? continuation, string? later)
    {
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var seq = book["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "06014")!
            ["abilities"]![0]!["effect"]!["chooseCard"]!["effect"]!["attack"]!["effect"]!["seq"]!.AsArray();
        if (continuation is not null) seq[1] = JsonNode.Parse(continuation);
        if (later is not null) seq.Add(JsonNode.Parse(later));
        return book;
    }

    private static Fixture Board(bool aerial, AbilityRunner? runner = null)
    {
        runner ??= AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a", players: 2, villain: "04076");
        world.Abilities = runner;
        if (aerial) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: Traits.Granted + "AERIAL", Affects: world.Seats[0].IdentityCard.ObjectId));
        var thor = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        var guard = world.CreateCard("04153", thor);
        var soldier = world.CreateCard("04153", thor);
        soldier.TakeDamage(3);
        var regular = world.CreateCard("04152", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var card = world.CreateCard("06014", world.Seats[0].Hand);
        return new(world, runner, soldier, guard, regular, card);
    }

    private sealed record Fixture(World World, AbilityRunner Runner, Card Soldier, Card Guard, Card Regular, Card Event);
}
