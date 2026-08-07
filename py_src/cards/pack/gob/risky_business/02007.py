from . import *

# Hired Gun

def GetAbilities() -> Sequence['Ability']:

    def hired_gun(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Give the villain 1 facedown boost card",
                lambda targets:
                    Faces.GiveFacedownBoostCards(targets, 1, effect)
            ).SetTarget(Villain),
            AbilityFactory.ForChoiceAbility(
                "Place 2 infamy counters on Criminal Enterprise",
                lambda targets:
                    Faces.PlaceCountersOn(targets, 2, 'infamy', effect)
            ).SetTarget(Environment, name="Criminal Enterprise")
        )

    return [
        AbilityFactory.WhenThisRevealed(
            None,
            hired_gun
        ),
        BoostPlaceInfamyOrRemoveMadness()
    ]

