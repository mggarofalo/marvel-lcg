using static Marvel.Cards.Run.AbilityCardStateExecution;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Executes card effects whose only authority is live card state.</summary>
/// <remarks>The runner retains prompts, agenda continuations, and resolution-ledger
/// bookkeeping. This owner receives immutable expression bindings and emits only
/// committed state changes and semantic events.</remarks>
internal static class AbilityCardMovementExecution
{
    internal static void AddToHand(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        var card = Find(selection, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' cannot find the card added to hand");
        MoveToHand(card, context.World.Seats[context.Player].Hand, "Add_To_Hand", context, linked: true);
    }

    internal static void ReturnOwnedToDiscard(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        foreach (var card in Every(selection, context))
        {
            if (card.Owner < 0) throw new RulesNotImplementedException("a returned physical card has no owning player");
            Rules.Play.Discard.Card(context.World, card, context.Trigger, context.Events, verb: "Return_To_Discard");
        }
    }

    internal static void ReturnOwnedToHand(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        var card = Find(selection, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' cannot find the card returned to hand");
        if (card.Owner < 0) throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' returns a card with no owning player");
        MoveToHand(card, context.World.Seats[card.Owner].Hand, "Return", context, linked: false);
    }

    internal static void ReturnToHand(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        foreach (var card in Every(selection, context))
        {
            var from = card.Area;
            MoveToHand(card, context.World.Seats[card.Owner].Hand, "Return", context, linked: false);
            card.TurnFaceUp();
            context.Events.Add(new CardDetached(card.ObjectId, from.Host)
            { Trigger = context.Trigger, Verb = "Return" });
        }
    }

    internal static void MoveToHand(Card card, Area hand, string verb, AbilityCardStateContext context, bool linked)
    {
        var from = card.Area;
        var ending = context.World.Effects.PreflightConstantsEnding(card);
        using var departure = ending.Begin();
        if (DeckTypes.IsInPlay(from.Type)) Rules.Play.Discard.Attachments(context.World, card, context.Trigger, context.Events);
        if (linked && !Characteristics.IsLost(context.World, card, "linked")
            && context.World.Facts.Attributes(card.FaceId).ContainsKey("Linked")) card.TransferLinkedOwnership(context.Player);
        World.MoveToTop(card, hand);
        context.Events.Add(new CardsMoved(Places.Reference(from), Places.Reference(hand),
            [new Landing(card.ObjectId, hand.Cards.Count - 1)])
        { Trigger = context.Trigger, Verb = verb });
        ending.Complete(context.Trigger, context.Events);
    }

    internal static void AttachTo(AbilityCardSelection selection, AbilityCardStateContext context)
    {
        var host = Find(selection, context) ?? throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' attaches to a card that is not there");
        var from = context.Source.Area;
        var onto = context.World.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId, host.Area.CardOwner);
        World.MoveToTop(context.Source, onto);
        context.Events.Add(new CardsMoved(Places.Reference(from), Places.Reference(onto),
            [new Landing(context.Source.ObjectId, onto.Cards.Count - 1)])
        { Trigger = context.Trigger, Verb = "Attach" });
        context.Events.Add(new CardAttached(context.Source.ObjectId, host.ObjectId) { Trigger = context.Trigger, Verb = "Attach" });
    }

}
