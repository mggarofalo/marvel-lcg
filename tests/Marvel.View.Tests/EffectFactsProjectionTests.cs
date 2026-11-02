using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.View.Tests;

// A prompt's printed effect is public instruction text; it reaches every viewer's decision frame unchanged.
public sealed class EffectFactsProjectionTests
{
    [Fact]
    public void APrintedEffectReachesTheDecisionFrame()
    {
        World world = VisibilityFixture.Board();
        Card villain = world.TheCardIn(DeckType.VillainArea)!;
        var prompt = new Prompt(0, Question.Element, TimingPriority.Untimed, "card_revealed", "Choose", false,
            [new Affordance(villain.ObjectId, "Choose", villain.ObjectId, 0, "Rhino")])
        {
            Effect = new PromptEffect(PromptEffectKind.Damage, 8),
        };
        VisibleResult visible = WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(0).Authorize(null, 2));

        Assert.Equal(prompt.Effect, visible.Prompt!.Effect);
        Assert.Equal(prompt.Effect, PromptPresentation.From(visible.Prompt, visible.World).Frame.Effect);
        Assert.Null(PromptPresentation.From(visible.Prompt with { Effect = null }, visible.World).Frame.Effect);
    }
}
