using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThreatResponseBindingTests : CoreCardTestBase
{
    [Fact]
    [Rule("rr:triggering-condition.1")]
    [Rule("rr:triggering-condition.2")]
    public void EachOptionalResponseExplainsAndCommitsItsExactScheme()
    {
        // Synthetic contract: "once per occurrence" within "a single ... response window".
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01007","abilities":[{
              "trigger":{"event":"WhenThreatPlaced","timing":"Response","subject":"game"},
              "effect":{"chooseCard":{"from":"trigger.target",
                "effect":{"removeThreat":{"scheme":"chosen","amount":1}}}}
            }]}]}
            """));
        var world = Hero("01001a");
        world.Abilities = runner;
        world.CreateCard("01007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var first = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        var second = world.CreateCard("01108", world.AreaOf(DeckType.SideSchemesArea));
        first.PlaceTokens("k_threat", 1);
        second.PlaceTokens("k_threat", 1);
        Threat.Schedule(world, new([first, second], null, 1, ThreatCause.CardAbility, "synthetic", 0));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(2, prompt.Affordances.Count);
        foreach (var scheme in new[] { first, second })
        {
            var offer = Assert.Single(prompt.Affordances, candidate =>
                candidate.Targets?.Legal.SequenceEqual([scheme.ObjectId]) == true);
            Assert.Contains(EffectiveCards.Title(scheme, Cards), offer.DisplayLabel);
            Assert.Equal(scheme.ObjectId, Assert.Single(offer.WindowChoices).TargetId);
        }
        var chosen = prompt.Affordances.Single(offer => offer.Targets!.Legal.Contains(second.ObjectId));
        var occurrence = world.Windows.Current!.Value.Occurrence;
        Sequence.Answer(world, Cards, runner, prompt, Decision.Take(chosen.Id, [second.ObjectId], []), []);
        Assert.Equal(2, first.Tokens["k_threat"]);
        Assert.Equal(1, second.Tokens["k_threat"]);
        var remaining = Assert.IsType<Prompt>(Sequence.Work(world, Cards, runner, []));
        var next = Assert.Single(remaining.Affordances);
        Sequence.Answer(world, Cards, runner, remaining, Decision.Take(next.Id, [first.ObjectId], []), []);
        Assert.Equal(1, first.Tokens["k_threat"]);
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(2, occurrence.Resolutions.Count);
        Assert.Equal(2, occurrence.Resolutions.Select(entry => entry.TriggeringThreatId).Distinct().Count());
        Assert.All(occurrence.Resolutions, entry => Assert.Equal(ResolutionStatus.Resolved, entry.Status));
    }
}
