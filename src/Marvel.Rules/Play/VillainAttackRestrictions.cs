using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Restrictions that allow attacking one villain while prohibiting the others.</summary>
public static class VillainAttackRestrictions
{
    /// <summary>A positive modifier on a villain prohibits attacks on every other villain.</summary>
    // The engine chooses this modifier spelling; printed text supplies its meaning.
    public const string OtherVillains = "other_villains_cannot_be_attacked";

    /// <summary>Whether each restricting villain is the selected villain itself.</summary>
    public static bool Allows(Card target, IEnumerable<Card> restrictingVillains) =>
        restrictingVillains.All(villain => villain.ObjectId == target.ObjectId);
}
