from . import *

# Thunderclap

def GetAbilities() -> Sequence['Ability']:

    def thunderclap(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            thunderclap
        ).SetPlay()
        .SetTarget(Enemy, range=(1, 3))
    ]

