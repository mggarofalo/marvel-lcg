from . import *

# Weakened

def GetAbilities() -> Sequence['Ability']:

    def take_damage(effect: 'Effect', message: 'Message.AfterUnitUseBasicPower'):
        this = effect.this.CastTo(Obligation)
        this.DealDamage(effect.targets, 1, effect)

    return [
        AbilityFactory.AfterUnitUseBasicPower(
            AbilityType.ForcedResponse,
            "You",
            take_damage,
            powers="Hero"
        ).SetTarget("Trigger"),
        AbilityFactory.PlayerActionToDiscardThis(
            AbilityType.AlterEgoAction,
        ).SetCostFunc(CostFunc.Discard(
            "YourHandCards",
            has_printed_res="R"
        ))
    ]

