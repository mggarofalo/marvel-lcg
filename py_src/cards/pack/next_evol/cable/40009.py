from . import *

# * Askani'son

def GetAbilities() -> Sequence['Ability']:

    def askanison(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        value = initiator.GetHero().thwart
        this.RemoveThreatFromSchemes(effect.targets, value, effect)


    return [
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.HeroResponse,
            "You",
            askanison,
            attacker=Enemy
        ).SetLabel('thwart')
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCost(Cost("Y"))
        .SetTarget(Scheme2),
    ]

