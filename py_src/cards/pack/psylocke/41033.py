from . import *

# Telekinesis

def GetAbilities() -> Sequence['Ability']:

    def telekinesis(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
        ).SetPlay(only_if_your_identity_has_trait="PSIONIC"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            telekinesis
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCost(Cost("BB"))
        .SetTarget(Enemy),
    ]

