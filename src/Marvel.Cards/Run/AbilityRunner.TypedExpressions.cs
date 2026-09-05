using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static bool AmountMayChange(AbilityNumber number) => number switch
    {
        AbilityNumber.Constant or AbilityNumber.PerPlayer => false,
        AbilityNumber.ResolutionValue { Kind: AbilityResolutionNumber.PowerAmount } => false,
        AbilityNumber.Sum sum => sum.Operands.Any(AmountMayChange),
        AbilityNumber.Product product => product.Operands.Any(AmountMayChange),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(AmountMayChange),
        AbilityNumber.Result or AbilityNumber.CardValue or AbilityNumber.Counters
            or AbilityNumber.Modified or AbilityNumber.Count or AbilityNumber.Conditional
            or AbilityNumber.PrintedResourcesDiscarded or AbilityNumber.DiscardedWithResource
            or AbilityNumber.ResolutionValue => true,
        _ => throw new InvalidOperationException("Unknown compiled number in mutation analysis"),
    };

    private static bool ContainsPowerAmount(AbilityNumber number) => number switch
    {
        AbilityNumber.ResolutionValue value => value.Kind == AbilityResolutionNumber.PowerAmount,
        AbilityNumber.Sum sum => sum.Operands.Any(ContainsPowerAmount),
        AbilityNumber.Product product => product.Operands.Any(ContainsPowerAmount),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(ContainsPowerAmount),
        AbilityNumber.Conditional conditional => ContainsPowerAmount(conditional.Test)
            || ContainsPowerAmount(conditional.Then) || ContainsPowerAmount(conditional.Else),
        AbilityNumber.Constant or AbilityNumber.PerPlayer or AbilityNumber.Result
            or AbilityNumber.CardValue or AbilityNumber.Counters or AbilityNumber.Modified
            or AbilityNumber.Count or AbilityNumber.PrintedResourcesDiscarded
            or AbilityNumber.DiscardedWithResource => false,
        _ => throw new InvalidOperationException("Unknown compiled number in power-binding analysis"),
    };

    private static bool ContainsPowerAmount(AbilityCondition condition) => condition switch
    {
        AbilityCondition.All all => all.Operands.Any(ContainsPowerAmount),
        AbilityCondition.Any any => any.Operands.Any(ContainsPowerAmount),
        AbilityCondition.Negated negated => ContainsPowerAmount(negated.Operand),
        AbilityCondition.AtLeast comparison => ContainsPowerAmount(comparison.Value)
            || ContainsPowerAmount(comparison.Count),
        AbilityCondition.Flag or AbilityCondition.PaidWithResource or AbilityCondition.DiscardedWithResource
            or AbilityCondition.CausedThreat or AbilityCondition.Exists or AbilityCondition.LegalPractice
            or AbilityCondition.AutomaticThwart or AbilityCondition.TitleInPlay or AbilityCondition.InForm
            or AbilityCondition.ActivationIs or AbilityCondition.CardText or AbilityCondition.IsKind
            or AbilityCondition.WasDefeated or AbilityCondition.IsYourIdentity => false,
        _ => throw new InvalidOperationException("Unknown compiled condition in power-binding analysis"),
    };

    private bool WhenHolds(CardAbility ability, Cast cast) =>
        WhenHolds(compiledAbilities[ability], cast);

    private static bool WhenHolds(CompiledCardAbility ability, Cast cast) =>
        ability.When is not { } condition || Test(condition, cast);

    private static bool ContainsYouOrYour(AbilityNumber number) => number switch
    {
        AbilityNumber.Sum sum => sum.Operands.Any(ContainsYouOrYour),
        AbilityNumber.Product product => product.Operands.Any(ContainsYouOrYour),
        AbilityNumber.Minimum minimum => minimum.Operands.Any(ContainsYouOrYour),
        AbilityNumber.CardValue value => ContainsYouOrYour(value.Card),
        AbilityNumber.Counters counters => ContainsYouOrYour(counters.Card),
        AbilityNumber.Modified modified => ContainsYouOrYour(modified.Card),
        AbilityNumber.Count count => ContainsYouOrYour(count.Cards),
        AbilityNumber.Conditional conditional => ContainsYouOrYour(conditional.Test)
            || ContainsYouOrYour(conditional.Then) || ContainsYouOrYour(conditional.Else),
        AbilityNumber.Constant or AbilityNumber.PerPlayer or AbilityNumber.Result
            or AbilityNumber.PrintedResourcesDiscarded or AbilityNumber.DiscardedWithResource
            or AbilityNumber.ResolutionValue => false,
        _ => throw new InvalidOperationException("Unknown compiled number in player-binding analysis"),
    };

    private static bool ContainsYouOrYour(AbilityCondition condition) => condition switch
    {
        AbilityCondition.All all => all.Operands.Any(ContainsYouOrYour),
        AbilityCondition.Any any => any.Operands.Any(ContainsYouOrYour),
        AbilityCondition.Negated negated => ContainsYouOrYour(negated.Operand),
        AbilityCondition.Flag flag => flag.Kind is AbilityConditionFact.DefeatedByYou
            or AbilityConditionFact.HeroDefended or AbilityConditionFact.UndefendedAttack,
        AbilityCondition.Exists exists => ContainsYouOrYour(exists.Cards),
        AbilityCondition.LegalPractice practice => ContainsYouOrYour(practice.Schemes),
        AbilityCondition.AutomaticThwart thwart => ContainsYouOrYour(thwart.Scheme),
        AbilityCondition.AtLeast comparison => ContainsYouOrYour(comparison.Value) || ContainsYouOrYour(comparison.Count),
        AbilityCondition.InForm form => form.Player == AbilityPlayer.You,
        AbilityCondition.CardText text => ContainsYouOrYour(text.Card),
        AbilityCondition.IsKind kind => ContainsYouOrYour(kind.Card),
        AbilityCondition.WasDefeated defeated => ContainsYouOrYour(defeated.Card),
        AbilityCondition.IsYourIdentity => true,
        AbilityCondition.PaidWithResource or AbilityCondition.DiscardedWithResource
            or AbilityCondition.CausedThreat or AbilityCondition.TitleInPlay or AbilityCondition.ActivationIs => false,
        _ => throw new InvalidOperationException("Unknown compiled condition in player-binding analysis"),
    };

    private static long Amount(AbilityNumber number, Cast cast) => number switch
    {
        AbilityNumber.Constant constant => constant.Value,
        AbilityNumber.PerPlayer perPlayer => perPlayer.Value * cast.World.Players,
        AbilityNumber.Result result => cast.Results.GetValueOrDefault(result.Name),
        AbilityNumber.Sum sum => sum.Operands.Sum(operand => Amount(operand, cast)),
        AbilityNumber.Product product => product.Operands.Aggregate(1L, (value, operand) => value * Amount(operand, cast)),
        AbilityNumber.Minimum minimum => minimum.Operands.Min(operand => Amount(operand, cast)),
        AbilityNumber.CardValue value => CardNumber(value, cast),
        AbilityNumber.Counters counters => Find(counters.Card, cast) is { } holder ? CounterCount(holder, counters.Counter) : 0,
        AbilityNumber.Modified modified => Find(modified.Card, cast) is { } holder
            ? StateFields.Modified(cast.World, holder, modified.Field, cast.World.Facts, cast.World.Players) : 0,
        AbilityNumber.Count count => Every(count.Cards, cast).Count,
        AbilityNumber.Conditional conditional => Amount(Test(conditional.Test, cast) ? conditional.Then : conditional.Else, cast),
        AbilityNumber.PrintedResourcesDiscarded resource => Resources.PrintedCount(cast.Discarded, resource.Resource, cast.World.Facts),
        AbilityNumber.DiscardedWithResource resource => cast.Discarded.Count(card =>
            Resources.GeneratedBy(card.FaceId, cast.World.Facts).Contains(resource.Resource)),
        AbilityNumber.ResolutionValue value => value.Kind switch
        {
            AbilityResolutionNumber.PowerAmount => cast.PowerAmount,
            AbilityResolutionNumber.PrintedBoostIconsDiscarded => cast.Discarded.Sum(card =>
                cast.World.Facts.PrintedValue(card.FaceId, "Boost", cast.World.Players)),
            AbilityResolutionNumber.TopEncounterDiscardBoostPlusOne => 1 + (cast.Discarded.LastOrDefault() is { } card
                ? cast.World.Facts.PrintedValue(card.FaceId, "Boost", cast.World.Players) : 0),
            _ => throw new InvalidOperationException("Unknown compiled resolution number"),
        },
        _ => throw new InvalidOperationException("Unknown compiled numeric expression"),
    };

    private static long CardNumber(AbilityNumber.CardValue value, Cast cast)
    {
        if (Find(value.Card, cast) is not { } card) return 0;
        return value.Property switch
        {
            AbilityCardNumberProperty.Threat => card.Tokens.GetValueOrDefault("k_threat"),
            AbilityCardNumberProperty.Damage => card.Damage,
            AbilityCardNumberProperty.RemainingHealth => Math.Max(0, Damage.Health(cast.World, cast.World.Facts, card) - card.Damage),
            AbilityCardNumberProperty.StartingHealth => StartingHealth(card, cast),
            _ => throw new InvalidOperationException("Unknown compiled card number"),
        };
    }

    private static bool Test(AbilityCondition condition, Cast cast) => condition switch
    {
        AbilityCondition.All all => all.Operands.All(operand => Test(operand, cast)),
        AbilityCondition.Any any => any.Operands.Any(operand => Test(operand, cast)),
        AbilityCondition.Negated negated => !Test(negated.Operand, cast),
        AbilityCondition.Flag flag => TestFact(flag.Kind, cast),
        AbilityCondition.PaidWithResource resource => PaidWith(cast, resource.Resource.ToString()),
        AbilityCondition.DiscardedWithResource resource => cast.Discarded.Any(card =>
            Resources.GeneratedBy(card.FaceId, cast.World.Facts).Contains(resource.Resource)),
        AbilityCondition.CausedThreat threat => cast.Occurrence.Threat?.Cause == threat.Cause,
        AbilityCondition.Exists exists => Every(exists.Cards, cast).Count > 0,
        AbilityCondition.LegalPractice practice => cast.World.Seats[cast.Player].Hand.Cards
            .Any(card => card.ObjectId != cast.Source.ObjectId)
            && Every(practice.Schemes, cast).Any(card => card.Tokens.GetValueOrDefault("k_threat") > 0),
        AbilityCondition.AutomaticThwart thwart => Find(thwart.Scheme, cast) is { } scheme
            && BasicPowers.CanAutomaticallyThwart(cast.World, cast.World.Facts, cast.Player, scheme),
        AbilityCondition.TitleInPlay title => cast.World.Areas.Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards).Any(card => string.Equals(cast.World.Facts.Title(card.FaceId), title.Title, StringComparison.Ordinal)),
        AbilityCondition.AtLeast comparison => Amount(comparison.Value, cast) >= Amount(comparison.Count, cast),
        AbilityCondition.InForm form => Forms.In(cast.World, cast.World.Seats[Seat(form.Player, cast)], cast.World.Facts, form.Form),
        AbilityCondition.ActivationIs activation => cast.World.Activation is { } current && current.Attacking == activation.Attack,
        AbilityCondition.CardText text => TestCardText(text, cast),
        AbilityCondition.IsKind kind => Find(kind.Card, cast) is { } card && cast.World.Facts.Kind(card.FaceId) == kind.Kind,
        AbilityCondition.WasDefeated defeated => Find(defeated.Card, cast) is { } card
            && cast.Occurrence.Defeats.Any(defeat => defeat.Card == card.ObjectId),
        AbilityCondition.IsYourIdentity identity => Find(identity.Card, cast)?.ObjectId == cast.World.Seats[Resolver(cast)].IdentityCard.ObjectId,
        _ => throw new InvalidOperationException("Unknown compiled condition"),
    };

    private static bool TestCardText(AbilityCondition.CardText text, Cast cast)
    {
        if (Find(text.Card, cast) is not { } card) return false;
        return text.Property switch
        {
            AbilityCardTextProperty.Status => Statuses.Has(cast.World, card, text.Text),
            AbilityCardTextProperty.Trait => Rules.State.Traits.Has(cast.World, card, text.Text, cast.World.Facts),
            AbilityCardTextProperty.Set => string.Equals(cast.World.Facts.EncounterSet(card.FaceId), text.Text, StringComparison.Ordinal),
            AbilityCardTextProperty.Title => string.Equals(cast.World.Facts.Title(card.FaceId), text.Text, StringComparison.Ordinal),
            _ => throw new InvalidOperationException("Unknown compiled card text property"),
        };
    }

    private static bool TestFact(AbilityConditionFact fact, Cast cast) => fact switch
    {
        AbilityConditionFact.FinalStep => cast.FinalStep,
        AbilityConditionFact.CanMakeTheCall => CanMakeTheCall(cast),
        AbilityConditionFact.AttackDamaged => cast.World.FinishedAttack is { Damaged: true } attack
            && attack.Enemy == cast.Occurrence.Actor && attack.Target == cast.Occurrence.Target,
        AbilityConditionFact.InExpertMode => cast.World.Expert,
        AbilityConditionFact.DefeatedByYou => cast.Occurrence.Defeat is { By: >= 0 } defeat && defeat.By == Resolver(cast),
        AbilityConditionFact.HeroDefended => cast.World.FinishedAttack is { } attack
            && attack.Defender == cast.World.Seats[Resolver(cast)].IdentityCard.ObjectId,
        AbilityConditionFact.UndefendedAttack => cast.World.Attack is { IsDefended: false },
        AbilityConditionFact.DefeatedByConsequentialDamage => cast.Occurrence.Defeat is { } defeat
            && string.Equals(defeat.How, Cause("consequentialDamage", cast), StringComparison.Ordinal),
        _ => throw new InvalidOperationException("Unknown compiled condition fact"),
    };

    private static int Seat(AbilityPlayer player, Cast cast) => player switch
    {
        AbilityPlayer.TriggerPlayer => cast.Occurrence.Player,
        AbilityPlayer.You => Resolver(cast),
        AbilityPlayer.Controller => cast.ProjectedPlayAreaPlayer ?? ControllerOf(cast.World, cast.Source),
        AbilityPlayer.ChosenPlayer => ChosenPlayer(cast).Owner,
        AbilityPlayer.EngagedPlayer => cast.ProjectedPlayAreaPlayer ?? (cast.Source.Area.PlayArea.Player >= 0
            ? cast.Source.Area.PlayArea.Player
            : throw new RulesNotImplementedException($"'{cast.Source.FaceId}' asks for its engaged player outside a player's engaged area")),
        AbilityPlayer.FirstPlayer => cast.World.FirstPlayer,
        _ => throw new InvalidOperationException("Unknown compiled player relation"),
    };
}
