from . import *

# Crime State

def GetAbilities() -> Sequence['Ability']:

    def crime_state(effect: 'Effect', message: 'Message.AfterResolveVillainPhaseStep') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        EachPlayerMustResolveFoulPlayAbility(effect)


    return [
        AbilityFactory.AfterResolveVillainPhaseStep(
            AbilityType.ForcedResponse,
            1,
            crime_state
        ),
    ]

