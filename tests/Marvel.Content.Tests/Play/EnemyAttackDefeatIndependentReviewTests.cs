using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EnemyAttackDefeatIndependentReviewTests
{
    [Fact]
    [Rule("rr:damage.step.9")]
    [Rule("rr:attack-enemy-activation.step.6.b")]
    public void ActualRapidResponseWaitsForTheCompletedAttackAndRetainsTheDefeatedCopy()
    {
        // Step 9's "after [character] defeats/is defeated" response shares
        // step 6.b's completed-attack window. Actual 08031 returns "that ally".
        var f = new RonanJudgeProbeFixture();
        var rapid = f.World.CreateCard("08031", f.World.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(1), cardOwner: 1));
        var ally = f.Ally(1, true);
        int incarnation = ally.Incarnation;
        var first = f.AttackUntilResponse(ally, responseSource: rapid.ObjectId);
        Assert.Null(f.World.Attack);
        Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.Equal(1, first.Player);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Take(f, first, rapid.ObjectId);
        f.Finish();
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(PlayArea.Of(1), ally.Area.PlayArea);
        Assert.True(ally.Incarnation > incarnation);
        Assert.Equal(1, ally.Damage);
        Assert.Equal(DeckType.DiscardPile, rapid.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:triggering-condition.2")]
    [Rule("rr:damage.step.9")]
    public void AttackResponseCanPrecedePlainDefeatResponse(bool indirect)
    {
        // Conditions created by one occurrence "may be used in any order".
        var f = Fixture();
        var observer = Observer(f);
        var ally = f.Ally(1, true);
        var prompt = f.AttackUntilResponse(ally,
            indirect ? [ally.ObjectId, ally.ObjectId] : null, observer.ObjectId);
        Assert.Null(f.World.Attack);
        Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.Equal(2, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Take(f, prompt, f.Observer.ObjectId);
        var remaining = Sequence.Work(f.World, f.World.Facts, f.Runner, f.Events)!;
        Assert.DoesNotContain(remaining.Affordances, option => option.AnchorId == f.Observer.ObjectId);
        Assert.Contains(remaining.Affordances, option => option.AnchorId == observer.ObjectId);
        Take(f, remaining, observer.ObjectId);
        f.Finish();
        Assert.Equal(1, observer.Tokens.GetValueOrDefault("c_plainDefeat"));
        Assert.Equal(1, f.Observer.Tokens.GetValueOrDefault("c_end"));
    }

    [Fact]
    [Rule("rr:triggering-condition.1")]
    [Rule("rr:triggering-condition.2")]
    public void NestedNonattackDefeatRetainsItsOwnEarlierResponseWindow()
    {
        // Only conditions created by "a single game occurrence" share a window;
        // each response can trigger "once per occurrence".
        var f = Fixture(nested: true);
        var observer = Observer(f);
        var unrelated = f.World.CreateCard("01002", f.World.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(0), cardOwner: 0));
        var ally = f.Ally(1, true);
        var nested = f.AttackUntilResponse(ally, responseSource: observer.ObjectId);
        Assert.Equal(Question.Opportunity, nested.Asking);
        Assert.NotNull(f.World.Attack);
        Assert.Empty(f.Events.OfType<AttackCompleted>());
        Assert.Equal(DeckType.DiscardPile, unrelated.Area.Type);
        Assert.Equal(unrelated.ObjectId, Assert.Single(f.World.Agenda.Occurrence!.Defeats).Card);
        Assert.Equal(0, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Take(f, nested, observer.ObjectId);
        var completed = WorkUntilOptional(f);
        Assert.Null(f.World.Attack);
        Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.Equal(ally.ObjectId, Assert.Single(f.World.Agenda.Occurrence!.Defeats).Card);
        Assert.Equal(2, f.Main.Tokens.GetValueOrDefault("k_threat"));
        Take(f, completed, observer.ObjectId);
        f.Finish();
        Assert.Equal(2, observer.Tokens.GetValueOrDefault("c_plainDefeat"));
    }

    private static RonanJudgeProbeFixture Fixture(bool nested = false)
    {
        const string nestedCard = """
            ,{"card":"01041","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"ForcedInterrupt","subject":"this"},
              "effect":{"dealDamage":{"cards":{"titled":"Black Cat"},"amount":2}}
            }]}
            """;
        return new(extraCards: $$$"""
        {"cards":[{"card":"01006","abilities":[{
          "trigger":{"event":"WhenCardDefeated","timing":"Response","subject":"game"},
          "effect":{"placeCounters":{"card":"this","counter":"plainDefeat","count":1}}
        }]}{{{(nested ? nestedCard : "")}}}]}
        """);
    }

    private static Prompt WorkUntilOptional(RonanJudgeProbeFixture f)
    {
        for (int step = 0; step < 20; step++)
        {
            var prompt = Sequence.Work(f.World, f.World.Facts, f.Runner, f.Events)!;
            if (prompt.Asking != Question.Order) return prompt;
            Sequence.Answer(f.World, f.World.Facts, f.Runner, prompt,
                Decision.Take(prompt.Affordances[0].Id), f.Events);
        }
        throw new InvalidOperationException("Forced ordering did not finish");
    }

    private static Card Observer(RonanJudgeProbeFixture f) =>
        f.World.CreateCard("01006", f.World.AreaOf(DeckType.SupportsArea,
            PlayArea.Of(0), cardOwner: 0));

    private static void Take(RonanJudgeProbeFixture f, Prompt prompt, int source) =>
        Sequence.Answer(f.World, f.World.Facts, f.Runner, prompt,
            Decision.Take(Assert.Single(prompt.Affordances, option => option.AnchorId == source).Id), f.Events);
}
