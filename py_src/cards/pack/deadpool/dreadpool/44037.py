from . import *

# Crisis of Infinite Deadpools

def GetAbilities() -> Sequence['Ability']:

    def crisis_of_infinite_deadpools_revealed(effect: 'Effect', message: 'Message.WhenCardRevealed') -> None:
        this = effect.this.CastTo(Treachery)
        Unused(this)

        player = message.GetToPlayer()

        faces = Worlds.GetSetAsideAreaCards(effect, CardFinder(set_name="Dreadpool"))

        minion = SetupCards.Reveal(
            effect,
            reveal=player,
            card_type=Minion,
            set_name="Dreadpool",
            only_set_aside=True
        )

        scheme = SetupCards.Reveal(
            effect,
            name="Dreadful Deeds",
            card_type=EncounterSideScheme,
            reveal=player,
            only_set_aside=True
        )

        faces = [x for x in faces if x != minion and x != scheme]

        Faces.ShuffleAllTo(faces, "EncounterDeck", effect)
        Faces.RemoveAllFromGame([this], effect)


    return [
        # When setting up a game in which at least
        # one player is using the ‘Pool aspect, shuffle 1 copy of the Crisis of
        # Infinite Deadpools ( 37) treachery card into the encounter deck.
        # Set the rest of the Dreadpool modular encounter set aside. This
        # encounter set is shuffled into the encounter deck when Crisis of
        # Infinite Deadpools is revealed
        # AbilityFactory.SetupWithSetAside([
        #     "44038",
        #     "44039",
        #     "44040",
        #     "44041",
        #     "44041",
        #     "44042"
        # ]),
        AbilityFactory.WhenThisRevealed(
            None,
            crisis_of_infinite_deadpools_revealed
        ),
    ]

