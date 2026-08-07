from . import *

# Legions of Hel

def GetAbilities() -> Sequence['Ability']:

    def legions_of_hel_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        value = len(Worlds.GetOnFieldMinions(effect, CardFinder2("UNDEAD", Minion)))
        if value == 0:
            ThisCardGainSurge(effect)
        else:
            value *= 2
            this.PlaceThreatOnSchemes([this], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            legions_of_hel_revealed
        ),
    ]

