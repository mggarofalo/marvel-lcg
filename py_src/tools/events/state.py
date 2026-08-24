"""A board built from engine objects rather than from a digest.

MARVEL-160 proved the event vocabulary lossless on everything the digest
records **except position**, where it reached 61.1%. The shortfall was never a
gap in the vocabulary. It was that a digest does not identify an area:
`HandsArea` names one area per player, `UpgradesArea` one per host, and the
`owner` a digest carries is the *card's controller*, not the area's. Grouping
by `(zone, owner, host)` therefore splits one area in two and renumbers across
the join, and the resulting index churn reads as reordering that never
happened.

An engine has no such problem. `Deck2` is an `Object`, so every area already
carries an `object_id` allocated deterministically as the game is built. This
module reads it.

## The record

Identical to a v2 digest record -- `digest.CARD_KEYS`, same values, same
meanings -- plus one key the digest cannot have:

    area   {"zone", "owner", "host", "id"}
    board  the parallel table this card is on

`id` is the area's own object id: real identity, no inference. `owner` beside
it is **the area's owner**, which is the thing `AreaRef` means and is not the
`owner` on the card record next to it. Both are carried so the two can be
compared, which is how `verify.py` answers whether `AreaRef` needs an id at all.

## The board is not the area

`board` is `card.game_area.object_id`, and it is a property of the **card**, not
of the area it sits in. Some scenarios split the table: The Once and Future Kang
gives each player their own board at main-scheme stage 3, each with its own main
scheme and its own Kang, and later rejoins them. Newer scenarios do the same.

The engine models that with `World.game_areas`, and the areas stay shared --
`world.area_schemes_main` is **one** `Deck2` for every board, and
`Worlds.GetMainSchemes(game_area)` filters it by `card.GetGameArea()`. So an
`AreaRef` can span boards, and a deck is not a table.

**The v2 digest does not record this.** Constructed at an ordinary Kang step:
creating a board and moving 47 cards onto it, main scheme included, left the
digest byte-identical. That is a hole in the oracle, not in this file -- see
`docs/state-digest-v2.md` -- and it is why `board` is carried here even though
nothing in the frozen corpus ever changes it.

## Faithfulness

`Serialize` drops the area key, so a snapshot serialises to exactly the v2
document the engine would have produced at the same moment. `verify.py` checks
that byte for byte on every step before it checks anything else -- a round trip
over a board that does not match the digest would be proving a property of this
file rather than of the event stream.
"""

from __future__ import annotations

from typing import Any, Dict, List

# `game.world.digest` is imported inside each function rather than here: the
# engine's package graph is circular at import time, and a tool that pulls
# `game.world` in before `Engine.Initialize` has run trips it.

def Snapshot(world: Any) -> Dict[int, Dict[str, Any]]:
    """Every card in `world`, keyed by object id, with its area's identity."""
    from game.world import digest

    card_dict = world.object_manager.card_dict
    places = _Places(card_dict)

    board: Dict[int, Dict[str, Any]] = {}
    for object_id in sorted(card_dict):
        card = card_dict[object_id]
        area = card.area
        zone, index, suffix = places.get(
            object_id, (area.deck_type.name + digest.SUFFIX_ABSENT, -1, digest.SUFFIX_ABSENT))
        board[object_id] = {
            "id": object_id,
            "card": card.face.paper.card_id,
            "zone": zone,
            "owner": digest._OwnerId(card),
            "index": index,
            "host": area.bind_card.object_id if area.bind_card is not None else -1,
            "face_up": bool(card.state.is_face_up),
            "fields": _Fields(card),
            "board": _Board(card),
            "area": {
                "zone": zone,
                "owner": _AreaOwner(area),
                "host": area.bind_card.object_id if area.bind_card is not None else -1,
                # The suffix is part of the identity: a detached attachment
                # waiting in `removed_cards` is in a different ordered list from
                # the same area's `cards`, and indices in the two are unrelated.
                "id": f"{area.object_id}{suffix}",
            },
        }
    return board


def _Places(card_dict: Dict[int, Any]) -> Dict[int, Any]:
    """object_id -> (zone name, index, suffix). One walk per area."""
    from game.world import digest

    places: Dict[int, Any] = {}
    seen: set[int] = set()
    for card in card_dict.values():
        area = card.area
        if id(area) in seen:
            continue
        seen.add(id(area))
        name = area.deck_type.name
        for index, member in enumerate(area.cards):
            places[member.object_id] = (name, index, "")
        for index, member in enumerate(area.removed_cards):
            places[member.object_id] = (name + digest.SUFFIX_REMOVED, index,
                                        digest.SUFFIX_REMOVED)
    return places


def _Board(card: Any) -> int:
    """Which parallel table the card is on. `-1` if it has not been placed.

    Read from the card because that is where the engine keeps it. A card is
    assigned to a board by `GameArea.AddCard`, which does not touch the deck it
    sits in -- so two main schemes on two boards share one `MainSchemesArea` and
    are told apart by this and nothing else.
    """
    area = getattr(card, "game_area", None)
    return area.object_id if area is not None else -1


def _AreaOwner(area: Any) -> int:
    """The player whose board the *area* sits on. `-1` for the scenario.

    Deliberately not `digest._OwnerId`, which answers a different question
    about a different object. A side scheme controlled by player 3 sits in the
    scenario's side-scheme area: the card's owner is 3, the area's is -1, and
    conflating them is what cost MARVEL-160 its position result.

    Nor is it `Deck2.GetOwner()` alone, which is also a different question.
    `player.engaged_minions` is `Deck2(world.GetScenario(), ...,
    related_player=self)`: the minions engaged with a player are *owned* by the
    scenario and *sit* in front of that player. `GetOwner()` answers -1 for
    every player's engagement area at once, and the first measurement of this
    file believed it -- 380 colliding steps out of 621 before `play_area` was
    read.
    """
    player = getattr(area, "play_area", None)
    if player is not None:
        return player.player_id
    owner = area.GetOwner()
    if owner is None:
        return -1
    return -1 if owner.is_scenario else owner.player_id


def _Fields(card: Any) -> Dict[str, int]:
    fields = card.face.GetStateFields()
    return {key: fields[key] for key in sorted(fields)}


def Serialize(board: Dict[int, Dict[str, Any]]) -> str:
    """The v2 canonical form of a snapshot.

    Neither `area` nor `board` is on the wire: `digest.CARD_KEYS` is a frozen
    format and adding to it would invalidate the corpus.
    """
    from game.world import digest

    cards: List[Dict[str, Any]] = []
    for _, record in sorted(board.items()):
        cards.append({key: record[key] for key in digest.CARD_KEYS})
    return digest.Serialize({"v": digest.DIGEST_VERSION, "cards": cards})
