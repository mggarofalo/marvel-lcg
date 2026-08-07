from . import *

# Acrobatic Move

def GetAbilities() -> Sequence['Ability']:

    def acrobatic_move(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 2, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            acrobatic_move
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

