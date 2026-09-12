using Marvel.Rules.Timing;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>How far through its three parts a step has got.</summary>
/// <remarks>
/// The parts are <c>rr:ability</c>'s: an interrupt window, the occurrence, a
/// response window. A step is in exactly one of them at any moment, which is
/// what makes the whole thing resumable.
/// </remarks>

/// <summary>The steps this engine knows how to take.</summary>
/// <remarks>
/// Named after the Rules Reference's own steps, so a divergence can be argued
/// against the published text rather than against a call graph.
/// </remarks>
public static class Steps
{
    /// <summary>The villain phase, which schedules its six steps.</summary>
    public const string VillainPhase = "VillainPhase";

    /// <summary>Step 1 — <c>rr:villain-phase.step.1</c>.</summary>
    public const string PlaceThreat = "PlaceThreat";

    /// <summary>Threat placed by a card ability or keyword.</summary>
    public const string PlaceThreatEffect = "PlaceThreatEffect";

    /// <summary>Step 2, a heading — <c>rr:villain-phase.step.2</c>.</summary>
    public const string EnemiesActivate = "EnemiesActivate";

    /// <summary>
    /// One enemy attacking one player — <c>rr:activation.1</c>,
    /// <c>rr:attack-enemy-activation</c>.
    /// </summary>
    public const string Attack = "Attack";

    /// <summary>
    /// One enemy scheming — <c>rr:activation.1</c>,
    /// <c>rr:scheme-enemy-activation</c>. Steps 1 and 2: the boost card.
    /// </summary>
    public const string Scheme = "Scheme";

    /// <summary>
    /// The initiating effect resumes after an attack activation fully resolves —
    /// <c>rr:activation.7</c>.
    /// </summary>
    public const string CompleteAttackActivation = "CompleteAttackActivation";

    /// <summary>The parallel completion sentinel for a scheme activation.</summary>
    public const string CompleteSchemeActivation = "CompleteSchemeActivation";

    /// <summary>An early scheme end after its activating minion leaves play.</summary>
    public const string EndSchemeEarly = "EndSchemeEarly";

    /// <summary>
    /// Damage step 8, after nested step-7 abilities and before the original
    /// occurrence's response window.
    /// </summary>
    public const string FinalizeCharacterDefeat = "FinalizeCharacterDefeat";

    /// <summary>The parallel step-8 continuation for a defeated side scheme.</summary>
    public const string FinalizeSchemeDefeat = "FinalizeSchemeDefeat";

    /// <summary>
    /// Step 3 of a scheme activation —
    /// <c>rr:scheme-enemy-activation.step.3</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A step of its own because step 2 can stop and ask.</b> "Resolve each
    /// of the scheming enemy's boost cards" is step 2 and "place threat on the
    /// main scheme equal to the scheming enemy's modified SCH value" is step 3,
    /// in that order — and a <b>Boost</b> ability that offers the player a
    /// choice suspends. Resolved inline, the threat went on the scheme while
    /// the question was still on the table, and whatever the player chose
    /// arrived too late to count.
    /// </para>
    /// <para>
    /// The attack activation has the same shape:
    /// <see cref="FlipBoostCards"/> is step 3 and
    /// <see cref="CalculateAttackDamage"/> is step 4, so a boost card's
    /// question is answered between them. This is the same split one
    /// activation over.
    /// </para>
    /// <para>
    /// <b>It is also where a scheme activation ends</b>, so it carries
    /// <see cref="SchemeEnds"/> — the parallel of <see cref="AttackEnds"/> on
    /// <see cref="EndAttack"/>. "After [enemy] schemes" is a claim about the
    /// activation being over, and <c>rr:activation.6</c> is where it is over.
    /// </para>
    /// <para>
    /// It does not carry <c>WhenThreatPlaced</c>. That is
    /// <see cref="PlaceThreat"/>'s, and <see cref="PlaceThreat"/> is villain
    /// phase step 1 — a different moment. Hunting Gene Traitors answers "after
    /// resolving step one of the villain phase" and must not fire again every
    /// time the villain schemes.
    /// </para>
    /// </remarks>
    public const string SchemeThreat = "SchemeThreat";

    /// <summary>
    /// Step 1 of an attack — <c>rr:attack-enemy-activation.step.1</c>.
    /// </summary>
    public const string GiveBoostCard = "GiveBoostCard";

    /// <summary>
    /// Step 2 of an attack — <c>rr:attack-enemy-activation.step.2</c>.
    /// </summary>
    public const string DeclareDefender = "DeclareDefender";

    /// <summary>
    /// Step 3 of an attack — <c>rr:attack-enemy-activation.step.3</c>.
    /// </summary>
    public const string FlipBoostCards = "FlipBoostCards";

    /// <summary>
    /// Step 4 of an attack — <c>rr:attack-enemy-activation.step.4</c>.
    /// The calculated amount is saved on the attack for the next step.
    /// </summary>
    public const string CalculateAttackDamage = "CalculateAttackDamage";

    /// <summary>
    /// Step 5 of an attack — <c>rr:attack-enemy-activation.step.5</c>.
    /// This deals the amount fixed by <see cref="CalculateAttackDamage"/>.
    /// </summary>
    public const string DealAttackDamage = "DealAttackDamage";

    /// <summary>
    /// Move the same attack to another hero before that hero's defender window.
    /// This is an engine plan and therefore opens no timing windows of its own.
    /// </summary>
    public const string NextAttackTarget = "NextAttackTarget";

    /// <summary>
    /// Step 6 of an attack — <c>rr:attack-enemy-activation.step.6</c>.
    /// </summary>
    public const string EndAttack = "EndAttack";

    /// <summary>
    /// A hero or ally attacking an enemy —
    /// <c>rr:attack-player-ability-type</c>.
    /// </summary>
    /// <remarks>
    /// A step and not a call, for the reason every other attack is: `.step.7`
    /// and `.step.8` put abilities around it — "after [character] attacks [and
    /// damages/defeats] [an enemy/a minion]", "after [character] is attacked" —
    /// and an ability may ask the player something. A basic attack that
    /// resolved inline had nowhere to open those windows.
    /// </remarks>
    public const string CharacterAttacks = "CharacterAttacks";

    /// <summary>
    /// A hero or ally thwarting a scheme — <c>rr:thwart.1</c>.
    /// </summary>
    /// <remarks>
    /// A step for the reason <see cref="CharacterAttacks"/> is one, arrived at
    /// from the other end. <c>rr:thwart</c> lists no steps of its own, but
    /// <c>rr:consequential-damage.1</c> deals an ally's consequential damage
    /// "after resolving abilities that are triggered by the ally attacking
    /// <b>or thwarting</b>" — so the rules take it for granted that a thwart
    /// has abilities triggered by it, and abilities triggered by something are
    /// abilities in its windows.
    /// </remarks>
    public const string CharacterThwarts = "CharacterThwarts";

    /// <summary>
    /// An ally's consequential damage —
    /// <c>rr:attack-player-ability-type.step.9</c>.
    /// </summary>
    /// <remarks>
    /// Last of the steps an attack's resolution runs, after the forced and
    /// non-forced abilities of <c>.step.7</c> and <c>.step.8</c> —
    /// <c>rr:consequential-damage.1</c> says the same thing the other way
    /// round, "after resolving abilities that are triggered by the ally
    /// attacking or thwarting". A step of its own because those abilities are
    /// windows and a window can ask.
    /// </remarks>
    public const string AllyConsequentialDamage = "AllyConsequentialDamage";

    /// <summary>
    /// An ally's consequential damage after a thwart —
    /// <c>rr:consequential-damage.1</c>.
    /// </summary>
    /// <remarks>
    /// The same rule as <see cref="AllyConsequentialDamage"/> and a separate
    /// step only because the two differ in what they record: an ally that
    /// thwarted takes its damage under the verb "Thwart", and the event stream
    /// is how a reader tells the two apart. Which <i>field</i> was used is a
    /// third question and not this one — <c>rr:assault.2</c> makes a thwart
    /// against an assaulted scheme take the damage printed under ATK.
    /// </remarks>
    public const string AllyThwartConsequentialDamage = "AllyThwartConsequentialDamage";

    /// <summary>Step 3 — <c>rr:villain-phase.step.3</c>.</summary>
    public const string DealEncounterCards = "DealEncounterCards";

    /// <summary>
    /// Step 4 — <c>rr:villain-phase.step.4</c>. A heading, and a loop.
    /// </summary>
    /// <remarks>
    /// "Each player repeats this process in player order, <b>until no dealt
    /// encounter cards remain</b>." So this step does not hand out a list of
    /// reveals; it finds the next card, schedules that one reveal, and puts
    /// itself back on the agenda. A card revealed here that deals another card
    /// has that card revealed here too — <c>rr:deal-deal-an-encounter-card.1</c>.
    /// </remarks>
    public const string RevealEncounterCards = "RevealEncounterCards";

    /// <summary>One card being revealed — <c>rr:reveal</c>, <c>rr:villain-phase.step.4</c>.</summary>
    public const string RevealEncounterCard = "RevealEncounterCard";

    /// <summary>Discard a resolved treachery after its final nested activation.</summary>
    public const string DiscardRevealedTreachery = "DiscardRevealedTreachery";

    /// <summary>Step 5 — <c>rr:villain-phase.step.5</c>.</summary>
    /// <summary>
    /// A card ability waiting for a player to choose between its options —
    /// <c>rr:choose-option</c>.
    /// </summary>
    /// <remarks>
    /// A step rather than a call for the same reason an attack is one: the
    /// ability has to stop and ask, and an interpreter that returns a list of
    /// events has nowhere to stop. What suspends is the ability; what resumes
    /// it is the answer to this.
    /// </remarks>
    public const string ChooseOption = "ChooseOption";

    /// <summary>Resume persisted card text after its enemy activations complete.</summary>
    public const string ResumeAbility = "ResumeAbility";

    /// <summary>
    /// The first player orders the frames of an effect that resolves for each
    /// player — <c>rr:each-player.1</c>.
    /// </summary>
    public const string OrderEachPlayer = "OrderEachPlayer";

    /// <summary>One persisted player frame of an each-player card effect.</summary>
    public const string ResolveEachPlayer = "ResolveEachPlayer";

    /// <summary>
    /// A card's explicit instruction to resolve a <b>Special</b> ability —
    /// <c>rr:special</c>.
    /// </summary>
    /// <remarks>
    /// Scheduled as a plan step: resolving the Special is the work, not a new
    /// triggering condition around it. Putting it on the agenda instead of a
    /// call stack lets a choice inside the Special suspend before the next
    /// Special in the parent sequence begins.
    /// </remarks>
    public const string ResolveSpecial = "ResolveSpecial";

    /// <summary>Step 5 — <c>rr:villain-phase.step.5</c>.</summary>
    public const string PassFirstPlayerToken = "PassFirstPlayerToken";

    /// <summary>Step 6 — <c>rr:villain-phase.step.6</c>.</summary>
    public const string EndVillainPhase = "EndVillainPhase";

    /// <summary>
    /// Step 2 — <c>rr:end-of-player-phase.step.2</c>.
    /// </summary>
    /// <remarks>
    /// "Each player <b>simultaneously</b> draws up to their hand size", so one
    /// step for the table rather than one per player. Step 1 is the opposite —
    /// it is "in player order" — and lives on the turn prompt, because it is a
    /// question rather than something that happens.
    /// </remarks>
    public const string DrawToHandSize = "DrawToHandSize";

    /// <summary>
    /// Step 3 — <c>rr:end-of-player-phase.step.3</c>. Simultaneous, as step 2 is.
    /// </summary>
    public const string ReadyCards = "ReadyCards";

    /// <summary>The end of the player phase — <c>rr:end-of-player-phase</c>.</summary>
    public const string EndPlayerPhase = "EndPlayerPhase";

    /// <summary>"Whenever an enemy attacks or schemes" — <c>rr:activation</c>.</summary>
    public const string EnemyActivates = "WhenEnemyActivates";

    /// <summary>
    /// An attack begins, whoever its actor and target are.
    /// </summary>
    /// <remarks>
    /// Enemy attacks use the timing in <c>rr:attack-enemy-activation.5</c>.
    /// Character attacks use <c>rr:attack-player-ability-type.step.7</c> and
    /// <c>.step.8</c>. The occurrence's actor and target roles distinguish the
    /// printed cases without source-specific condition names.
    /// </remarks>
    public const string AttackInitiated = "WhenAttackInitiated";

    /// <summary>
    /// "When an enemy schemes" — <c>rr:scheme-enemy-activation</c>. The
    /// <i>start</i> of the activation, which is what an interrupt to it means.
    /// </summary>
    public const string EnemySchemes = "WhenEnemySchemes";

    /// <summary>
    /// "After [enemy] schemes" — the end of a scheme activation,
    /// <c>rr:activation.6</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The parallel of <see cref="AttackEnds"/>, and separate from
    /// <see cref="EnemySchemes"/> for the same reason the attack keeps its two
    /// apart: <c>rr:attack-enemy-activation.5</c> puts "when [enemy name]
    /// attacks" at the moment the attack is <i>initiated</i>, before any of its
    /// steps, and <c>.step.6.a</c> is where the abilities that ask what the
    /// attack <i>did</i> live. A scheme has the same two moments and had only
    /// one name for them.
    /// </para>
    /// <para>
    /// It matters because the threat is placed in between. Prelate Armor's
    /// "<b>Forced Response</b>: After Unus schemes, give him a tough status
    /// card" resolved at the start of the activation while the two steps were
    /// one call, and nothing showed it — a tough card is a tough card whichever
    /// side of the scheme it lands on. The event order is what shows it.
    /// </para>
    /// </remarks>
    public const string SchemeEnds = "WhenSchemeEnds";

    /// <summary>"When an attack ends" — <c>rr:attack-enemy-activation.step.6</c>.</summary>
    public const string AttackEnds = "WhenAttackEnds";

    /// <summary>"When a card is revealed" — <c>rr:reveal</c>.</summary>
    public const string CardRevealed = "WhenCardRevealed";

    /// <summary>
    /// Resolving setup's abilities — <c>rr:appendix-ii-setup.step.12</c>.
    /// </summary>
    /// <remarks>
    /// <b>Not a triggering condition</b>, and deliberately absent from
    /// <see cref="EveryCondition"/>: <c>rr:setup-triggered-ability.2</c> times a
    /// "Setup" ability to a step of setup rather than to something happening,
    /// and setup is not on the agenda. This is the label its events carry, so
    /// that a board built during setup can be told apart in the stream from one
    /// built during a round.
    /// </remarks>
    public const string Setup = "Setup";

    /// <summary>
    /// A player triggering an "Action" ability on their turn —
    /// <c>rr:player-turn.5</c>.
    /// </summary>
    /// <remarks>
    /// The player chooses it directly rather than from a timing window. Once
    /// chosen it is an agenda step, because <c>rr:ability</c> puts interrupt and
    /// response windows around the occurrence and its costs and effects must
    /// remain resumable between them.
    /// </remarks>
    public const string TurnAction = "WhenActionTriggered";

    /// <summary>
    /// Damage about to be dealt to a character —
    /// <c>rr:damage.step.1</c>.
    /// </summary>
    /// <remarks>
    /// The first of the nine steps <c>rr:damage</c> lists: "abilities that
    /// trigger <i>when [character] would deal/be dealt any amount of
    /// damage</i>". This is the "be dealt" half; the dealer's half is the same
    /// step and nothing in the pool that the engine reaches uses it yet.
    /// </remarks>
    public const string DamageWouldBeDealt = "WhenDamageWouldBeDealt";

    /// <summary>A character was dealt damage.</summary>
    public const string DamageDealt = "WhenDamageDealt";

    /// <summary>
    /// A character whose remaining hit points have reached zero is about to be
    /// defeated — <c>rr:damage.step.6</c>.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="CardDefeated"/>: <c>rr:would</c> gives this
    /// condition higher priority, and an interrupt that changes the imminent
    /// defeat prevents the later condition from occurring.
    /// </remarks>
    public const string CardWouldBeDefeated = "WhenCardWouldBeDefeated";

    /// <summary>A player card has finished entering play.</summary>
    public const string CardPlayed = "WhenCardPlayed";

    /// <summary>
    /// An event played inside another timing window. This engine step separates
    /// its nested response boundary from <see cref="CardPlayed"/>, whose
    /// ordinary non-event card also enters play at the same moment.
    /// </summary>
    public const string EventPlayed = "EventPlayed";

    /// <summary>Choose an ally to discard after exceeding the ally limit.</summary>
    public const string ChooseAllyForLimit = "ChooseAllyForLimit";

    /// <summary>Choose a restricted card to discard — <c>rr:restricted</c>.</summary>
    public const string ChooseRestrictedCard = "ChooseRestrictedCard";

    /// <summary>Choose an eligible host for a revealed attachment.</summary>
    public const string ChooseAttachmentTarget = "ChooseAttachmentTarget";

    /// <summary>Assign the calculated damage from one indirect enemy attack.</summary>
    public const string AssignIndirectAttackDamage = "AssignIndirectAttackDamage";

    /// <summary>Open one recipient's damage-step interrupt window.</summary>
    public const string PrepareIndirectAttackDamage = "PrepareIndirectAttackDamage";

    /// <summary>Place every assigned share after recipient windows finish.</summary>
    public const string ApplyIndirectAttackDamage = "ApplyIndirectAttackDamage";

    /// <summary>Finish retaliation after indirect damage suspended on defeat.</summary>
    public const string FinishIndirectAttackDamage = "FinishIndirectAttackDamage";

    /// <summary>Apply icons and discard after one boost ability finishes.</summary>
    public const string FinishBoostCard = "FinishBoostCard";

    /// <summary>Resolve damage step 6 through a player decision.</summary>
    public const string ChooseWouldBeDefeated = "ChooseWouldBeDefeated";

    /// <summary>Continue damage step 6 after a selected ability fully resolves.</summary>
    public const string ResumeWouldBeDefeated = "ResumeWouldBeDefeated";

    /// <summary>Resolve damage step 7 through a player decision.</summary>
    public const string ChooseCardDefeatedAbility = "ChooseCardDefeatedAbility";

    /// <summary>Continue damage step 7 after a selected ability fully resolves.</summary>
    public const string ResumeCardDefeatedAbility = "ResumeCardDefeatedAbility";

    /// <summary>Order simultaneous keyword and printed reveal abilities.</summary>
    public const string ChooseRevealAbility = "ChooseRevealAbility";

    /// <summary>Continue reveal step 3 after one ability fully resolves.</summary>
    public const string ResumeRevealAbility = "ResumeRevealAbility";

    /// <summary>Finish an attack after a suspended damage procedure resolves.</summary>
    public const string FinishAttackDamage = "FinishAttackDamage";

    /// <summary>Order simultaneous post-reveal keyword responses.</summary>
    public const string ChoosePostRevealAbility = "ChoosePostRevealAbility";

    /// <summary>Apply an ally's entry state after an ally-limit choice.</summary>
    public const string FinalizeAllyEntry = "FinalizeAllyEntry";

    /// <summary>A card finished entering play, however it got there.</summary>
    public const string CardEntersPlay = "WhenCardEntersPlay";

    /// <summary>An identity finished changing form.</summary>
    public const string FormChanged = "WhenFormChanged";

    /// <summary>A card being defeated — <c>rr:defeat</c>.</summary>
    /// <remarks>
    /// <para>
    /// A condition rather than a step, and <c>rr:triggering-condition.2</c> is
    /// why: "a single attack causing a character to both take damage and be
    /// defeated" gets "a single interrupt window and a single response window",
    /// so the defeat joins the occurrence that caused it instead of being
    /// scheduled beside it. <c>Occurrence.Also</c> is where it joins.
    /// </para>
    /// <para>
    /// <b>Reachable in a response window, and not in an interrupt one.</b> Not
    /// a gap: <c>rr:damage.step.7</c> puts "abilities that trigger <i>when
    /// [character] is defeated…</i>" after <c>.step.5</c> has placed the
    /// damage, which is past the window. So the interrupt tier is reached from
    /// inside the damage — <c>ICardDamageAbilities.WhenCardDefeated</c> — and every
    /// ability there is forced, with nothing to offer and nothing to decline.
    /// The response tier is <c>.step.9</c>, which is the window.
    /// </para>
    /// <para>
    /// <c>rr:damage.step.6</c> is a different condition:
    /// <see cref="CardWouldBeDefeated"/>. It happens after damage is placed
    /// and before this condition, so a replacement there can prevent this one.
    /// </para>
    /// </remarks>
    public const string CardDefeated = "WhenCardDefeated";

    /// <summary>
    /// A character thwarting a scheme — <c>rr:thwart.1</c>.
    /// </summary>
    /// <remarks>
    /// <see cref="Occurrence.Subject"/> is the scheme, so a card on it answers
    /// with <c>this</c>, and <see cref="Occurrence.Player"/> is the seat
    /// thwarting, so a player card answers with <c>you</c>.
    /// </remarks>
    public const string CharacterThwartsScheme = "WhenCharacterThwarts";

    /// <summary>Threat is imminent, before prevention or replacement.</summary>
    public const string ThreatWouldBePlaced = "WhenThreatWouldBePlaced";

    /// <summary>A positive amount of threat was placed.</summary>
    public const string ThreatPlaced = "WhenThreatPlaced";

    /// <summary>Step one of the villain phase finished resolving.</summary>
    public const string VillainPhaseStepOneEnds = "WhenVillainPhaseStepOneEnds";


    /// <summary>The triggering conditions a step creates.</summary>
    public static IReadOnlyList<string> ConditionsOf(string what) =>
        StepConditions.ConditionsOf(what);

    /// <summary>Every triggering condition any known step produces.</summary>
    public static IReadOnlySet<string> EveryCondition => StepConditions.EveryCondition;
}
