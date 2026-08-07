from . import *

# * Front Line Specialist

def GetAbilities() -> Sequence['Ability']:

    def front_line_specialist(effect: 'Effect', message: 'Message.AfterUnitTookDamage') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        *AbilityFactory.GiveKeywordToAttached(
            Identity,
            health=4,
        ),
        AbilityFactory.AfterUnitTookDamage(
            AbilityType.Response,
            "You",
            front_line_specialist,
            is_from_attack=True,
            who_dealt_damage=Enemy,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("This"),
    ]

