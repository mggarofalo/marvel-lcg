from . import *

# Behind Enemy Lines

def GetAbilities() -> Sequence['Ability']:

    def behind_enemy_lines(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        this.RemoveThreatFromSchemes(effect.targets, 3, effect)
        if effect.GetPaidResources().OnlyHasColor("B"):
            Faces.GiveStatus(effect.targets2, "Confused", effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            behind_enemy_lines
        ).SetPlay().SetLabel('thwart')
        .SetTarget(Scheme2)
        .SetTarget2(Enemy, canbe_confused=True),
    ]

