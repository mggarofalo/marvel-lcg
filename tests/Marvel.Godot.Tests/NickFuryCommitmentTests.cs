using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Decisions;
using Marvel.Godot;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class NickFuryCommitmentTests
{
    [Fact]
    public void DeliberatelyChoosingDrawThreeIsTheCompleteNamedCommitment()
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = new World(cards, 1, seed: 1) { Abilities = runner };
        Seat seat = world.CreateSeat("Spider-Man");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        Card scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 3);
        for (int index = 0; index < 4; index++) world.CreateCard("01044", seat.Deck);
        Card nick = world.CreateCard("01084", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var occurrence = new Occurrence(1, [Steps.CardEntersPlay], Subject: nick.ObjectId, Player: 0);
        var response = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        runner.Resolve(world, occurrence, response, [], []);
        Prompt prompt = Sequence.Work(world, cards, runner, [])!;
        Assert.Equal(3, prompt.Affordances.Count);
        var composer = new DecisionComposer(prompt);
        Assert.False(DirectCommitment.IsReady(composer));
        Affordance draw = Assert.Single(prompt.Affordances, offer => offer.DisplayLabel == "Draw 3 cards");
        composer.SelectAffordance(draw.Id);
        Assert.True(DirectCommitment.IsReady(composer));
        Assert.True(composer.TryBuild(out Decision? decision, out _));
        var events = new List<Marvel.Rules.Events.GameEvent>();
        Sequence.Answer(world, cards, runner, prompt, Decision.Take(decision!.Affordance), events);
        Sequence.Finish(world, cards, runner, events);
        Assert.Equal(3, seat.Hand.Cards.Count);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
