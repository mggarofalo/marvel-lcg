using Marvel.Cards.Dsl;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerStateProjection;

namespace Marvel.Cards.Run;

/// <summary>Proves health dependencies unaffected by an opaque self-recovery.</summary>
internal sealed class AbilityRecoveryDependencies(
    AbilityAdmissionScope cast, AbilityPowerState state, Card subject)
{
    internal bool Preserves(Card returned)
    {
        var leaving = AbilityPowerHealthTrace.PowerLeavingTree(subject, cast).ToHashSet();
        if (leaving.Contains(returned.ObjectId)) return false;
        return ConstantsPreserved(leaving) && RegisteredHealthPreserved(leaving);
    }

    private bool ConstantsPreserved(HashSet<int> leaving) =>
        cast.World.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type))
            .All(card => AbilityProgramQueries.ActiveOn(cast.World, cast.Context.Program, card)
                .Where(ability => ability.Trigger.Timing == AbilityType.Constant)
                .All(ability => Supported(ability.Effect) && HealthPreserved(card, ability.Effect, leaving)));

    private bool HealthPreserved(Card card, AbilityEffect effect, HashSet<int> leaving)
    {
        var villain = cast.World.Cards[state.CurrentVillain];
        var healed = new Dictionary<int, long>(state.CardDamage) { [subject.ObjectId] = 0 };
        var absent = new HashSet<int>(state.Discarded);
        absent.UnionWith(leaving);
        var constantCast = TraceConstantCast(cast, card, state.Engagement);
        foreach (var character in cast.World.Cards.Where(character =>
            DeckTypes.IsInPlay(character.Area.Type)
            && CardKinds.IsCharacter(EffectiveCards.Kind(character, cast.World.Facts))
            && !leaving.Contains(character.ObjectId)))
        {
            if (leaving.Contains(card.ObjectId)
                && ContainsFieldGrant(effect, card, character, "health", constantCast)) return false;
            if (HealthCanDiffer(effect, card, character, constantCast, healed, state.Discarded)
                || HealthCanDiffer(effect, card, character, constantCast, state.CardDamage, absent))
                return false;
        }
        return true;

        bool HealthCanDiffer(AbilityEffect effect, Card source, Card character,
            AbilityAdmissionScope current, IReadOnlyDictionary<int, long> damage, HashSet<int> discarded) =>
            ConstantFieldCanDiffer(effect, source, character, "health", villain, villain,
                current, discarded, state.SchemeThreat, damage, state.Modifiers,
                state.Traits, state.StatusChanges, state.Engagement,
                state.FormsMayChange, state.FirstPlayer, 16);
    }

    // Fixed lasting health has no dependency on recovery. A trait-scoped
    // recipient set or a disappearing registered constant needs its own trace.
    private bool RegisteredHealthPreserved(HashSet<int> leaving) =>
        cast.World.Effects.Registered.Where(effect => effect.Kind == "health")
            .All(effect => effect.Scope.Length == 0
                && (effect.Source != EffectSource.ConstantAbility
                    || effect.Card is not { } source || !leaving.Contains(source)));

    private static bool Supported(AbilityEffect effect) => effect switch
    {
        AbilityEffect.GrantField { Until: null, Field: "health" } health =>
            health.Amount is AbilityNumber.Constant && StableRecipient(health.Cards),
        AbilityEffect.GrantField { Until: null } => true,
        AbilityEffect.GrantTrait { Until: null } => true,
        AbilityEffect.Sequence sequence => sequence.Effects.All(Supported),
        AbilityEffect.Conditional conditional => (conditional.Then is null || Supported(conditional.Then))
            && (conditional.Else is null || Supported(conditional.Else)),
        _ => false,
    };

    private static bool StableRecipient(AbilityCardSelection selection) => selection is
        AbilityCardSelection.Titled or AbilityCardSelection.Bound
        { Binding: AbilityCardBinding.This or AbilityCardBinding.You or AbilityCardBinding.YourHero
            or AbilityCardBinding.YourAlterEgo or AbilityCardBinding.AttachedTo };
}
