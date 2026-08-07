from . import *

# Slippery Conditions

def GetAbilities() -> Sequence['Ability']:

    def slippery_conditions(effect: 'Effect', message: 'Message.AfterCardEnterPlay') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        Faces.ExhaustAll([message.trigger], effect)


    return [
        AbilityFactory.AfterCardEnterPlay(
            AbilityType.ForcedResponse,
            Ally,
            slippery_conditions
        ),
    ]

