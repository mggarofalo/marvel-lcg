from . import *

# Rocket's Pistol

def GetAbilities() -> Sequence['Ability']:

    def rockets_pistol(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.ThisEnterPlayWithCounters(3, 'charge'),
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            rockets_pistol
        ).SetLabel('attack')
        .SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'charge'))
        .SetTarget(Enemy),
    ]

