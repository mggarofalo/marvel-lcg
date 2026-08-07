from . import *

# * Sandman

def GetAbilities() -> Sequence['Ability']:

    def sandman_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterVillain)
        Unused(this)

        player = message.GetToPlayer()
        city_streets = GetCityStreets(effect)
        if city_streets:
            Faces.PlaceCountersOn([city_streets], 1, 'sand', effect)
            city_streets.ResolveSpecialAbility(player, effect, name=SURGING_SANDS)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sandman_revealed
        ),
        SandBlastWave(3)
    ]

