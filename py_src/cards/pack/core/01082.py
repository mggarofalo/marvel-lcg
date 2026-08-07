from . import *

# Indomitable

def GetAbilities() -> Sequence['Ability']:

    def indomitable(effect: 'Effect', message: 'Message.AfterUnitDefendEnd') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        hero = initiator.GetHero()

        Faces.ReadyAll([hero], effect)


    return [
        AbilityFactory.AfterUnitDefendEnd(
            AbilityType.Response,
            "YourHero",
            indomitable
        ).SetCostFunc(CostFunc.Discard("This"))
        .SetTarget("YourHero", canbe_ready=True)
    ]

