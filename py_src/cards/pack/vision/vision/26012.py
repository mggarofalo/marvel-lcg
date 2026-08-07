from . import *

# Mass Increase

def GetAbilities() -> Sequence['Ability']:

    def mass_increase(effect: 'Effect', message: 'Message.WhenUnitWouldDefend') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        message.PreventAllDamage(effect)

        def action():
            if message.attacker:
                Faces.GiveStatus(effect.targets, "Stunned", effect)

        RunAt.AfterResolveMessage(effect, message.would_atk_message, action)


    return [
        AbilityFactory.WhenUnitDefendAgainstAttack(
            AbilityType.HeroInterrupt,
            CardFinder(name="Vision"),
            mass_increase
        ).SetPlay(only_if_you_are_in_additional_from="Dense").SetLabel('defense')
        .SetTarget("Attacker"),
    ]

