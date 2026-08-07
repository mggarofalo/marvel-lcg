from . import *

# * White Hot Room

def GetAbilities() -> Sequence['Ability']:

    def white_hot_room(effect: 'Effect', message: 'Message.WhenPlayerInTurn') -> None:
        this = effect.this.CastTo(Support)
        Unused(this)

        initiator = effect.GetInitiator()
        initiator.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Place 1 power counter on Phoenix Force",
                lambda targets:
                    Faces.PlaceCountersOn(targets, 1, 'power', effect)
            ).SetTarget(name="Phoenix Force"),
            AbilityFactory.ForChoiceAbility(
                "Heal 2 damage from Jean Grey",
                lambda targets:
                    this.HealthUnits(targets, 2, effect)
            ).SetTarget(name="Jean Grey", canbe_heal=True),
        )

    return [
        AbilityFactory.WhenInYourPlayTurn(
            AbilityType.AlterEgoAction,
            white_hot_room
        ).SetCostFunc(CostFunc.Exhaust("This"))
        .SetTarget2(name="Phoenix Force")
        .SetTarget2(name="Jean Grey", canbe_heal=True),
    ]

