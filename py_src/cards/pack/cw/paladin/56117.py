from . import *

# * Paladin

def GetAbilities() -> Sequence['Ability']:

    def paladin_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        identity = player.GetIdentity()

        if identity.HasTrait("UNREGISTERED"):
            player.DiscardRandomHandCards(1, effect)
        else:
            player.DiscardHandCards((1, 1), effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            paladin_revealed
        ),
    ]

