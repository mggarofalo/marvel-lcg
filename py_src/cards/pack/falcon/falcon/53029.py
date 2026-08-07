from . import *

# Harlem's Protector

def GetAbilities() -> Sequence['Ability']:

    def harlems_protector(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Obligation)
        Unused(this)

        Faces.RemoveCountersOn([this], 1, 'emergency', effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            harlems_protector
        ).SetCost(Cost("1")),
    ]

