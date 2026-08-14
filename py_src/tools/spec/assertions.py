"""Evaluating `Then` steps, and saying what broke in game terms.

Every failure names the assertion, the value the engine produced, and enough
board context to see why. "expected 12, got 14" is not useful on its own;
"Rhino (01094) in VillainArea, 14/14 hp" is.

A `Then` subject is one of:

    "01094" / "Rhino" / "Rhino in VillainArea"   a card
    "player" / "player 2"                        a player (1-based, as written)
    "game"                                       the game itself
"""

from __future__ import annotations

import re
from dataclasses import dataclass
from typing import Any, List, Optional, Tuple

from tools.spec.case import Render, ThenStep
from tools.spec.resolve import CardRef, CardRefError, IsMainScheme, IsSelf
from tools.spec.state import CardState, PlayerState, StateView, UnknownProperty

PLAYER_SUBJECT = re.compile(r"^player(?:\s+(?P<n>\d+))?$", re.IGNORECASE)
GAME_SUBJECT = re.compile(r"^(?:the\s+)?game$", re.IGNORECASE)


@dataclass(frozen=True)
class AssertionResult:
    step: ThenStep
    passed: bool
    actual: Any = None
    message: str = ""
    unresolvable: bool = False
    """True when the subject or property could not be read at all.

    This is the difference between "the engine disagrees with the spec" and
    "the spec describes something that is not in this game", which is what the
    validation runner's verdict split turns on.
    """
    label: str = ""
    """How the beat read in the scenario, when it was not a plain `Then`.

    Prompt and no-prompt beats are assertions too, but they are not about a
    subject and a property, so they carry their own wording rather than being
    forced through `ThenStep.Describe`.
    """

    def Title(self) -> str:
        return self.label or self.step.Describe()

    def Describe(self) -> str:
        if self.passed:
            return f"ok   {self.Title()}"
        if not self.message:
            return f"FAIL {self.Title()}"
        return f"FAIL {self.Title()}\n     {self.message}"


################################################################################
#

def Compare(actual: Any, op: str, expected: Any) -> bool:
    actual, expected = Coerce(actual, expected)
    if op == "==":
        return bool(actual == expected)
    if op == "!=":
        return bool(actual != expected)
    if op == ">=":
        return bool(actual >= expected)
    if op == "<=":
        return bool(actual <= expected)
    if op == ">":
        return bool(actual > expected)
    if op == "<":
        return bool(actual < expected)
    raise UnknownProperty(f"unknown comparison {op!r}")


def Coerce(actual: Any, expected: Any) -> Tuple[Any, Any]:
    """Line up the two sides so `zone == "HandsArea"` is case-insensitive.

    Numbers and booleans are left alone; a bool compared to an int would be a
    spec bug worth surfacing, not something to paper over.
    """
    if isinstance(actual, bool) or isinstance(expected, bool):
        return actual, expected
    if isinstance(actual, str) and isinstance(expected, str):
        return actual.strip().lower(), expected.strip().lower()
    return actual, expected


################################################################################
#

def ResolveSubject(state: StateView, subject: str) -> Tuple[str, Any, str]:
    """(kind, target, error). `error` is set when the subject names nothing."""
    text = subject.strip().strip('"')

    if GAME_SUBJECT.match(text):
        return "game", state, ""

    player_match = PLAYER_SUBJECT.match(text)
    if player_match:
        number = int(player_match.group("n") or 1)
        try:
            return "player", state.Player(number - 1), ""
        except UnknownProperty as exc:
            return "player", None, str(exc)

    # Named roles, so a scenario says "I" and "the main scheme" rather than
    # looking up whichever card id either one is at this point in the game.
    if IsSelf(text):
        return ResolveSelf(state)

    if IsMainScheme(text):
        schemes = [card for card in state.cards if card.is_main_scheme and card.in_play]
        if not schemes:
            return "card", None, "there is no main scheme in play"
        return "card", schemes[0], ""

    try:
        ref = CardRef.Parse(text)
    except CardRefError as exc:
        return "card", None, str(exc)

    found = state.FindCards(ref.key, ref.zone)
    if ref.ordinal:
        if len(found) < ref.ordinal:
            return "card", None, (
                f"wanted copy #{ref.ordinal} of {ref.key!r} but the game has {len(found)}")
        # Same rule the live resolver enforces (MARVEL-42): `#N` counts the
        # copies the scenario created. Over cards the engine allocated during
        # setup there is no such order, so the ref has to name a zone instead.
        if len(found) > 1 and any(card.engine_allocated for card in found):
            listing = "; ".join(card.Describe() for card in found)
            return "card", None, (
                f"{ref.Describe()} would index cards the scenario did not create, "
                f"so which one it names is the engine's allocation order rather "
                f"than anything the scenario says. Name the zone instead. "
                f"Candidates: {listing}")
        return "card", found[ref.ordinal - 1], ""
    if not found:
        return "card", None, DescribeMissing(state, ref)
    if len(found) > 1:
        # Same rule the live resolver uses: a name that matches several cards,
        # only one of which is on the board, means the one on the board.
        in_play = [card for card in found if card.in_play]
        if len(in_play) == 1:
            return "card", in_play[0], ""
        listing = "; ".join(card.Describe() for card in found)
        return "card", None, (
            f"{ref.Describe()} matches {len(found)} cards ({listing}). "
            f"Add a zone or an ordinal to say which one.")
    return "card", found[0], ""


def ResolveSelf(state: StateView) -> Tuple[str, Any, str]:
    """`I` / `me` on the `Then` side: **seat 1's identity card** (MARVEL-107).

    The third and last reading of the first person to be made a seat. The zone
    steps (`I have <n> cards in hand`) were made seat 1 by MARVEL-101 and `"me"`
    as a card ref by MARVEL-104; both call `harness.SeatOf(world, 0)` over
    `world.const_seat_order_players`. This one picked the first `is_identity`
    card out of `state.cards`, which `resolve.AllCards` documents as *object-id*
    order -- stable, which is what that function needs, but not seat order.

    Nothing failed: this engine allocates identity cards seat by seat during
    setup, so the lowest-id identity is seat 1's, and the three readings agreed
    by coincidence. It is a port hazard rather than a Python bug --
    docs/migration.md lists allocation order among the things the two engines
    must be *made* to agree on, and MARVEL-42 already refuses to let a scenario
    lean on it for `#N`. An engine that numbered identities by pack, by hero
    name, or alter-ego before hero would move `I am in hero form` to another
    player while `I have 3 cards in hand` and `"me"` stayed on seat 1, and the
    scenario would fail in C# as an apparent engine disagreement.

    `StateView.players` is built from `const_seat_order_players`, so the seat
    order is already in the view; the only thing missing was which card each
    seat's identity is, which `PlayerState.identity_object_id` now carries.
    That is smaller than giving every `CardState` an owner, and it puts the seat
    where seats already live.
    """
    try:
        seat = state.Player(0)
    except UnknownProperty as exc:
        return "card", None, str(exc)
    if seat.identity_object_id is None:
        return "card", None, "player 1 has no identity in this game"
    card = state.CardByObjectId(seat.identity_object_id)
    if card is None:
        return "card", None, (
            f"player 1's identity ({seat.identity}) is not in this snapshot")
    return "card", card, ""


def DescribeMissing(state: StateView, ref: CardRef) -> str:
    """A missing card, with the near misses that usually explain it."""
    if ref.zone:
        elsewhere = state.FindCards(ref.key)
        if elsewhere:
            where = ", ".join(sorted({card.zone for card in elsewhere}))
            return f"no {ref.key!r} in {ref.zone}; it is in {where}"
    wanted = ref.key.strip().lower()
    near = [card.Describe() for card in state.cards
            if len(wanted) >= 3 and any(wanted in name for name in card.names)]
    if near:
        return f"no card matches {ref.key!r}; did you mean {near[0]}?"
    return f"no card matches {ref.key!r} anywhere in this game"


################################################################################
#

def Evaluate(state: StateView, step: ThenStep) -> AssertionResult:
    kind, target, error = ResolveSubject(state, step.subject)
    if error:
        return AssertionResult(step=step, passed=False, message=error, unresolvable=True)

    try:
        actual = target.Get(step.prop)
    except UnknownProperty as exc:
        return AssertionResult(step=step, passed=False, message=str(exc), unresolvable=True)

    try:
        passed = Compare(actual, step.op, step.value)
    except UnknownProperty as exc:
        return AssertionResult(step=step, passed=False, message=str(exc), unresolvable=True)

    if passed:
        return AssertionResult(step=step, passed=True, actual=actual)

    lines = [f"expected {Render(step.value)}, got {Render(actual)}"]
    if isinstance(target, (CardState, PlayerState)):
        lines.append(target.Describe())
    return AssertionResult(step=step, passed=False, actual=actual, message="\n     ".join(lines))


def EvaluateAll(state: StateView, steps: "List[ThenStep]|Tuple[ThenStep, ...]") -> List[AssertionResult]:
    return [Evaluate(state, step) for step in steps]


def FirstFailure(results: "List[AssertionResult]") -> Optional[AssertionResult]:
    for result in results:
        if not result.passed:
            return result
    return None
