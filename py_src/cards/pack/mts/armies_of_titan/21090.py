from . import *

# Outrider

def GetAbilities() -> Sequence['Ability']:

    def outrider_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardRandomHandCards(1, effect)

    def outrider_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardRandomHandCards(1, effect)

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            outrider_revealed
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            outrider_boost
        ),
    ]

