from . import *

# * Hard-Drive

def GetAbilities() -> Sequence['Ability']:

    def hard_drive(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        this.PlaceThreatOnSchemes("EachScheme", 1, effect)


    return [
        AbilityFactory.AfterUnitAttackYou(
            AbilityType.ForcedResponse,
            "This",
            hard_drive
        ),
    ]

