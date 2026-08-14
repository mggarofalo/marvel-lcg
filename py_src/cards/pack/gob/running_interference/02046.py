from . import *

# Running Interference

def GetAbilities() -> Sequence['Ability']:

    def running_interference_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbilityToSpend(
                    Cost("BR"),
                ),
                AbilityFactory.ForChoiceAbility(
                    "Place 2 threat here",
                    lambda targets:
                        this.PlaceThreatOnSchemes(targets, 2, effect),
                ).SetTarget([this]),
            )

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            running_interference_revealed
        ),
    ]

