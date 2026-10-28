using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Printed faces on synthetic research boards; neither row admits Ant-Man.
public sealed class AntManTeamStageCompositionTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void SwarmOtherHeroThenReadyIsRejectedBeforePaymentWithSameCostControl()
    {
        const string desired = """{"seq":[{"changeForm":{"player":"you","to":"other-hero"}},{"ready":"yourHero"}]}""";
        const string control = """{"draw":{"player":"you","count":1}}""";
        var runner = Runner("12020", desired);
        var (world, source, payment) = Board(runner, "12020");
        var identity = world.Seats[0].IdentityCard;
        identity.Exhaust();
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var pending = world.Agenda.Outstanding.ToArray();
        var error = Assert.Throws<RulesNotImplementedException>(() => runner.Actions(world, 0));
        Assert.Contains("composed", error.Message);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Assert.Same(world.Seats[0].Hand, source.Area);
        Assert.Same(world.Seats[0].Hand, payment.Area);

        var controlRunner = Runner("12020", control);
        var (controlWorld, controlSource, controlPayment) = Board(controlRunner, "12020");
        controlWorld.Seats[0].IdentityCard.Exhaust();
        var action = Assert.Single(controlRunner.Actions(controlWorld, 0), a => a.Card == controlSource.ObjectId);
        controlRunner.ExecuteAction(controlWorld, new PlayerAction(action, [controlPayment.ObjectId], []));
        Sequence.Finish(controlWorld, controlWorld.Facts, controlRunner, []);
        Assert.Equal(DeckType.DiscardPile, controlSource.Area.Type);
        Assert.Equal(DeckType.DiscardPile, controlPayment.Area.Type);
        Assert.Single(controlWorld.Seats[0].Hand.Cards);
    }

    [Fact]
    public void MusterStageBoundCannotLowerWhileFixedBoundCan()
    {
        // Printed 12032: up to the villain's stage number, maximum three.
        const string dynamicMaximum = """{"modified":{"card":{"query":"villain"},"field":"printed_stage"}}""";
        string effect = """{"chooseStatusRecipients":{"from":{"query":"heroesAndAllies"},"status":"tough","maximum":MAXIMUM}}""";
        var error = Assert.Throws<AbilityException>(() => Runner("12032", effect.Replace("MAXIMUM", dynamicMaximum, StringComparison.Ordinal)));
        Assert.Contains("maximum", error.Message);
        var runner = Runner("12032", effect.Replace("MAXIMUM", "3", StringComparison.Ordinal));
        var (world, source, payment) = Board(runner, "12032");
        var payment2 = world.CreateCard("01089", world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [payment.ObjectId, payment2.ObjectId], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Assert.Contains(world.Seats[0].IdentityCard.ObjectId, offer.Targets!.Legal);
        Sequence.Answer(world, world.Facts, runner, prompt, new Decision(offer.Id, [world.Seats[0].IdentityCard.ObjectId]), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment2.Area.Type);
    }

    private static AbilityRunner Runner(string face, string effect) => new(AbilityCatalog.Parse($$"""
        {"cards":[{"card":"{{face}}","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "effect":{{effect}}
        }]}]}
        """));

    private static (World World, Card Source, Card Payment) Board(AbilityRunner runner, string face)
    {
        var world = new World(Cards, 2, 241);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("12001a,12001b,12001c", seat.Hero);
        var other = world.CreateSeat("p1");
        other.IdentityCard = world.CreateCard("01001a,01001b", other.Hero);
        world.CreateCard("12002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01087", seat.Deck);
        world.CreateCard("01087", seat.Deck);
        var source = world.CreateCard(face, seat.Hand);
        var payment = world.CreateCard("01088", seat.Hand);
        world.Abilities = runner;
        return (world, source, payment);
    }
}

