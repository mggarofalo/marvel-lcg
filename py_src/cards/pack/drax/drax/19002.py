from . import *

# * Mantis

def GetAbilities() -> Sequence['Ability']:

    def mantis(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.HealthUnits(effect.targets, 3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            mantis
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.DealDamage(1, "This"))
        .SetTarget(Identity, canbe_heal=True)
    ]

