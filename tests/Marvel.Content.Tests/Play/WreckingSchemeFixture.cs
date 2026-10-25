using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Canonical scheme runner and explicit completion of arranged test worlds.</summary>
internal static class WreckingSchemeFixture
{
    internal static readonly string[] Faces = ["07004", "07019", "07034", "07048"];
    internal static AbilityRunner Runner() => Marvel.Content.Tests.Cards.AuthoredCards.Runner();

    internal static void Finish(World world, AbilityRunner runner, List<GameEvent>? events = null)
    {
        events ??= [];
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) return;
            Assert.True(step < 30);
            var decision = Decision.Decline;
            if (!prompt.Cancellable)
            {
                var offer = Assert.Single(prompt.Affordances, item => item.IsLegal);
                decision = new(offer.Id, offer.Targets?.Legal.Take(offer.Targets.Min).ToArray() ?? []);
            }
            Sequence.Answer(world, world.Facts, runner, prompt, decision, events);
        }
    }
}
