from . import *

# Across the Mojoverse

def GetAbilities() -> Sequence['Ability']:

    def across_the_mojoverse(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        SetupCards.PutIntoPlay(
            effect,
            name="The Search for Spiral",
            card_type=SchemeSide2
        )

        first_player = Worlds.GetFirstPlayer(effect)

        shows: List['CardFace']
        shows = Worlds.GetEncounterDeck(effect).FindCards(
            trait="SHOW",
            card_type=Environment
        )

        show = Rand.RandomChoice(shows, effect)
        show.PutIntoPlay(first_player, effect)
        shows.remove(show)

        cornered = Worlds.AsideDeck(effect).FindCard(
            name="Cornered!",
            card_type=Treachery
        )

        if cornered:
            shows.append(cornered)

        villain = Worlds.FindVillain(effect)
        assert villain
        GetShowDeck(effect).Create(effect, villain)
        GetShowDeck(effect).PushCards(
            shows,
            effect,
        )


    return [
        AbilityFactory.WhenCardSetup(
            "This",
            across_the_mojoverse
        ),
    ]

