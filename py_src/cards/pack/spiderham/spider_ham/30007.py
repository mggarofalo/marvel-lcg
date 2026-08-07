from . import *

# Swinging Web Pig

def GetAbilities() -> Sequence['Ability']:

    def swinging_web_pig(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.DealDamage(effect.targets, 6, effect)
        Faces.GiveStatus(effect.targets, "Confused", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            swinging_web_pig
        ).SetPlay().SetLabel('attack')
        .SetTarget(Enemy),
    ]

