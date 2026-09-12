using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityPaymentRules;
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

/// <summary>
/// Runs authored card abilities. The one way a card's text enters the engine.
/// </summary>
/// <remarks>
/// <para>
/// Cards compose the supported operations as inert JSON. Construction validates
/// that data and lowers it to an immutable program; gameplay reads the checked
/// instructions rather than the supplied syntax maps.
/// </para>
/// <para>
/// Adding an operation requires engine behavior and tests. Authoring another
/// card combines those operations without introducing a card-specific class.
/// Unknown data fails validation; an unsupported rule situation raises during
/// resolution rather than inventing an outcome.
/// </para>
/// <para>
/// See <c>docs/card-dsl.md</c> for the language and <c>docs/timing.md</c> for
/// scheduling and continuation contracts.
/// </para>
/// </remarks>
internal sealed class AbilityResolutionExecution
{
    internal readonly AbilityProgram program;
    internal readonly AbilityOfferQueries offerQueries;
    internal readonly AbilityGameRuntimes runtimes;
    internal readonly IEncounterCardAbilities encounterAbilities;
    internal readonly ICardPlayAbilities cardPlayAbilities;
    internal readonly ICardReadinessAbilities readinessAbilities;
    internal readonly IResourceCardAbilities resourceAbilities;
    internal readonly IThreatCardAbilities threatAbilities;

    internal AbilityResolutionExecution(
        AbilityProgram program,
        AbilityGameRuntimes runtimes,
        IEncounterCardAbilities encounterAbilities,
        ICardPlayAbilities cardPlayAbilities,
        ICardReadinessAbilities readinessAbilities,
        IResourceCardAbilities resourceAbilities,
        IThreatCardAbilities threatAbilities,
        AbilityOfferQueries offerQueries)
    {
        ArgumentNullException.ThrowIfNull(program);
        this.program = program;
        this.runtimes = runtimes;
        this.encounterAbilities = encounterAbilities;
        this.cardPlayAbilities = cardPlayAbilities;
        this.readinessAbilities = readinessAbilities;
        this.resourceAbilities = resourceAbilities;
        this.threatAbilities = threatAbilities;
        this.offerQueries = offerQueries;
    }

    /// <summary>The verb an option carries on the wire.</summary>
    public const string ChooseVerb = AbilityStructuralExecution.ChooseVerb;

    internal static readonly string[] Branches = ["then", "else"];

    internal static readonly DeckType[] Owned = [DeckType.UpgradesArea, DeckType.SupportsArea];

    // A facedown Ultron Drone retains the underlying player-card face id for
    // the state digest, but `rr:in-play-and-out-of-play.5` and `.13` make that
    // facedown card text inactive. Every authored-ability entry point goes
    // through this boundary so no trigger, action, constant, boost, or query
    // can accidentally execute the hidden card.
    internal ImmutableArray<CompiledCardAbility> On(Card card) =>
        FacedownDrones.Is(card) ? [] : program.On(card.FaceId);

}
