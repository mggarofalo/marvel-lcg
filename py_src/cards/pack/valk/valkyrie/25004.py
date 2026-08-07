from . import *

# * Valhalla

def GetAbilities() -> Sequence['Ability']:

    def valhalla(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)
        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.Response,
            CardFinder(name="Valkyrie"),
            HAS_DEATH_GLOW_FINDER,
            valhalla,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Trigger"),
    ]

