from . import *

# Interdimensional Plunder

def GetAbilities() -> Sequence['Ability']:

    def interdimensional_plunder_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        value = Faces.FindCardSize(Worlds.GetOnFieldCards(effect), CardFinder(card_type=Upgrade))
        this.PlaceThreatOnSchemes([this], value, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            interdimensional_plunder_revealed
        ),
    ]

