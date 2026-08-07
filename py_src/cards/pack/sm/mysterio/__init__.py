from cards.pack import *

MYSTERIO = "Mysterio"

def Mysterio(level: int) -> 'Ability':

    name = {
        1: "Seeds of Fear",
        2: "Creeping Fear",
        3: "Bound by Fear",
    }[level]

    def mysterio(effect: 'Effect', message: 'Message.AfterCardBecomeBoost'):
        player = message.GetAgainstPlayer()
        if player:
            face = message.trigger
            if level == 1:
                Faces.MoveAllTo([face], player.discard_pile, effect)
            elif level == 2:
                Faces.MoveAllToDeck([face], player.player_deck, "Bottom", effect)
            elif level == 3:
                Faces.MoveAllToDeck([face], player.player_deck, "Top", effect)

    return AbilityFactory.AfterCardBecomeBoost(
        AbilityType.ForcedResponse,
        None,
        mysterio,
        conditions=[
            lambda effect, message:
                effect.this == message.boost_message.being_message.by_face and \
                message.trigger.HasTrait("ILLUSION"),
        ]
    ).SetName(name)


def ShuffleTopEncounterDeckIntoEachPlayersDeck(num: int, by_effect: 'Effect'):
    this = by_effect.this
    Unused(this)

    def action(player: 'Player'):
        ShuffleTopEncounterDeckIntoPlayerDeck(player, num, by_effect)

    Players.ForEachPlayer(by_effect, action)


def ShuffleTopEncounterDeckIntoPlayerDeck(player: 'Player', num: int, by_effect: 'Effect'):
    encounter_deck = Worlds.GetEncounterDeck(by_effect)
    faces = encounter_deck.GetTops(num)
    Faces.ShuffleAllTo(faces, player.player_deck, by_effect)

