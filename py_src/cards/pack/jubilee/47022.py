from . import *

# Unlikely Duo

def GetAbilities() -> Sequence['Ability']:

    def unlikely_duo(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets2, "Confused", effect)
        this.DealDamage(effect.targets3, 4, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            unlikely_duo
        ).SetPlay().SetLabel('attack')
        .SetTarget("TeamUp")
        .SetTarget2(Enemy, canbe_confused=True)
        .SetTarget3(Enemy, is_confused=True)
    ]

