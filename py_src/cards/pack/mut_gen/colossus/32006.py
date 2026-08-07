from . import *

# Organic Steel

def GetAbilities() -> Sequence['Ability']:

    def organic_steel(effect: 'Effect', message: 'Message.AfterStatusDiscardFrom') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)


    return [
        AbilityFactory.AfterStatusDiscardFrom(
            AbilityType.HeroResponse,
            CardFinder(name="Colossus"),
            "Tough",
            organic_steel
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'steel'))
        .SetTarget("Trigger", canbe_tough=True)
    ]

