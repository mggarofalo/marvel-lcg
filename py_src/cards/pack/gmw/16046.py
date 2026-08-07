from . import *

# Hand Cannon

def GetAbilities() -> Sequence['Ability']:

    def hand_cannon(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.GainATKForThisAttack(+2, effect)
        message.GainOverKill(effect)


    return [
       AbilityFactory.WhenUnitMakeAttack(
            AbilityType.HeroInterrupt,
            "YourHero",
            hand_cannon,
            is_basic_attack=True,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'charge'))
        .SetTarget("This"),
    ]

