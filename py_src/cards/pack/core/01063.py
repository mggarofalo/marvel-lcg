from . import *

# Interrogation Room

def GetAbilities() -> Sequence['Ability']:

    def interrogation_room(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)
        this.RemoveThreatFromSchemes(effect.targets, 1, effect)


    return [
        # TODO: Check multiple player
        AbilityFactory.AfterUnitDefeatedUnit(
            AbilityType.Response,
            "You",
            Minion,
            interrogation_room
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget(Scheme2)
        # .SetDesc("exhaust Interrogation Room → remove 1 threat from a scheme"),
    ]

