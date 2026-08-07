from . import *

# * Hela

def GetAbilities() -> Sequence['Ability']:

    def hela_revealed(effect: 'Effect', message: 'Message.AfterSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        this.FlipTo(effect, trait="MYSTIC")


    return [
        *AbilityFactory.UnitCannotBeDefeatedWhile(
            AbilityType.NonKeyword,
            "This",
        ),
        AbilityFactory.AfterSchemeBeDefeated(
            AbilityType.ForcedResponse,
            SchemeSide2,
            hela_revealed
        ),
    ]

