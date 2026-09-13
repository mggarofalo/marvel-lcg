namespace Marvel.View;

/// <summary>The small reviewed vocabulary of table connections a renderer may draw.</summary>
public enum RelationshipKind
{
    /// <summary>An attachment or status is hosted by a card.</summary>
    Attachment,

    /// <summary>An enemy is engaged with a player seat.</summary>
    Engagement,

    /// <summary>An offered action has a visible source and a candidate target.</summary>
    OfferedTarget,

    /// <summary>An offered payment source contributes to an action.</summary>
    OfferedGenerator,

    /// <summary>An engine result names the card it changed.</summary>
    Result,
}
