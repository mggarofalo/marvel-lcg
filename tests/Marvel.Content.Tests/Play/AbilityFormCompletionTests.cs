using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AbilityFormCompletionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:interrupt.3")]
    [Rule("rr:then.2")]
    public void DependentDrawUsesTheActualFlipReceiptAfterInterrupts(bool cancel)
    {
        // rr:then.2: pre-then text "must resolve in full" before post-then text.
        // Synthetic cards exercise cancellation without changing published rows.
        string interruption = cancel ? "{\"cancelOccurrence\":\"trigger\"}"
            : "{\"heal\":{\"card\":\"you\",\"amount\":1}}";
        var local = AbilityCatalog.Parse($$$$$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"exhaust":"this"},
              "effect":{"then":{"effect":{"changeForm":{"player":"you","to":"alter-ego"}},
                "then":{"draw":{"player":"you","count":1}}}}
            }]},{"card":"01092","abilities":[{
              "trigger":{"event":"WhenFormChanged","timing":"ForcedInterrupt","subject":"game"},
              "effect":{{{{{interruption}}}}}
            }]}]}
            """);
        var runner = new AbilityRunner(local);
        var world = Board();
        var seat = world.Seats[0];
        seat.IdentityCard.TurnTo("01019a");
        seat.IdentityCard.TakeDamage(1);
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0));
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        Assert.False(source.Ready);
        Assert.Equal("01019a", seat.IdentityCard.FaceId);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(cancel ? "01019a" : "01019b", seat.IdentityCard.FaceId);
        Assert.Equal(cancel ? 0 : 1, seat.Hand.Cards.Count);
        Assert.Equal(cancel ? 1 : 0, seat.IdentityCard.Damage);
        Assert.Equal(cancel ? 0 : 1, events.OfType<CardsFlipped>().Count());
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:player-elimination.5")]
    public void FormChangeResponseEliminationStillFinishesTheInitiatedAbility()
    {
        // rr:player-elimination.5: "resolve the entire ability".
        // The synthetic suffix damages the surviving villain after actual 04093 eliminates its owner.
        var runner = ActionAbilitySupportFixtures.Runner("01006", "Action", """
            {"seq":[{"changeForm":{"player":"you","to":"hero"}},
              {"dealDamage":{"cards":{"query":"villain"},"amount":1}}]}
            """, cost: """{"exhaust":"this"}""", includeAuthored: true);
        var world = Board(2);
        var seat = world.Seats[0];
        seat.IdentityCard.TakeDamage(14);
        var neighbour = world.CreateSeat("p1");
        neighbour.IdentityCard = world.CreateCard("01001a,01001b", neighbour.Hero);
        var villain = world.CreateCard("04093", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01104", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("04064", world.AreaOf(DeckType.EncounterDeck));
        var source = world.CreateCard("01006", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0), offer => offer.Card == source.ObjectId && offer.Ordinal == 1);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.True(seat.Eliminated);
        Assert.False(neighbour.Eliminated);
        Assert.Equal(1, villain.Damage);
        Assert.Equal(DeckType.RemovedArea, source.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    private static World Board(int players = 1)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, players, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01019a,01019b", seat.Hero);
        seat.IdentityCard.TurnTo("01019b");
        for (int i = 0; i < 8; i++) world.CreateCard("01087", seat.Deck);
        return world;
    }
}
