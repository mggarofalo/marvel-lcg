from . import *

# * Belasco

def GetAbilities() -> Sequence['Ability']:

    def belasco(effect: 'Effect', message: 'Message.AfterEnemyActivationEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetAgainstPlayer()

        faces = player.DiscardDeckTopCards(3, effect)

        face = Worlds.FindCardOnField(
            effect,
            name="Ruler of Limbo",
            card_type=SchemeSide2
        )

        if face:
            face.PlaceCardHere(faces, False, effect)


    return [
        AbilityFactory.AfterEnemyActivationEnd(
            AbilityType.ForcedResponse,
            "This",
            belasco
        ),
    ]

