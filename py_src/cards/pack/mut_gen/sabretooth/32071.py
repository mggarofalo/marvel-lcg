from . import *

# Medical Emergency

def GetAbilities() -> Sequence['Ability']:

    def medical_emergency(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        ally = Worlds.FindCardOnField(
            effect,
            ROBERT_KELLY_FINDER
        )
        if ally:
            this.HealthUnits([ally], 2, effect)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            medical_emergency,
        ),
    ]

