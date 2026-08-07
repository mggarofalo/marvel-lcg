from . import *

# * Aunt May

def GetAbilities() -> Sequence['Ability']:

    def aunt_may(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        this.HealthUnits(effect.targets, 4, effect)

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            aunt_may,
            conditions=[
                lambda effect, message:
                    effect.GetInitiator().GetAlterEgo().CanHeath()
            ],
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget("YourIdentity")
    ]

