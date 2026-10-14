using Marvel.Decisions;
using Marvel.Godot;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CardInspectionProtectionTests
{
    [Fact]
    public void PaymentKeepsItsSourceChosenTargetAndCurrentCauseExposed()
    {
        Affordance offer = new(14, "Play", 101, 0, "Visible",
            new TargetRequest([31, 32], 1, 1), []);
        var draft = new DecisionComposer(new Prompt(0, Question.Option,
            TimingPriority.Untimed, "test", "Choose", false, [offer])
            { ContextCardIds = [51], CauseCardIds = [52] });
        draft.SelectAffordance(14);
        draft.SelectTargets([31]);

        IReadOnlySet<int> protectedIds = CardInspectionProtection.Participants(draft, null);

        Assert.True(protectedIds.SetEquals([101, 31, 51, 52]));
        draft.SelectTargets([32]);
        Assert.True(CardInspectionProtection.Participants(draft, null).SetEquals([101, 32, 51, 52]));
    }

    [Fact]
    public void WaitingSeatRetainsAuthorizedAttackParticipantsWithoutADraft()
    {
        var world = new WorldDescriptor([], [], [], Marvel.Rules.Play.Outcome.Unfinished)
        {
            ActiveAttack = new(new(1, "Rhino", ActivitySubjectRole.Source),
                new(2, "Mockingbird", ActivitySubjectRole.Target)),
        };
        Assert.True(CardInspectionProtection.Participants(null, world).SetEquals([1, 2]));
        Assert.Empty(CardInspectionProtection.Participants(null, world with { ActiveAttack = null }));
    }
}
