using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Current semantic participants whose live table faces remain available during inspection.</summary>
internal static class CardInspectionProtection
{
    internal static IReadOnlySet<int> Participants(DecisionComposer? draft, WorldDescriptor? world)
    {
        var ids = new HashSet<int>();
        if (draft is not null)
        {
            if (draft.Selected?.AnchorId is { } source) ids.Add(source);
            ids.UnionWith(draft.Targets);
            ids.UnionWith(draft.Prompt.ContextCardIds);
            ids.UnionWith(draft.Prompt.CauseCardIds);
        }
        if (world?.ActiveAttack is { } attack)
        {
            ids.Add(attack.Source.Card);
            ids.Add(attack.Target.Card);
        }
        return ids;
    }
}
