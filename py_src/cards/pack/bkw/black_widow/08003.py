from . import *

# Covert Ops

def GetAbilities() -> Sequence['Ability']:

    def covert_ops(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 4, effect)
        Faces.GiveStatus(effect.targets2, "Confused", effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            covert_ops
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetTarget2(Villain, canbe_confused=True),
    ]

