namespace Marvel.Rules.Prompts;

/// <summary>The kind of a <see cref="PromptEffect"/>; a wire type, so members keep their values.</summary>
public enum PromptEffectKind
{
    /// <summary>Deals damage (rr:damage).</summary>
    Damage = 0,

    /// <summary>Removes threat from a scheme (rr:threat).</summary>
    RemoveThreat = 1,

    /// <summary>Draws cards (rr:draw-drawing-cards).</summary>
    Draw = 2,

    /// <summary>Gives a stunned status card (rr:stun-stunned).</summary>
    Stun = 3,

    /// <summary>Gives a confused status card (rr:confuse-confused).</summary>
    Confuse = 4,

    /// <summary>Gives a tough status card (rr:tough).</summary>
    Tough = 5,

    /// <summary>Heals damage (rr:heal).</summary>
    Heal = 6,

    /// <summary>Readies cards (rr:ready).</summary>
    Ready = 7,

    /// <summary>Exhausts cards (rr:exhausted).</summary>
    Exhaust = 8,

    /// <summary>Places threat on a scheme (rr:threat).</summary>
    PlaceThreat = 9,

    /// <summary>Moves threat from one scheme to another (rr:threat).</summary>
    MoveThreat = 10,
}
