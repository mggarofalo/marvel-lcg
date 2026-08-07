from . import *

# * Beetle

def GetAbilities() -> Sequence['Ability']:

    def beetle(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, upgrade=True, lowest_cost=True)


    def beetle_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, upgrade=True)


    return [
        AbilityFactory.AfterUnitAttackAndDamageUnit(
            AbilityType.ForcedInterrupt,
            "This",
            "You",
            beetle,
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            beetle_boost
        ),
    ]

