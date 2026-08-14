from . import *

# Power Siphon

def GetAbilities() -> Sequence['Ability']:

    def power_siphon_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        first_player = Worlds.GetFirstPlayer(effect)

        player = message.GetToPlayer()
        player.ChooseAbilities(
            effect,
            ExhaustTheMilanoForChoiceAbility(effect),
            AbilityFactory.ForChoiceAbilityToSpend(
                Cost("YY")
            ),
            AbilityFactory.ForChoiceAbility(
                "Discard 1 card at random from the first player's hand",
                lambda targets:
                    first_player.DiscardRandomHandCards(1, effect),
                condition=first_player.hand_cards.GetSize() > 0,
            )
        )


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            power_siphon_revealed
        ),
    ]

