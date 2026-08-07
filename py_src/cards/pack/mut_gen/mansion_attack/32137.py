from . import *

# Under Siege

def GetAbilities() -> Sequence['Ability']:

    def under_siege_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        value = 3 * Worlds.FindCardSizeOnField(effect, CardFinder2("BROTHERHOOD OF MUTANTS", Unit2))
        this.PlaceThreatOnSchemes([this], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            under_siege_revealed
        ),
    ]

