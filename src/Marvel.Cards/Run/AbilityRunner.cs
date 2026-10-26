using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

#pragma warning disable CS1591 // Engine port implementations inherit their contracts.

/// <summary>Composes immutable authored programs with engine ability ports.</summary>
/// <remarks>
/// Construction lowers immutable authored data and composes focused readers,
/// resource commitment, and live ability resolution. The runner supplies the
/// two nested-entry views required by card play and encounter reveal; it does
/// not resolve card text.
/// </remarks>
public sealed class AbilityRunner : ICardAbilities
{
    private readonly AbilityProgram program;
    private readonly AbilityConstantQueries constantQueries;
    private readonly AbilityDamageProjection damageProjection;
    private readonly AbilityOfferQueries offerQueries;
    private readonly AbilityResourceExecution resourceExecution;
    private readonly AbilityResolutionExecution resolution;

    public AbilityRunner(AbilityBook book) : this(AbilityLowering.Book(book)) { }

    public AbilityRunner(AbilityProgram program)
    {
        ArgumentNullException.ThrowIfNull(program);
        this.program = program;
        constantQueries = new AbilityConstantQueries(program);
        damageProjection = new AbilityDamageProjection(program, this);
        offerQueries = new AbilityOfferQueries(program, this);
        resourceExecution = new AbilityResourceExecution(program, offerQueries);
        resolution = new AbilityResolutionExecution(
            program, this, this, this, this, this, offerQueries);
    }

    public const string ChooseVerb = AbilityStructuralExecution.ChooseVerb;
    public IReadOnlySet<string> Authored => program.Authored;
    /// <inheritdoc/>
    public bool ObservesHeroDefense(World world) => AbilityHeroDefenseTiming.Observed(world, program);

    public bool ObservesPlayerTurnBeginning(World world) => AbilityPlayerTurnTiming.Observed(world, program);
    public bool ObservesVillainStepOneCompletion(World world) => AbilityVillainPhaseTiming.Observed(world, program);
    public bool RequiresAllyPlayHistory(World world) => AbilityAllyPlayHistory.Required(world, program);
    public long SelfPlayCostReduction(World world, Card card, int player) => AbilitySelfPlayCost.Reduction(world, program, card, player);
    public bool CanPlayFromStorage(World world, Card card, int player) => AbilityStoragePermission.CanPlay(world, program, card, player);
    public bool CanPlayFromDiscard(World world, Card card, int player) => AbilityStoragePermission.CanPlayFromDiscard(world, program, card, player);
    public CardCounterPool? CounterPool(World world, Card card) => AbilityProgramQueries.CounterPool(world, program, card);
    public IReadOnlyList<ContinuousEffect> Constant(World world, Card card) => constantQueries.Constant(world, card);
    public string ResourcesGeneratedBy(World world, Card source, Card? payingFor) => AbilityProgramQueries.ResourcesGeneratedBy(world, program, source, payingFor);
    public DefenderChoice Defenders(World world, EnemyAttack attack, IReadOnlyList<Card> candidates) => AbilityProgramQueries.Defenders(world, program, attack, candidates);
    public bool CanRemoveThreat(World world, Card scheme, int ignoredSource = -1) => AbilityProgramQueries.CanRemoveThreat(world, program, scheme, ignoredSource);
    /// <inheritdoc/>
    public bool CanThwartScheme(World world, Card scheme, int ignoredSource = -1) => AbilityProgramQueries.CanRemoveThreat(world, program, scheme, ignoredSource, byThwarting: true);
    public Card? SchemeDestination(World world, Card enemy) => AbilitySchemeDestinations.Resolve(world, program, enemy);
    public IReadOnlyList<PendingAbility> Waiting(World world, Occurrence occurrence, WindowKind window) => resolution.Waiting(world, occurrence, window);
    public Affordance Describe(World world, PendingAbility ability) => resolution.Describe(world, ability);
    public IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player) => offerQueries.ResourceAbilities(world, player);
    public string ResourceGeneratorName(World world, int player, int card) => offerQueries.ResourceGeneratorName(world, player, card);
    public IReadOnlyList<ResourceSource> PrintedResourceAbilities(World world, int player) => offerQueries.PrintedResourceAbilities(world, player);
    public ResourceExecutionResult UseResource(World world, AbilityExecution entry, int player, int card) =>
        resourceExecution.UseResource(world, entry, player, card, this, this);
    public bool CanTakeDamage(World world, Card target, Card source) => AbilityProgramQueries.CanTakeDamage(world, program, target, source);
    public DamageProjection PreviewDamageReplacement(World world, Card target, Card source, long amount) => damageProjection.PreviewDamageReplacement(world, target, source, amount);
    public DefeatProjection? PreviewDefeatReplacement(World world, Card target, long maximumHealth) => damageProjection.PreviewDefeatReplacement(world, target, maximumHealth);
    public bool CanReady(World world, Card target, Card source) => AbilityProgramQueries.CanReady(world, program, target);
    public int? AttachesTo(World world, Card card, int? player = null) => AbilityProgramQueries.AttachesTo(world, program, card, player);
    public int? SetupController(World world, Card card) => AbilityProgramQueries.SetupController(world, program, card);
    public void ValidateForPlay(World world) => AbilityProgramQueries.ValidateForPlay(world, program);
    public IReadOnlyList<int>? AttachmentTargets(World world, Card card, int? player = null) => AbilityProgramQueries.AttachmentTargets(world, program, card, player);
    public IReadOnlyList<Card> PlayerSetupCards(World world, int player) => AbilityProgramQueries.PlayerSetupCards(world, program, player);
    public IReadOnlyList<PendingAbility> Actions(World world, int player) => offerQueries.Actions(world, player);

    public AbilityExecutionResult EntersPlay(World world, AbilityExecution entry, Card card) =>
        AbilityEntryExecution.Run(world, entry, () => resolution.EntersPlay(world, card));
    public AbilityExecutionResult ActivationCompleted(World world, AbilityExecution entry, EnemyActivation result) =>
        AbilityEntryExecution.Run(world, entry, () => resolution.ActivationCompleted(world, result));
    public IReadOnlyList<GameEvent> ResumeAbility(World world, PhaseStep continuation) =>
        continuation.Operation is AbilityContinuationAgendaOperation { Continuation: ResourceAbilityContinuation }
            ? resourceExecution.Resume(world, continuation, this)
            : continuation.Operation is AbilityContinuationAgendaOperation { Continuation: DamageReplacementContinuation }
                ? AbilityDamageReplacementExecution.Resume(resolution, world, continuation)
                : resolution.ResumeAbility(world, continuation);
    public void ResolveCardAttack(World world, CharacterAttack attack, Occurrence occurrence, List<GameEvent> events) => resolution.ResolveCardAttack(world, attack, occurrence, events);
    public void ResolveCardThwart(World world, CharacterThwart thwart, Occurrence occurrence, List<GameEvent> events) => resolution.ResolveCardThwart(world, thwart, occurrence, events);
    public Func<AbilityExecution, AbilityExecutionResult> PrepareWindow(
        World world, Occurrence occurrence, PendingAbility ability, Decision answer) =>
        resolution.PrepareWindow(world, occurrence, ability, answer);

    public AbilityExecutionResult Resolve(World world, AbilityExecution invocation, Decision answer) => resolution.Resolve(world, invocation, answer);
    public IReadOnlyList<PendingAbility> WhenRevealedAbilities(World world, Card card, int player) => resolution.WhenRevealedAbilities(world, card, player);
    public void RequireWhenRevealed(string face) => resolution.RequireWhenRevealed(face);
    public bool ObservesMainSchemeCompletion(World world, Card scheme) =>
        MainSchemeCompletionAbilities.Observes(program, world, scheme);
    public AbilityExecutionResult WhenRevealed(World world, AbilityExecution entry, Card card, int player) =>
        AbilityEntryExecution.Run(world, entry, () => resolution.WhenRevealed(world, card, player, entry.Occurrence));
    public WhenRevealedCanceled? CancelWhenRevealed(World world, Card card, int player, Occurrence occurrence) => resolution.CancelWhenRevealed(world, card, player, occurrence);
    public AbilityExecutionResult Boost(World world, AbilityExecution entry, Card card, int player) =>
        AbilityEntryExecution.Run(world, entry, () => resolution.Boost(world, card, player));
    public AbilityExecutionResult ResolveSpecial(World world, AbilityExecution entry, Card card, int player, bool finalStep) =>
        AbilityEntryExecution.Run(world, entry, () => resolution.ResolveSpecial(world, card, player, finalStep));
    public AbilityExecutionResult WouldBeDealt(World world, AbilityExecution entry, DamageReplacement replacement) =>
        AbilityDamageReplacementExecution.Begin(resolution, world, entry, replacement);
    public long WouldTake(World world, Card target, Card source, long amount, List<GameEvent> events) => resolution.WouldTake(world, target, source, amount, events);
    public void DamagePreventedByTough(World world, Card target, Card source, List<GameEvent> events) => resolution.DamagePreventedByTough(world, target, source, events);
    public AbilityExecutionResult WouldBeDefeated(World world, AbilityExecution entry,
        Card target, DefeatCause cause) =>
        AbilityEntryExecution.Run(world, entry, () => resolution.WouldBeDefeated(world, target, cause, entry.Occurrence));
    public AbilityExecutionResult Setup(World world, AbilityExecution entry, Card card) =>
        AbilityEntryExecution.Run(world, entry, () => resolution.Setup(world, card));
    public IReadOnlyList<GameEvent> ResolveEachPlayer(World world, PhaseStep continuation) => new EachPlayerAbilityResolution(resolution).Resolve(world, continuation);
    public AbilityExecutionResult WhenCardsDefeated(World world, AbilityExecution entry,
        IReadOnlyList<Defeated> defeated, string trigger) =>
        AbilityEntryExecution.Run(world, entry, () => resolution.WhenCardsDefeated(world, defeated, trigger, entry.Occurrence));
    public AbilityExecutionResult WhenCardDefeated(World world, AbilityExecution entry,
        Card card, Defeated defeated, string trigger) =>
        AbilityEntryExecution.Run(world, entry, () => resolution.WhenCardDefeated(world, card, defeated, trigger, entry.Occurrence));
    public Prompt? Choosing(World world, PhaseStep continuation) =>
        continuation.Operation is AbilityContinuationAgendaOperation { Continuation: DamageReplacementContinuation }
            ? AbilityDamageReplacementExecution.Choosing(resolution, world, continuation)
            : resolution.Choosing(world, continuation);
    public Prompt? PreviewChoice(World world, Card source, int player, int position, AbilityType? tier = null, bool finalStep = false) =>
        AbilityChoicePreview.Describe(resolution, world, source, player, position, tier, finalStep);
    public AbilityExecutionResult Act(World world, AbilityExecution invocation, Decision answer) => resolution.Act(world, invocation, answer);
    public IReadOnlyList<GameEvent> Chose(World world, PhaseStep continuation, Decision input) =>
        continuation.Operation is AbilityContinuationAgendaOperation { Continuation: DamageReplacementContinuation }
            ? AbilityDamageReplacementExecution.Chose(resolution, world, continuation, input)
            : resolution.Chose(world, continuation, input);
    public PersistentAbilityDescription DescribePersistent(Card card) => AbilityPersistentQueries.Describe(program, card);
}

#pragma warning restore CS1591
