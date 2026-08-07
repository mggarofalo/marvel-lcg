"""A readable snapshot of the world, and the properties a spec may assert on.

Assertions run against this, not against `World.CalculateCRC()`. The CRC is the
right oracle for "did this replay reproduce"; it is the wrong one for "does
Swinging Web Kick deal 5 damage", because a mismatch is a hex diff rather than a
sentence. Everything here is named the way a rulebook names it -- health,
damage, threat, zone, counters, statuses -- so a failure reads as a claim about
the game.

The snapshot is taken while the engine is paused on a decision, and is a plain
data copy: nothing here holds a reference to a live card, so a `StateView`
survives the engine unwinding and can be serialised into a triage record.
"""

from __future__ import annotations

from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional, Tuple


class UnknownProperty(Exception):
    """A `Then` step naming a property the harness does not know how to read."""


################################################################################
#

@dataclass(frozen=True)
class CardState:
    object_id: int
    name: str
    card_id: str
    card_ids: Tuple[str, ...]
    names: Tuple[str, ...]
    zone: str
    in_play: bool
    exhausted: bool
    face_up: bool
    health: Optional[int] = None
    max_health: Optional[int] = None
    threat: Optional[int] = None
    stunned: bool = False
    confused: bool = False
    tough: bool = False
    counters: Dict[str, int] = field(default_factory=dict)
    tokens: Dict[str, int] = field(default_factory=dict)
    info: Dict[str, int] = field(default_factory=dict)

    ############################################################################
    #
    def Get(self, prop: str) -> Any:
        """One assertable property, or `UnknownProperty`.

        `counter:web` and `token:threat` read a named counter/token. Anything
        else falls through to the engine's own render info, so a card-specific
        value like `is_completed` or `printed_stage` is assertable without this
        module having to enumerate it.
        """
        key = prop.strip().lower()

        if key in ("health", "hit_points", "hp"):
            return self.Require(self.health, prop)
        if key in ("max_health", "max_hp"):
            return self.Require(self.max_health, prop)
        if key == "damage":
            health = self.Require(self.health, prop)
            return self.Require(self.max_health, prop) - health
        if key == "threat":
            return self.Require(self.threat, prop)
        if key == "zone":
            return self.zone
        if key == "in_play":
            return self.in_play
        if key in ("exhausted", "is_exhausted"):
            return self.exhausted
        if key == "ready":
            return not self.exhausted
        if key in ("face_up", "is_face_up"):
            return self.face_up
        if key == "stunned":
            return self.stunned
        if key == "confused":
            return self.confused
        if key == "tough":
            return self.tough
        if key.startswith("counter:"):
            return self.counters.get(key.split(":", 1)[1], 0)
        if key.startswith("token:"):
            return self.tokens.get(key.split(":", 1)[1], 0)
        if key in self.info:
            return self.info[key]

        raise UnknownProperty(
            f"{self.name} has no property {prop!r}. "
            f"Known: health, max_health, damage, threat, zone, in_play, exhausted, "
            f"ready, face_up, stunned, confused, tough, counter:<name>, token:<name>"
            + (f", plus {', '.join(sorted(self.info))}" if self.info else ""))

    def Require(self, value: Optional[int], prop: str) -> int:
        if value is None:
            raise UnknownProperty(
                f"{self.name} ({self.card_id}) has no {prop} -- it is a "
                f"{self.zone} card with no such value")
        return value

    def Describe(self) -> str:
        """One line, enough to see why an assertion missed."""
        parts = [f"{self.name} ({self.card_id}) in {self.zone}"]
        if self.health is not None and self.max_health is not None:
            parts.append(f"{self.health}/{self.max_health} hp")
        if self.threat:
            parts.append(f"{self.threat} threat")
        if self.exhausted:
            parts.append("exhausted")
        for status, present in (("stunned", self.stunned),
                                ("confused", self.confused),
                                ("tough", self.tough)):
            if present:
                parts.append(status)
        for name, size in sorted(self.counters.items()):
            if size:
                parts.append(f"{size} {name} counter(s)")
        return ", ".join(parts)


@dataclass(frozen=True)
class PlayerState:
    player_id: int
    identity: str
    hand_size: int
    deck_size: int
    discard_size: int
    eliminated: bool
    resources: str

    def Get(self, prop: str) -> Any:
        key = prop.strip().lower()
        if key == "hand_size":
            return self.hand_size
        if key == "deck_size":
            return self.deck_size
        if key == "discard_size":
            return self.discard_size
        if key in ("eliminated", "is_eliminated"):
            return self.eliminated
        if key == "resources":
            return self.resources
        if key == "identity":
            return self.identity
        raise UnknownProperty(
            f"player {self.player_id} has no property {prop!r}. "
            f"Known: hand_size, deck_size, discard_size, eliminated, resources, identity")

    def Describe(self) -> str:
        return (f"player {self.player_id} ({self.identity}): {self.hand_size} in hand, "
                f"{self.deck_size} in deck, {self.discard_size} in discard")


@dataclass(frozen=True)
class StateView:
    round_id: int
    phase: str
    game_over: bool
    game_over_reason: str
    players_won: Optional[bool]
    cards: Tuple[CardState, ...]
    players: Tuple[PlayerState, ...]

    ############################################################################
    #
    def Get(self, prop: str) -> Any:
        key = prop.strip().lower()
        if key == "round":
            return self.round_id
        if key == "phase":
            return self.phase
        if key in ("game_over", "over"):
            return self.game_over
        if key in ("players_won", "won"):
            return self.players_won
        raise UnknownProperty(
            f"the game has no property {prop!r}. Known: round, phase, game_over, players_won")

    def FindCards(self, key: str, zone: str = "") -> List[CardState]:
        wanted = key.strip().lower()
        found = [card for card in self.cards
                 if wanted in card.card_ids or wanted in card.names]
        if zone:
            from tools.spec.resolve import NormaliseZone, ZONE_IN_PLAY
            target = NormaliseZone(zone)
            if target == ZONE_IN_PLAY:
                found = [card for card in found if card.in_play]
            else:
                found = [card for card in found if card.zone.lower() == target.lower()]
        return found

    def Player(self, player_id: int) -> PlayerState:
        for player in self.players:
            if player.player_id == player_id:
                return player
        raise UnknownProperty(
            f"no player {player_id}; this game has {len(self.players)} player(s)")

    def ToDict(self) -> Dict[str, Any]:
        return {
            "round": self.round_id,
            "phase": self.phase,
            "game_over": self.game_over,
            "game_over_reason": self.game_over_reason,
            "players_won": self.players_won,
            "players": [
                {"player_id": p.player_id, "identity": p.identity, "hand_size": p.hand_size,
                 "deck_size": p.deck_size, "discard_size": p.discard_size,
                 "eliminated": p.eliminated, "resources": p.resources}
                for p in self.players
            ],
            "cards": [
                {"object_id": c.object_id, "name": c.name, "card_id": c.card_id,
                 "zone": c.zone, "in_play": c.in_play, "exhausted": c.exhausted,
                 "health": c.health, "max_health": c.max_health, "threat": c.threat,
                 "stunned": c.stunned, "confused": c.confused, "tough": c.tough,
                 "counters": dict(c.counters), "tokens": dict(c.tokens)}
                for c in self.cards
            ],
        }


################################################################################
# Capture

def CaptureCard(card: Any) -> CardState:
    from game.card.face.attribute.can_health import CanHealth
    from game.card.face.attribute.can_place_counter import CanPlaceCounter
    from game.card.face.attribute.can_place_token import CanPlaceToken
    from game.card.face.attribute.can_status import CanStatus
    from game.card.face.base import Scheme2
    from tools.spec.resolve import ZoneName

    face = card.face

    health: Optional[int] = None
    max_health: Optional[int] = None
    if CanHealth.IsType(face) and not face.is_infinite_health:
        health = int(face.health)
        max_health = int(face.max_health)

    threat: Optional[int] = None
    if Scheme2.IsType(face):
        threat = int(face.threat)

    stunned = confused = tough = False
    if CanStatus.IsType(face):
        stunned = bool(face.IsStunned())
        confused = bool(face.IsConfused())
        tough = bool(face.IsTough())

    counters: Dict[str, int] = {}
    if isinstance(face, CanPlaceCounter):
        for name in face.components.counter.GetCounterNames():
            counters[str(name)] = int(face.GetCounters(name))

    tokens: Dict[str, int] = {}
    if isinstance(face, CanPlaceToken):
        for name in face.components.token.GetTokenNames():
            tokens[str(name)] = int(face.GetTokens(name))

    card_ids = tuple(f.paper.card_id.lower() for f in card.printed_faces)
    names: List[str] = []
    for f in card.printed_faces:
        for value in (str(f.name), str(f.printed_name)):
            if value and value.lower() not in names:
                names.append(value.lower())

    # `GetInfoDict` is the engine's own render info -- the same numbers the web
    # client shows. Keeping it lets a spec assert a card-specific value without
    # this module enumerating every attribute in the game.
    try:
        info = {str(k): int(v) for k, v in face.GetInfoDict().items()}
    except Exception:
        info = {}

    return CardState(
        object_id=int(card.object_id),
        name=str(face.name),
        card_id=str(face.paper.card_id),
        card_ids=card_ids,
        names=tuple(names),
        zone=ZoneName(card),
        in_play=bool(card.IsOnField()),
        exhausted=bool(card.IsExhaust()),
        face_up=bool(card.IsFaceUp()),
        health=health,
        max_health=max_health,
        threat=threat,
        stunned=stunned,
        confused=confused,
        tough=tough,
        counters=counters,
        tokens=tokens,
        info=info,
    )


def CapturePlayer(player: Any) -> PlayerState:
    identity = ""
    try:
        identity = str(player.GetIdentity().name)
    except Exception:
        identity = str(player.name)
    return PlayerState(
        player_id=int(player.player_id),
        identity=identity,
        hand_size=int(player.hand_cards.GetSize()),
        deck_size=int(player.player_deck.GetSize()),
        discard_size=int(player.discard_pile.GetSize()),
        eliminated=bool(player.is_eliminated),
        resources=str(player.res_pool.Get().text_legacy),
    )


def Capture(world: Any) -> StateView:
    """Snapshot `world`. Reads only -- never mutates game state."""
    from tools.spec.resolve import AllCards

    players_won: Optional[bool] = None
    reason = world.game_over.reason or ""
    if reason not in ("", "Exit", "Undo"):
        players_won = bool(getattr(world.game_over, "players_won", False))

    # A game halted by the harness is not "over" in any sense the spec cares
    # about; `Exit` is our own stop signal.
    genuinely_over = bool(world.is_game_over) and reason not in ("Exit", "Undo")

    # `GetPhaseText` asserts a current player during a player turn, which does
    # not hold once the engine has unwound. A snapshot must never be the thing
    # that raises.
    try:
        phase = str(world.GetPhaseText())
    except Exception:
        phase = str(world.phase.state)

    return StateView(
        round_id=int(world.round_id),
        phase=phase,
        game_over=genuinely_over,
        game_over_reason="" if reason in ("Exit", "Undo") else reason,
        players_won=players_won,
        cards=tuple(CaptureCard(card) for card in AllCards(world)),
        players=tuple(CapturePlayer(p) for p in world.const_seat_order_players),
    )
