from . import *

# Haymaker

def GetAbilities() -> Sequence['Ability']:

    def haymaker(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        this.DealDamage(effect.targets, 3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            haymaker
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

