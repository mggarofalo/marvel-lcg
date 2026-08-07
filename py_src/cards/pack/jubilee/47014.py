from . import *

# Waylay

def GetAbilities() -> Sequence['Ability']:

    def waylay(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        damage = 4
        if message.remove_the_last_threat:
            damage = 7
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.HeroResponse,
            "YourHero",
            waylay
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

