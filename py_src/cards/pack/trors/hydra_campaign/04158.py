from . import *

# Laser Cannon

def GetAbilities() -> Sequence['Ability']:

    def laser_cannon(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 5, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            laser_cannon
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetCostFunc(CostFunc.RemoveFromCampaignLog("This"))
        .SetTarget("VillainAndYouEngagedMinion")
    ]

