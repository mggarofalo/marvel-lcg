using Marvel.Rules.State;

namespace Marvel.Rules.Play;

// Resolves the destination and attachment host for one played non-event card.
internal static class PlayedCardDestination
{
    internal static (Area Into, int UpgradeHost) Resolve(World world, ICardPlayAbilities abilities,
        Seat seat, Card card, CardKind kind, IReadOnlyList<int> targets)
    {
        int upgradeHost = seat.IdentityCard.ObjectId;
        var eligibleHosts = kind == CardKind.Upgrade
            ? abilities.AttachmentTargets(world, card)
            : null;
        if (eligibleHosts is not null)
        {
            if (targets.Count != 1 || !eligibleHosts.Contains(targets[0]))
            {
                throw new RulesNotImplementedException(
                    $"card {card.ObjectId} must attach to exactly one eligible target");
            }
            upgradeHost = targets[0];
        }

        // An encounter card's play area is the villain's, but attaching a
        // player upgrade to it does not give control to the scenario. The
        // exception in `rr:ownership-and-control.2.1` applies only when the
        // attached card is controlled by another *player*.
        var hostArea = world.Cards[upgradeHost].Area.PlayArea;
        int controller = kind == CardKind.Upgrade && hostArea.IsPlayers
            ? hostArea.Player
            : seat.Index;

        var into = kind switch
        {
            CardKind.Ally => world.AreaOf(
                DeckType.AlliesArea, PlayArea.Of(seat.Index), cardOwner: seat.Index),
            CardKind.Support => world.AreaOf(
                DeckType.SupportsArea, PlayArea.Of(seat.Index), cardOwner: seat.Index),

            // `rr:upgrade`: an upgrade attaches to a card, and with no ability
            // saying otherwise that card is the identity playing it.
            // `rr:ownership-and-control.2.1`: when it attaches to another
            // player's card, that player controls the upgrade, so it enters
            // that player's play area while retaining its original owner.
            CardKind.Upgrade => world.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(controller),
                host: upgradeHost, cardOwner: seat.Index),
            _ => throw new RulesNotImplementedException(
                $"a {kind} played from hand has nowhere to enter play"),
        };

        return (into, upgradeHost);
    }
}
