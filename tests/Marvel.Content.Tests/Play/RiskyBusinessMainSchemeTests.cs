using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RiskyBusinessMainSchemeTests : CoreCardTestBase
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(2, false)]
    [InlineData(4, false)]
    [InlineData(2, true)]
    [Rule("rr:when-completed-abilities.1")]
    [Rule("rr:main-scheme-main-scheme-deck.2.1")]
    public void CompletionFinishesBeforeStageTwoEntryAndFinalLoss(int players, bool madness)
    {
        // 02004b: "Place 1[per_hero] infamy counters on Criminal Enterprise. Then
        // discard 1 card from each player's deck for each infamy counter ..."
        // "When Completed" is a Forced Interrupt. 02005a advances to 2B;
        // 02005b: "If this stage is completed, the players lose the game."
        var world = Hero("01001b", players);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var scheme = world.CreateCard("02004a,02004b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.TurnTo("02004b");
        var next = world.CreateCard("02005a,02005b", world.AreaOf(DeckType.MainSchemesDeck));
        var environment = world.CreateCard("02006a,02006b", world.AreaOf(DeckType.EnvironmentArea));
        if (madness) environment.TurnTo("02006b");
        environment.PlaceTokens(madness ? "c_madness" : "c_infamy", 2 * players);
        foreach (var seat in world.Seats)
            for (int i = 0; i < 20; i++) world.CreateCard("01089", seat.Deck);
        var events = new List<GameEvent>();

        Threat.Place(world, Cards, runner, scheme, 7 * players, "test", events);
        Finish(world, events);

        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Equal(DeckType.RemovedArea, scheme.Area.Type);
        Assert.Same(next, world.TheCardIn(DeckType.MainSchemesArea));
        Assert.Equal("02005b", next.FaceId);
        Assert.Equal(players, next.Tokens["k_threat"]);
        Assert.Equal(madness ? 0 : 3 * players, environment.Tokens.GetValueOrDefault("c_infamy"));
        Assert.Equal(madness ? 2 * players : 0, environment.Tokens.GetValueOrDefault("c_madness"));
        Assert.All(world.Seats, seat => Assert.Equal(madness ? 20 : 20 - 3 * players, seat.Deck.Cards.Count));
        Assert.All(world.Seats, seat => Assert.Equal(madness ? 0 : 3 * players, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(seat.Index), cardOwner: seat.Index).Cards.Count));

        Threat.Place(world, Cards, runner, next, 9 * players, "test", events);
        Finish(world, events);

        Assert.Equal(Outcome.VillainWins, world.Result);
        Assert.False(world.Agenda.IsBusy);
    }

    private static void Finish(World world, List<GameEvent> events)
    {
        var runner = AuthoredCards.Runner();
        for (int attempt = 0; attempt < 12; attempt++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) return;
            Assert.Equal(Question.Order, prompt.Asking);
            var option = Assert.Single(prompt.Affordances);
            Sequence.Answer(world, Cards, runner, prompt, Decision.Take(option.Id, option.Targets!.Legal, []), events);
        }
        Assert.Fail("Main scheme completion did not settle within its bounded player ordering.");
    }
}
