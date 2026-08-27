namespace Marvel.Rules.State;

/// <summary>
/// One enemy attack, part-way through its six steps.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:attack-enemy-activation</c> opens with "when an enemy initiates an
/// attack, it targets a specific player, then resolves that attack against that
/// player", and the six steps that follow all read from that targeting. So an
/// attack in progress is a value the board carries, not arguments threaded down
/// a call: the game can be put down between declaring a defender and dealing
/// the damage.
/// </para>
/// <para>
/// <b>Player and character are two questions.</b>
/// <c>rr:attack-enemy-activation.1</c> — "enemy attacks are always initiated
/// against both a player and a character" — and they come apart the moment an
/// ally defends: <c>rr:defend-defense.3.1</c> makes the ally the target
/// character, and <c>.5</c> makes its controller the target player. An ability
/// triggering "when the villain attacks <b>you</b>" reads the player
/// (<c>rr:attack-enemy-activation.1.4</c>), and the damage goes to the
/// character.
/// </para>
/// <para>
/// One at a time, which is <c>rr:activation.8</c>: an activation initiated
/// during another "resolves after the current activation has finished
/// resolving". So this is a single value rather than a stack.
/// </para>
/// </remarks>
/// <param name="Enemy">The attacking enemy's object id.</param>
/// <param name="Player">The seat being attacked.</param>
/// <param name="Target">The object id of the character the damage will go to.</param>
/// <param name="Defender">
/// The object id of the character declared as defender, or <c>-1</c> for an
/// undefended attack — <c>rr:attack-enemy-activation.4</c>.
/// </param>
/// <param name="BasicDefense">
/// Whether the defender is a hero using their basic defense power, whose DEF
/// reduces the damage — <c>rr:defend-defense.2</c>. An ally defending does not
/// reduce it (<c>rr:defend-defense.3</c>), and neither does a defense-labeled
/// ability (<c>rr:defend-defense.4.3</c>).
/// </param>
/// <param name="Damaged">
/// Whether the attack actually dealt damage to a character.
/// <c>rr:attack-enemy-activation.step.6.a</c> lists "after [character] attacks
/// <b>and damages</b> ... you" as a trigger of its own, so "it attacked" and
/// "it landed" are two different facts. <c>rr:tough.3</c> is what makes them
/// come apart in an ordinary game: a character whose tough status card absorbed
/// the attack "is not considered to have taken damage".
/// </param>
/// <param name="CalculatedDamage">
/// The damage fixed by step 4, or <c>null</c> before that step resolves.
/// <c>rr:attack-enemy-activation.step.4</c> calculates the amount and
/// <c>.step.5</c> deals that amount as a separate step, so effects between the
/// two do not recalculate it.
/// </param>
/// <param name="AdditionalPlayers">
/// Hero seats still to resolve this same attack against, in player order.
/// </param>
public sealed record EnemyAttack(
    int Enemy, int Player, int Target, int Defender = -1, bool BasicDefense = false,
    bool Damaged = false, long? CalculatedDamage = null,
    IReadOnlyList<int>? AdditionalPlayers = null)
{
    /// <summary>Whether any character was declared the defender.</summary>
    public bool IsDefended => Defender >= 0;

    /// <summary>The other heroes against whom this same attack must resolve.</summary>
    public IReadOnlyList<int> RemainingPlayers => AdditionalPlayers ?? [];
}
