from . import *

# Foiled!

def GetAbilities() -> Sequence['Ability']:

    def foiled(effect: 'Effect', message: 'Message.WhenBoostCardTurnedFaceUp') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.CancelAllBoostIcons(effect)

    return [
        AbilityFactory.WhenBoostCardTurnedFaceUp(
            AbilityType.Interrupt,
            "BoostIcons",
            foiled,
            while_scheme=True,
        ).SetPlay().SetLabel(),
    ]

