using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>Where unselected draw-pile cards go while arranging a deck boundary.</summary>
public enum PlayerDeckRemainder
{
#pragma warning disable CS1591, SA1602
    Leave,
    Discard,
    Hand,
#pragma warning restore CS1591, SA1602
}
