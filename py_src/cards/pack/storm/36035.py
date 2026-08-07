from . import *

# Hangar Bay

def GetAbilities() -> Sequence['Ability']:

    def hangar_bay(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.ReadyAll(effect.targets, effect)


    return [
        AbilityFactory.AfterUnitDefendAgainstAttack(
            AbilityType.Response,
            Ally,
            hangar_bay,
            conditions=[
                lambda effect, message:
                    # message.defender != None and \
                    not message.defender.IsDefeated(),
            ]
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("Trigger"),
    ]

