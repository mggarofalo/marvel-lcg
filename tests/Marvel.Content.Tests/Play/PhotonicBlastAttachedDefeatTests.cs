using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PhotonicBlastAttachedDefeatTests : CoreCardTestBase
{
    [Fact]
    public void AttachedDefeatWindowStillRefusesAnotherDamageBatchRecipientBeforeCosts()
    {
        // Synthetic attack: Spider-Tracer's chosen threat removal is unknown
        // when the next damage recipient would read the projected board.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[
              {"dealDamage":{"cards":{"query":"minions"},"amount":100}},
              {"draw":{"player":"you","count":1}}
            ]}}}
            """, cost: """{"exhaust":"this"}""", includeAuthored: true);
        Card? source = null;
        Card? thrall = null;
        Card? other = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(world =>
        {
            source = InPlay(world, AuthoredCards.AuntMay);
            thrall = world.CreateCard("02024", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            other = world.CreateCard("01101", thrall.Area);
            world.CreateCard("01007", world.AreaOf(DeckType.UpgradesArea, thrall.Area.PlayArea, thrall.ObjectId, 0));
            world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 4);
        }, hero: true, abilities: runner));
        Assert.Contains("damage batch", refused.Message);
        Assert.True(source!.Ready);
        Assert.Equal(0, thrall!.Damage);
        Assert.Equal(0, other!.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, thrall.Area.Type);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:damage.step.7")]
    [Rule("rr:damage.step.8")]
    public void AnotherPlayersTracerChoosesItsSchemeBeforeBlastsPaidDraw(bool energy)
    {
        // Step 7 resolves abilities "when [character] is defeated" before
        // step 8 discards it. 01007: "remove 3 threat from a scheme".
        // 01013: "If you paid for this card using a [energy] resource, draw 1 card."
        var world = new World(Cards, 2);
        var spiderMan = world.CreateSeat("Spider-Man");
        spiderMan.IdentityCard = world.CreateCard("01001a", spiderMan.Hero);
        var captainMarvel = world.CreateSeat("Captain Marvel");
        captainMarvel.IdentityCard = world.CreateCard("01010a", captainMarvel.Hero);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var scheme = world.CreateCard("01097a", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 4);
        var thrall = world.CreateCard("02024", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var tracer = world.CreateCard("01007", world.AreaOf(DeckType.UpgradesArea, thrall.Area.PlayArea, thrall.ObjectId, 0));
        var blast = world.CreateCard("01013", world.Seats[1].Hand);
        var resource = world.CreateCard(energy ? "01014" : "01015", world.Seats[1].Hand);
        int[] payment = energy ? [resource.ObjectId]
            : [resource.ObjectId, world.CreateCard("01089", world.Seats[1].Hand).ObjectId];
        world.CreateCard("01088", world.Seats[0].Deck);
        world.CreateCard("01089", world.Seats[0].Deck);
        world.CreateCard("01088", world.Seats[1].Deck);
        var drawn = world.CreateCard("01089", world.Seats[1].Deck);
        var action = Assert.Single(runner.Actions(world, 1), option => option.Card == blast.ObjectId);
        var events = runner.ExecuteAction(world, new PlayerAction(action, payment, [])).Events.ToList();
        var target = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Sequence.Answer(world, Cards, runner, target, Decision.Take(thrall.ObjectId), events);
        var selection = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(0, selection.Player);
        Assert.Equal(Question.Element, selection.Asking);
        Assert.Equal(DeckType.EngagedEnemiesArea, thrall.Area.Type);
        Assert.Equal(thrall.ObjectId, tracer.Area.Host);
        Assert.Same(world.Seats[1].Deck, drawn.Area);
        Assert.Empty(world.Seats[1].Hand.Cards);
        Sequence.Answer(world, Cards, runner, selection, Decision.Take(scheme.ObjectId), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(1, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.EncounterDiscardPile, thrall.Area.Type);
        Assert.Equal(DeckType.DiscardPile, tracer.Area.Type);
        Assert.Equal(DeckType.DiscardPile, blast.Area.Type);
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        Assert.Same(energy ? world.Seats[1].Hand : world.Seats[1].Deck, drawn.Area);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        if (energy)
        {
            int departure = events.FindIndex(item => item is CardsMoved moved && moved.Cards.Any(card => card.Card == thrall.ObjectId));
            int draw = events.FindIndex(item => item is CardsMoved moved && moved.To.Zone == nameof(DeckType.HandsArea)
                && moved.Cards.Any(card => card.Card == drawn.ObjectId));
            Assert.True(departure >= 0 && draw > departure);
        }
    }
}
