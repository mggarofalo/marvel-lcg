using System.Collections.Immutable;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Commits an accepted card-resource payment through the Rules-owned payment procedure.</summary>
public static class CardResourcePayments
{
    /// <summary>Validates the accepted sources and allocations and retains them through generator suspension.</summary>
    public static ResourcePaymentReceipt Spend(
        World world, IResourceCardAbilities abilities, CardResourcePaymentRequest request, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(abilities);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(events);
        var captured = request with
        {
            Hands = request.Hands.ToImmutableArray(),
            Paying = request.Paying.ToImmutableArray(),
            Allocations = request.Allocations.ToImmutableArray(),
            ResourcePayers = request.ResourcePayers?.ToImmutableDictionary(),
            Price = Capture(request.Price),
        };
        return new CardResourcePayment(world, world.Facts, abilities, captured).Spend(events);
    }

    private static CostOption? Capture(CostOption? price) => price is null ? null : price with
    {
        Rule = price.Rule is { } rule ? rule.ToImmutableArray() : null,
        OrRule = price.OrRule is { } orRule ? orRule.ToImmutableArray() : null,
        Sources = price.Sources is { } sources ? sources.ToImmutableArray() : null,
        Variables = price.Variables is { } variables ? variables.ToImmutableArray() : null,
        Components = price.Components is { } components
            ? components.Select(cost => cost with
            { Rule = cost.Rule is { } componentRule ? componentRule.ToImmutableArray() : null }).ToImmutableArray()
            : null,
    };
}
