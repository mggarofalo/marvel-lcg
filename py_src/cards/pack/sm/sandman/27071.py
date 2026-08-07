from . import *

# Sand Storm

def GetAbilities() -> Sequence['Ability']:

    def sand_storm_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        city_streets = GetCityStreets(effect)
        if city_streets:
            sand = city_streets.GetCounters('sand')
            if sand:
                player.AssignDamage(Worlds.GetOnFieldFriendlyCharacters(effect), this, sand, effect)
            else:
                Faces.PlaceCountersOn([city_streets], 3, 'sand', effect)
                encounter_deck = Worlds.GetEncounterDeck(effect)
                Faces.ShuffleAllTo([this], encounter_deck, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            sand_storm_revealed
        ),
    ]

