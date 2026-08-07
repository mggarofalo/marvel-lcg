from . import *

# Critical Hit

def GetAbilities() -> Sequence['Ability']:

    def critical_hit(effect: 'Effect', message: 'Message.AfterUnitAttackUnit') -> None:
        this = effect.this.CastTo(Event)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Stunned", effect)


    return [
        AbilityFactory.AfterUnitAttackUnit(
            AbilityType.HeroResponse,
            "You",
            Enemy,
            critical_hit
        ).SetPlay(only_if_there_is_side_scheme_in_victory=True)
        .SetLabel()
        .SetTarget("Targets", canbe_stunned=True),
    ]

