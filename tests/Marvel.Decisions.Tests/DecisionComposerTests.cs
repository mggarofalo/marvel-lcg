using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Decisions.Tests;
public abstract class DecisionComposerTestBase
{
    protected static Prompt Prompt(bool cancellable, params Affordance[] affordances) => new(0, Question.Order, TimingPriority.Untimed, "test", "Choose now", cancellable, affordances);
    protected static CardDescriptor Card(int id, string title) => new(id, CardBack.Player, true, true, -1, new CardFaceDescriptor("face", title, "", CardKind.Hero, new Dictionary<string, long>(StringComparer.Ordinal)));
}
