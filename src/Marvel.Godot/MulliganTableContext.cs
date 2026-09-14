using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Dependencies for one desktop opening-hand tabletop render.</summary>
internal sealed class MulliganTableContext
{
    internal required BoardPresentation Board { get; init; }
    internal required HBoxContainer Hand { get; init; }
    internal required BoardRenderResult Result { get; init; }
    internal required InterfaceScale Scale { get; init; }
    internal ICardArtProvider? Art { get; init; }
    internal required int Player { get; init; }
    internal required int PromptOwner { get; init; }
    internal required Action<int> SwitchSeat { get; init; }
}
