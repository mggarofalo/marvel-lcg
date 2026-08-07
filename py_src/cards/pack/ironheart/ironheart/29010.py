from . import *

# * Ronnie Williams

def GetAbilities() -> Sequence['Ability']:

    def ronnie_williams(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Heal 2 damage from Riri Williams",
                lambda targets:
                    this.HealthUnits(targets, 2, effect)
            ).SetTarget(name="Riri Williams", canbe_heal=True),
            AbilityFactory.ForChoiceAbility(
                "Place 1 progress counter on Riri Williams",
                lambda targets:
                    Faces.PlaceCountersOn(targets, 1, 'progress', effect)
            ).SetTarget(name="Riri Williams"),
        )


    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            ronnie_williams
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget2(name="Riri Williams", canbe_heal=True)
        .SetTarget2(name="Riri Williams")
    ]

