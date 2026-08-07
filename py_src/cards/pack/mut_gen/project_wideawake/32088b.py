from . import *

# * Jubilee: Jubilation Lee

def GetAbilities() -> Sequence['Ability']:

    def jubilee(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.FirstPlayerControlThis(),
        *AbilityFactory.ThisNotCountAllyLimit(),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            jubilee
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCost(Cost("Y"))
        .SetTarget(Enemy),
    ]

