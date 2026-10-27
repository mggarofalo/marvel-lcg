using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EnemyAttackDefeatResponseOrderingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:damage.step.7")]
    [Rule("rr:damage.step.9")]
    [Rule("rr:triggering-condition.2")]
    [Rule("rr:attack-enemy-activation.step.6.b")]
    public void PlainDefeatResponseSharesTheFinishedAttacksWindowAndCannotRepeat(bool indirect)
    {
        // Damage step 9 lists "after [character] defeats/is defeated". One attack
        // causing damage and defeat has "a single response window", and abilities
        // referring to "any of the triggering conditions" may be used in any order.
        // Enemy attack step 6.b puts non-forced responses after the forced tier.
        var f = new RonanJudgeProbeFixture(extraCards: """
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"Response","subject":"game"},
              "effect":{"placeCounters":{"card":"this","counter":"plainDefeat","count":1}}
            }]}]}
            """);
        var observer = f.World.CreateCard("01006", f.World.AreaOf(DeckType.SupportsArea,
            PlayArea.Of(0), cardOwner: 0));
        var ally = f.Ally(1, true);
        var response = f.AttackUntilResponse(ally,
            indirect ? [ally.ObjectId, ally.ObjectId] : null, observer.ObjectId);
        Assert.Equal(Question.Opportunity, response.Asking);
        Assert.Null(f.World.Attack);
        Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(1, f.Observer.Tokens.GetValueOrDefault("c_defeatInterrupt"));
        Assert.Equal(2, f.Main.Tokens.GetValueOrDefault("k_threat"));
        var plainDefeat = Assert.Single(response.Affordances, option => option.AnchorId == observer.ObjectId);
        Assert.Contains(response.Affordances, option => option.AnchorId == f.Observer.ObjectId);
        Sequence.Answer(f.World, f.World.Facts, f.Runner, response, Decision.Take(plainDefeat.Id), f.Events);
        var remaining = Sequence.Work(f.World, f.World.Facts, f.Runner, f.Events)!;
        Assert.DoesNotContain(remaining.Affordances, option => option.AnchorId == observer.ObjectId);
        Assert.Contains(remaining.Affordances, option => option.AnchorId == f.Observer.ObjectId);
        f.Finish();
        Assert.Equal(1, observer.Tokens.GetValueOrDefault("c_plainDefeat"));
    }
}
