namespace Marvel.Rules.State;

/// <summary>
/// The enemy activation being resolved, of either kind — <c>rr:activation</c>.
/// </summary>
/// <remarks>
/// <para>
/// "Whenever an enemy attacks or schemes, it is considered to have activated.
/// There are two types of enemy activations: an attack activation and a scheme
/// activation." The two run through different steps and only one of them had a
/// value on the board: <see cref="EnemyAttack"/> carries the six steps of
/// <c>rr:attack-enemy-activation</c> and a scheme has nothing like it, so a card
/// asking about "the activating enemy" could be answered during half the game.
/// </para>
/// <para>
/// <b>Not a second copy of <see cref="EnemyAttack"/>.</b> That record is the
/// state of an attack part-way through — who is defending, what the boost cards
/// came to. This is the umbrella both kinds sit under, and it holds only what is
/// true of both: which enemy, against which seat, and which kind it is.
/// </para>
/// <para>
/// One at a time, which is <c>rr:activation.8</c>: an activation initiated
/// during another "resolves after the current activation has finished
/// resolving". So this is a single value rather than a stack, the same as
/// <see cref="EnemyAttack"/>.
/// </para>
/// </remarks>
/// <param name="Enemy">The activating enemy's object id.</param>
/// <param name="Player">The seat it is activating against.</param>
/// <param name="Attacking">
/// Whether this is an attack activation. <c>rr:activation.1</c> reads the
/// player's form to choose, and the two kinds are told apart here because
/// <c>rr:lasting-effects</c> gives them different endings —
/// <c>TimingPoints.EndOfAttack</c> is not <c>TimingPoints.EndOfActivation</c>.
/// </param>
public sealed record EnemyActivation(int Enemy, int Player, bool Attacking);
