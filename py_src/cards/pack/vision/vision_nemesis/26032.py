from . import *

# Relentless Android

def GetAbilities() -> Sequence['Ability']:

    def relentless_android_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()
        face = Worlds.FindCardOnField(
            effect,
            name="Ultron Drones"
        )
        if face:
            CreateUltronFacedownDrone(player, 2, effect)
        else:
            player.DiscardRandomHandCards(2, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            relentless_android_revealed
        ),
    ]

