namespace Marvel.Rules.Timing;

/// <summary>Which of an occurrence's two windows is open.</summary>
public enum WindowKind
{
    /// <summary>Before the occurrence resolves — <c>rr:interrupt.3</c>.</summary>
    Interrupt,

    /// <summary>After it has resolved — <c>rr:response</c>.</summary>
    Response,
}

/// <summary>
/// One thing happening in the game, and the two windows around it.
/// </summary>
/// <remarks>
/// <para>
/// A triggering condition is "a specific occurrence that takes place in the
/// game" (<c>rr:triggering-condition</c>). This is that occurrence, and it
/// exists as an object rather than a moment for one reason: two rules are about
/// an occurrence's <i>identity</i> and cannot be written without it.
/// </para>
/// <para>
/// <b>Once each.</b> <c>rr:triggering-condition.1</c> — each interrupt and each
/// response may be triggered only once per occurrence of its triggering
/// condition, though <c>rr:triggering-condition.1.1</c> lets two copies of a
/// card each trigger. So the bookkeeping is per card, not per printed face.
/// </para>
/// <para>
/// <b>One window, however many conditions.</b>
/// <c>rr:triggering-condition.2</c> — a single game occurrence that creates
/// several triggering conditions, such as one attack that both damages a
/// character and defeats it, is handled with a single interrupt window and a
/// single response window. An engine that opened a window per condition would
/// let one interrupt fire twice against what the rules call one moment.
/// </para>
/// </remarks>
/// <param name="Id">Distinguishes this occurrence from another of the same shape.</param>
/// <param name="Conditions">
/// The triggering conditions this occurrence is known to create when it is
/// scheduled. More than one is the <c>rr:triggering-condition.2</c> case, and
/// they still share these windows. <b>More can be added while it happens</b> —
/// see the property of the same name.
/// </param>
/// <param name="Subject">
/// The card this is happening to or because of, or <c>-1</c>. An enemy for an
/// activation, the revealed card for a reveal.
/// </param>
/// <param name="Player">
/// The seat it concerns, or <c>-1</c>. A card cannot answer "when the villain
/// attacks <b>you</b>" without it — <c>rr:attack-enemy-activation.1.4</c> makes
/// that phrase mean the attacked <i>player</i>, whichever character was
/// targeted.
/// </param>
/// <param name="Actor">
/// The card performing the occurrence — an attacker or thwarter — or <c>-1</c>.
/// </param>
/// <param name="Target">
/// The game element it acts on — an attacked character or thwarted scheme — or <c>-1</c>.
/// </param>
/// <param name="ActorFacts">Stable classifications and relationships for <paramref name="Actor"/>.</param>
/// <param name="TargetFacts">Stable classifications and relationships for <paramref name="Target"/>.</param>
public sealed record Occurrence(
    int Id,
    IReadOnlyList<string> Conditions,
    int Subject = -1,
    int Player = -1,
    int Actor = -1,
    int Target = -1,
    OccurrenceCard? ActorFacts = null,
    OccurrenceCard? TargetFacts = null)
{
    private readonly HashSet<(WindowKind Window, int Card)> spent = [];

    // The positional parameter of the same name, copied. Inside this body
    // `Conditions` is the constructor's argument; outside it is the property
    // below, which is this list. The two are the same set until something adds
    // to it -- see `Also`.
    private readonly List<string> conditions = [.. Conditions];

    private readonly List<State.Defeated> defeats = [];

    /// <summary>An occurrence creating a single triggering condition.</summary>
    /// <param name="id">Distinguishes this occurrence from another of the same shape.</param>
    /// <param name="condition">What happened.</param>
    public Occurrence(int id, string condition)
        : this(id, [condition])
    {
    }

    /// <summary>Create an occurrence with explicit attack roles.</summary>
    /// <remarks>
    /// The actor and target ids identify the participants. Their companion
    /// values freeze every derived fact that trigger matching may use. The
    /// occurrence deliberately leaves <see cref="Subject"/> empty: an attack
    /// has two card roles, and choosing either one as "the subject" loses the
    /// other.
    /// </remarks>
    public static Occurrence ForAttack(
        int id,
        IReadOnlyList<string> conditions,
        State.World world,
        State.ICardFacts facts,
        int actor,
        int target,
        int player = -1)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        return new Occurrence(
            id,
            conditions,
            Player: player,
            Actor: actor,
            Target: target,
            ActorFacts: OccurrenceCard.Capture(world.Cards[actor], facts),
            TargetFacts: OccurrenceCard.Capture(world.Cards[target], facts));
    }

    /// <summary>Create a thwart occurrence with explicit actor and scheme roles.</summary>
    /// <remarks>
    /// A thwart is not an attack, but cards still distinguish the character
    /// doing it from the scheme it acts on. <see cref="Subject"/> retains the
    /// scheme for existing "this scheme" triggers; actor and target add the
    /// two roles without changing that established meaning.
    /// </remarks>
    public static Occurrence ForThwart(
        int id,
        IReadOnlyList<string> conditions,
        State.World world,
        State.ICardFacts facts,
        int actor,
        int scheme,
        int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);

        return new Occurrence(
            id,
            conditions,
            Subject: scheme,
            Player: player,
            Actor: actor,
            Target: scheme,
            ActorFacts: OccurrenceCard.Capture(world.Cards[actor], facts),
            TargetFacts: OccurrenceCard.Capture(world.Cards[scheme], facts));
    }

    /// <summary>
    /// Every triggering condition this occurrence has created so far.
    /// </summary>
    /// <remarks>
    /// <b>Not fixed when the occurrence is made.</b> Some triggering conditions
    /// are only known once the occurrence is part-way through happening, and
    /// <c>rr:triggering-condition.2</c> names the example: "a single attack
    /// causing a character to both take damage <b>and be defeated</b>". Whether
    /// the damage defeats the character is not knowable until it is dealt, and
    /// the rule says the two conditions share one window pair rather than
    /// getting one each — so the list grows and the windows do not.
    /// </remarks>
    public IReadOnlyList<string> Conditions => conditions;

    /// <summary>
    /// The cards this occurrence defeated, in the order it defeated them.
    /// </summary>
    /// <remarks>
    /// Provenance lives here rather than on the board because <b>this is the
    /// thing that lasts exactly as long as the question does</b>. A card
    /// answering "after an ally is defeated" is asked in the response window,
    /// which is after the defeat and after whatever else the occurrence did; a
    /// field on <see cref="State.World"/> would have to be set before that and
    /// cleared after it, and the clearing is what nobody remembers.
    /// </remarks>
    public IReadOnlyList<State.Defeated> Defeats => defeats;

    /// <summary>
    /// The one card this occurrence defeated, or null.
    /// </summary>
    /// <remarks>
    /// <b>Refuses rather than picks</b> when there is more than one. A card
    /// says "the defeated card" in the singular, and one effect that defeats
    /// two characters at once leaves nothing in the rules to say which of them
    /// a response is about — <c>rr:triggering-condition.1</c> would let the
    /// ability trigger once, and once is the wrong number for two allies. That
    /// is a real question and this engine has not answered it, so it says so
    /// where the ambiguity actually bites rather than in every multiple defeat.
    /// </remarks>
    public State.Defeated? Defeat => defeats.Count switch
    {
        0 => null,
        1 => defeats[0],
        _ => throw new Play.RulesNotImplementedException(
            $"{defeats.Count} cards were defeated by one occurrence, and a card asking for "
            + "'the defeated card' names one. rr:triggering-condition.2 gives them a single "
            + "response window between them and nothing says which defeat it is about"),
    };

    /// <summary>Whether this occurrence creates a named triggering condition.</summary>
    /// <param name="condition">One of <c>rr:triggering-condition</c>'s occurrences.</param>
    public bool Is(string condition) =>
        Conditions.Contains(condition, StringComparer.Ordinal);

    /// <summary>
    /// Adds a triggering condition this occurrence turned out to create —
    /// <c>rr:triggering-condition.2</c>.
    /// </summary>
    /// <remarks>
    /// Idempotent, because the rule is about which conditions the occurrence
    /// creates and not how many times it created them. Two allies defeated by
    /// one blast is one <c>WhenCardDefeated</c> in the list, and
    /// <c>rr:triggering-condition.1</c> then lets each answering ability
    /// trigger once.
    /// </remarks>
    /// <param name="condition">What else happened.</param>
    public void Also(string condition)
    {
        if (!conditions.Contains(condition, StringComparer.Ordinal))
        {
            conditions.Add(condition);
        }
    }

    /// <summary>
    /// Records that this occurrence defeated a card — <c>rr:defeat</c>.
    /// </summary>
    /// <remarks>
    /// The defeat is not an occurrence of its own. <c>rr:triggering-condition.2</c>
    /// is explicit about the case and uses it as its own example: "a single
    /// attack causing a character to both take damage and be defeated" gets
    /// "<b>a single interrupt window and a single response window</b>". So the
    /// attack that killed the ally and the ally's death are one moment, and
    /// this is how the second half joins the first.
    /// </remarks>
    /// <param name="what">The card, who did it, and how.</param>
    public void Also(State.Defeated what)
    {
        defeats.Add(what);
        Also(Play.Steps.CardDefeated);
    }

    /// <summary>Whether a card's ability may still be triggered in this window.</summary>
    /// <param name="window">Which window.</param>
    /// <param name="card">The object id of the card carrying the ability.</param>
    public bool MayTrigger(WindowKind window, int card) => !spent.Contains((window, card));

    /// <summary>Record that a card's ability has been triggered in this window.</summary>
    /// <remarks>
    /// Keyed on the card's object id, so two copies of the same printed card
    /// each get a turn — <c>rr:triggering-condition.1.1</c>.
    /// </remarks>
    /// <param name="window">Which window.</param>
    /// <param name="card">The object id of the card carrying the ability.</param>
    /// <returns>False when it had already been triggered.</returns>
    public bool Trigger(WindowKind window, int card) => spent.Add((window, card));
}
