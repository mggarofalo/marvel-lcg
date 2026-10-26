using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Binds the public physical Invocation and its cost before paying an arrow.</summary>
internal static class AbilityInvocationPayment
{
    private const string CardKey = "__invocation.card";
    private const string CopyKey = "__invocation.incarnation";
    private const string OwnerKey = "__invocation.owner";
    private const string CostKey = "__invocation.cost";

    internal static Card? Top(World world, int player) => player >= 0 && player < world.Seats.Count
        && world.Seats[player].InvocationDeck?.PublicTop is { } top && top.Owner == player ? top : null;

    internal static bool Contains(AbilityCost? cost) => cost is AbilityCost.InvocationTop
        || cost is AbilityCost.Sequence sequence && sequence.Costs.Any(Contains);

    internal static AbilityCost? Expand(World world, int player, AbilityCost? cost) => cost switch
    {
        AbilityCost.InvocationTop => new AbilityCost.Spend("", false) { Generic = Amount(world, player) },
        AbilityCost.Sequence sequence when Contains(sequence) =>
            new AbilityCost.Sequence([.. sequence.Costs.Select(step => Expand(world, player, step)!)]),
        _ => cost,
    };

    private static int Amount(World world, int player)
    {
        var top = Top(world, player) ?? throw new RulesNotImplementedException("no owned public Invocation top");
        // Spell Mastery pays an ability cost, so ordinary event-play discounts do not apply.
        long amount = Resources.Cost(top.FaceId, world.Facts, world.Players)
            ?? throw new RulesNotImplementedException("the Invocation has no fixed payable cost");
        return checked((int)amount);
    }

    internal static void Capture(AbilityResolutionState cast, AbilityCost? cost, IReadOnlyList<int> chosen)
    {
        if (!Contains(cost)) return;
        var top = Top(cast.World, cast.Player);
        if (top is null || chosen.Count != 1 || chosen[0] != top.ObjectId)
            throw new RulesNotImplementedException("Spell Mastery requires the exact current owned Invocation top");
        cast.Results[CardKey] = top.ObjectId;
        cast.Results[CopyKey] = top.Incarnation;
        cast.Results[OwnerKey] = top.Owner;
        cast.Results[CostKey] = Amount(cast.World, cast.Player);
    }

    internal static NextSpecialCommand Resolve(AbilityStructuralContext context)
    {
        var results = context.Expressions.Results;
        var top = Top(context.Expressions.World, context.Player);
        if (top is null || !results.TryGetValue(CardKey, out long id) || top.ObjectId != id
            || !results.TryGetValue(CopyKey, out long copy) || top.Incarnation != copy
            || !results.TryGetValue(OwnerKey, out long owner) || top.Owner != owner
            || !results.ContainsKey(CostKey))
            throw new RulesNotImplementedException("the captured Invocation top is no longer current");
        // rr:special: only another ability's explicit instruction resolves a Special.
        // Reuse the existing owned Special continuation, without Wakanda Forever's final bonus.
        return new NextSpecialCommand(top.ObjectId, true) { Invocation = true };
    }

    // The predecessor completes the instructed Special procedure, whose child effects
    // still resolve as much as possible before the parent resumes.
    internal static AbilityAdmission.AdmissionResolution? ResolutionOf(AbilityEffect effect, AbilityAdmission.AbilityAdmissionScope scope) =>
        effect is AbilityEffect.ResolveInvocation
            ? Top(scope.World, scope.Player) is not null
                ? AbilityAdmission.AdmissionResolution.Full : AbilityAdmission.AdmissionResolution.None
            : null;

    internal static void Return(AbilityResolutionState cast)
    {
        if (!cast.Results.TryGetValue(CardKey, out long id)
            || !cast.Results.TryGetValue(OwnerKey, out long owner) || owner != cast.Player
            || id < 0 || id >= cast.World.Cards.Count)
            throw new RulesNotImplementedException("no captured owned Invocation to return");
        var card = cast.World.Cards[checked((int)id)];
        if (!cast.Results.TryGetValue(CopyKey, out long copy) || card.Incarnation != copy)
            throw new RulesNotImplementedException("the captured Invocation incarnation cannot return");
        var deck = cast.World.Seats[cast.Player].InvocationDeck
            ?? throw new RulesNotImplementedException("no resolving player Invocation deck");
        deck.ReturnToTop(card, cast.Trigger, cast.Events);
    }
}
