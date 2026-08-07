from . import *

# En Sabah Nur's Pyramid

def GetAbilities() -> Sequence['Ability']:

    def en_sabah_nurs_pyramid(effect: 'Effect', message: 'Message.AfterResolveVillainPhaseStep') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        Faces.PlaceCountersOn([this], 1, 'power', effect)

        if this.GetCounters('power') >= 4:
            player = Worlds.GetFirstPlayer(effect)
            Faces.RemoveCountersOn([this], 4, 'power', effect, by_player=player)
            face = Worlds.DiscardEncounterCardsUntil(
                effect,
                trait="SUPERPOWER",
            )
            if face:
                face.Reveal(player, effect)

    return [
        AbilityFactory.AfterResolveVillainPhaseStep(
            AbilityType.ForcedResponse,
            1,
            en_sabah_nurs_pyramid
        ),
    ]

