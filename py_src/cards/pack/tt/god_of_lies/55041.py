from . import *

# * The Mangog

def GetAbilities() -> Sequence['Ability']:

    def the_mangog_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        # player = message.GetToPlayer()
        # identity = player.GetIdentity()

    def the_mangog_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        # player = message.GetToPlayer()
        # identity = player.GetIdentity()


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            the_mangog_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            the_mangog_boost
        ),
    ]

