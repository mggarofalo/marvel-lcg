from . import *

# Change of Fortune

def GetAbilities() -> Sequence['Ability']:

    def change_of_fortune(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(2, effect)

        AbilityFactory.WhenVillainPhaseBegin


    return [
        AbilityFactory.AfterUnitDefeatedUnit(
            AbilityType.Response,
            "You",
            Enemy,
            change_of_fortune,
            conditions=[
                lambda effect, message:
                    Worlds.Phase(effect).IsVillainPhase()
            ],
        ).SetCostFunc(CostFunc.Exhaust("This")),
    ]

