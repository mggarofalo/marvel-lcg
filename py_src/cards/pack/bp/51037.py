from . import *

# * White Wolf: Hunter

def GetAbilities() -> Sequence['Ability']:

    def white_wolf(effect: 'Effect', message: 'Message.AfterUnitAttackEnd') -> None:
        this = effect.this.CastTo(Ally)
        Unused(this)

        this.PlaceThreatOnSchemes("MainScheme", 1, effect)


    return [
        AbilityFactory.AfterUnitAttackEnd(
            AbilityType.ForcedResponse,
            "This",
            white_wolf
        ),
    ]

