from . import *

# Metacidal Tendencies

def GetAbilities() -> Sequence['Ability']:

    def metacidal_tendencies_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        faces = Worlds.FindCardsOnField(
            effect,
            trait="DEADPOOL CORPS"
        )

        if Worlds.FindCardOnField(effect, name="Dreadpool"):
            value = 3
        else:
            value = 2

        if not this.DealDamage(faces, value, effect):
            scheme = Worlds.FindMainScheme(effect)
            if scheme:
                scheme.PlaceAccelerationToken(1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            metacidal_tendencies_revealed
        ),
    ]

