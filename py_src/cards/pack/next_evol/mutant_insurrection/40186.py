from . import *

# * Forearm

def GetAbilities() -> Sequence['Ability']:

    def forearm(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            value = FacesCounter.GetPrintedResourcesCount(player.hand_cards.Get(), "R")
            player.DiscardDeckTopCards(
                value,
                effect
            )


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "This",
            forearm,
            against_who="You"
        ),
    ]

