from . import *

# Booster Boots

def GetAbilities() -> Sequence['Ability']:

    def booster_boots(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.PreventDamage(1, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
        ).SetPlay(only_if_your_identity_has_trait="GUARDIAN"),
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            "You",
            booster_boots,
            is_from_attack=True,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Discard("YourPlayerDeckTop"))
        .SetTarget("This"),
    ]

