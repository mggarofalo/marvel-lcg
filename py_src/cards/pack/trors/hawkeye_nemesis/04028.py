from . import *

# Marked for Death

def GetAbilities() -> Sequence['Ability']:

    def marked_for_death(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(EncounterSideScheme)

        name = "Mockingbird"
        player = Worlds.FindPlayerByName("Clint Barton", effect)
        if player:
            need_shuffle = False
            cards: List[Ally] = []
            if cards == []:
                cards += Worlds.FindCardsOnField(
                    effect,
                    name=name,
                    card_type=Ally
                )
            if cards == []:
                cards += player.hand_cards.FindCards(name=name, card_type=Ally)
            if cards == []:
                cards += player.player_deck.FindCards(name=name, card_type=Ally)
                need_shuffle = True
            if cards == []:
                cards += player.discard_pile.FindCards(name=name, card_type=Ally)

            Faces.MoveAllToProcessingArea(cards, effect)
            this.PlaceCardHere(cards, True, effect)

            if need_shuffle:
                player.player_deck.Shuffle(effect)

    def marked_for_death_return(effect: 'Effect', message: 'Message.WhenSchemeBeDefeated') -> None:
        this = effect.this.CastTo(EncounterSideScheme)

        faces = Faces.MoveAllToProcessingArea(this.GetPlacedCardArea().Get(), effect)
        Faces.ReturnToHand(faces, "Owner", effect)


    return [
        AbilityFactory.WhenThisRevealed(
            None,
            marked_for_death
        ),
        AbilityFactory.WhenSchemeBeDefeated(
            AbilityType.WhenDefeated,
            "This",
            marked_for_death_return
        )
    ]

