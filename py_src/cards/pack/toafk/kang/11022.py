from . import *

# Corrupted Timestream

def GetAbilities() -> Sequence['Ability']:

    def corrupted_timestream(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)
        Unused(this)

        def action(player: 'Player'):
            player.ChooseAbilities(
                effect,
                AbilityFactory.ForChoiceAbility(
                    "Discard 1 random card from hand",
                    lambda targets:
                        player.DiscardRandomHandCards(1, effect),
                    condition=player.hand_cards.GetSize() > 0,
                ),
                AbilityFactory.ForChoiceAbility(
                    "Place 2 threat here",
                    lambda targets:
                        this.PlaceThreatOnSchemes([this], 2, effect)
                ).SetTarget("This"),
            )

        Players.ForEachPlayer(effect, action)


    return [
        AbilityFactory.PlayersCannotTriggerAbility(
            "AnyPlayer",
            "AlterEgoAction",
            on_which_card=Obligation
        ),
        AbilityFactory.WhenThisRevealed(
            None,
            corrupted_timestream
        )
    ]

