using System.Text.Json.Serialization;

namespace Marvel.Rules.Timing;

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
/// card each trigger. Bookkeeping identifies the card, ability and exact triggering fact.
/// </para>
/// <para>
/// <b>One window, however many conditions.</b>
/// <c>rr:triggering-condition.2</c> — a single game occurrence that creates
/// several triggering conditions, such as one attack that both damages a
/// character and defeats it, is handled with a single interrupt window and a
/// single response window. An engine that opened a window per condition would
/// lose the shared priority and ordering of abilities answering that moment.
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
        Resolution = new OccurrenceResolutions(Resolutions);
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

    /// <summary>Whether the exact paid event's reachable body removes threat at initiation.</summary>
    [JsonIgnore]
    public bool? EventRemovesThreat { get; init; }

    /// <summary>Original card-play payment for this exact entry, reconstructed by replay.</summary>
    [JsonIgnore]
    public Play.PlayedCardPayment? CardPlayPayment { get; init; }

    /// <summary>The imminent threat placement, when this occurrence is one.</summary>
    public State.ThreatPlacement? Threat { get; }

    /// <summary>The imminent damage owned by this window, reconstructed during replay.</summary>
    [JsonIgnore]
    public Play.DamageReplacement? Damage { get; internal set; }

    /// <summary>Whether this exact imminent assignment is damage from an attack.</summary>
    [JsonIgnore]
    public bool DamageFromAttack { get; internal init; }

    /// <summary>The exact damage-taking assignment, reconstructed during replay.</summary>
    [JsonIgnore]
    public Play.DirectDamagePrevention? DirectDamage { get; internal set; }

    /// <summary>The shared step-six window owning this exact recipient's authored interrupts.</summary>
    [JsonIgnore]
    public Play.DefeatWindow? SharedDefeatWindow { get; internal set; }


    /// <summary>The exact numeric boost contribution at its faceup interrupt.</summary>
    [JsonIgnore]
    public Play.BoostIconCancellation? BoostIcons { get; internal set; }

    /// <summary>The exact boost ability available before its execution.</summary>
    [JsonIgnore]
    public Play.BoostAbilityCancellation? BoostAbility { get; internal set; }

    private readonly TriggerUses triggers = new();

    private readonly List<Occurrence> threatPlacements = [];

    // The positional parameter of the same name, copied. Inside this body
    // `Conditions` is the constructor's argument; outside it is the property
    // below, which is this list. The two are the same set until something adds
    // to it -- see `Also`.
    private readonly List<string> conditions;

    private readonly OccurrenceDefeats defeats = new();

    private readonly AttackRecipients attacks = new();

    /// <summary>Actual enemy-attack placements belonging only to this occurrence.</summary>
    [JsonIgnore]
    public ReceivedAttackDamagePlacements ReceivedAttackDamage { get; } = new();

    /// <summary>The next play generation of this physical event in this occurrence.</summary>
    public int EventPlay(int card) => triggers.EventPlay(card);

    /// <summary>Complete one played event after its effects finish and it is discarded.</summary>
    public void CompleteEventPlay(int card) => triggers.CompleteEventPlay(card);

    /// <summary>Exact positive placements belonging to one threat instruction.</summary>
    [JsonIgnore]
    public IReadOnlyList<Occurrence> ThreatPlacements => threatPlacements;

    internal void RecordThreatPlacement(Occurrence placement)
    {
        threatPlacements.Add(placement);
        Also(Play.Steps.ThreatPlaced);
    }

    /// <summary>The exact printed basic power being used, reconstructed by replay.</summary>
    [JsonIgnore]
    public Play.BasicUse? BasicUse { get; init; }

    /// <summary>The lifecycle owner for abilities and cards resolving in this occurrence.</summary>
    [JsonIgnore]
    public OccurrenceResolutions Resolution { get; }

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
        int player = -1) =>
        OccurrenceCapture.Attack(id, conditions, world, facts, actor, target, player);

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
        int player) =>
        OccurrenceCapture.Thwart(id, conditions, world, facts, actor, scheme, player);

    /// <summary>Create an occurrence for one imminent threat assignment.</summary>
    public static Occurrence ForThreat(
        int id, IReadOnlyList<string> conditions, State.World world,
        State.ICardFacts facts, State.ThreatPlacement placement, int? subject = null) =>
        OccurrenceCapture.Threat(id, conditions, world, facts, placement, subject);

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
    public IReadOnlyList<State.Defeated> Defeats => defeats.Items;

    /// <summary>Engine-owned completion facts for native retaliate responses.</summary>
    [JsonIgnore]
    public State.RetaliationContext? Retaliation { get; set; }

    /// <summary>The enemy attack completed by this occurrence, when present.</summary>
    [JsonIgnore]
    public State.CompletedEnemyAttack? CompletedEnemyAttack { get; internal set; }

    /// <summary>Ordered recipient facts for one multi-target attack.</summary>
    public IReadOnlyList<State.Attacked> Attacks => attacks.Items;

    /// <summary>Record a recipient without creating another attack or window.</summary>
    public void Also(State.Attacked what) => attacks.Add(what);

    /// <summary>Ability and card resolution state accumulated in this occurrence.</summary>
    /// <remarks>
    /// These are data because a resolution can cross a player prompt, an enemy
    /// activation, and a save boundary before the occurrence reaches its
    /// response window. The address spelling is the engine's choice; the
    /// resolved/unresolved distinction is <c>rr:resolve.2-.8</c>.
    /// </remarks>
    public IReadOnlyList<ResolutionEntry> Resolutions => Resolution.Entries;

    /// <summary>
    /// The one card this occurrence defeated, or null.
    /// </summary>
    /// <remarks>
    /// A window can contain several defeat instances. Callers evaluating a
    /// singular defeated-card reference must bind the chosen triggering fact;
    /// only an unambiguous singleton can be read without that binding.
    /// </remarks>
    public State.Defeated? Defeat => defeats.Single;

    /// <summary>Whether this occurrence creates a named triggering condition.</summary>
    /// <param name="condition">One of <c>rr:triggering-condition</c>'s occurrences.</param>
    public bool Is(string condition) =>
        Conditions.Contains(condition, StringComparer.Ordinal);

    /// <summary>
    /// Adds a triggering condition this occurrence turned out to create —
    /// <c>rr:triggering-condition.2</c>.
    /// </summary>
    /// <remarks>
    /// Condition names identify the shared window; individual defeats retain
    /// their own facts in <see cref="Defeats"/>. Each ability can answer each
    /// matching instance once (<c>rr:triggering-condition.1</c>).
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

    /// <summary>Whether the exact ability and triggering fact remain unused.</summary>
    public bool MayTrigger(WindowKind window, PendingAbility ability) => triggers.MayTrigger(window, ability);

    /// <summary>Consume one exact ability use in this occurrence.</summary>
    public bool Trigger(WindowKind window, PendingAbility ability) => triggers.Trigger(window, ability);
}
