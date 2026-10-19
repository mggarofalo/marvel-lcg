namespace Marvel.Rules.Play;

/// <summary>Scenario decks with lifecycle rules distinct from the encounter deck.</summary>
public enum AuxiliaryDeckKind
{
    /// <summary>Crossbones' weapons become ordinary encounter cards after entering play.</summary>
    ExperimentalWeapons,

    /// <summary>Red Skull's side schemes recycle through their own discard pile.</summary>
    SideSchemes,
}
