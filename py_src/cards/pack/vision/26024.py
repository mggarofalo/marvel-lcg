from . import *

# Reboot

def GetAbilities() -> Sequence['Ability']:

    def reboot(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)
        this.HealthUnits(effect.targets, 1, effect)


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.Action,
            reboot
        ).SetPlay().SetLabel()
        .SetTarget(Friend, trait="ANDROID", check_fn=lambda effect, face:
            Friend.IsType(face) and (face.CanReady() or face.CanHeath())),
    ]

