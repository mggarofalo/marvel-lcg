from . import *

# Telepathy

def GetAbilities() -> Sequence['Ability']:

    def telepathy(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 2, effect)


    return [
        AbilityFactory.CanPlayThisUpgradeCard(
        ).SetPlay(only_if_your_identity_has_trait="PSIONIC"),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            telepathy
        ).SetLabel('thwart')
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCost(Cost("BB"))
        .SetTarget(Scheme2),
    ]

