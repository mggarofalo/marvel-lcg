using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

// Synthetic action timing isolates the named-reference and authorization contracts.
// Actual Synth-Suit response timing is exercised by the Content composition tests.
public sealed class NamedReadinessPresentationTests
{
    [Theory]
    [InlineData("answering")]
    [InlineData("other-seat")]
    [InlineData("cooperative")]
    [InlineData("observer")]
    public void NamedReadyMeaningPreservesSourceAndDoesNotGrantAnotherSeatThePrompt(string viewer)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"08009","name":"Synthetic response source","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"exhaust":"this"},"effect":{"ready":{"titled":"Black Widow"}}
            }]}]}
            """));
        var world = new World(cards, players: 2, seed: 7) { Abilities = runner };
        Seat first = world.CreateSeat("Peter");
        first.IdentityCard = world.CreateCard("01001a", first.Hero);
        Seat second = world.CreateSeat("Natasha");
        second.IdentityCard = world.CreateCard("08001a", second.Hero);
        second.IdentityCard.Exhaust();
        Card source = world.CreateCard("08009",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        string before = world.Digest().Canonical();
        var randomBefore = world.Random.Generator.GetState();
        long wordsBefore = world.Random.Generator.WordsConsumed;
        var pendingBefore = world.Agenda.Outstanding.ToArray();

        var ability = Assert.Single(runner.Actions(world, 0));
        Affordance offer = runner.Describe(world, ability);
        var prompt = new Prompt(0, Question.Opportunity, TimingPriority.Untimed,
            "synthetic", "Use the named ready instruction?", true, [offer]);
        ViewScope scope = viewer switch
        {
            "answering" => new RestrictedVisibilityPolicy(0).Authorize(null, 2),
            "other-seat" => new RestrictedVisibilityPolicy(1).Authorize(null, 2),
            "cooperative" => new PermissiveVisibilityPolicy().Authorize(null, 2),
            _ => ViewScope.None,
        };

        VisibleResult visible = WorldProjection.For(world, prompt, [], scope);

        Assert.Equal("Ready Black Widow.", offer.Description);
        Assert.Equal("Exhaust Synth-Suit", offer.CostDescription);
        Assert.Equal(before, world.Digest().Canonical());
        var randomAfter = world.Random.Generator.GetState();
        Assert.Equal(randomBefore.Index, randomAfter.Index);
        Assert.Equal(randomBefore.Words, randomAfter.Words);
        Assert.Equal(wordsBefore, world.Random.Generator.WordsConsumed);
        Assert.Equal(pendingBefore, world.Agenda.Outstanding.ToArray());
        if (viewer is "other-seat" or "observer")
        {
            Assert.Null(visible.Prompt);
            return;
        }
        AffordancePresentation choice = Assert.Single(
            PromptPresentation.From(visible.Prompt!, visible.World).Affordances);
        Assert.Equal(source.ObjectId, choice.CardAnchorId);
        Assert.Equal("Ready Black Widow.", choice.Description);
        Assert.Equal("Exhaust Synth-Suit", choice.CostDescription);
        Assert.DoesNotContain("your identity", choice.Description, StringComparison.Ordinal);
    }
}
