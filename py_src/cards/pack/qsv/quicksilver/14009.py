from . import *

# Friction Resistance

def GetAbilities() -> Sequence['Ability']:

    def friction_resistance(effect: 'Effect', message: 'Message.AfterCardReady') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterCardReady(
            AbilityType.HeroResponse,
            CardFinder(name="Quicksilver"),
            friction_resistance,
            by_player="You",
        ).SetTarget("This", canbe_ready=True),
        AbilityFactory.CanGenerateResources(
            AbilityType.Resource,
            Resources("R")
        ).SetCostFunc(CostFunc.Exhaust("This"))
    ]

