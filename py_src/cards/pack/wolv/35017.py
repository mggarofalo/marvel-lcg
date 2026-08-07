from . import *

# Outta My Way!

def GetAbilities() -> Sequence['Ability']:

    def outta_my_way(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        target = [0]
        for target in effect.targets:
            value = 3
            if type(target) is Minion:
                if target.IsGuard() or target.IsPatrol():
                    value = 5
            this.DealDamage([target], value, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            outta_my_way
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

