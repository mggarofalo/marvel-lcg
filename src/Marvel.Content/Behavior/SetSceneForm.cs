using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>Shows one of the selected player's two printed identity faces.</summary>
public sealed record SetSceneForm(int Seat, string FaceId)
    : CoreSceneOperation
{
    /// <inheritdoc />
    public override string Name => "set-form";
}
