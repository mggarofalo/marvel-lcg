using Godot;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Inputs that travel together for one fixed-workspace render pass.</summary>
public sealed record BoardRenderRequest(
    VBoxContainer Destination,
    BoardPresentation Board,
    HFlowContainer Hand,
    Label HandHeading,
    InterfaceScale Scale,
    IDictionary<int, bool> ExpandedAreas,
    BoardPageState Pages,
    int? ViewedSeat,
    Prompt? Prompt,
    int? SelectedAffordance,
    IReadOnlyList<int> SelectedTargets,
    ICardArtProvider? Art = null);
