using System.Text.Json.Serialization;

namespace Marvel.Rules.Timing;

/// <summary>Which of an occurrence's two windows is open.</summary>
public enum WindowKind
{
    /// <summary>Before the occurrence resolves — <c>rr:interrupt.3</c>.</summary>
    Interrupt,

    /// <summary>After it has resolved — <c>rr:response</c>.</summary>
    Response,
}

/// <summary>Whether rule-defined resolution is still pending, succeeded, or did not occur.</summary>
public enum ResolutionStatus
{
    /// <summary>The ability or card has initiated but has not finished.</summary>
    Pending,

    /// <summary>At least one required child effect or ability resolved.</summary>
    Resolved,

    /// <summary>Completion or cancellation left no resolved child.</summary>
    Unresolved,
}

/// <summary>Resolution state for a card or one exact ability on it.</summary>
/// <param name="Card">The source card's object id.</param>
/// <param name="Ability">The ability type, or null when this entry is for the card.</param>
/// <param name="Ordinal">The same-type ability ordinal, or -1 for a card entry.</param>
/// <param name="Status">Its current rule-defined status.</param>
/// <param name="Applied">Whether at least one child effect has applied so far.</param>
public sealed record ResolutionEntry(
    int Card, AbilityType? Ability, int Ordinal, ResolutionStatus Status,
    bool Applied = false);

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
public sealed record Occurrence
{
    /// <summary>Creates one saveable occurrence.</summary>
    /// <param name="Id">Distinguishes this occurrence from another of the same shape.</param>
    /// <param name="Conditions">Its triggering conditions.</param>
    /// <param name="Subject">The card this happens to or because of.</param>
    /// <param name="Player">The seat it concerns.</param>
    /// <param name="Actor">The card performing it.</param>
    /// <param name="Target">The game element acted on.</param>
    /// <param name="ActorFacts">Stable facts about the actor.</param>
    /// <param name="TargetFacts">Stable facts about the target.</param>
    /// <param name="Threat">The imminent threat assignment, when present.</param>
    /// <param name="Resolutions">Persisted ability and card resolution state.</param>
    [JsonConstructor]
    public Occurrence(
        int Id,
        IReadOnlyList<string> Conditions,
        int Subject = -1,
        int Player = -1,
        int Actor = -1,
        int Target = -1,
        OccurrenceCard? ActorFacts = null,
        OccurrenceCard? TargetFacts = null,
        State.ThreatPlacement? Threat = null,
        IReadOnlyList<ResolutionEntry>? Resolutions = null)
    {
        ArgumentNullException.ThrowIfNull(Conditions);
        this.Id = Id;
        this.Subject = Subject;
        this.Player = Player;
        this.Actor = Actor;
        this.Target = Target;
        this.ActorFacts = ActorFacts;
        this.TargetFacts = TargetFacts;
        this.Threat = Threat;
        conditions = [.. Conditions];
        resolutions = [.. Resolutions ?? []];
    }

    /// <summary>Distinguishes this occurrence from another of the same shape.</summary>
    public int Id { get; }

    /// <summary>The card this is happening to or because of, or -1.</summary>
    public int Subject { get; }

    /// <summary>The player this occurrence concerns, or -1.</summary>
    public int Player { get; }

    /// <summary>The card performing this occurrence, or -1.</summary>
    public int Actor { get; }

    /// <summary>The game element acted on, or -1.</summary>
    public int Target { get; }

    /// <summary>Stable facts about <see cref="Actor"/>.</summary>
    public OccurrenceCard? ActorFacts { get; }

    /// <summary>Stable facts about <see cref="Target"/>.</summary>
    public OccurrenceCard? TargetFacts { get; }

    /// <summary>The imminent threat placement, when this occurrence is one.</summary>
    public State.ThreatPlacement? Threat { get; }

    private readonly HashSet<(WindowKind Window, int Card)> spent = [];

    // The positional parameter of the same name, copied. Inside this body
    // `Conditions` is the constructor's argument; outside it is the property
    // below, which is this list. The two are the same set until something adds
    // to it -- see `Also`.
    private readonly List<string> conditions;

    private readonly List<State.Defeated> defeats = [];

    private readonly List<ResolutionEntry> resolutions;

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

    /// <summary>Create an occurrence for one imminent threat assignment.</summary>
    public static Occurrence ForThreat(
        int id, IReadOnlyList<string> conditions, State.World world,
        State.ICardFacts facts, State.ThreatPlacement placement, int? subject = null)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(placement);

        OccurrenceCard? source = placement.Source >= 0
            ? OccurrenceCard.Capture(world.Cards[placement.Source], facts)
            : null;
        OccurrenceCard? target = placement.Scheme >= 0
            ? OccurrenceCard.Capture(world.Cards[placement.Scheme], facts)
            : null;

        return new Occurrence(
            id,
            conditions,
            Subject: subject ?? placement.Scheme,
            Player: placement.Player,
            Actor: placement.Source,
            Target: placement.Scheme,
            ActorFacts: source,
            TargetFacts: target,
            Threat: placement);
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

    /// <summary>Ability and card resolution state accumulated in this occurrence.</summary>
    /// <remarks>
    /// These are data because a resolution can cross a player prompt, an enemy
    /// activation, and a save boundary before the occurrence reaches its
    /// response window. The address spelling is the engine's choice; the
    /// resolved/unresolved distinction is <c>rr:resolve.2-.8</c>.
    /// </remarks>
    public IReadOnlyList<ResolutionEntry> Resolutions => resolutions;

    /// <summary>Begin resolving one exact triggered ability.</summary>
    public void Begin(PendingAbility ability)
    {
        int found = ResolutionIndex(ability);
        if (found < 0)
        {
            resolutions.Add(new ResolutionEntry(
                ability.Card, ability.Type, ability.Ordinal,
                ability.Type == AbilityType.Constant
                    ? ResolutionStatus.Unresolved
                    : ResolutionStatus.Pending));
        }
    }

    /// <summary>Begin resolving an event or treachery and all of its abilities.</summary>
    public void BeginCard(int card, IReadOnlyList<PendingAbility> abilities)
    {
        if (resolutions.All(entry => entry.Card != card || entry.Ability is not null))
        {
            resolutions.Add(new ResolutionEntry(
                card, null, -1, ResolutionStatus.Pending));
        }
        foreach (var ability in abilities)
        {
            Begin(ability);
        }
        RefreshCard(card);
    }

    /// <summary>Record that at least one effect applied while the ability remains in progress.</summary>
    public void Resolve(PendingAbility ability)
    {
        int found = ResolutionIndex(ability);
        if (found < 0)
        {
            Begin(ability);
            found = ResolutionIndex(ability);
        }
        if (ability.Type != AbilityType.Constant)
        {
            resolutions[found] = resolutions[found] with
            {
                Applied = true,
                Status = resolutions[found].Status == ResolutionStatus.Unresolved
                    ? ResolutionStatus.Resolved
                    : resolutions[found].Status,
            };
            RefreshCard(ability.Card);
        }
    }

    /// <summary>Finish an ability whose effects did or did not apply.</summary>
    public void Complete(PendingAbility ability)
    {
        int found = ResolutionIndex(ability);
        if (found < 0)
        {
            Begin(ability);
            found = ResolutionIndex(ability);
        }
        if (resolutions[found].Status == ResolutionStatus.Pending)
        {
            resolutions[found] = resolutions[found] with
            {
                Status = resolutions[found].Applied
                    ? ResolutionStatus.Resolved
                    : ResolutionStatus.Unresolved,
            };
        }
        RefreshCard(ability.Card);
    }

    /// <summary>Cancel an ability before any of its effects resolve.</summary>
    public void Cancel(PendingAbility ability)
    {
        Set(ability, ResolutionStatus.Unresolved, applied: false);
        RefreshCard(ability.Card);
    }

    /// <summary>The current status of an exact ability; absent abilities are unresolved.</summary>
    public ResolutionStatus StatusOf(PendingAbility ability)
    {
        int found = ResolutionIndex(ability);
        return found < 0 ? ResolutionStatus.Unresolved : resolutions[found].Status;
    }

    /// <summary>The current status of a card; absent cards are unresolved.</summary>
    public ResolutionStatus CardStatus(int card) => resolutions.FirstOrDefault(entry =>
        entry.Card == card && entry.Ability is null)?.Status
        ?? ResolutionStatus.Unresolved;

    private int ResolutionIndex(PendingAbility ability) => resolutions.FindIndex(entry =>
        entry.Card == ability.Card
        && entry.Ability == ability.Type
        && entry.Ordinal == ability.Ordinal);

    private void Set(
        PendingAbility ability, ResolutionStatus status, bool applied = false)
    {
        if (ability.Type == AbilityType.Constant)
        {
            status = ResolutionStatus.Unresolved;
        }
        int found = ResolutionIndex(ability);
        if (found < 0)
        {
            resolutions.Add(new ResolutionEntry(
                ability.Card, ability.Type, ability.Ordinal, status, applied));
        }
        else
        {
            resolutions[found] = resolutions[found] with
            {
                Status = status,
                Applied = applied,
            };
        }
    }

    private void RefreshCard(int card)
    {
        int cardEntry = resolutions.FindIndex(entry =>
            entry.Card == card && entry.Ability is null);
        if (cardEntry < 0)
        {
            return;
        }

        var abilities = resolutions.Where(entry =>
            entry.Card == card && entry.Ability is not null).ToList();
        var status = abilities.Any(entry => entry.Status == ResolutionStatus.Resolved)
            ? ResolutionStatus.Resolved
            : abilities.Any(entry => entry.Status == ResolutionStatus.Pending)
                ? ResolutionStatus.Pending
                : ResolutionStatus.Unresolved;
        resolutions[cardEntry] = resolutions[cardEntry] with { Status = status };
    }

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
