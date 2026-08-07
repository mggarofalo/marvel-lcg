from . import *

# Kang's Arrival - 1A

def GetAbilities() -> Sequence['Ability']:

    def kangs_arrival(effect: 'Effect', message: 'Message.WhenCardSetup') -> None:
        this = effect.this.CastTo(MainScheme)
        Unused(this)

        # Set each Kang (II), Kang (III), and Kang's Dominion side scheme aside.
        # remove each player's obligation cards from the game. Shuffle the encounter deck.
        def is_player_obligation(face: CardFace):
            if Obligation.IsType(face):
                return face.give_to != ""
            return False
        encounter_deck = Worlds.GetEncounterDeck(effect)
        faces = encounter_deck.FindCards(
            card_type=Obligation,
            check_face_fn=is_player_obligation
        )
        Faces.FlipAllTo(faces, True, effect)
        Faces.RemoveAllFromGame(faces, effect)

    def when_game_begin(effect: 'Effect', message: 'Message.WhenGameBeginSetup') -> None:
        message.no_use_obligation = True

    return [
        AbilityFactory.WhenCardSetup(
            "This",
            kangs_arrival
        ),
        AbilityFactory.WhenGameBeginSetup(
            when_game_begin
        )
    ]

