"""A readable snapshot of the world, and the properties a spec may assert on.

Assertions run against this, not against `World.CalculateDigest()`. The digest is
the right oracle for "did this replay reproduce"; it is the wrong one for "does
Swinging Web Kick deal 5 damage", because a mismatch is a card dump rather than a
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


# The four resource icons, named the way the card prints them rather than the
# way the engine spells them. `Resources` is `rbyg` internally -- R physical,
# B mental, Y energy, G wild -- and a scenario should not have to know that.
RESOURCE_ICONS: Tuple[str, ...] = ("physical", "mental", "energy", "wild")


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
    resource_icons: Optional[Dict[str, int]] = None
    """The resource icons this printing carries, keyed by `RESOURCE_ICONS`.

    `None` for a card that cannot carry them at all -- only `ClassCard`, the
    player-card base, mixes in `HasResourceIcon`, so an encounter card answers
    "not that kind of card" rather than "zero of each".

    Read from `printed_resource_internal`, which is the `RES` attribute as
    parsed plus any icon a `GainResIcon` effect has added to this copy
    (`face_gain.py`). Deliberately **not** `printed_resource`: that property is
    a `Message.WhenCountingResourcesOnCards` query, and constructing a message
    registers an object in the world, so reading it here would make a snapshot
    mutate the game it is snapshotting. Two cards in the whole corpus answer
    that query (Domino 40037a, Zzzax 29038); nothing in this vocabulary can
    reach either yet, and when something does it wants its own step rather than
    a snapshot with a side effect.
    """
    # Roles, so a scenario can say "I" and "the main scheme" without looking up
    # which card id either one happens to be at this point in the game.
    is_identity: bool = False
    is_main_scheme: bool = False
    is_hero_form: bool = False
    # Was this card put there by scenario setup rather than by a Given? An
    # ordinal over engine-allocated cards means allocation order, which a spec
    # may not depend on -- see `resolve.RejectEngineOrdinal` (MARVEL-42).
    engine_allocated: bool = False

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
        if key in ("hero_form", "in_hero_form"):
            return self.RequireIdentity(prop) and self.is_hero_form
        if key in ("alter_ego_form", "in_alter_ego_form"):
            return self.RequireIdentity(prop) and not self.is_hero_form
        if key.startswith("counter:"):
            return self.counters.get(key.split(":", 1)[1], 0)
        if key.startswith("token:"):
            return self.tokens.get(key.split(":", 1)[1], 0)
        if key.startswith("resource:"):
            return self.ResourceIcon(key.split(":", 1)[1], prop)
        if key in self.info:
            return self.info[key]

        raise UnknownProperty(
            f"{self.name} has no property {prop!r}. "
            f"Known: health, max_health, damage, threat, zone, in_play, exhausted, "
            f"ready, face_up, stunned, confused, tough, counter:<name>, token:<name>, "
            f"resource:<icon>"
            + (f", plus {', '.join(sorted(self.info))}" if self.info else ""))

    def ResourceIcon(self, icon: str, prop: str) -> int:
        """How many of one printed resource icon this card carries.

        The count, not "can it pay for that". A wild icon pays a physical cost
        and this still reports zero physical, because the claim a scenario is
        making here is about what is printed on the card -- which is what tells
        the four printings of Wakanda Forever! apart. What an icon *buys* is
        already observable the way every other cost is: play a card and see
        whether the engine took the payment.
        """
        if icon not in RESOURCE_ICONS:
            raise UnknownProperty(
                f"there is no {icon!r} resource icon; the four are "
                f"{', '.join(RESOURCE_ICONS)}")
        if self.resource_icons is None:
            raise UnknownProperty(
                f"{self.name} ({self.card_id}) is not a player card, so it "
                f"prints no resource icons")
        return self.resource_icons.get(icon, 0)

    def RequireIdentity(self, prop: str) -> bool:
        if not self.is_identity:
            raise UnknownProperty(
                f"{self.name} ({self.card_id}) is not an identity, so it has no {prop}")
        return True

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
        icons = ", ".join(f"{count} {icon}" for icon, count
                          in (self.resource_icons or {}).items() if count)
        if icons:
            parts.append(f"resource icons: {icons}")
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
    identity_object_id: Optional[int] = None
    """Which card in `StateView.cards` is this seat's identity.

    The seat -> card link, and the only thing in the view that carries it: a
    `CardState` knows it *is* an identity but not whose. `I am in hero form`
    resolves through this rather than by picking the first identity out of
    `cards`, which is object-id order and not seat order -- see
    `assertions.ResolveSelf` and MARVEL-107.

    `None` when the seat has no identity card yet, which is every snapshot taken
    before identity selection has run.
    """

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


# The rulebook names three phases; the engine walks eleven states. A scenario
# about "the villain phase" means the rulebook's, so both are assertable and the
# grouping is written down here rather than inferred from the state's spelling.
#
# `Phase.State` is not imported: this module is the harness's view of a world,
# and the values are the wire the C# runner has to reproduce. A mapping keyed by
# a string fails loudly on an unlisted state; one keyed by an imported enum
# would quietly follow a rename.
PHASE_GROUPS: Dict[str, str] = {
    "Initialize":                "setup",
    "Scenario Setup":            "setup",
    "Resolve Mulligans":         "setup",
    "Init Finished":             "setup",
    "Start Round":               "start",
    "Player Turn":               "player",
    "Player Turn End":           "player",
    "Main Scheme Place Threat":  "villain",
    "Enemy Activation":          "villain",
    "Deal Encounter Cards":      "villain",
    "Reveal Encounter Cards":    "villain",
    "End Phase":                 "end",
    "End Round":                 "end",
}

PHASE_NAMES: Tuple[str, ...] = ("player", "villain", "end")


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
        if key == "phase_group":
            return self.PhaseGroup()
        if key in ("game_over", "over"):
            return self.game_over
        if key in ("players_won", "won"):
            return self.players_won
        raise UnknownProperty(
            f"the game has no property {prop!r}. Known: round, phase, "
            f"phase_group, game_over, players_won")

    def PhaseGroup(self) -> str:
        """Which rulebook phase this engine state belongs to.

        An unmapped state raises rather than defaulting. A scenario that says
        "it is the villain phase" while the engine sits in a state nobody
        classified has not established anything, and silently answering "no"
        would make that scenario read as a genuine disagreement.
        """
        try:
            return PHASE_GROUPS[self.phase]
        except KeyError:
            raise UnknownProperty(
                f"phase {self.phase!r} is in no rulebook phase. Add it to "
                f"PHASE_GROUPS in tools/spec/state.py (it is a new "
                f"Phase.State) -- known: {', '.join(sorted(PHASE_GROUPS))}")

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

    def Player(self, seat: int) -> PlayerState:
        """The player in seat `seat`, 0-based. Same seats `harness.SeatOf` names.

        **By position, not by `player_id`.** `players` is built from
        `world.const_seat_order_players`, which is built once and never
        reordered, so position *is* the seat. `player_id` is a number the engine
        stamps on a `Player`; it happens to be the seat index today, because
        `World.__init__` passes the loop index as it fills both lists. Reading
        the position makes `player 2`, seat 2 and `I` one rule instead of three
        that agree by construction elsewhere (MARVEL-107).

        The lower bound is not decoration: `player 0` in a scenario compiles to
        seat -1, and a bare `self.players[-1]` would quietly answer with the
        last seat.
        """
        if seat < 0 or seat >= len(self.players):
            raise UnknownProperty(
                f"no player {seat + 1}; this game has {len(self.players)} player(s)")
        return self.players[seat]

    def CardByObjectId(self, object_id: int) -> Optional[CardState]:
        for card in self.cards:
            if card.object_id == object_id:
                return card
        return None

    def ToDict(self) -> Dict[str, Any]:
        return {
            "round": self.round_id,
            "phase": self.phase,
            "game_over": self.game_over,
            "game_over_reason": self.game_over_reason,
            "players_won": self.players_won,
            "players": [
                {"player_id": p.player_id, "identity": p.identity,
                 "identity_object_id": p.identity_object_id, "hand_size": p.hand_size,
                 "deck_size": p.deck_size, "discard_size": p.discard_size,
                 "eliminated": p.eliminated, "resources": p.resources}
                for p in self.players
            ],
            "cards": [
                {"object_id": c.object_id, "name": c.name, "card_id": c.card_id,
                 "zone": c.zone, "in_play": c.in_play, "exhausted": c.exhausted,
                 "health": c.health, "max_health": c.max_health, "threat": c.threat,
                 "stunned": c.stunned, "confused": c.confused, "tough": c.tough,
                 "counters": dict(c.counters), "tokens": dict(c.tokens),
                 "resource_icons": dict(c.resource_icons or {})}
                for c in self.cards
            ],
        }


################################################################################
# Capture

def CaptureCard(card: Any, engine_allocated: bool = False) -> CardState:
    from game.card.face.attribute.can_health import CanHealth
    from game.card.face.attribute.can_place_counter import CanPlaceCounter
    from game.card.face.attribute.can_place_token import CanPlaceToken
    from game.card.face.attribute.can_status import CanStatus
    from game.card.face.attribute.has_resources import HasResourceIcon
    from game.card.face.base import Scheme2
    from game.card.face.card_type import Hero, Identity, MainScheme
    from tools.spec.resolve import NameableFaces, ZoneName

    face = card.face

    is_identity = bool(Identity.IsType(face))
    is_main_scheme = bool(MainScheme.IsType(face))
    is_hero_form = is_identity and bool(Hero.IsType(face))

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

    # `rbyg` -> the names the card prints. Only player cards carry icons at all,
    # and the difference between "no icons" and "not that kind of card" is worth
    # keeping: a scenario asking an encounter card for its icons has misread
    # something, and should be told so rather than answered zero.
    resource_icons: Optional[Dict[str, int]] = None
    if HasResourceIcon.IsType(face):
        printed = face.printed_resource_internal
        resource_icons = {"physical": int(printed.r), "mental": int(printed.b),
                          "energy": int(printed.y), "wild": int(printed.g)}

    # Every face the card answers to, not only its printed ones. A facedown
    # DRONE keeps the printed identity of the card it was made from and presents
    # a "Drone Minion" face on top of it, and a scenario has reason to name
    # either -- see `resolve.NameableFaces` and MARVEL-102. `card_id` and `name`
    # above stay the *current* face, because that is what a failure message
    # should call the card sitting on the board.
    nameable = NameableFaces(card)
    card_ids = tuple(f.paper.card_id.lower() for f in nameable)
    names: List[str] = []
    for f in nameable:
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
        resource_icons=resource_icons,
        is_identity=is_identity,
        is_main_scheme=is_main_scheme,
        is_hero_form=is_hero_form,
        engine_allocated=engine_allocated,
    )


def CapturePlayer(player: Any) -> PlayerState:
    # The identity card, not only its name. `GetIdentity()` returns the face
    # this seat is presenting; both forms live on one card object, so the id is
    # the same in either form and survives a change of form mid-transcript.
    #
    # A player with no identity card yet answers with `world.insert`, a rules
    # card standing in for one. `Identity.IsType` is the same predicate
    # `CaptureCard` sets `is_identity` from, so the card a seat points at always
    # reads as an identity and a seat that has none says so instead of naming
    # the stand-in. A snapshot must never be the thing that raises either.
    from game.card.face.card_type import Identity

    identity = ""
    identity_object_id: Optional[int] = None
    try:
        face = player.GetIdentity()
        identity = str(face.name)
        if Identity.IsType(face):
            identity_object_id = int(face.card.object_id)
    except Exception:
        if not identity:
            identity = str(player.name)
    return PlayerState(
        player_id=int(player.player_id),
        identity=identity,
        identity_object_id=identity_object_id,
        hand_size=int(player.hand_cards.GetSize()),
        deck_size=int(player.player_deck.GetSize()),
        discard_size=int(player.discard_pile.GetSize()),
        eliminated=bool(player.is_eliminated),
        resources=str(player.res_pool.Get().text_legacy),
    )


def Capture(world: Any) -> StateView:
    """Snapshot `world`. Reads only -- never mutates game state."""
    from tools.spec.resolve import AllCards, IsEngineAllocated

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
    # `Phase.State`'s value, not `GetPhaseText()`. The text renders a player turn
    # as "Player 1 Turn", so it varies with who is seated and a scenario could
    # not name it; the state does not. Which player is in turn is a separate
    # question with its own steps.
    #
    # `.value` because `Phase.State` is a `str, Enum`, which keeps `Enum.__str__`
    # -- `str()` of a member is "State.PlayerTurnEnd", not "Player Turn End".
    phase = str(world.phase.state.value)

    return StateView(
        round_id=int(world.round_id),
        phase=phase,
        game_over=genuinely_over,
        game_over_reason="" if reason in ("Exit", "Undo") else reason,
        players_won=players_won,
        cards=tuple(CaptureCard(card, IsEngineAllocated(world, card))
                    for card in AllCards(world)),
        players=tuple(CapturePlayer(p) for p in world.const_seat_order_players),
    )
