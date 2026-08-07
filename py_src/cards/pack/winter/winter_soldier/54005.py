from . import *

# Metal Punch

def GetAbilities() -> Sequence['Ability']:

    def metal_punch(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        if YouExhaustedCyberneticArmToPayForThisEvent(effect):
            overkill = True
        else:
            overkill = False

        this.DealDamage(effect.targets, 7, effect, property=AttackProperty(overkill=overkill))

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            metal_punch
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

