from . import *

# Adrenal Stims

def GetAbilities() -> Sequence['Ability']:

    def adrenal_stims(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        this.HealthUnits(effect.targets, 5, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            adrenal_stims
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetCostFunc(CostFunc.RemoveFromCampaignLog("This"))
        .SetTarget("YourHero", finder=
            CardFinder(canbe_heal=True)|
            CardFinder(canbe_ready=True)
        ),
    ]

