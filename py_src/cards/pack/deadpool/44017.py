from . import *

# Barely a Scratch

def GetAbilities() -> Sequence['Ability']:

    def barely_a_scratch(effect: 'Effect', message: 'Message.WhenUnitWouldTakeDamage') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        value = Worlds.GetIconsNum(effect, ["Crisis", "Acceleration", "Amplify", "Hazard"])

        message.PreventDamage(value, effect)


    return [
        AbilityFactory.WhenUnitWouldTakeDamage(
            AbilityType.HeroInterrupt,
            "You",
            barely_a_scratch,
            is_from_attack=True,
            conditions=[
                lambda effect, message:
                    Worlds.GetIconsNum(effect, ["Crisis", "Acceleration", "Amplify", "Hazard"]) > 0
            ]
        ).SetPlay().SetLabel('defense'),
    ]

