using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static bool TryRunCardMovement(AbilityEffect instruction, Cast cast)
    {
        switch (instruction)
        {
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.RemoveFromGame } removal:
                RemoveFromGame(removal.Selection, cast);
                return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Discard } discard:
                Discard(discard.Selection, cast);
                return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.AttachTo } attachment:
                AttachTo(attachment.Selection, cast);
                return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.ReturnToHand } returned:
                ReturnToHand(returned.Selection, cast);
                return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.Reveal } reveal:
                RevealCard(ResolveCard(reveal.Selection, cast), cast);
                return true;
            case AbilityEffect.Fixed { Instruction: AbilityFixedInstruction.RevealTop }:
                RevealCard(TopOfTheEncounterDeck(cast), cast);
                return true;
            case AbilityEffect.DealEncounterCards effect:
                DealEncounterCards(effect, cast);
                return true;
            case AbilityEffect.CreateDrones effect:
                CreateDrones(effect, cast);
                return true;
            case AbilityEffect.PlaceAtRandom effect:
                PlaceAtRandom(effect, cast);
                return true;
            case AbilityEffect.DiscardAtRandom effect:
                DiscardAtRandom(effect, cast);
                return true;
            case AbilityEffect.DiscardUntil effect:
                DiscardUntil(effect, cast);
                return true;
            case AbilityEffect.DiscardTop effect:
                DiscardTop(effect, cast);
                return true;
            case AbilityEffect.RecoverDiscardedByResource effect:
                RecoverDiscardedByResource(effect, cast);
                return true;
            case AbilityEffect.ShuffleInto effect:
                ShuffleInto(effect, cast);
                return true;
            case AbilityEffect.Search effect:
                Search(effect, cast);
                return true;
            case AbilityEffect.PutIntoPlay effect:
                PutIntoPlay(effect, cast);
                return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.AddToHand } added:
                AddToHand(added.Selection, cast);
                return true;
            case AbilityEffect.CardAction { Instruction: AbilityCardInstruction.ReturnOwnedToHand } returned:
                ReturnOwnedToHand(returned.Selection, cast);
                return true;
            default:
                return false;
        }
    }

    private static void AddToHand(AbilityCardSelection selection, Cast cast)
    {
        var added = ResolveCard(selection, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot find the card added to hand");
        var oldArea = added.Area;
        var newHand = cast.World.Seats[cast.Player].Hand;
        var addedConstantsEnding = cast.World.Effects.PreflightConstantsEnding(added);
        using var addedDeparture = addedConstantsEnding.Begin();
        if (DeckTypes.IsInPlay(oldArea.Type))
        {
            Rules.Play.Discard.Attachments(
                cast.World, added, cast.Trigger, cast.Events);
        }
        if (!Characteristics.IsLost(cast.World, added, "linked")
            && cast.World.Facts.Attributes(added.FaceId).ContainsKey("Linked"))
        {
            // rr:linked-card-title.4 changes ownership at the moment
            // the player takes control. A linked ally added from the
            // set-aside area reaches their hand before it enters play.
            added.TransferLinkedOwnership(cast.Player);
        }
        World.MoveToTop(added, newHand);
        cast.Events.Add(new CardsMoved(
            Places.Reference(oldArea), Places.Reference(newHand),
            [new Landing(added.ObjectId, newHand.Cards.Count - 1)])
        {
            Trigger = cast.Trigger, Verb = "Add_To_Hand",
        });
        addedConstantsEnding.Complete(cast.Trigger, cast.Events);
    }

    private static void ReturnOwnedToHand(AbilityCardSelection selection, Cast cast)
    {
        var returned = ResolveCard(selection, cast)
            ?? throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' cannot find the card returned to hand");
        if (returned.Owner < 0)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' returns a card with no owning player");
        }
        var returnedFrom = returned.Area;
        var ownersHand = cast.World.Seats[returned.Owner].Hand;
        var returnedConstantsEnding =
            cast.World.Effects.PreflightConstantsEnding(returned);
        using var returnedDeparture = returnedConstantsEnding.Begin();
        if (DeckTypes.IsInPlay(returnedFrom.Type))
        {
            Rules.Play.Discard.Attachments(
                cast.World, returned, cast.Trigger, cast.Events);
        }
        World.MoveToTop(returned, ownersHand);
        cast.Events.Add(new CardsMoved(
            Places.Reference(returnedFrom), Places.Reference(ownersHand),
            [new Landing(returned.ObjectId, ownersHand.Cards.Count - 1)])
        {
            Trigger = cast.Trigger, Verb = "Return",
        });
        returnedConstantsEnding.Complete(cast.Trigger, cast.Events);
    }
}
