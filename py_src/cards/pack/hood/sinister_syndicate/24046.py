from . import *

# * Speed Demon

def GetAbilities() -> Sequence['Ability']:

    def speed_demon(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        this.BasicAttack([message.attacker], effect)


    def speed_demon_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()
        player.DiscardControlCards(effect, support=True, lowest_cost=True)


    return [
        AbilityFactory.WhenUnitWouldBeAttacked(
            AbilityType.ForcedInterrupt,
            "This",
            speed_demon
        ),
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            speed_demon_boost
        ),
    ]

