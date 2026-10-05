using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Finds the ability source whose continuation awaits this activation.</summary>
internal static class ActivationCausalCards
{
    internal static IReadOnlyList<int> For(World world, PhaseStep step)
    {
        var activation = world.Activation ?? (step.What == Steps.EndAttack
            && world.FinishedActivation?.Id == step.ActivationId ? world.FinishedActivation : null);
        if (activation is not { Id: >= 0 }) return [];
        PhaseStep? continuation = world.Agenda.ActivationWait(activation.Id);
        return continuation is { Subject: >= 0 } waiting && waiting.Subject < world.Cards.Count
            ? [waiting.Subject] : [];
    }
}
