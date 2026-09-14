using Marvel.Decisions;
using Marvel.Godot;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class TableDraftBindingTests
{
    [Fact]
    public void StaleGenerationRevisionAndHiddenIdentifiersCannotMutateTheCurrentDraft()
    {
        long liveRevision = 9;
        int liveGeneration = 4;
        Affordance offer = new(14, "Play", 101, 0, "Visible",
            new TargetRequest([31, 32], 1, 1),
            [new CostOption(101, "1", Sources: [new ResourceSource(41, "Y")])]);
        var composer = new DecisionComposer(new Prompt(0, Question.Order,
            TimingPriority.Untimed, "test", "Choose", false, [offer]));
        var binding = new TableDraftBinding(composer, 4, 9,
            (generation, revision) => generation == liveGeneration && revision == liveRevision);

        Assert.False(binding.TrySelectAffordance(99));
        Assert.Null(composer.Selected);
        Assert.True(binding.TrySelectAffordance(14));
        Assert.False(binding.TryToggleTarget(999));
        Assert.True(binding.TryToggleTarget(31));

        liveGeneration++;
        Assert.False(binding.TryToggleGenerator(41));
        Assert.Empty(composer.Resources);

        liveGeneration--;
        liveRevision++;
        Assert.False(binding.TryRemoveTarget(31));
        Assert.Equal([31], composer.Targets);
    }
}
