from . import *

# * Winter Mask

def GetAbilities() -> Sequence['Ability']:

    def winter_mask(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            trait="SPY"
        ),
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.HeroResponse,
            "You",
            Enemy,
            winter_mask
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

