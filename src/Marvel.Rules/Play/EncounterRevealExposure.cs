using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Exposes the one encounter card whose reveal procedure has begun.</summary>
public static class EncounterRevealExposure
{
    // rr:reveal.4.1: the player a card specifies is considered to reveal it.
    // An absent named identity leaves the original player to receive the
    // replacement encounter card under rr:obligation.5.
    internal static int RevealingPlayer(World world, Card card, int dealtPlayer) =>
        RevealKeywords.Names(world, world.Facts, card) is >= 0 and var named ? named : dealtPlayer;

    internal static void Prepare(World world, Card card, int player, List<GameEvent> events)
    {
        if (card.Area.Type is not (DeckType.DealtEncounterCardsDeck or DeckType.RevealingArea)) return;
        Expose(world, card, "villain phase", events);
        int revealing = RevealingPlayer(world, card, player);
        var area = world.AreaOf(DeckType.RevealingArea, PlayArea.Of(revealing));
        if (card.Area != area) World.MoveToTop(card, area);
    }

    /// <summary>Publishes a concealed encounter face before any move can expose it implicitly.</summary>
    /// <remarks>
    /// <c>rr:reveal.step.1</c>: "Turn the encounter card faceup." This information
    /// precedes interrupts to its When Revealed effects; those may be cancelled
    /// while the card remains revealed (<c>rr:cancel.4</c>). Re-reading an already
    /// exposed card adds neither another flip event nor another information signal.
    /// </remarks>
    public static void Expose(World world, Card card, string trigger, List<GameEvent> events)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(events);
        if (card.FaceUp) return;
        card.TurnFaceUp();
        world.RecordInformation(InformationKind.Reveal);
        events.Add(new CardsFlipped([card.ObjectId], true)
        {
            Trigger = trigger,
            Verb = "Reveal",
        });
    }
}
