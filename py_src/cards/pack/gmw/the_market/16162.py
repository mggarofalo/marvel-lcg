from . import *

# Armor Plating

def GetAbilities() -> Sequence['Ability']:

    def armor_plating(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        if initiator.IsControl(CardFinder(name="Milano")):
            value = 2
        else:
            value = 1
        message.PreventDamage(value, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            Identity,
            armor_plating
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

