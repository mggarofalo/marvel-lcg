using System.Text.Json.Serialization;
using Marvel.Rules.Timing;

namespace Marvel.Rules.State;

/// <summary>One incarnation-bound recipient of a completed multi-target attack.</summary>
/// <remarks>
/// rr:attack-player-ability-type.5 considers the attacker to have attacked each
/// enemy. This fact identifies a response instance, not a separate attack.
/// </remarks>
public sealed record Attacked(int Card, int Incarnation, OccurrenceCard Facts, long DamageTaken)
{
    /// <summary>Excess from settled damage placements that defeated this incarnation.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public long ExcessDamage { get; init; }

    /// <summary>The original public play area, retained across stage departure.</summary>
    public int Area { get; init; } = -1;

    /// <summary>The target name captured while it was a public attack recipient.</summary>
    public string? Title { get; init; }

    /// <summary>Capture public target facts before defeat or departure changes them.</summary>
    public static Attacked Capture(Card card, ICardFacts facts, long damageTaken = 0) =>
        new(card.ObjectId, card.Incarnation, OccurrenceCard.Capture(card, facts), damageTaken) { Title = EffectiveCards.Title(card, facts), Area = card.Area.Id };
}
