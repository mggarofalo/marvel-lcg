using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Rules.Play.CardPayment;
using static Marvel.Rules.Play.CardPlayLegality;
using static Marvel.Rules.Play.CardControlTransfer;
using static Marvel.Rules.Play.CardEntry;
using static Marvel.Rules.Play.CardPlay;

namespace Marvel.Rules.Play;

/// <summary>Transfers card control while preserving hosted-card structure.</summary>
public static class CardControlTransfer
{

    /// <summary>Transfers control of an in-play player card to another player.</summary>
    /// <remarks>
    /// The destination is validated before the card moves. In particular,
    /// <c>rr:max-maximum.3.1</c> forbids taking control of another copy of a
    /// “Max 1 per player” card already controlled there.
    /// </remarks>
    public static void TakeControl(
        World world, ICardFacts facts, Card card, int player)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(card);
        if (player < 0 || player >= world.Seats.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(player));
        }
        if (!DeckTypes.IsInPlay(card.Area.Type) || !card.Area.PlayArea.IsPlayers)
        {
            throw new RulesNotImplementedException(
                $"card {card.ObjectId} is not an in-play player card whose control can transfer");
        }
        if (card.Area.PlayArea == PlayArea.Of(player))
        {
            return;
        }

        var moving = ControlTree(world, card);
        PreflightControlMaxima(world, facts, moving, player);
        MoveControlTree(world, moving, player);
        foreach (var (_, controlled) in moving)
        {
            TakeScenarioCardOwnership(facts, controlled, player);
        }
    }

    /// <summary>Returns a temporarily controlled card to its owner's control.</summary>
    public static void ReturnToOwnerControl(World world, ICardFacts facts, Card card)
    {
        ArgumentNullException.ThrowIfNull(card);
        if (card.Owner < 0)
        {
            throw new RulesNotImplementedException(
                $"card {card.ObjectId} has no player owner whose control can resume");
        }

        // `rr:ownership-and-control.7.1`: when the ability changing control
        // ceases, the card reverts to its owner's control. Reuse TakeControl so
        // maxima are checked before any member of the hosted tree moves.
        TakeControl(world, facts, card, card.Owner);
    }

    /// <summary>Transfers a root and every card hosted by it to one play area.</summary>
    internal static List<(Area Source, Card Card)> ControlTree(World world, Card root)
    {
        var moving = new List<(Area Source, Card Card)> { (root.Area, root) };
        var pending = new Stack<Card>([root]);
        var seen = new HashSet<int>();
        while (pending.TryPop(out var host))
        {
            if (!seen.Add(host.ObjectId))
            {
                throw new RulesNotImplementedException(
                    $"card {host.ObjectId} forms a hosting cycle during a control change");
            }

            foreach (var child in world.Areas
                         .Where(area => area.Host == host.ObjectId)
                         .SelectMany(area => area.Cards)
                         .Reverse())
            {
                moving.Add((child.Area, child));
                pending.Push(child);
            }
        }

        return moving;
    }

    /// <summary>Validates every root and descendant before a control tree moves.</summary>
    internal static void PreflightControlMaxima(
        World world, ICardFacts facts, IReadOnlyList<(Area Source, Card Card)> moving,
        int player)
    {
        var movingIds = moving.Select(item => item.Card.ObjectId).ToHashSet();
        foreach (var (_, candidate) in moving.Where(item =>
                     DeckTypes.IsInPlay(item.Source.Type)))
        {
            long maximum = facts.PrintedValue(
                candidate.FaceId, "MaxPerUnit", world.Players);
            string unit = facts.Attributes(candidate.FaceId)
                .GetValueOrDefault("MaxPerUnitKind", "player");
            if (maximum <= 0 || !string.Equals(unit, "player", StringComparison.Ordinal))
            {
                continue;
            }

            string title = facts.Title(candidate.FaceId);
            long alreadyThere = world.Areas
                .Where(area => area.PlayArea == PlayArea.Of(player))
                .SelectMany(area => area.Cards)
                .Count(card => DeckTypes.IsInPlay(card.Area.Type)
                    && !movingIds.Contains(card.ObjectId)
                    && string.Equals(
                        facts.Title(card.FaceId), title, StringComparison.Ordinal));
            long arriving = moving.Count(item =>
                DeckTypes.IsInPlay(item.Source.Type)
                && string.Equals(
                    facts.Title(item.Card.FaceId), title, StringComparison.Ordinal));
            if (alreadyThere + arriving > maximum)
            {
                throw new RulesNotImplementedException(
                    $"player {player} would control more than {maximum} copies of "
                    + $"'{title}' after the control change");
            }
        }
    }

    internal static void MoveControlTree(
        World world, IReadOnlyList<(Area Source, Card Card)> moving, int player)
    {

        foreach (var (source, movingCard) in moving)
        {
            bool faceUp = movingCard.FaceUp;
            var destination = world.AreaOf(
                source.Type, PlayArea.Of(player), source.Host, source.CardOwner);
            World.MoveToTop(movingCard, destination);
            if (!faceUp)
            {
                movingCard.TurnFaceDown();
            }
        }
    }

    /// <summary>Applies the scenario-player-card ownership exception.</summary>
    /// <remarks>
    /// The Rules Reference names a player card back; the joined card dataset
    /// does not store backs. The engine maps that physical distinction to a
    /// player-card kind created for the scenario, or to the printed Campaign,
    /// Encounter, or Scenario class after ownership has already transferred.
    /// </remarks>
    internal static void TakeScenarioCardOwnership(
        ICardFacts facts, Card card, int player)
    {
        string printedClass = facts.Attributes(card.FaceId)
            .GetValueOrDefault("Class", string.Empty);
        if (facts.Kind(card.FaceId) is CardKind.Ally or CardKind.Support
                or CardKind.Upgrade or CardKind.Event or CardKind.Resource
            && (card.Owner < 0
                || printedClass is "Campaign" or "Encounter" or "Scenario"))
        {
            card.TransferScenarioOwnership(player);
        }
    }
}
