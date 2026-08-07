from . import *

# Quick Quip

def GetAbilities() -> Sequence['Ability']:

    def quick_quip(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Confused", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            quick_quip
        ).SetPlay().SetLabel()
        .SetCostFunc(CostFunc.DealDamage(1, "YouControlUnit", trait="WEB-WARRIOR"))
        .SetTarget(Enemy, canbe_confused=True, range=(1, 2)),
    ]

