using static Marvel.Cards.Run.AbilityEffectStructure;
using static Marvel.Cards.Run.AbilityRuntimeQueries;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Queries card selection and resolution against a projected board.</summary>
internal static class AbilityProjectedDefeat
{
    internal static bool DefeatTreeChangesArea(
        Card root, IReadOnlySet<DeckType> queried, AbilityAdmissionContext context)
    {
        var kind = EffectiveCards.Kind(root, context.World.Facts);
        if (RootDestinationChanges(root, kind, queried, context))
        {
            return true;
        }
        if (!RootLeavesPlay(root, kind, context))
        {
            return false;
        }
        return HostedDefeatChangesArea(root, queried, context);
    }

    private static bool RootDestinationChanges(
        Card root, CardKind kind, IReadOnlySet<DeckType> queried,
        AbilityAdmissionContext context)
    {
        bool discards = kind is CardKind.Minion or CardKind.Ally
                or CardKind.EncounterSideScheme
            && !Keywords.Has(context.World, root, "victory", context.World.Facts);
        var destination = root.Owner < 0
            ? DeckType.EncounterDiscardPile : DeckType.DiscardPile;
        return discards && queried.Contains(destination);
    }

    private static bool RootLeavesPlay(
        Card root, CardKind kind, AbilityAdmissionContext context)
    {
        if (!CardKinds.IsVillain(kind))
        {
            return kind is CardKind.Minion or CardKind.Ally
                or CardKind.EncounterSideScheme;
        }
        var villainDeck = context.World.AreaOf(DeckType.VillainDeck).Cards;
        var next = villainDeck.Count > 0 ? villainDeck[^1] : null;
        return next is null || !string.Equals(
            context.World.Facts.Title(root.FaceId),
            context.World.Facts.Title(next.FaceId),
            StringComparison.Ordinal);
    }

    private static bool HostedDefeatChangesArea(
        Card root, IReadOnlySet<DeckType> queried, AbilityAdmissionContext context)
    {
        var pending = DefeatDescendants(root, context);
        var seen = new HashSet<int> { root.ObjectId };
        while (pending.TryPop(out var card))
        {
            RequireAcyclicDefeat(card, seen, context);
            var destination = card.Owner < 0
                ? DeckType.EncounterDiscardPile : DeckType.DiscardPile;
            if (queried.Contains(destination))
            {
                return true;
            }
            PushHostedCards(pending, card, context);
        }
        return false;
    }

    private static Stack<Card> DefeatDescendants(
        Card root, AbilityAdmissionContext context)
    {
        var pending = new Stack<Card>();
        foreach (var card in HostedCards(root, context))
        {
            if (MovesToVictory(card, context))
            {
                foreach (var child in HostedCards(card, context))
                {
                    pending.Push(child);
                }
            }
            else
            {
                pending.Push(card);
            }
        }
        return pending;
    }

    private static IEnumerable<Card> HostedCards(
        Card host, AbilityAdmissionContext context) => context.World.Areas
        .Where(area => area.Host == host.ObjectId)
        .SelectMany(area => area.Cards);

    private static bool MovesToVictory(
        Card card, AbilityAdmissionContext context) =>
        EffectiveCards.Kind(card, context.World.Facts) is CardKind.Attachment or CardKind.Upgrade
        && DeckTypes.IsInPlay(card.Area.Type)
        && Keywords.Has(context.World, card, "victory", context.World.Facts);

    private static void PushHostedCards(
        Stack<Card> pending, Card host, AbilityAdmissionContext context)
    {
        foreach (var child in HostedCards(host, context))
        {
            pending.Push(child);
        }
    }

    private static void RequireAcyclicDefeat(
        Card card, HashSet<int> seen, AbilityAdmissionContext context)
    {
        if (!seen.Add(card.ObjectId))
        {
            throw new RulesNotImplementedException(
                $"'{context.Source.FaceId}' reaches a hosted-card cycle while "
                + "projecting defeat");
        }
    }

}
