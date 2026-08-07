from . import *

# Speed Cyclone

def GetAbilities() -> Sequence['Ability']:

    def speed_cyclone(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        cost = effect.GetCostX()
        targets = effect.targets[:cost]
        Faces.GiveStatus(targets, "Stunned", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            speed_cyclone
        ).SetPlay()
        .SetTarget(Enemy, range=(1, "All"))
    ]

