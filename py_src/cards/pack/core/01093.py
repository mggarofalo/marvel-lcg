from . import *

# Tenacity

def GetAbilities() -> Sequence['Ability']:

    def tenacity(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            tenacity
        ).SetCost(Cost("R"))
        .SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("YourIdentity", canbe_ready=True)
    ]

