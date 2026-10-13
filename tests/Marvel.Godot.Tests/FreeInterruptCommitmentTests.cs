using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Decisions;
using Marvel.Godot;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class FreeInterruptCommitmentTests
{
    [Fact]
    public void GreatResponsibilityCurrentChoiceBuildsAndResolvesWithoutPaymentSelection()
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse(File.ReadAllText(
            RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = new World(cards, 1, seed: 1) { Abilities = runner };
        Seat seat = world.CreateSeat("Spider-Man");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        Card scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        world.CreateCard("01087", seat.Deck);
        Card interrupt = world.CreateCard("01061", seat.Hand);
        world.Agenda.Add(new PhaseStep(Steps.PlaceThreat, 1, 1));
        Prompt prompt = Sequence.Work(world, cards, runner, [])!;
        var composer = new DecisionComposer(prompt);
        Affordance offer = Assert.Single(prompt.Affordances);
        Assert.Single(offer.CostOptions);
        Assert.False(DirectCommitment.IsReady(composer));

        composer.SelectAffordance(offer.Id);

        Assert.True(DirectCommitment.IsReady(composer));
        Assert.True(composer.TryBuild(out EngineDecision? decision, out _));
        Assert.Empty(composer.Resources);
        Sequence.Answer(world, cards, runner, prompt, Decision.Take(decision!.Affordance), []);
        Assert.Equal(1, seat.IdentityCard.Damage);
        Assert.False(scheme.Tokens.ContainsKey("k_threat"));
        Assert.Equal(DeckType.DiscardPile, interrupt.Area.Type);
    }
}
