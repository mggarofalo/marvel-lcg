from . import *

# Mission Planning

def GetAbilities() -> Sequence['Ability']:

    def mission_planning(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        initiator = effect.GetInitiator()
        this.effect.RegisterTemp(
            AbilityFactory.WhenUnitWouldTakeDamage(
                AbilityType.Temp0,
                Ally,
                lambda effect, message:
                    message.SetBeInstead(effect),
                is_consequential_damage=True,
                control_by=initiator
            ),
            unregister_after_exec=False,
            until_phase_end=True
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.HeroAction,
            mission_planning
        ).SetPlay(only_if_there_is_side_scheme_in_victory=True).SetLabel(),
    ]

