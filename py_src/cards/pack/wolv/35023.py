from . import *

# Fastball Special

def GetAbilities() -> Sequence['Ability']:

    def fastball_special(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = 0
        for target in effect.targets:
            if HasAttack.IsType(target):
                value += target.attack

        this.DealDamage(effect.targets2, value, effect, property=AttackProperty(overkill=True, piercing=True))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            fastball_special
        ).SetPlay().SetLabel('attack')
        .SetTarget("TeamUp")
        .SetTarget2(Enemy)
    ]

