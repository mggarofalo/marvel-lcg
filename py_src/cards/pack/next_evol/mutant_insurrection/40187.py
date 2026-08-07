from . import *

# * Reaper

def GetAbilities() -> Sequence['Ability']:

    def reaper(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()
        if player:
            identity = player.GetIdentity()
            b = FacesCounter.GetPrintedResourcesCount(player.hand_cards.Get(), "B")
            if b >= 2:
                Faces.GiveStatus([identity], "Stunned", effect)
            if b >= 4:
                Faces.ExhaustAll([identity], effect)


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "This",
            reaper,
            against_who="You"
        ),
    ]

