from . import *

# Uppercut

def GetAbilities() -> Sequence['Ability']:

    def uppercut(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        this.DealDamage(effect.targets, 5, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            uppercut
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

