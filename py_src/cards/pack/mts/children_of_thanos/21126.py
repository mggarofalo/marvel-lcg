from . import *

# * Proxima Midnight

def GetAbilities() -> Sequence['Ability']:

    def proxima_midnight_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, ally=True, upgrade=True)

    return [
        AbilityFactory.UnitAttackGainKeyword(
            "This",
            piercing=True
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            proxima_midnight_boost
        ),
    ]

