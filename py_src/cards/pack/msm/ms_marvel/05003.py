from . import *

# Big Hands

def GetAbilities() -> Sequence['Ability']:

    def big_hands(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 4, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            big_hands
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

