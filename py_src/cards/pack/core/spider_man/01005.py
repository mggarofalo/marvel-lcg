from . import *

# Swinging Web Kick

def GetAbilities() -> Sequence['Ability']:

    def swinging_web_kick(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        this.DealDamage(effect.targets, 8, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            swinging_web_kick
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy)
    ]

