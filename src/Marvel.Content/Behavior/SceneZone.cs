using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Behavior;

/// <summary>The legal places the behavioral state vocabulary may arrange directly.</summary>
public enum SceneZone
{
#pragma warning disable CS1591, SA1602
    PlayerDeck,
    PlayerHand,
    PlayerDiscard,
    Ally,
    Support,
    Upgrade,
    Attachment,
    EngagedMinion,
    Obligation,
    EncounterDeck,
    EncounterDiscard,
    SideScheme,
    Environment,
    SetAside,
#pragma warning restore CS1591, SA1602
}
