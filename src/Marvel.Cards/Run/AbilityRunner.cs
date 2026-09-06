using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

#pragma warning disable CS1591 // Compatibility members inherit their port contracts.

/// <summary>Public compatibility facade for the authored card-ability program.</summary>
/// <remarks>
/// Construction lowers immutable authored data and composes focused readers,
/// resource commitment, and live ability resolution. The facade supplies the
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
    private readonly AbilityGameRuntimes runtimes;
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
        runtimes = new AbilityGameRuntimes();
        resolution = new AbilityResolutionExecution(
            program, runtimes, this, this, this, this, offerQueries);
    }

    public const string ChooseVerb = AbilityStructuralExecution.ChooseVerb;
    public IReadOnlySet<string> Authored => program.Authored;
    public CardCounterPool? CounterPool(World world, Card card) => AbilityProgramQueries.CounterPool(world, program, card);
    public IReadOnlyList<ContinuousEffect> Constant(World world, Card card) => constantQueries.Constant(world, card);
    public string ResourcesGeneratedBy(World world, Card source, Card? payingFor) => AbilityProgramQueries.ResourcesGeneratedBy(world, program, source, payingFor);
    public DefenderChoice Defenders(World world, EnemyAttack attack, IReadOnlyList<Card> candidates) => AbilityProgramQueries.Defenders(world, program, attack, candidates);
    public bool CanRemoveThreat(World world, Card scheme, int ignoredSource = -1) => AbilityProgramQueries.CanRemoveThreat(world, program, scheme, ignoredSource);
    public IReadOnlyList<PendingAbility> Waiting(World world, Occurrence occurrence, WindowKind window) => resolution.Waiting(world, occurrence, window);
    public Affordance Describe(World world, PendingAbility ability) => resolution.Describe(world, ability);
    public IReadOnlyList<ResourceSource> ResourceAbilities(World world, int player) => offerQueries.ResourceAbilities(world, player);
    public string ResourceGeneratorName(World world, int player, int card) => offerQueries.ResourceGeneratorName(world, player, card);
    public IReadOnlyList<ResourceSource> PrintedResourceAbilities(World world, int player) => offerQueries.PrintedResourceAbilities(world, player);
    public string UseResource(World world, int player, int card, List<GameEvent> events) => resourceExecution.UseResource(world, player, card, events, this, this);
    public bool CanTakeDamage(World world, Card target, Card source) => AbilityProgramQueries.CanTakeDamage(world, program, target, source);
    public DamageProjection PreviewDamageReplacement(World world, Card target, Card source, long amount) => damageProjection.PreviewDamageReplacement(world, target, source, amount);
    public DefeatProjection? PreviewDefeatReplacement(World world, Card target, long maximumHealth) => damageProjection.PreviewDefeatReplacement(world, target, maximumHealth);
    public bool CanReady(World world, Card target, Card source) => AbilityProgramQueries.CanReady(world, program, target);
    public int? AttachesTo(World world, Card card) => AbilityProgramQueries.AttachesTo(world, program, card);
    public int? SetupController(World world, Card card) => AbilityProgramQueries.SetupController(world, program, card);
    public void ValidateForPlay(World world) => AbilityProgramQueries.ValidateForPlay(world, program);
    public IReadOnlyList<int>? AttachmentTargets(World world, Card card) => AbilityProgramQueries.AttachmentTargets(world, program, card);
    public IReadOnlyList<Card> PlayerSetupCards(World world, int player) => AbilityProgramQueries.PlayerSetupCards(world, program, player);
    public IReadOnlyList<PendingAbility> Actions(World world, int player) => offerQueries.Actions(world, player);

    public IReadOnlyList<GameEvent> EntersPlay(World world, Card card) => resolution.EntersPlay(world, card);
    public IReadOnlyList<GameEvent> ActivationCompleted(World world, EnemyActivation result) => resolution.ActivationCompleted(world, result);
    public IReadOnlyList<GameEvent> ResumeAbility(World world, PhaseStep continuation) => resolution.ResumeAbility(world, continuation);
    public void ResolveCardAttack(World world, CharacterAttack attack, Occurrence occurrence, List<GameEvent> events) => resolution.ResolveCardAttack(world, attack, occurrence, events);
    public void ResolveCardThwart(World world, CharacterThwart thwart, Occurrence occurrence, List<GameEvent> events) => resolution.ResolveCardThwart(world, thwart, occurrence, events);
    public IReadOnlyList<GameEvent> Resolve(World world, Occurrence occurrence, PendingAbility ability, IReadOnlyList<int> paying, IReadOnlyList<int> chosen) => resolution.Resolve(world, occurrence, ability, paying, chosen);
    public IReadOnlyList<GameEvent> Resolve(World world, Occurrence occurrence, PendingAbility ability, IReadOnlyList<int> paying, IReadOnlyList<int> chosen, IReadOnlyDictionary<string, long>? values = null, IReadOnlyList<ResourceAllocation>? allocations = null) => resolution.Resolve(world, occurrence, ability, paying, chosen, values, allocations);
    public IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player) => resolution.WhenRevealed(world, card, player);
    public IReadOnlyList<PendingAbility> WhenRevealedAbilities(World world, Card card, int player) => resolution.WhenRevealedAbilities(world, card, player);
    public IReadOnlyList<GameEvent> WhenRevealed(World world, Card card, int player, Occurrence occurrence) => resolution.WhenRevealed(world, card, player, occurrence);
    public bool CancelWhenRevealed(World world, Card card, int player, Occurrence occurrence) => resolution.CancelWhenRevealed(world, card, player, occurrence);
    public IReadOnlyList<GameEvent> Boost(World world, Card card, int player) => resolution.Boost(world, card, player);
    public IReadOnlyList<GameEvent> ResolveSpecial(World world, Card card, int player, bool finalStep) => resolution.ResolveSpecial(world, card, player, finalStep);
    public long WouldBeDealt(World world, Card target, Card source, long amount, List<GameEvent> events) => resolution.WouldBeDealt(world, target, source, amount, events);
    public long WouldTake(World world, Card target, Card source, long amount, List<GameEvent> events) => AbilityResolutionExecution.WouldTake(world, target, source, amount, events);
    public void DamagePreventedByTough(World world, Card target, Card source, List<GameEvent> events) => AbilityResolutionExecution.DamagePreventedByTough(world, target, source, events);
    public void WouldBeDefeated(World world, Card target, List<GameEvent> events) => resolution.WouldBeDefeated(world, target, events);
    public bool WouldBeDefeated(World world, Card target, Card source, string trigger, string verb, int by, List<GameEvent> events, Occurrence? recordDefeatOn = null) => resolution.WouldBeDefeated(world, target, source, trigger, verb, by, events, recordDefeatOn);
    public IReadOnlyList<GameEvent> Setup(World world, Card card) => resolution.Setup(world, card);
    public IReadOnlyList<GameEvent> ResolveEachPlayer(World world, Card source, int player, int stoppedAt, AbilityType? tier, bool finalStep, bool finalPlayer) => resolution.ResolveEachPlayer(world, source, player, stoppedAt, tier, finalStep, finalPlayer);
    public IReadOnlyList<GameEvent> WhenCardDefeated(World world, Card card, Defeated defeated) => resolution.WhenCardDefeated(world, card, defeated);
    public bool WhenCardDefeated(World world, Card card, Defeated defeated, string trigger, List<GameEvent> events) => resolution.WhenCardDefeated(world, card, defeated, trigger, events);
    public IReadOnlyList<GameEvent> Act(World world, PendingAbility ability, IReadOnlyList<int> paying, IReadOnlyList<int> chosen, IReadOnlyDictionary<string, long>? values = null, IReadOnlyList<ResourceAllocation>? allocations = null) => resolution.Act(world, ability, paying, chosen, values, allocations);
    public IReadOnlyList<GameEvent> Act(World world, PendingAbility ability, IReadOnlyList<int> paying, IReadOnlyList<int> chosen, Occurrence occurrence, IReadOnlyDictionary<string, long>? values = null, IReadOnlyList<ResourceAllocation>? allocations = null) => resolution.Act(world, ability, paying, chosen, occurrence, values, allocations);
    public Prompt? Choosing(World world, Card source, int player, int stoppedAt, AbilityType? tier = null) => resolution.Choosing(world, source, player, stoppedAt, tier);
    public Prompt? Choosing(World world, Card source, int player, int stoppedAt, AbilityType? tier, bool finalStep) => resolution.Choosing(world, source, player, stoppedAt, tier, finalStep);
    public Prompt? Choosing(World world, Card source, int player, int stoppedAt, AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer) => resolution.Choosing(world, source, player, stoppedAt, tier, finalStep, eachPlayerFrame, finalPlayer);
    public IReadOnlyList<GameEvent> Chose(World world, Card source, int player, int stoppedAt, Decision input, AbilityType? tier = null) => resolution.Chose(world, source, player, stoppedAt, input, tier);
    public IReadOnlyList<GameEvent> Chose(World world, Card source, int player, int stoppedAt, Decision input, AbilityType? tier, bool finalStep) => resolution.Chose(world, source, player, stoppedAt, input, tier, finalStep);
    public IReadOnlyList<GameEvent> Chose(World world, Card source, int player, int stoppedAt, Decision input, AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer) => resolution.Chose(world, source, player, stoppedAt, input, tier, finalStep, eachPlayerFrame, finalPlayer);
    public IReadOnlyList<GameEvent> Chose(World world, Card source, int player, int stoppedAt, Decision input, AbilityType? tier, bool finalStep, bool eachPlayerFrame, bool finalPlayer, string trigger) => resolution.Chose(world, source, player, stoppedAt, input, tier, finalStep, eachPlayerFrame, finalPlayer, trigger);
}

#pragma warning restore CS1591
