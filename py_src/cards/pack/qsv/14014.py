from . import *

# Never Back Down

def GetAbilities() -> Sequence['Ability']:

    def never_back_down(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.GainDEFForThisAttack(2, effect)

        atk_message = message.would_atk_message

        message.IfYouTakeNoDamage(
            lambda: Faces.GiveStatus([atk_message.attacker], "Stunned", effect)
        )


    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.HeroInterrupt,
            "You",
            never_back_down,
            is_basic_defense=True
        ).SetPlay().SetLabel('defense')
    ]

