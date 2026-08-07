from . import *

# Dirt Trap

def GetAbilities() -> Sequence['Ability']:

    def dirt_trap(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        ResolveSurgingSandsOnCityStreets(effect)
        ResolveSurgingSandsOnCityStreets(effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            dirt_trap,
        ),
    ]

