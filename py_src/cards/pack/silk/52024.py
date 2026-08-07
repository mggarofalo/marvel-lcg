from . import *

# Investigative Journalism

def GetAbilities() -> Sequence['Ability']:

    def investigative_journalism(effect: 'Effect', message: 'Message.WhenUnitWouldScheme') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.SetBeInstead(effect)
        Faces.GiveStatus(effect.targets2, "Confused", effect)


    return [
        AbilityFactory.WhenUnitWouldScheme(
            AbilityType.AlterEgoInterrupt,
            Enemy,
            investigative_journalism
        ).SetPlay().SetLabel()
        .SetTarget("TeamUp")
        .SetTarget2("Trigger", canbe_confused=True),
    ]

