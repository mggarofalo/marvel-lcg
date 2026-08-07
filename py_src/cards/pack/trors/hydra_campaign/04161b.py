from . import *

# Improved Defense Upgrade

def GetAbilities() -> Sequence['Ability']:

    def improved_defense_upgrade(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)

    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=3,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            defense=1,
        ),
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.HeroResponse,
            "You",
            improved_defense_upgrade
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

