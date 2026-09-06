using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

internal static partial class AbilityInitiation
{
    private static T EffectOf<T>(AbilityEffect effect, AbilityAdmissionScope _) where T : AbilityEffect =>
        (T)effect;

    private static AbilityEffect.Conditional ConditionalOf(
        AbilityEffect effect, AbilityAdmissionScope _) =>
        (AbilityEffect.Conditional)effect;

    private static AbilityEffect.ForEach ForEachOf(
        AbilityEffect effect, AbilityAdmissionScope _) =>
        (AbilityEffect.ForEach)effect;

    private static AbilityEffect.DiscardTop EachTimePreceding(
        AbilityEffect effect, AbilityAdmissionScope scope)
    {
        if (((AbilityEffect.EachTime)effect).Effect is not AbilityEffect.DiscardTop
            { From: AbilitySearchArea.EncounterDeck, Players: null } preceding)
        {
            throw new RulesNotImplementedException(
                $"'{scope.Source.FaceId}' uses each-time around an unsupported preceding effect");
        }
        return preceding;
    }

    private static AbilityEffect.EachTime EachTimeOf(
        AbilityEffect effect, AbilityAdmissionScope _) =>
        (AbilityEffect.EachTime)effect;

    private static AbilityEffect.ChangeForm FormChangeOf(
        AbilityEffect effect, AbilityAdmissionScope _) =>
        (AbilityEffect.ChangeForm)effect;

    private static AbilityEffect.ActivateEnemies ActivationOf(
        AbilityEffect effect, AbilityAdmissionScope _) =>
        (AbilityEffect.ActivateEnemies)effect;

    private static AbilityCardSelection DamageSelectionOf(
        AbilityEffect effect, AbilityAdmissionScope _) => effect switch
        {
            AbilityEffect.Damage damage => damage.Cards,
            AbilityEffect.AttackDamage damage => damage.Cards,
            AbilityEffect.IndirectDamage damage => damage.Among,
            _ => throw new InvalidOperationException("Expected a compiled damage instruction"),
        };

    private static AbilityNumber DamageAmountOf(
        AbilityEffect effect, AbilityAdmissionScope _) => effect switch
        {
            AbilityEffect.Damage damage => damage.Amount,
            AbilityEffect.AttackDamage damage => damage.Amount,
            AbilityEffect.IndirectDamage damage => damage.Amount,
            _ => throw new InvalidOperationException("Expected a compiled damage instruction"),
        };

    private static AbilityCardSelection GrantSelectionOf(
        AbilityEffect effect, AbilityAdmissionScope _) => effect switch
        {
            AbilityEffect.GrantField grant => grant.Cards,
            AbilityEffect.GrantTrait grant => grant.Cards,
            _ => throw new InvalidOperationException("Expected a compiled grant instruction"),
        };

    private static AbilityCardSelection ThreatSelectionOf(
        AbilityEffect effect, AbilityAdmissionScope _) => effect switch
        {
            AbilityEffect.PlaceThreat threat => threat.Schemes,
            AbilityEffect.RemoveThreat threat => threat.Schemes,
            _ => throw new InvalidOperationException("Expected a compiled threat instruction"),
        };

    private static AbilityExpressionEvaluation Expressions(AbilityAdmissionScope scope) =>
        scope.Context.Evaluator(
            areas => AbilityRuntimeQueries.SingularAreaQueryIsStable(areas, scope.Context));

    private static long Amount(AbilityNumber number, AbilityAdmissionScope scope) =>
        Expressions(scope).Amount(number);

    private static bool Test(AbilityCondition condition, AbilityAdmissionScope scope) =>
        Expressions(scope).Test(condition);

    private static int Seat(AbilityPlayer player, AbilityAdmissionScope scope) =>
        Expressions(scope).Seat(player);

    private static IReadOnlyList<Card> Every(
        AbilityCardSelection selection, AbilityAdmissionScope scope) =>
        scope.Context.Selectors().Every(selection);

    private static Card? Find(
        AbilityCardSelection selection, AbilityAdmissionScope scope) =>
        scope.Context.Selectors(
            areas => AbilityRuntimeQueries.SingularAreaQueryIsStable(areas, scope.Context))
            .Find(selection);

    private static bool CanRemoveByEffect(
        AbilityCardSelection selection, AbilityAdmissionScope scope, Card card) =>
        scope.Context.Selectors().CanRemove(selection, card);

    private static int Resolver(AbilityAdmissionScope scope) =>
        AbilityCardQueries.Resolver(scope.Context.Query);

    private static bool IsPlayerCard(AbilityAdmissionScope scope) =>
        AbilityCardQueries.IsPlayerCard(scope.World.Facts, scope.Source);

    private static IReadOnlyList<Card> DamageTargets(
        AbilityCardSelection selection, AbilityAdmissionScope scope) =>
        [.. Every(selection, scope).Where(card => AbilityProgramQueries.CanTakeDamage(
            scope.World, scope.Context.Program, card, scope.Source))];

    private static List<Card> Assignable(
        AbilityCardSelection selection, AbilityAdmissionScope scope) =>
        [.. DamageTargets(selection, scope).Where(card =>
            Damage.Health(scope.World, scope.World.Facts, card) - card.Damage > 0)];

    private static bool MayChangeAnyArea(
        AbilityEffect effect, IReadOnlySet<DeckType> areas, AbilityAdmissionScope scope) =>
        AbilityRuntimeQueries.MayChangeAnyArea(effect, areas, scope.Context);

    private static ulong PlayerSeat(int player) => 1UL << player;
    private static bool SeatMayChange(ulong seats, int player) =>
        (seats & PlayerSeat(player)) != 0;

    private static bool BindingCanChange(AbilityEffect? effect) =>
        AbilityBindingAnalysis.BindingCanChange(effect);

    private static bool BindingCanChange(AbilityPlayerSelection players) =>
        AbilityBindingAnalysis.BindingCanChange(players);

    private static bool BindingCanChange(AbilityCondition condition) =>
        AbilityBindingAnalysis.BindingCanChange(condition);

    private static bool BindingCanChange(AbilityNumber number) =>
        AbilityBindingAnalysis.BindingCanChange(number);

    private static bool BindingCanChange(AbilityCardSelection selection) =>
        AbilityBindingAnalysis.BindingCanChange(selection);

    private static bool AmountMayChange(AbilityNumber number) =>
        AbilityBindingAnalysis.AmountMayChange(number);

    private static bool ContainsPowerAmount(AbilityNumber number) =>
        AbilityBindingAnalysis.ContainsPowerAmount(number);

    private static bool ContainsPowerAmount(AbilityCondition condition) =>
        AbilityBindingAnalysis.ContainsPowerAmount(condition);

    private static int ReplaceableDefenseDefender(AbilityAdmissionScope scope) =>
        checked((int)scope.Context.Expressions.Results.GetValueOrDefault(
            "defenseAbilityDefender", -1));

    private static int ControllerOf(World world, Card card) =>
        AbilityCardQueries.ControllerOf(world, card);

    private static IEnumerable<int> Seats(
        AbilityPlayerSelection players, AbilityAdmissionScope scope) => players switch
        {
            AbilityPlayerSelection.OnePlayer one => [Seat(one.Player, scope)],
            AbilityPlayerSelection.AllPlayers => scope.World.PlayerOrder,
            _ => throw new InvalidOperationException("Unknown compiled player selection"),
        };

    private static bool AlreadyInForm(
        AbilityEffect.ChangeForm change, AbilityAdmissionScope scope) =>
        AbilityAdmissionFacts.AlreadyInForm(
            scope.World, Seat(change.Player, scope), change.Form);

    private static bool CanAdvanceMainScheme(AbilityAdmissionScope scope) =>
        AbilityAdmissionFacts.CanAdvanceMainScheme(scope.World);

    private static AbilityEffect.RemoveCounters CounterRemovalOf(
        AbilityEffect effect, AbilityAdmissionScope _) =>
        (AbilityEffect.RemoveCounters)effect;

    private static string? CounterKeyForRemoval(
        Card card, string type, long count) =>
        AbilityCostSelection.CounterKeyForRemoval(card, type, count);

    private static bool CanDrawToPrintedHandSize(
        AbilityEffect effect, AbilityAdmissionScope scope) =>
        AbilityAdmissionFacts.CanDrawToPrintedHandSize(
            scope.World, scope.Source,
            Seat(((AbilityEffect.DrawToHandSize)effect).Player, scope));

    private static bool CanCreateDrones(
        AbilityEffect effect, AbilityAdmissionScope scope) =>
        effect is AbilityEffect.CreateDrones drones
        && AbilityAdmissionFacts.CanCreateDrones(
            scope.World, Seats(drones.Players, scope), drones.Count);

    private static IReadOnlyList<Card> QueryCards(
        AbilityCardQuery query, AbilityAdmissionScope scope) =>
        AbilityCardQueries.Cards(query, scope.Context.Query, scope.Context.Program);

    private static HashSet<DeckType> SearchAreaTypes(
        AbilityEffect effect, AbilityAdmissionScope _) =>
        ((AbilityEffect.Search)effect).Areas
            .Select(AbilitySelectorEvaluation.AreaType).ToHashSet();

    private static Area Area(AbilitySearchArea area, AbilityAdmissionScope scope) =>
        area switch
        {
            AbilitySearchArea.EncounterDeck => scope.World.AreaOf(DeckType.EncounterDeck),
            AbilitySearchArea.EncounterDiscardPile => scope.World.AreaOf(DeckType.EncounterDiscardPile),
            AbilitySearchArea.ScenarioSetAside => scope.World.AreaOf(DeckType.AsideDeck),
            AbilitySearchArea.YourDeck => scope.World.Seats[scope.Player].Deck,
            _ => throw new InvalidOperationException("Unknown compiled search area"),
        };

    private static bool CostMayChangeAnyArea(
        AbilityCost cost, IReadOnlySet<DeckType> areas, AbilityAdmissionScope scope) =>
        AbilityRuntimeQueries.CostMayChangeAnyArea(cost, areas, scope.Context);

    private static IReadOnlyList<Card> ActivationCandidates(
        AbilityEffect.ActivateEnemies activation, AbilityAdmissionScope scope) =>
        [.. Every(activation.Enemies, scope).Where(enemy => !activation.Dynamic
            || scope.Context.Expressions.Results.GetValueOrDefault(
                $"dynamicActivation:{enemy.ObjectId}") == 0)];

    private static IEnumerable<AbilityEffect> ReachableMutationBranches(
        AbilityEffect effect, AbilityAdmissionScope scope) =>
        AbilityRuntimeQueries.ReachableMutationBranches(effect, scope.Context);

    internal static bool ContainsForEachTarget(AbilityEffect node) =>
        node is AbilityEffect.DelayedStun
        || node.OperationName() is "removeFromGame" or "exhaust" or "ready" or "reveal"
            or "returnToHand" or "returnOwnedToHand" or "soakDamage"
            or "addToHand" or "giveStatus" or "attachTo" or "grantUntil"
            or "discard" or "heal" or "placeCounters" or "shuffleInto" or "search"
            or "indirectDamage" or "dealDamage" or "moveDamage"
            or "dealAttackDamage" or "moveAttackDamage" or "placeThreat"
            or "removeThreat" or "replaceThreatWithDamage" or "enemyAttacks"
            or "enemySchemes" or "putIntoPlay" or "placeAtRandom" or "thwartSchemes"
            or "thwartDifferentSchemes" or "legalPractice"
        || ContinuationChildren(node).Any(ContainsForEachTarget);
}
