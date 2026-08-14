from . import *

# Beetle

def GetAbilities() -> Sequence['Ability']:

    def beetle(effect: 'Effect', message: 'Message.WhenUnitBeDefeated') -> None:
        this = effect.this.CastTo(Minion)
        Unused(this)

        player = message.defeating_player
        if player:
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbilityToSpend(
                    Cost("R"),
                ),
                AbilityFactory.ForChoiceAbility(
                    "Shuffle Beetle into the encounter deck",
                    lambda targets:
                        Faces.ShuffleAllTo([this], "EncounterDeck", effect)
                )
            )

    return [
        AbilityFactory.WhenUnitBeDefeated(
            AbilityType.ForcedInterrupt,
            "This",
            beetle
        ),
    ]

