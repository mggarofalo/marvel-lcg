from . import *

# Superdense Strike

def GetAbilities() -> Sequence['Ability']:

    def superdense_strike(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 5, effect, property=AttackProperty(piercing=True))


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            superdense_strike
        ).SetPlay(only_if_you_are_in_additional_from="Dense").SetLabel('attack')
        .SetTarget(Enemy),
    ]

