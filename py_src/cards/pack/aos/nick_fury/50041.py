from . import *

# * Safe House #221

def GetAbilities() -> Sequence['Ability']:

    def safe_house_221(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Heal 2 damage from Nick Fury",
                lambda targets:
                    this.HealthUnits(targets, 2, effect)
            ).SetTarget(name="Nick Fury", canbe_heal=True),
            AbilityFactory.ForChoiceAbility(
                "Place 1 threat on your suit form upgrade",
                lambda targets:
                    Faces.PlaceTokensOn(targets, 1, 'threat', effect)
            ).SetTarget("YourSuitFormUpgrade")
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            safe_house_221
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget2(name="Nick Fury", canbe_heal=True)
        .SetTarget2("YourSuitFormUpgrade"),
    ]

