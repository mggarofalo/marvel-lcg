from . import *

# Mean Swing

def GetAbilities() -> Sequence['Ability']:

    def mean_swing(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.GainATKForThisAttack(3, effect)


    return [
        AbilityFactory.WhenUnitMakeAttack(
            AbilityType.HeroInterrupt,
            "YourHero",
            mean_swing,
            is_basic_attack=True,
        ).SetPlay()
        .SetCostFunc(CostFunc.Exhaust(
            card_type=Upgrade,
            trait="WEAPON",
            from_where=["YouControlCards"]
        )),
    ]

