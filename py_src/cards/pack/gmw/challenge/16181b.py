from . import *

# Guerrilla Tactics

def GetAbilities() -> Sequence['Ability']:

    def guerrilla_tactics(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        ship = FindNebulaShip(effect)
        if ship:
            Faces.PlaceCountersOn([ship], 3, 'evasion', effect)


    return [
        AbilityFactory.ExpertModeOnly(),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            guerrilla_tactics,
        ),
    ]

