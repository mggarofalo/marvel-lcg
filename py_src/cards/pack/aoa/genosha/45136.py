from . import *

# Genoshan Mech

def GetAbilities() -> Sequence['Ability']:

    def genoshan_mech(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.target.GetControlByPlayer()
        ResolveSpecialAbilityOnTheSETTINGEnvironment(player, effect)
        ResolveSpecialAbilityOnTheSETTINGEnvironment(player, effect)


    return [
        AbilityFactory.AfterUnitAttackAndDefeatUnit(
            AbilityType.ForcedResponse,
            "This",
            Ally,
            genoshan_mech
        ),
    ]

