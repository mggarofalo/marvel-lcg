from . import *

# * SP//dr: Peni Parker

def GetAbilities() -> Sequence['Ability']:

    def sp_dr(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.GainCard(this, effect)

    return [
        AbilityFactory.CanPlayThisAllyCard(
        ).SetPlay(only_if_you_control_trait="WEB-WARRIOR"),
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            sp_dr,
            by_consequential=True,
            took_excess_damage=True,
        ),
    ]

