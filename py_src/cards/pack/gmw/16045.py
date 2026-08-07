from . import *

# Follow Through

def GetAbilities() -> Sequence['Ability']:

    def follow_through(effect: 'Effect', message: 'Message.WhenCalcDealExcessDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.IncreaseDamage(1, effect)


    return [
        AbilityFactory.WhenUnitAttackDealExcessDamage(
            AbilityType.HeroInterrupt,
            "You",
            follow_through
        ).SetTarget("This"),
    ]

