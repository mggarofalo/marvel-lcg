from . import *

# Defensive Stance

def GetAbilities() -> Sequence['Ability']:

    def defensive_stance(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.PreventDamage(3, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            "You",
            defensive_stance
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

