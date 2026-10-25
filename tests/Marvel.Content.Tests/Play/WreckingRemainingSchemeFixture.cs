using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

internal static class WreckingRemainingSchemeFixture
{
    internal static void Finish(World world, AbilityRunner runner)
    {
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) return;
            Assert.True(step < 30);
            var decision = Decision.Decline;
            if (!prompt.Cancellable)
            {
                var offer = Assert.Single(prompt.Affordances, item => item.IsLegal);
                decision = new(offer.Id, offer.Targets?.Legal.Take(offer.Targets.Min).ToArray() ?? []);
            }
            Sequence.Answer(world, world.Facts, runner, prompt, decision, []);
        }
    }
}
