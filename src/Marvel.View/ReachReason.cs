namespace Marvel.View;

/// <summary>Why a seat's cards cannot affect a card in play; a client words each reason itself.</summary>
public enum ReachReason
{
    /// <summary>
    /// The card is in another game area: "Cards and components in one game area cannot affect
    /// another game area" (<c>pack:mc11:game-areas</c>). <c>Places.CanAffect</c> answers no for this
    /// reason only, so it is the one reason the engine can give today.
    /// </summary>
    OtherGameArea = 1,
}
