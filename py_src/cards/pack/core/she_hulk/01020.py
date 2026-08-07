from . import *

# * Hellcat: Patsy Walker

def GetAbilities() -> Sequence['Ability']:

    def hellcat(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        Faces.ReturnToHand(effect.targets, initiator, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            hellcat
        ).SetTarget("This"),
    ]

