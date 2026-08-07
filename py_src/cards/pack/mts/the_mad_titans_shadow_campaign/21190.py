from . import *

# * Lady Sif

def GetAbilities() -> Sequence['Ability']:

    def lady_sif(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            lady_sif
        ).SetCost(Cost("R"))
        .SetTarget("This", canbe_ready=True),
    ]

