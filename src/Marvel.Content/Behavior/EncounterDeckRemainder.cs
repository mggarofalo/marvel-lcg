using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>Where unselected encounter cards go while arranging a deck boundary.</summary>
public enum EncounterDeckRemainder
{
#pragma warning disable CS1591, SA1602
    Leave,
    Discard,
    Dealt,
#pragma warning restore CS1591, SA1602
}
