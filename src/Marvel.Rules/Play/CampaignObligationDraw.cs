using Marvel.Rules.Campaign;
using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Applies campaign obligation entry to a drawn card without drawing a replacement.</summary>
internal static class CampaignObligationDraw
{
    internal static void Enter(World world, Card card, int player, string trigger, List<GameEvent> events)
    {
        if (world.Campaign?.Cards.Any(progress => progress.Binding.ObjectId == card.ObjectId
            && progress.Binding.Card.Role == CampaignCardRole.Obligation && progress.Binding.Seat == player) != true) return;
        // pack:mc10:obligations-in-player-decks: immediately put the drawn
        // obligation into the player's play area; do not draw a replacement.
        Area from = card.Area;
        Area destination = world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(player));
        World.MoveToTop(card, destination);
        card.TurnFaceUp();
        events.Add(new CardsMoved(Places.Reference(from), Places.Reference(destination),
            [new Landing(card.ObjectId, destination.Cards.Count - 1)]) { Trigger = trigger, Verb = "Put_Into_Play" });
        Reveal.EnterPlay(world, world.Facts, card, events);
        CardEntry.Entered(world, card, player);
    }
}
