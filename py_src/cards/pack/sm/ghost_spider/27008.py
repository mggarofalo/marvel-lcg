from . import *

# Ticket to the Multiverse

def GetAbilities() -> Sequence['Ability']:

    def ticket_to_the_multiverse(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DiscardAllHands(effect)
        Faces.ShuffleAllTo(initiator.discard_pile.Get(True), initiator.player_deck, effect)
        initiator.DrawUp(initiator.GetIdentity().hand_size, effect)
        Faces.ReadyAll([x for x in initiator.GetControlCards() if x.paper.IsFromSet("Ghost-Spider")], effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            ticket_to_the_multiverse
        ).SetCostFunc(CostFunc.RemoveFromGame("This")),
    ]

