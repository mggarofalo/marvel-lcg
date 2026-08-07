from . import *

# * Hidden Base

def GetAbilities() -> Sequence['Ability']:

    def hidden_base(effect: 'Effect', message: 'Message.AfterUnitChangeForm') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        Faces.GiveStatus(effect.targets, "Tough", effect)


    return [
        AbilityFactory.AfterUnitChangeForm(
            AbilityType.AlterEgoResponse,
            "You",
            hidden_base
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.Counter("This", 1, 'stronghold'))
        .SetTarget("YourIdentity", canbe_tough=True),
    ]

