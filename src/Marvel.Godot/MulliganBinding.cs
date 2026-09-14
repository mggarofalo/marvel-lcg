using Marvel.Decisions;
using Marvel.Rules.Play;

namespace Marvel.Godot;

/// <summary>Binds opening-hand card controls to the current composer draft.</summary>
internal static class MulliganBinding
{
    internal static void Bind(DecisionPanel panel, BoardRenderResult? board)
    {
        if (board is null
            || panel.composer?.Selected is not { Targets: { } request } selected
            || !string.Equals(selected.Verb, Game.ResolveMulligans, StringComparison.Ordinal))
        {
            return;
        }

        DecisionComposer draft = panel.composer;
        board.BindMulliganTargets(request.Legal, panel.composer.Targets, target =>
        {
            int generation = panel.GetRenderGeneration();
            if (!panel.IsCurrentDraft(draft, generation))
            {
                return;
            }

            panel.ToggleMulliganTarget(target, draft, generation);
        });
    }
}
