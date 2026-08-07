from . import *

# Blood Rage

def GetAbilities() -> Sequence['Ability']:

    def blood_rage(effect: 'Effect', message: 'Message.AfterUnitDefeatedUnit') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.DrawUp(1, effect)


    return [
        AbilityFactory.AfterUnitDefeatedUnit(
            AbilityType.Response,
            "You",
            Enemy,
            blood_rage,
            is_basic_attack=True,
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetCostFunc(CostFunc.TakeDamage(1, "YourIdentity")),
    ]

