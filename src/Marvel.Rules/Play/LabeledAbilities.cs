using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Who performs a labeled ability and whether it begins resolving.</summary>
public static class LabeledAbilities
{
    /// <summary>The labels whose rules the engine executes.</summary>
    public static IReadOnlySet<string> Known { get; } =
        new HashSet<string>(StringComparer.Ordinal)
        {
            BasicPowers.AttackVerb,
            Attack.DefenseVerb,
            BasicPowers.ThwartVerb,
        };

    /// <summary>The card considered to perform an ability from <paramref name="source"/>.</summary>
    /// <remarks>
    /// Player events, resources, identities, and ordinary upgrades are
    /// extensions of the resolving identity. <c>rr:support.3</c> excludes a
    /// support in play, and <c>rr:upgrade.4</c> excludes an upgrade attached to
    /// a different friendly character; that host performs the upgrade's
    /// ability. Allies perform their own abilities under <c>rr:ally.5</c>.
    /// </remarks>
    public static Card Performer(World world, ICardFacts facts, int player, Card source)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(facts);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(player);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(player, world.Players);

        var identity = world.Seats[player].IdentityCard;
        var kind = FacedownDrones.Kind(source, facts);
        if (kind == CardKind.Support || kind == CardKind.Ally)
        {
            return source;
        }

        if (kind == CardKind.Upgrade
            && source.Area.Host >= 0
            && source.Area.Host < world.Cards.Count)
        {
            var host = world.Cards[source.Area.Host];
            bool friendlyCharacter = host.Area.PlayArea.IsPlayers
                && FacedownDrones.Kind(host, facts) is CardKind.Hero
                    or CardKind.AlterEgo
                    or CardKind.Ally;
            if (friendlyCharacter && host.ObjectId != identity.ObjectId)
            {
                return host;
            }
        }

        return kind is CardKind.Hero
                or CardKind.AlterEgo
                or CardKind.Event
                or CardKind.Resource
                or CardKind.Upgrade
            || source.Owner == player
            ? identity
            : source;
    }

    /// <summary>Begins one ability carrying one or more labels.</summary>
    /// <returns>The performer, or <c>null</c> when a status cancels the ability.</returns>
    /// <remarks>
    /// <c>rr:labeled-ability.1</c> places this after costs. Clauses <c>.5</c>
    /// and <c>.6</c> make the labels a set: any matching status cancels the
    /// whole effect, and <c>.6.2</c> removes every matching status rather than
    /// stopping after the first.
    /// </remarks>
    public static Card? Begin(
        World world, ICardFacts facts, int player, Card source,
        IEnumerable<string> labels, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(events);

        var normalized = labels.Distinct(StringComparer.Ordinal).ToList();
        if (normalized.Count == 0 || normalized.Any(label => !Known.Contains(label)))
        {
            throw new RulesNotImplementedException(
                $"'{source.FaceId}' has an unknown or empty labeled-ability type");
        }

        var performer = Performer(world, facts, player, source);
        var cancelling = new List<string>();
        if (normalized.Contains(BasicPowers.AttackVerb, StringComparer.Ordinal))
        {
            cancelling.Add(Statuses.Stunned);
        }
        if (normalized.Contains(BasicPowers.ThwartVerb, StringComparer.Ordinal))
        {
            cancelling.Add(Statuses.Confused);
        }

        bool cancelled = false;
        foreach (string status in cancelling.Distinct(StringComparer.Ordinal))
        {
            if (BasicPowers.Cancelled(world, facts, performer, status, events))
            {
                cancelled = true;
            }
        }

        return cancelled ? null : performer;
    }
}
