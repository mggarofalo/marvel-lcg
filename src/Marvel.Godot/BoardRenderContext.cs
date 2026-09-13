namespace Marvel.Godot;

/// <summary>Shared presentation state for one board render pass.</summary>
internal sealed record BoardRenderContext(
    BoardRenderResult Result,
    InterfaceScale Scale,
    IDictionary<int, bool> ExpandedAreas,
    BoardPageState Pages,
    BoardInteractionPresentation Interaction,
    ICardArtProvider? Art);
