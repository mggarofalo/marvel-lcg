namespace Marvel.Content.Setup;

/// <summary>Why a card exists — which setup step asked for it.</summary>
public enum CreationSource
{
    /// <summary>The <c>rule_a,rule_b</c> pseudo-card every world has.</summary>
    Rules,

    /// <summary>A campaign-level challenge card.</summary>
    Challenge,

    /// <summary>The hero, b-face first.</summary>
    Identity,

    /// <summary>Set aside into the player's nemesis pile.</summary>
    Obligation,

    /// <summary>The rest of the nemesis set.</summary>
    Nemesis,

    /// <summary>The identity's signature cards.</summary>
    HeroDeck,

    /// <summary>The aspect cards.</summary>
    PlayerDeck,

    /// <summary>One per entry in the campaign's scheme list.</summary>
    MainScheme,

    /// <summary>Every villain stage, in printed order.</summary>
    Villain,

    /// <summary>The scenario's own encounter cards.</summary>
    Encounter,

    /// <summary>A scenario card its main scheme says to set aside.</summary>
    ScenarioSetAside,

    /// <summary>A named encounter set.</summary>
    EncounterSet,
}
