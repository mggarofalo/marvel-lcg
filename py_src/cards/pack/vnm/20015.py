from . import *

# Sonic Rifle

def GetAbilities() -> Sequence['Ability']:

    def sonic_rifle(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        for target in effect.targets:

            enemy = target.CastTo(Enemy)

            if enemy.IsConfused():
                this.DealDamage([enemy], 3, effect)
            else:
                Faces.GiveStatus([enemy], "Confused", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            sonic_rifle
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'charge'))
        .SetTarget(Enemy),
    ]

