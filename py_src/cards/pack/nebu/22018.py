from . import *

# Brains Over Brawn

def GetAbilities() -> Sequence['Ability']:

    def brains_over_brawn(effect: 'Effect', message: 'Message.AfterUnitThwartEnd') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        damage = initiator.GetHero().thwart
        this.DealDamage(effect.targets, damage, effect)


    return [
        AbilityFactory.AfterUnitThwartEnd(
            AbilityType.HeroResponse,
            "YourHero",
            brains_over_brawn,
            is_basic_thwart=True
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

