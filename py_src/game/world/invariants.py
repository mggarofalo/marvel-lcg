"""What must be true of the world at every decision the engine takes.

Random self-play only finds bugs if something is watching. A crash announces
itself; a silently corrupt game state does not, and those are exactly the
divergences that would later read as C# port errors when they were pre-existing
Python behaviour.

This is deliberately *not* the digest. `game/world/digest.py` records what the
state is so that two runs can be compared; it has no opinion about whether the
state is legal. A run that corrupts itself the same way every time reproduces
perfectly and the oracle says nothing. These rules say what legal means, so one
run can be caught in the act.

**Every rule here is read-only.** Nothing may send a `Message`, allocate an
`Effect`, or touch the RNG: a checker that perturbs the game would change the
thing it is measuring and break the determinism the corpus rests on
(`docs/determinism-audit.md`). That rules out some inviting engine helpers --
`Player.GetCountHandSizeFaces` sends `CheckIfFaceCountHandSize` once per card,
so a rule that needs it cannot live here at all.

Each rule is written with its sentinel: the state that looks like a violation
and is not. A rule with no honest sentinel is not a rule, it is a false alarm
waiting to happen, and the checker aborts the game when one fires. The full
table with reasoning is `docs/invariants.md`.
"""

from __future__ import annotations

from typing import Any, Dict, List, NamedTuple, Sequence, Tuple

from core.errors import EngineIntegrityError
from game.world.digest import SUFFIX_REMOVED


class InvariantViolation(EngineIntegrityError):
    """A rule below was broken, so the game state is already wrong.

    Derived from `EngineIntegrityError` because the alternative is a saved
    replay that looks clean: `EffectInvoker`, `Message2.Send`, the cost and
    target checkers and `Engine.EngineRun` all catch broadly so one bad card
    cannot end the game, and `Log.OnCrash` re-raises only this class regardless
    of the build. See `core/errors.py` and MARVEL-32.
    """


class Violation(NamedTuple):
    """One broken rule, named well enough to act on without a debugger."""

    rule: str
    subject: str
    detail: str

    def __str__(self) -> str:
        return f"{self.rule:<30}{self.subject:<24}{self.detail}"


class Progress:
    """Per-game memory for the rules that compare one decision against the last.

    Owned by the caller and reset when a game starts, so that reloading a scene
    -- which is how undo works here -- does not read as a rewind.
    """

    def __init__(self) -> None:
        self.Reset()

    def Reset(self) -> None:
        self.step_id = -1
        self.round_id = -1
        self.phase_id = -1

    def Advance(self, step_id: int, round_id: int, phase_id: int) -> List[Violation]:
        violations = _CheckProgress(self, step_id, round_id, phase_id)
        self.step_id = step_id
        self.round_id = round_id
        self.phase_id = phase_id
        return violations


################################################################################
# Entry point


def Check(world: Any, progress: 'Progress|None'=None) -> List[Violation]:
    """Every rule against `world`, in a stable order. Empty means legal.

    `progress` carries the state the cross-decision rules need; omit it and
    those rules are skipped, which is what a caller checking a single snapshot
    wants.
    """
    areas = _CollectAreas(world)
    violations: List[Violation] = []
    violations += _CheckZones(world, areas)
    violations += _CheckIdentity(world, areas)
    violations += _CheckCards(world)
    violations += _CheckReplay(world)
    if progress is not None:
        violations += progress.Advance(
            _StepId(world), int(world.round_id), int(world.phase_id))
    return violations


################################################################################
# Finding the places a card can be


def _IsArea(value: Any) -> bool:
    """Whether `value` is a card container.

    Duck-typed rather than `isinstance(value, Deck2)` for the same reason
    `digest.py` takes `world: Any` -- importing the game model here would make
    the rules untestable without booting the engine, and the three attributes
    below are the whole interface this module uses.
    """
    return (hasattr(value, "cards")
            and hasattr(value, "removed_cards")
            and hasattr(value, "deck_type"))


def _CollectAreas(world: Any) -> List[Any]:
    """Every card container reachable from `world`, each one once.

    Reflective rather than a list of attribute names: the decks hang off the
    world, the scenario, each player and each card's components, and a hand
    written list would silently stop covering a deck someone adds later --
    which is the failure mode this whole module exists to prevent.

    `card.area` is included even when nothing else names it, because a card
    claiming an area nobody else can see is itself worth reporting rather than
    quietly skipping.

    One hop into a list or a dict, because several containers are held that way
    rather than named directly: `world.additional_decks` and
    `world.additional_discard_piles` are where `SetAsideDeck.Create` puts the
    decks it builds, and those are the only handle on them once the villain that
    owns them has advanced.
    """
    areas: Dict[int, Any] = {}

    def add(value: Any) -> None:
        if _IsArea(value):
            areas.setdefault(id(value), value)
        elif isinstance(value, (list, tuple)):
            for item in value:
                if _IsArea(item):
                    areas.setdefault(id(item), item)
        elif isinstance(value, dict):
            for item in value.values():
                if _IsArea(item):
                    areas.setdefault(id(item), item)

    def scan(holder: Any) -> None:
        try:
            members = vars(holder)
        except TypeError:
            return
        for value in members.values():
            add(value)

    scan(world)
    scan(getattr(world, "scenario", None))
    for player in _Players(world):
        scan(player)
    for object_id in sorted(world.object_manager.card_dict):
        card = world.object_manager.card_dict[object_id]
        area = getattr(card, "area", None)
        if _IsArea(area):
            areas.setdefault(id(area), area)
        for component in card.components.GetAll():
            scan(component)

    return list(areas.values())


def _Players(world: Any) -> Sequence[Any]:
    """Seat order, not turn order: `world.players` rotates every round, and a
    report that reorders itself between two runs is harder to diff."""
    return getattr(world, "const_seat_order_players", None) or []


def _ZoneName(area: Any) -> str:
    return area.deck_type.name


################################################################################
# Rules: where a card is


def _CheckZones(world: Any, areas: Sequence[Any]) -> List[Violation]:
    """A card is in exactly one place, and that place is the one it names.

    `Deck2.Insert` writes `card.area` and then edits the two lists, so the
    three facts can disagree if a move is interrupted or a list is edited
    directly. The digest cannot see the disagreement: `_BuildPositionIndex`
    reads whichever list it walks first and `_Record` falls back to an
    `/absent` zone, so a duplicated card is recorded in one place and reproduces
    from the recording perfectly.
    """
    violations: List[Violation] = []
    places = _PlaceIndex(areas)
    card_dict = world.object_manager.card_dict

    for object_id in sorted(card_dict):
        card = card_dict[object_id]
        found = places.get(id(card), [])
        subject = _CardName(object_id, card)

        if len(found) > 1:
            violations.append(Violation(
                "zone/duplicate", subject,
                "in " + str(len(found)) + " places at once: " + ", ".join(
                    label for _, label in found)))
        elif not found:
            violations.append(Violation(
                "zone/absent", subject,
                f"in no zone's card list; claims {_ZoneName(card.area)}"))
        else:
            area, label = found[0]
            if area is not card.area:
                violations.append(Violation(
                    "zone/unclaimed", subject,
                    f"sits in {label} but claims {_ZoneName(card.area)}"))

    return violations


def _PlaceIndex(areas: Sequence[Any]) -> Dict[int, List[Tuple[Any, str]]]:
    """id(card) -> every `(area, label)` slot it occupies.

    Keyed by object identity rather than `object_id`, because a card that is
    not registered with the object manager has no trustworthy id and is exactly
    what `identity/unregistered` is looking for.
    """
    places: Dict[int, List[Tuple[Any, str]]] = {}
    for area in areas:
        name = _ZoneName(area)
        for suffix, members in (("", area.cards), (SUFFIX_REMOVED, area.removed_cards)):
            for index, member in enumerate(members):
                places.setdefault(id(member), []).append(
                    (area, f"{name}{suffix}#{index}"))
    return places


def _CheckIdentity(world: Any, areas: Sequence[Any]) -> List[Violation]:
    """Every card the world holds is one the object manager knows about.

    The digest is built from `object_manager.card_dict`, so a card that reached
    an area without being registered is invisible to the oracle: it can change
    the outcome of a game and never appear in a single recorded step. The same
    goes for the card an area hangs off -- `digest._Record` writes
    `area.bind_card.object_id` straight onto the wire.
    """
    violations: List[Violation] = []
    card_dict = world.object_manager.card_dict
    registered = {id(card) for card in card_dict.values()}

    for area in areas:
        for suffix, members in (("", area.cards), (SUFFIX_REMOVED, area.removed_cards)):
            for index, member in enumerate(members):
                if id(member) not in registered:
                    violations.append(Violation(
                        "identity/unregistered",
                        _CardName(getattr(member, "object_id", -1), member),
                        f"in {_ZoneName(area)}{suffix}#{index} but not in card_dict"))

        host = area.bind_card
        if host is not None and id(host) not in registered:
            violations.append(Violation(
                "identity/host",
                _CardName(getattr(host, "object_id", -1), host),
                f"hosts {_ZoneName(area)} but is not in card_dict"))

    return violations


################################################################################
# Rules: what a single card may look like


def _CheckCards(world: Any) -> List[Violation]:
    """The per-card rules, in one pass and in object id order.

    One walk rather than one per rule because `DeckType.flags` rebuilds a
    dictionary on every access, and this runs at every decision of every game.
    """
    violations: List[Violation] = []
    card_dict = world.object_manager.card_dict

    for object_id in sorted(card_dict):
        card = card_dict[object_id]
        subject = _CardName(object_id, card)
        in_play = card.area.flags.is_in_play

        violations += _CheckCounters(subject, card)
        violations += _CheckHealth(subject, card, in_play)
        violations += _CheckReadyState(subject, card, in_play)

    return violations


def _CheckCounters(subject: str, card: Any) -> List[Violation]:
    """Counters and tokens never go negative.

    Read off the components rather than `GetStateFields`, which covers only
    cards in play, in a status area or in a boost area -- a counter that went
    negative on a card in a discard pile is still a bug and still reproduces.

    Threat is a token (`Scheme2.threat` is `GetTokens('threat')`), so
    `tokens/negative` is the threat floor. There is no ceiling: a scheme is not
    capped at its threshold, it advances when it reaches one, and being over it
    for the moment before that resolves is legal play.
    """
    violations: List[Violation] = []

    counters = card.components.counter.counters
    for name in sorted(counters):
        if counters[name] < 0:
            violations.append(Violation(
                "counters/negative", subject, f"{name} = {counters[name]}"))

    tokens = card.components.token.token
    for name in sorted(tokens):
        if tokens[name] < 0:
            violations.append(Violation(
                "tokens/negative", subject, f"{name} = {tokens[name]}"))

    return violations


def _CheckHealth(subject: str, card: Any, in_play: bool) -> List[Violation]:
    """Hit points: a ceiling in play, and a floor under the ceiling itself.

    **There is no lower bound on health, anywhere.** That is a calibration
    result, not an omission. In play, `CanHealth.UpdateHealth` writes a negative
    value and `TakeDamageWithOverkillTarget` then asks the first player for a
    "Simultaneous Overkill" order while the unit still stands at `health <= 0`
    -- a decision that runs through `ChoiceOne`, so the checker sees it. Out of
    play the negative simply stays: `Card.MoveToArea` resets *ready* but not
    health, and `Health.OnParentReset` only runs from `Reset(is_flip=False)`,
    so a minion defeated by 2 overkill sits in the encounter discard pile at
    -2 until something puts it back into play. Neither is a bug and neither
    reaches the wire -- `digest._Fields` returns nothing for a card off the
    field. A rule against either fires on the first multiplayer game.

    Infinite-health cards are exempt from the ceiling: `HasHealth.health`
    reports 1 for them while `max_health` reports 0, and the raw components
    carry the printed zero.
    """
    violations: List[Violation] = []
    health = card.components.health

    if health.max_health < 0:
        violations.append(Violation(
            "health/max-negative", subject, f"max_health = {health.max_health}"))

    if in_play and \
            not getattr(card.face, "is_infinite_health", False) and \
            health.health > health.max_health:
        violations.append(Violation(
            "health/over-max", subject,
            f"health {health.health} > max_health {health.max_health}"))

    return violations


def _CheckReadyState(subject: str, card: Any, in_play: bool) -> List[Violation]:
    """Nothing outside play is exhausted.

    `Card.MoveToArea` calls `ResetReady()` on the way out of play precisely so
    that a card cannot carry an exhausted state into a deck and back out again.
    An exhausted card in a hand or a discard pile therefore means something
    moved it without going through that path, and the digest records
    `is_exhaust` for status and boost areas -- so the wrong value reproduces.
    """
    if in_play or card.state.is_ready:
        return []
    return [Violation(
        "ready/exhausted-out-of-play", subject,
        f"exhausted in {_ZoneName(card.area)}")]


################################################################################
# Rules: hand size -- removed, see docs/invariants.md
#
# `_CheckHandSize` rejected a hand larger than its owner's hand size during
# `Phase.State.PlaceThreat`, on the reasoning that the villain phase begins
# immediately after `PlayerPhase.EndPhase` has run the discard step. Two things
# were wrong with it, and MARVEL-76 caught both on Thor's printed
# "Have at thee!" -- draw 2 cards after a minion engages you:
#
#   1. `PlaceThreat` is a span, not an instant. Encounter cards are dealt,
#      minions engage, and every effect they trigger resolves under it.
#   2. More fundamentally, *any* card that draws outside the end phase puts a
#      hand legitimately over its limit until the next end phase discards it
#      down. No decision point in a round satisfies the bound.
#
# The property is real but it is a post-condition of
# `PlayerPhase.MayDiscardHandCardsAndDrawUpToMax`, which is where it is now
# asserted. Do not bring it back here.


################################################################################
# Rules: the step counter and the phase counters


def _StepId(world: Any) -> int:
    return int(world.controller_manager.replay.current_step_id)


def _CheckReplay(world: Any) -> List[Violation]:
    """The step counter and the recorded history say the same thing.

    They move together -- `InputModule.Push` increments both, `Pop` decrements
    both -- and every saved scene pairs step *n* with `history_inputs[n]`. If
    they drift, the replay is written against the wrong step and the oracle
    compares digests taken at different moments, which surfaces much later as
    an unexplainable mismatch.
    """
    replay = world.controller_manager.replay
    recorded = len(replay.history_inputs)
    if replay.current_step_id == recorded:
        return []
    return [Violation(
        "replay/step-count", "replay",
        f"step id {replay.current_step_id} but {recorded} recorded inputs")]


def _CheckProgress(progress: 'Progress', step_id: int,
                   round_id: int, phase_id: int) -> List[Violation]:
    """A game moves forward.

    Rounds and phases only ever increment, so any decrease is corruption. The
    step counter is the awkward one: it is *not* monotone, because
    `PlayerAction.AskChooseAbility` pops the recorded step when a chosen turn
    option fails to resolve (`player_action.py:364`) and then asks again. That
    is one step, once, before the next decision -- so the rule is that the
    counter never falls by more than one, which still catches a wild rewind.

    Nothing bounds the rise: the debug console pushes its own operations from
    inside `ChoiceOne`'s retry loop, so several steps can land between two
    decisions without anything being wrong.
    """
    violations: List[Violation] = []

    if progress.step_id >= 0 and step_id < progress.step_id - 1:
        violations.append(Violation(
            "progress/step", "replay",
            f"step id fell from {progress.step_id} to {step_id}"))
    if round_id < progress.round_id:
        violations.append(Violation(
            "progress/round", "world",
            f"round fell from {progress.round_id} to {round_id}"))
    if phase_id < progress.phase_id:
        violations.append(Violation(
            "progress/phase", "world",
            f"phase fell from {progress.phase_id} to {phase_id}"))

    return violations


################################################################################
# Reporting


def _CardName(object_id: int, card: Any) -> str:
    """`c49 01095`, the same shape the digest diff prints."""
    try:
        return f"c{object_id} {card.face.paper.card_id}"
    except AttributeError:
        return f"c{object_id}"


def Report(violations: Sequence[Violation]) -> str:
    """The printable form. One rule per line, most specific detail last."""
    return "\n".join(str(violation) for violation in violations)
