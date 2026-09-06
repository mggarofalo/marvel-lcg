using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// One read-only expression evaluation and its ordered information observations.
// Recursion shares captured inputs and the concrete selector collaborator; no
// expression can resolve an effect, resume an agenda frame or alter payment.
internal sealed class AbilityExpressionEvaluation(
    AbilityExpressionContext context, AbilitySelectorEvaluation selectors,
    IResourceCardAbilities? resourceAbilities = null)
{
    internal AbilityQueryResult<T> Result<T>(T value) => selectors.Result(value);

    // rr:player-elimination.6: effects referring to players ignore eliminated
    // players, “except for the per player icon.” Its multiplier is World.Players.
    // Result bindings read what an earlier effect actually did; an unwritten
    // result is zero, including when no damage was healed this way.
    internal long Amount(AbilityNumber number) => number switch
    {
        AbilityNumber.Constant constant => constant.Value,
        AbilityNumber.PerPlayer perPlayer => perPlayer.Value * context.World.Players,
        AbilityNumber.Result result => context.Results.GetValueOrDefault(result.Name),
        AbilityNumber.Sum sum => sum.Operands.Sum(operand => Amount(operand)),
        AbilityNumber.Product product => product.Operands.Aggregate(1L, (value, operand) => value * Amount(operand)),
        AbilityNumber.Minimum minimum => minimum.Operands.Min(operand => Amount(operand)),
        AbilityNumber.CardValue value => CardNumber(value),
        AbilityNumber.Counters counters => selectors.Find(counters.Card) is { } holder ? CounterCount(holder, counters.Counter) : 0,
        AbilityNumber.Modified modified => selectors.Find(modified.Card) is { } holder
            ? StateFields.Modified(context.World, holder, modified.Field, context.World.Facts, context.World.Players) : 0,
        AbilityNumber.Count count => selectors.Every(count.Cards).Count,
        AbilityNumber.Conditional conditional => Amount(Test(conditional.Test) ? conditional.Then : conditional.Else),
        AbilityNumber.PrintedResourcesDiscarded resource => Resources.PrintedCount(context.Discarded, resource.Resource, context.World.Facts),
        AbilityNumber.DiscardedWithResource resource => context.Discarded.Count(card =>
            Resources.GeneratedBy(card.FaceId, context.World.Facts).Contains(resource.Resource)),
        AbilityNumber.ResolutionValue value => value.Kind switch
        {
            AbilityResolutionNumber.PowerAmount => context.PowerAmount,
            AbilityResolutionNumber.PrintedBoostIconsDiscarded => context.Discarded.Sum(card =>
                context.World.Facts.PrintedValue(card.FaceId, "Boost", context.World.Players)),
            AbilityResolutionNumber.TopEncounterDiscardBoostPlusOne => 1 + (context.Discarded.LastOrDefault() is { } card
                ? context.World.Facts.PrintedValue(card.FaceId, "Boost", context.World.Players) : 0),
            _ => throw new InvalidOperationException("Unknown compiled resolution number"),
        },
        _ => throw new InvalidOperationException("Unknown compiled numeric expression"),
    };

    private long CardNumber(AbilityNumber.CardValue value)
    {
        if (selectors.Find(value.Card) is not { } card) return 0;
        return value.Property switch
        {
            AbilityCardNumberProperty.Threat => card.Tokens.GetValueOrDefault("k_threat"),
            AbilityCardNumberProperty.Damage => card.Damage,
            AbilityCardNumberProperty.RemainingHealth => Math.Max(0, Damage.Health(context.World, context.World.Facts, card) - card.Damage),
            AbilityCardNumberProperty.StartingHealth => StartingHealth(card),
            _ => throw new InvalidOperationException("Unknown compiled card number"),
        };
    }

    internal bool Test(AbilityCondition condition) => condition switch
    {
        AbilityCondition.All all => all.Operands.All(operand => Test(operand)),
        AbilityCondition.Any any => any.Operands.Any(operand => Test(operand)),
        AbilityCondition.Negated negated => !Test(negated.Operand),
        AbilityCondition.Flag flag => TestFact(flag.Kind),
        AbilityCondition.PaidWithResource resource => PaidWith(resource.Resource.ToString()),
        AbilityCondition.DiscardedWithResource resource => context.Discarded.Any(card =>
            Resources.GeneratedBy(card.FaceId, context.World.Facts).Contains(resource.Resource)),
        AbilityCondition.CausedThreat threat => context.Occurrence.Threat?.Cause == threat.Cause,
        AbilityCondition.Exists exists => selectors.Every(exists.Cards).Count > 0,
        AbilityCondition.LegalPractice practice => context.World.Seats[context.Player].Hand.Cards
            .Any(card => card.ObjectId != context.Source.ObjectId)
            && selectors.Every(practice.Schemes).Any(card => card.Tokens.GetValueOrDefault("k_threat") > 0),
        AbilityCondition.AutomaticThwart thwart => selectors.Find(thwart.Scheme) is { } scheme
            && BasicPowers.CanAutomaticallyThwart(context.World, context.World.Facts, context.Player, scheme),
        AbilityCondition.TitleInPlay title => context.World.Areas.Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards).Any(card => string.Equals(context.World.Facts.Title(card.FaceId), title.Title, StringComparison.Ordinal)),
        AbilityCondition.AtLeast comparison => Amount(comparison.Value) >= Amount(comparison.Count),
        AbilityCondition.InForm form => Forms.In(context.World, context.World.Seats[Seat(form.Player)], context.World.Facts, form.Form),
        AbilityCondition.ActivationIs activation => context.World.Activation is { } current && current.Attacking == activation.Attack,
        AbilityCondition.CardText text => TestCardText(text),
        AbilityCondition.IsKind kind => selectors.Find(kind.Card) is { } card && context.World.Facts.Kind(card.FaceId) == kind.Kind,
        AbilityCondition.WasDefeated defeated => selectors.Find(defeated.Card) is { } card
            && context.Occurrence.Defeats.Any(defeat => defeat.Card == card.ObjectId),
        AbilityCondition.IsYourIdentity identity => selectors.Find(identity.Card)?.ObjectId == context.World.Seats[AbilityCardQueries.Resolver(context.Bindings)].IdentityCard.ObjectId,
        _ => throw new InvalidOperationException("Unknown compiled condition"),
    };

    private bool TestCardText(AbilityCondition.CardText text)
    {
        if (selectors.Find(text.Card) is not { } card) return false;
        return text.Property switch
        {
            AbilityCardTextProperty.Status => Statuses.Has(context.World, card, text.Text),
            AbilityCardTextProperty.Trait => Rules.State.Traits.Has(context.World, card, text.Text, context.World.Facts),
            AbilityCardTextProperty.Set => string.Equals(context.World.Facts.EncounterSet(card.FaceId), text.Text, StringComparison.Ordinal),
            AbilityCardTextProperty.Title => string.Equals(context.World.Facts.Title(card.FaceId), text.Text, StringComparison.Ordinal),
            _ => throw new InvalidOperationException("Unknown compiled card text property"),
        };
    }

    private bool TestFact(AbilityConditionFact fact) => fact switch
    {
        AbilityConditionFact.FinalStep => context.FinalStep,
        AbilityConditionFact.CanMakeTheCall => CanMakeTheCall(),
        AbilityConditionFact.AttackDamaged => context.World.FinishedAttack is { Damaged: true } attack
            && attack.Enemy == context.Occurrence.Actor && attack.Target == context.Occurrence.Target,
        AbilityConditionFact.InExpertMode => context.World.Expert,
        AbilityConditionFact.DefeatedByYou => context.Occurrence.Defeat is { By: >= 0 } defeat && defeat.By == AbilityCardQueries.Resolver(context.Bindings),
        AbilityConditionFact.HeroDefended => context.World.FinishedAttack is { } attack
            && attack.Defender == context.World.Seats[AbilityCardQueries.Resolver(context.Bindings)].IdentityCard.ObjectId,
        AbilityConditionFact.UndefendedAttack => context.World.Attack is { IsDefended: false },
        AbilityConditionFact.DefeatedByConsequentialDamage => context.Occurrence.Defeat is { } defeat
            && string.Equals(defeat.How, "Consequential_Damage", StringComparison.Ordinal),
        _ => throw new InvalidOperationException("Unknown compiled condition fact"),
    };

    internal int Seat(AbilityPlayer player) => player switch
    {
        AbilityPlayer.TriggerPlayer => context.Occurrence.Player,
        AbilityPlayer.You => AbilityCardQueries.Resolver(context.Bindings),
        AbilityPlayer.Controller => context.ProjectedPlayAreaPlayer ?? AbilityCardQueries.ControllerOf(context.World, context.Source),
        AbilityPlayer.ChosenPlayer => AbilityCardQueries.ChosenPlayer(context.Bindings).Owner,
        AbilityPlayer.EngagedPlayer => context.ProjectedPlayAreaPlayer ?? (context.Source.Area.PlayArea.Player >= 0
            ? context.Source.Area.PlayArea.Player
            : throw new RulesNotImplementedException($"'{context.Source.FaceId}' asks for its engaged player outside a player's engaged area")),
        AbilityPlayer.FirstPlayer => context.World.FirstPlayer,
        _ => throw new InvalidOperationException("Unknown compiled player relation"),
    };

    private long StartingHealth(Card identity)
    {
        if (FacedownDrones.Kind(identity, context.World.Facts)
            is not (CardKind.Hero or CardKind.AlterEgo))
        {
            throw new RulesNotImplementedException(
                $"'{context.Source.FaceId}' asks for starting hit points of "
                + $"non-identity card {identity.ObjectId}");
        }

        return FacedownDrones.BaseValue(
            identity, context.World.Facts, "HP", context.World.Players);
    }

    // rr:all-purpose-counter.1-.2: read every typed pool for all-purpose counters.
    internal static long CounterCount(Card card, string type) =>
        string.Equals(type, "allPurpose", StringComparison.Ordinal)
            ? card.Tokens
                .Where(pair => pair.Key.StartsWith("c_", StringComparison.Ordinal))
                .Sum(pair => pair.Value)
            : card.Tokens.GetValueOrDefault("c_" + type);

    private bool PaidWith(string resource) =>
        context.Payment.Contains(resource[0])
        || context.World.Effects.Active().Any(effect =>
            effect.Card == context.Source.ObjectId
            && string.Equals(effect.Kind, "paid:" + resource, StringComparison.Ordinal));

    // Player order and then pile order match the rules query; absent piles are empty.
    internal static IReadOnlyList<Card> AlliesInPlayerDiscards(World world) =>
    [
        .. world.PlayerOrder.SelectMany(player => world.Areas.FirstOrDefault(area =>
                area.Type == DeckType.DiscardPile && area.PlayArea == PlayArea.Of(player)
                && area.Host == -1)?.Cards ?? [])
            .Where(card => world.Facts.Kind(card.FaceId) == CardKind.Ally),
    ];

    private bool CanMakeTheCall()
    {
        var resources = resourceAbilities
            ?? throw new InvalidOperationException(
                "canMakeTheCall requires the initiating resource capability");
        return AlliesInPlayerDiscards(context.World).Any(ally => Resources.Pays(
            string.Concat(MakeTheCallSources(
                    context.World, context.Player, context.Source, ally,
                    resources)
                .Select(source => source.Generates)),
            Resources.Cost(ally.FaceId, context.World.Facts, context.World.Players) ?? 0,
            Resources.Required(context.World, ally, context.World.Facts)));
    }

    internal static IReadOnlyList<ResourceSource> MakeTheCallSources(
        World world, int player, Card source, Card ally,
        IResourceCardAbilities resourceAbilities) =>
    [
        .. CardPlay.Generators(
                world, world.Facts, world.Seats[player], resourceAbilities, payingFor: ally)
            .Where(generator => generator.Effect != source.ObjectId),
    ];
}
