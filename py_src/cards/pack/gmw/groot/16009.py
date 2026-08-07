from . import *

# Lashing Vines

def GetAbilities() -> Sequence['Ability']:

    def lashing_vines(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.HeroResponse,
            CardFinder(name="Groot"),
            lashing_vines
        ).SetCostFunc(CostFunc.Counter(Select.From("Trigger"), 2, 'growth'))
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Trigger", canbe_ready=True),
    ]

