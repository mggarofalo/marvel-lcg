from . import *

# Unbridled Ambition

def GetAbilities() -> Sequence['Ability']:

    def unbridled_ambition(effect: 'Effect', message: 'Message.WhenPhaseBegin') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        EachPlayerMustResolveFoulPlayAbility(effect)


    return [
        AbilityFactory.WhenVillainPhaseBegin(
            AbilityType.ForcedInterrupt,
            unbridled_ambition
        )
    ]

