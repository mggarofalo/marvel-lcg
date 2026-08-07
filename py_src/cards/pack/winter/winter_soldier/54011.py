from . import *

# * Winter Rifle

def GetAbilities() -> Sequence['Ability']:

    def winter_rifle(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.GainATKForThisAttack(2, effect)
        message.GainPiercing(effect)
        message.GainRanged(effect)


    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.HeroInterrupt,
            CardFinder(name="Winter Soldier"),
            winter_rifle,
            is_basic_attack=True
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

