from . import *

# Draugr

def GetAbilities() -> Sequence['Ability']:

    def draugr_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.GetToPlayer()

        player.ChooseAbilities(
            effect,
            AbilityFactory.ForChoiceAbility(
                "Take 1 damage",
                lambda targets:
                    player.GetIdentity().TakeDamage(this, 1, effect)
            ).SetTarget("YourIdentity"),
            AbilityFactory.ForChoiceAbility(
                "Place 1 threat on the main scheme",
                lambda targets:
                    this.PlaceThreatOnSchemes(targets, 1, effect),
            ).SetTarget(MainScheme, can_place_threat=True)
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            draugr_revealed
        ),
    ]

