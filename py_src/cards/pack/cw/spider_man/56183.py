from . import *

# Neighborhood Hero

def GetAbilities() -> Sequence['Ability']:

    def neighborhood_hero_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        # player = message.GetToPlayer()
        # identity = player.GetIdentity()

        if Worlds.IsCompetitiveMode(effect):
            ...
        else:
            this.PlaceThreatOnSchemes([this], "2*", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            neighborhood_hero_revealed
        ),
    ]

