from . import *

# Scare Tactic

def GetAbilities() -> Sequence['Ability']:

    def scare_tactic(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 3, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            scare_tactic
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy, is_confused=True),
    ]

