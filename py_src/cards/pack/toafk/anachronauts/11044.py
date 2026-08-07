from . import *

# * Wildrun

def GetAbilities() -> Sequence['Ability']:

    def wildrun_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardRandomHandCards(1, effect)

    def wildrun_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardRandomHandCards(1, effect)
        message.GiveBoostCardForThisActivation(Enemy, 1, effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            wildrun_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            wildrun_boost
        ),
    ]

