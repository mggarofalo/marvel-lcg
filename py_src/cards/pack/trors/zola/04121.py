from . import *

# Technological Enhancements

def GetAbilities() -> Sequence['Ability']:

    def technological_enhancements(effect: 'Effect', message: 'Message2') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        scheme = Worlds.FindMainScheme(this)
        if scheme:
            Faces.PlaceCountersOn([scheme], 1, 'test', effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            technological_enhancements
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            technological_enhancements
        )
    ]

