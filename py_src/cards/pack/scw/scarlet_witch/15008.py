from . import *

# Magic Shield

def GetAbilities() -> Sequence['Ability']:

    def magic_shield(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.PreventDamage(3, effect)

    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            Friend,
            magic_shield
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

