using System.Collections.Immutable;
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

/// <summary>Hammer Throw (06005) defeats Hydra Soldier (04153) before returning Mjolnir (06009).</summary>
public sealed class HammerThrowHydraSoldierDefeatTests
{
    // Hammer Throw: "deal 8 damage to an enemy and return Mjolnir to your hand. This attack
    // gains overkill." Hydra Soldier: "When Defeated: Deal the engaged player an encounter card."
    // The board is Marvel.Sim's Absorbing Man standard game with Thor solo, seed 35, at the
    // Hammer Throw target choice: Absorbing Man holds a tough status card and Hydra Soldier
    // and Hydra Regular are engaged with Thor.
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [Rule("rr:overkill.1")]
    [Rule("rr:when-defeated-abilities.2.1")]
    [Rule("rr:deal-deal-an-encounter-card")]
    [Rule("rr:encounter-deck.1")]
    public void TheSoldiersFacedownDealResolvesBeforeMjolnirReturnsOnTheLiveBoard(bool tough, bool lastEncounterCard)
    {
        // rr:overkill.1: "If this attack defeats a minion, deal the damage beyond its hit
        // points to the villain." rr:when-defeated-abilities.2.1: "A defeated card leaves
        // play after its 'When Defeated' ability is resolved". rr:deal-deal-an-encounter-card:
        // "the player takes the top card of the encounter deck and places it facedown in front
        // of them. This card is not revealed at this time." rr:encounter-deck.1: "If the
        // encounter deck is empty, the encounter discard pile is immediately shuffled to create
        // a new encounter deck. When this occurs, place an acceleration token". Ruling 2c98d8b065c9e8b0: "Resolve its 'When
        // Defeated' ability, dealing the last encounter card to the engaged player. The moment
        // the encounter deck is emptied, shuffle the discard pile to make a new encounter deck
        // and place 1 acceleration token on the main scheme. Discard Hydra Soldier from play."
        // Mjolnir is then found on the board the deal left.
        var game = Board(tough);
        var deck = game.World.AreaOf(DeckType.EncounterDeck);
        var discard = game.World.AreaOf(DeckType.EncounterDiscardPile);
        if (lastEncounterCard)
            while (deck.Cards.Count > 1) World.MoveToTop(deck.Cards[0], discard);
        int pool = deck.Cards.Count + discard.Cards.Count;
        var main = game.World.TheCardIn(DeckType.MainSchemesArea)!;

        var action = Assert.Single(game.Runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
        game.Runner.ExecuteAction(game.World, new PlayerAction(action, game.Resources, []));
        var target = Sequence.Work(game.World, game.World.Facts, game.Runner, [])!;
        Sequence.Answer(game.World, game.World.Facts, game.Runner, target, Decision.Take(game.Soldier.ObjectId), []);
        Sequence.Finish(game.World, game.World.Facts, game.Runner, []);

        var dealt = Assert.Single(game.World.Cards, card => card.Area.Type == DeckType.DealtEncounterCardsDeck);
        Assert.False(dealt.FaceUp);
        Assert.Equal(PlayArea.Of(0), dealt.Area.PlayArea);
        Assert.Equal([game.Soldier], discard.Cards);
        Assert.Equal(pool - 1, deck.Cards.Count);
        Assert.Equal(lastEncounterCard ? 1 : 0, main.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
        Assert.Same(game.World.Seats[0].Hand, game.Hammer.Area);
        Assert.Equal(tough ? 0 : 4, game.Villain.Damage);
        Assert.False(Statuses.Has(game.World, game.Villain, Statuses.Tough));
        Assert.Equal(DeckType.EngagedEnemiesArea, game.Regular.Area.Type);
        Assert.Equal(0, game.Regular.Damage);
        Assert.Empty(game.World.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:deal-deal-an-encounter-card")]
    public void AnEncounterDeckReadAfterTheReturnStillRaisesBeforeTheSoldierIsDamaged()
    {
        // Synthetic continuation: a test of the encounter deck after the return reads the
        // piles the soldier's deal changed, so the power refuses before it begins.
        var book = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        book["cards"]!.AsArray().Single(row => row!["card"]!.GetValue<string>() == "06005")!
            ["abilities"]![0]!["effect"]!["chooseCard"]!["effect"]!["attack"]!["effect"]!["seq"]!.AsArray()
            .Add(JsonNode.Parse("""
                {"if":{"test":{"exists":{"cardsIn":{"area":"encounterDeck"}}},
                  "then":{"draw":{"player":"you","count":1}}}}
                """));
        var game = Board(tough: true, new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString())));
        var action = Assert.Single(game.Runner.Actions(game.World, 0), a => a.Card == game.Event.ObjectId);
        game.Runner.ExecuteAction(game.World, new PlayerAction(action, game.Resources, []));
        var target = Sequence.Work(game.World, game.World.Facts, game.Runner, [])!;
        var before = Snapshot(game.World);

        var error = Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(
            game.World, game.World.Facts, game.Runner, target, Decision.Take(game.Soldier.ObjectId), []));

        Assert.Contains("defeat-triggered encounter deal", error.Message);
        Assert.Equal(before, Snapshot(game.World));
        Assert.Equal(0, game.Soldier.Damage);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [Rule("rr:deal-deal-an-encounter-card")]
    public void OnlyAnEncounterDealOutsideARecoveryWindowHidesTheReturnsLiveWork(
        bool encounterDeal, bool recoveryWindow, bool finishesLive)
    {
        // A step-6 recovery keeps its minion in play and keeps its own read proof, so a path
        // carrying one is never finished live on the encounter deal's account.
        var game = Board(tough: true);
        var query = new AbilityQueryContext(game.World, game.Event, new Occurrence(0, []), 0,
            game.Event.Incarnation, null, null, null, []);
        var expression = new AbilityExpressionContext(query, ImmutableDictionary<string, long>.Empty,
            [], "", 0, false, null);
        var cast = new AbilityAdmission.AbilityAdmissionScope(new AbilityAdmissionContext(
            AuthoredCards.Program, game.World.ResourceAbilities, expression, new(), null), []);
        var path = AbilityPowerStateFactory.Initial(cast) with
        {
            EncounterCardsChanged = encounterDeal,
            DefeatWindowDeferred = recoveryWindow,
            RecoveryWindowDeferred = recoveryWindow,
        };
        var returned = new AbilityEffect.CardAction(
            AbilityCardInstruction.ReturnToHand, new AbilityCardSelection.Titled("Mjolnir"));

        Assert.Equal(finishesLive, AbilityLiveUpgradeReturn.CanFinish(returned, cast, terminal: true, [path]));
    }

    private static Fixture Board(bool tough, AbilityRunner? runner = null)
    {
        runner ??= AuthoredCards.Runner();
        var world = ThorActualFixture.Board("06001a", villain: "04076");
        world.Abilities = runner;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        if (tough) Statuses.Give(world, villain, Statuses.Tough);
        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        var regular = world.CreateCard("04152", engaged);
        var soldier = world.CreateCard("04153", engaged);
        var hammer = world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        var card = world.CreateCard("06005", world.Seats[0].Hand);
        var resource = world.CreateCard("01090", world.Seats[0].Hand);
        var other = world.CreateCard("01003", world.Seats[0].Hand);
        return new(world, runner, villain, soldier, regular, hammer, card, [resource.ObjectId, other.ObjectId]);
    }

    private sealed record Fixture(World World, AbilityRunner Runner, Card Villain, Card Soldier,
        Card Regular, Card Hammer, Card Event, int[] Resources);
}
