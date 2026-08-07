from . import *

# Improved Thwart Upgrade

def GetAbilities() -> Sequence['Ability']:

    def improved_thwart_upgrade(effect: 'Effect', message: 'Message.AfterUnitDefeatedScheme') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=2,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            thwart=1,
        ),
        AbilityFactory.AfterUnitDefeatedScheme(
            AbilityType.Response,
            "You",
            SchemeSide2,
            improved_thwart_upgrade
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

