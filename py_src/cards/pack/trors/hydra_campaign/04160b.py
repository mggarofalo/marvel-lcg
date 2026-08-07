from . import *

# Improved Attack Upgrade

def GetAbilities() -> Sequence['Ability']:

    def improved_attack_upgrade(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            "You",
            health=1,
        ),
        *AbilityFactory.GiveKeywordToAttached(
            Hero,
            attack=1,
        ),
        AbilityFactory.AfterUnitDefeatedUnit(
            AbilityType.HeroResponse,
            "You",
            Minion,
            improved_attack_upgrade
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

