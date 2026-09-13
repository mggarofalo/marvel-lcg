namespace Marvel.Rules.State;

/// <summary>Rules-level relationships between printed card kinds.</summary>
public static class CardKinds
{
    /// <summary>Whether a printed kind functions as a villain.</summary>
    /// <remarks>
    /// <c>pack:mc56:leaders</c> and <c>pack:mc57:new-card-type-leader</c> call
    /// Leader a new card type, then say leaders function exactly like villains
    /// and every game rule and card ability affecting villains affects leaders.
    /// The kind stays distinct; this predicate is the shared rules meaning.
    /// </remarks>
    /// <param name="kind">The printed face kind.</param>
    public static bool IsVillain(CardKind kind) =>
        kind is CardKind.EncounterVillain or CardKind.Leader;

    /// <summary>Whether a printed kind is an enemy.</summary>
    /// <param name="kind">The printed face kind.</param>
    public static bool IsEnemy(CardKind kind) =>
        kind == CardKind.Minion || IsVillain(kind);

    /// <summary>Whether a printed kind is a character.</summary>
    /// <param name="kind">The printed face kind.</param>
    public static bool IsCharacter(CardKind kind) =>
        kind is CardKind.Hero or CardKind.AlterEgo or CardKind.Ally
        || IsEnemy(kind);
}
