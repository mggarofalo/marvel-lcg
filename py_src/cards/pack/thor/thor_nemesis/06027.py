from . import *

# Family Feud

def GetAbilities() -> Sequence['Ability']:

    def family_feud_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        value = Worlds.FindCardSizeOnField(effect, trait="ASGARD")
        this.PlaceThreatOnSchemes([this], value, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            family_feud_revealed
        ),
    ]

