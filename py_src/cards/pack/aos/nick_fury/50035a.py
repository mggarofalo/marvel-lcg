from . import *

# Assault

def GetAbilities() -> Sequence['Ability']:

    def assault(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        value = effect.cost_func.Get(CostFunc.RemoveTokens).return_removed_tokens
        message.DealAdditionalDamage(value, effect)


    return [
        AbilityFactory.WhenUnitWouldAttack(
            AbilityType.Interrupt,
            "You",
            assault
        ).SetCostFunc(CostFunc.RemoveTokens("This", (1, 3), 'threat')),
    ]

