using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Keeps a card its own text gives to the first player under the first player's control.</summary>
/// <remarks>
/// The Milano (16142) prints "The first player controls the Milano", a constant ability.
/// Ruling 027e5dbfef0d5861: such a card changes control when the first player changes, and
/// "the first player controls" is the same as "the first player gains control of". A card that
/// changes control moves to its new controller's play area in the same state
/// (<c>rr:ownership-and-control.5</c>). The engine moves it as soon as the token passes.
/// </remarks>
public static class FirstPlayerControl
{
    /// <summary>Moves every such card into the current first player's play area.</summary>
    public static void Follow(World world, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(events);
        var moving = world.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type)
                && world.SetupAbilities.SetupController(world, card) is { } player
                && card.Area.PlayArea != PlayArea.Of(player))
            .ToList();
        foreach (var card in moving)
        {
            if (EffectiveCards.Kind(card, world.Facts) != CardKind.Support
                || world.Areas.Any(area => area.Host == card.ObjectId && area.Cards.Count > 0))
                throw new RulesNotImplementedException(
                    $"card {card.ObjectId} follows the first player, and only a support with nothing attached is modelled");
            var from = card.Area;
            var onto = world.AreaOf(from.Type, PlayArea.Of(world.FirstPlayer), cardOwner: world.FirstPlayer);
            World.MoveToTop(card, onto);
            events.Add(new CardsMoved(Places.Reference(from), Places.Reference(onto),
                [new Landing(card.ObjectId, onto.Cards.Count - 1)]) { Trigger = trigger, Verb = "Move" });
        }
    }
}
