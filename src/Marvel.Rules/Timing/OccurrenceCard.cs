using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>The stable facts about a card in one occurrence role.</summary>
/// <remarks>
/// An occurrence outlives its interrupt window. These facts are captured when
/// that window opens, so moving a participant or changing its control cannot
/// rewrite the event an ability is answering. The rulebook defines the facts;
/// keeping the snapshot on the occurrence is the engine's choice.
/// </remarks>
/// <param name="Card">The card object's id.</param>
/// <param name="Kind">The card's classification when the occurrence began.</param>
/// <param name="Owner">The card's owner when the occurrence began.</param>
/// <param name="Controller">The card's controller when the occurrence began.</param>
public sealed record OccurrenceCard(int Card, CardKind Kind, int Owner, int Controller)
{
    /// <summary>Whether the participant was a villain.</summary>
    public bool IsVillain => Kind == CardKind.EncounterVillain;

    /// <summary>Whether the participant was a minion.</summary>
    public bool IsMinion => Kind == CardKind.Minion;

    /// <summary>Whether the participant was a hero.</summary>
    public bool IsHero => Kind == CardKind.Hero;

    /// <summary>Whether the participant was an ally.</summary>
    public bool IsAlly => Kind == CardKind.Ally;

    /// <summary>Whether the players controlled the participant.</summary>
    /// <remarks>
    /// <c>rr:friendly</c>: "Friendly is a blanket term that refers to cards the
    /// players control."
    /// </remarks>
    public bool IsFriendly => Controller >= 0;

    /// <summary>Whether the participant was an enemy.</summary>
    public bool IsEnemy => IsVillain || IsMinion;

    /// <summary>Capture one card's occurrence facts from the board.</summary>
    internal static OccurrenceCard Capture(Card card, ICardFacts facts)
    {
        CardKind kind = FacedownDrones.Kind(card, facts);

        // `rr:ownership-and-control.1` and `.2`: identities and player cards
        // are controlled by players; encounter cards are controlled by the
        // scenario. A character that changes control moves to its controller's
        // play area (`.5`), while Card.Owner remains who brought it to the game.
        int controller = kind is CardKind.Hero or CardKind.AlterEgo or CardKind.Ally
            ? card.Area.PlayArea.Player
            : World.Scenario;

        return new OccurrenceCard(card.ObjectId, kind, card.Owner, controller);
    }
}
