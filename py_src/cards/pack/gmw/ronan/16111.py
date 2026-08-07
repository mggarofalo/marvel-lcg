from . import *

# Cut the Power

def GetAbilities() -> Sequence['Ability']:

    def cut_the_power_boost(effect: 'Effect', message: 'Message.WhenCardBecomeBoost') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            ExhaustTheMilanoForChoiceAbility(effect),
            AbilityFactory.ForChoiceAbility(
                "Place 2 threat on the main scheme",
                lambda targets:
                    this.PlaceThreatOnSchemes(targets, 2, effect),
            ).SetTarget(MainScheme, can_place_threat=True)
        )


    return [
        AbilityFactory.WhenCardBecomeBoost(
            "This",
            cut_the_power_boost
        ),
    ]

