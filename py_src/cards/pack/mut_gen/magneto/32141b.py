from . import *

# Asteroid M B

def GetAbilities() -> Sequence['Ability']:

    def asteroid_m(effect: 'Effect', message: 'Message.AfterCardPlacedCounter') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        if this.GetCounters('magnet') >= 3:

            Faces.RemoveCountersOn([this], 3, 'magnet', effect)

            face = Worlds.DiscardEncounterCardsUntil(
                effect,
                trait="MAGNETIC",
            )
            if face:
                face.Reveal(None, effect)

    return [
        AbilityFactory.AfterCounterPlacedOn(
            AbilityType.ForcedResponse,
            "This",
            'magnet',
            asteroid_m,
            at_least=3
        ),
    ]

