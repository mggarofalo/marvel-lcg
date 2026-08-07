from . import *

# Rampaging Juggernaut

def GetAbilities() -> Sequence['Ability']:

    def rampaging_juggernaut_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        value = 0
        for unit in Worlds.GetOnFieldFriendlyCharacters(effect):
            value += unit.DiscardAllTough(effect)
        this.PlaceThreatOnSchemes([this], 2 * value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            rampaging_juggernaut_revealed
        ),
    ]

