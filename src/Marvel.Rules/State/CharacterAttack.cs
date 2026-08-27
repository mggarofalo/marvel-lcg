namespace Marvel.Rules.State;

/// <summary>
/// A hero or ally attacking an enemy — <c>rr:attack-player-ability-type</c>.
/// </summary>
/// <remarks>
/// <para>
/// The player's half of <see cref="EnemyAttack"/>. It exists for the same
/// reason: <c>rr:attack-player-ability-type.step.7</c> and <c>.step.8</c> put
/// abilities around the attack — "after [character] attacks [and
/// damages/defeats] [an enemy/a minion]", "after [character] is attacked" —
/// and one of them may ask the player something, so the attack cannot be a
/// call that returns.
/// </para>
/// <para>
/// <b>Fewer fields than an enemy's attack, and the difference is the rules'.</b>
/// There is no boost card (<c>rr:boost-boost-icon</c> gives those to enemies),
/// no defender (<c>rr:defend-defense</c> is a player defending an enemy
/// attack), and no attacked <i>player</i> — <c>rr:attack-player-ability-type.4</c>
/// makes the target an enemy, and an enemy has no seat.
/// </para>
/// </remarks>
/// <param name="Attacker">
/// The object id of the character attacking. Not the seat: <c>rr:ally.2</c>
/// lets a player attack with an ally, and <c>rr:you-your.15</c> says an ally's
/// attack is <b>not</b> performed by that player's identity — so a card acting
/// on "the attacking character" needs the character and not its controller.
/// </param>
/// <param name="Enemy">The object id of the enemy being attacked.</param>
/// <param name="Player">
/// The seat whose turn it is. <c>rr:you-your.6</c> is why this travels beside
/// the attacker: an ability that triggers "after <b>you</b> attack" is about
/// the player, and a card in their play area reads it.
/// </param>
/// <param name="Amount">Fixed card-ability damage, or -1 for the attack statistic.</param>
/// <param name="Source">The damage-source card, or -1 when it is the attacker.</param>
/// <param name="MoveFrom">A card damage is moved from, or -1 for ordinary damage.</param>
/// <param name="Overkill">Whether this attack temporarily has overkill.</param>
/// <param name="Trigger">Event-stream provenance for the card ability.</param>
public sealed record CharacterAttack(
    int Attacker,
    int Enemy,
    int Player,
    long Amount = -1,
    int Source = -1,
    int MoveFrom = -1,
    bool Overkill = false,
    string Trigger = "Attack");
