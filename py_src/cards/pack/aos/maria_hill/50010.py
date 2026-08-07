from . import *

# Life Model Decoy

def GetAbilities() -> Sequence['Ability']:

    def life_model_decoy(effect: 'Effect', message: 'Message.WhenUnitWouldAttack') -> None:
        this = effect.this.CastTo(Upgrade)
        Unused(this)

        message.PreventDamage("All", effect)


    return [
        AbilityFactory.WhenUnitAttackYou(
            AbilityType.Interrupt,
            Enemy,
            life_model_decoy
        ).SetCostFunc(CostFunc.Discard("This")),
    ]

