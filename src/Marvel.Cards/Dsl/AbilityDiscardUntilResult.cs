namespace Marvel.Cards.Dsl;

/// <summary>The printed instruction applied to a matching discarded encounter card.</summary>
public enum AbilityDiscardUntilResult
{
    /// <summary>Resolve the card through the encounter reveal procedure.</summary>
    Reveal,
    /// <summary>Engage the first player without revealing the minion.</summary>
    PutIntoPlayFirstPlayer,
    /// <summary>Attach to the resolving source without resolving printed attachment selection.</summary>
    AttachToThis,
    /// <summary>Enter the printed encounter destination without revealing the card.</summary>
    PutIntoPlay,
}
