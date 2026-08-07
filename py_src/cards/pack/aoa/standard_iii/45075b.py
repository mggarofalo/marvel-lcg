from . import *

# Pursued by the Past

def GetAbilities() -> Sequence['Ability']:

    def pursued_by_the_past(effect: 'Effect', message: 'Message.AfterCardFlip') -> None:
        this = effect.this.CastTo(Environment)
        Unused(this)

        player = None

        for_message = message.by_effect.bind_message
        if isinstance(for_message, Message.AfterCardPlacedCounter):
            player = for_message.place_player

        if player:
            SetupCards.Reveal(
                effect,
                reveal=player,
                card_type=Minion,
                is_nemesis=player,
                range="All"
            )

            SetupCards.Reveal(
                effect,
                reveal=player,
                card_type=SchemeSide2,
                is_nemesis=player,
                range="All",
                only_set_aside=True,
            )

            encounter_deck = Worlds.GetEncounterDeck(effect)
            Faces.ShuffleAllTo(player.set_aside_nemesis_sets.Get(), encounter_deck, effect, ui_group=False)

        this.card.Flip(effect)


    return [
        AbilityFactory.AfterFlipToThisFace(
            AbilityType.ForcedResponse,
            pursued_by_the_past,
        )
    ]

