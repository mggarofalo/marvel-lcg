from . import *

# Censor the Past

def GetAbilities() -> Sequence['Ability']:

    def censor_the_past(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "",
                    lambda targets:
                        Faces.ShuffleAllTo(targets, player.player_deck, effect)
                ).SetTarget(range=(0, 3), from_where=["YourDiscardPile"])
            )

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            censor_the_past
        ),
    ]

