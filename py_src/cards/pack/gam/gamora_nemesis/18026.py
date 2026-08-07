from . import *

# * Nebula

def GetAbilities() -> Sequence['Ability']:

    def nebula(effect: 'Effect', message: 'Message.WhenCardWouldMoveToArea') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        faces = Worlds.FindCardsOnField(
            effect,
            card_type=Ally,
            name="Nebula"
        )
        Faces.DiscardAll(faces, effect)

    return [
        AbilityFactory.WhenCardWouldEnterPlay(
            AbilityType.ForcedInterrupt,
            "This",
            nebula
        ),
    ]

