from . import *

# Telekinetic Force Field

def GetAbilities() -> Sequence['Ability']:

    def telekinetic_force_field(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.PreventDamage("All", effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
        ).SetPlay(hero_form_only=True),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            Friend,
            telekinetic_force_field
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

