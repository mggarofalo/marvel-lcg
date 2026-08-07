from . import *

# Pinned Down

def GetAbilities() -> Sequence['Ability']:

    def pinned_down(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        count = 0
        for player in Worlds.GetPlayers(effect):
            count += player.obligations_area.GetSize()

        this.PlaceThreatOnSchemes([this], 2*count, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            pinned_down
        )
    ]

