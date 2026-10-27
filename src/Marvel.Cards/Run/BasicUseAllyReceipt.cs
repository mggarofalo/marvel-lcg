using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>The physical ally and matching modified power committed by a basic-use cost.</summary>
internal sealed record BasicUseAllyReceipt(int Card, int Incarnation, int Player, long Amount)
{
    // Engine-chosen continuation keys; the existing result map survives suspended effects.
    private const string Prefix = "basicUseAlly:";

    internal static BasicUseAllyReceipt Capture(World world, Card ally, int player)
    {
        var use = world.Agenda.Occurrence?.BasicUse
            ?? throw new RulesNotImplementedException("selected ally power requires a basic-use occurrence");
        if (!use.IsCurrent(world) || !ControlledAlly(world, ally, player))
            throw new RulesNotImplementedException("selected ally power requires its current controlled copy");
        return new(ally.ObjectId, ally.Incarnation, player, Power(world, ally, use));
    }

    internal void Store(IDictionary<string, long> results)
    {
        results[Prefix + "card"] = Card;
        results[Prefix + "incarnation"] = Incarnation;
        results[Prefix + "player"] = Player;
        results[Prefix + "amount"] = Amount;
    }

    internal static long Read(IReadOnlyDictionary<string, long> results, World world, BasicUse use)
    {
        if (!results.TryGetValue(Prefix + "card", out long id))
            throw new RulesNotImplementedException("matching basic power requires its selected ally cost receipt");
        var ally = world.Cards[checked((int)id)];
        return use.IsCurrent(world) && ally.Incarnation == results[Prefix + "incarnation"]
            && ControlledAlly(world, ally, checked((int)results[Prefix + "player"]))
                ? results[Prefix + "amount"] : 0;
    }

    internal static bool CanApply(AbilityEffect.BoostBasicUse effect, AbilityAdmissionContext context)
    {
        if (context.Query.Occurrence.BasicUse is not { } use) return false;
        if (effect.Overkill && use.Field != "attack") return false;
        // A committed payment may fizzle when its physical participant departs.
        // Keeping its continuation admissible lets earlier choices finish normally.
        if (effect.MatchingAlly && context.Expressions.Results.ContainsKey(Prefix + "card")) return true;
        if (!use.IsCurrent(context.World)) return false;
        if (!effect.MatchingAlly) return true;
        return MatchingAllyAvailable(context, use);
    }

    private static bool MatchingAllyAvailable(AbilityAdmissionContext context, BasicUse use)
    {
        // Engine boundary: matching payment is supported only for the live agenda
        // occurrence whose cost selection and exhaustion own the receipt.
        if (!ReferenceEquals(context.World.Agenda.Occurrence, context.Query.Occurrence)) return false;
        if (context.Query.ExhaustedForCost.Count == 0)
            return AbilityCostSelection.Choices(context.World, context.Query.Player, AbilityCardQuery.AlliesYouControl)
                .Any(ally => ally.Ready && ControlledAlly(context.World, ally, context.Query.Player) && Power(context.World, ally, use) > 0);
        return context.Query.ExhaustedForCost.Count == 1
            && context.Query.ExhaustedForCost.Select(id => context.World.Cards[id]).Any(ally =>
                ControlledAlly(context.World, ally, context.Query.Player) && Power(context.World, ally, use) > 0);
    }

    internal static long Power(World world, Card ally, BasicUse use) =>
        Math.Max(0, StateFields.Modified(world, ally, use.Field, world.Facts, world.Players));

    private static bool ControlledAlly(World world, Card ally, int player) =>
        ally.Area.Type == DeckType.AlliesArea && EffectiveCards.Kind(ally, world.Facts) == CardKind.Ally
            && CardControl.ControllerOf(world, ally) == player;
}
