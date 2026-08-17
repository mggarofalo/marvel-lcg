"""The structured test-case format: a setup block, then a transcript.

A `SpecCase` is the intermediate representation every front end compiles to.
`tools/spec/gherkin.py` produces one from a `.feature` file; this module can
load and save the same thing as JSON so a case is inspectable and diffable
without a parser in the loop.

**A scenario is a transcript, not a setup-action-outcome triple.** The engine
is a fold `(state, input) -> (state, prompt)`, and a scenario is a literal trace
of that fold: one `When` per decision, with assertions interleaved wherever the
board is worth checking. That is why `beats` is a single ordered sequence rather
than separate `when` and `then` lists -- the order is the point.

The alternative, batching every action and asserting once at the end, encodes
the *number of prompts* implicitly. A scenario written that way passes against
an engine that asks a different set of questions and lands on the same final
state, which is exactly the failure the format exists to prevent (MARVEL-22).

Nine kinds of beat:

- `WhenStep`      answer the decision the engine is currently asking
- `PromptStep`    assert what the engine is asking, and with which options
- `NoPromptStep`  assert the resolution is over -- no further mid-resolution ask
- `NotOfferedStep` assert an option bound to a card is absent
- `CannotStep`    assert an action will not take a card as its target
- `TargetsStep`   assert which cards an offered option will take
- `MinimumStep`   assert the fewest of them it will take
- `LimitStep`     assert the most of them it will take
- `ThenStep`      assert one thing about readable state

`GivenStep` stays a block ahead of the transcript: it builds the board before
the first decision. Every verb maps to a `RunPuzzle` method or to a deck on a
`Player`, and the mapping is a closed allowlist checked at load time -- an
unknown verb is a load error, never a runtime surprise. Nothing here is
`exec`-ed.
"""

from __future__ import annotations

import hashlib
import json
from dataclasses import dataclass, field
from typing import Any, Dict, List, Sequence, Tuple, Union


class SpecCaseError(Exception):
    """A case that cannot be loaded. Raised at load time, not run time."""


################################################################################
# Given
#
# kind -> what the verb's arguments mean:
#   "create"            variadic card ids, generated into a shared zone
#   "create_player"     variadic card ids, generated into one seat's own zone
#   "card"              one card, a state flip with no magnitude
#   "card_value"        one card plus an integer
#   "card_named_value"  one card, a counter/token name, and an integer
#   "value"             just an integer
#
# A `create_player` verb names a deck attribute on `Player` rather than a
# `RunPuzzle` method, and that is the whole of MARVEL-101. `RunPuzzle`'s four
# player-zone helpers all stock `world.GetFirstPlayer()`, so there is no way
# through them to give a *second* player a known top card -- and "each player
# puts the top card of their deck into play" is printed on 242 cards, 197 of
# which the engine implements. `player.set_aside_deck` *is*
# `player.additional_discard_pile`, one object under two names (`player.py`).
#
# The first-person spellings are **sugar for player 1**: `my deck is` compiles
# to `player_deck` with `player=0`, the same step `player 1's deck is` compiles
# to. One meaning, one code path, no drift between two spellings of one thing.
# See `GivenStep.player` for why that is a change and why it is the right one.

GIVEN_VERBS: Dict[str, Tuple[str, str]] = {
    # zone fills, one seat's own
    "hand":                 ("create_player", "hand_cards"),
    "player_deck":          ("create_player", "player_deck"),
    "player_discard":       ("create_player", "discard_pile"),
    "player_set_aside":     ("create_player", "additional_discard_pile"),
    # zone fills, the whole table's
    "encounter_deck":       ("create", "CreateEncounterDeck"),
    "encounter_discard":    ("create", "CreateEncounterDiscardPile"),
    # magnitudes
    "damage":               ("card_value", "Damage"),
    "heal":                 ("card_value", "Heal"),
    "threat":               ("card_value", "SetThreat"),
    "counters":             ("card_named_value", "Counter"),
    "tokens":               ("card_named_value", "Token"),
    # states
    "stunned":              ("card", "Stun"),
    "confused":             ("card", "Confuse"),
    "tough":                ("card", "Tough"),
    "exhausted":            ("card", "Exhaust"),
    "ready":                ("card", "Ready"),
    "discarded":            ("card", "Discard"),
    "in_play":              ("card", "PutIntoPlay"),
    "revealed":             ("card", "Reveal"),
    "hero_form":            ("card", "ChangeForm"),
    "alter_ego_form":       ("card", "ChangeForm"),
    # player
    "draw":                 ("value", "Draw"),
}

GIVEN_KIND = {verb: kind for verb, (kind, _) in GIVEN_VERBS.items()}


@dataclass(frozen=True)
class GivenStep:
    """One setup command. `verb` is a key of `GIVEN_VERBS`."""

    verb: str
    cards: Tuple[str, ...] = ()
    value: int = 0
    name: str = ""
    player: int = 0
    """Which seat a `create_player` verb stocks, 0-based. `player 1` is 0.

    Seat order, not turn order, and that is a decision rather than a detail.
    `world.players` is **rotated by one at the end of every round** to pass the
    first player token (`world.py`), and an eliminated player is removed from it
    outright, so `world.GetFirstPlayer()` names the token holder rather than a
    seat. `world.const_seat_order_players` does not move.

    The first-person Given steps used to route through `RunPuzzle`, which stocks
    `GetFirstPlayer()`; the first-person *Then* steps have always read
    `state.Player(0)`, which is seat 1. So `my deck is` and `I have <n> cards in
    my deck` already meant two different players, and coincided only because a
    `Given` block cannot run after a `When` and therefore never runs after a
    rotation. Making both mean seat 1 removes that disagreement instead of
    adding a third reading, and it is observationally inert for every scenario
    that can be written today.
    """

    kind = "given"

    def __post_init__(self) -> None:
        if self.verb not in GIVEN_VERBS:
            known = ", ".join(sorted(GIVEN_VERBS))
            raise SpecCaseError(f"unknown Given verb {self.verb!r}; known verbs: {known}")

        shape = GIVEN_KIND[self.verb]
        if shape in ("create", "create_player") and not self.cards:
            raise SpecCaseError(f"Given {self.verb!r} needs at least one card")
        if shape in ("card", "card_value", "card_named_value") and len(self.cards) != 1:
            raise SpecCaseError(
                f"Given {self.verb!r} names exactly one card, got {len(self.cards)}")
        if shape == "card_named_value" and not self.name:
            raise SpecCaseError(f"Given {self.verb!r} needs a counter/token name")
        if shape == "value" and self.cards:
            raise SpecCaseError(f"Given {self.verb!r} takes no cards")
        if self.player < 0:
            raise SpecCaseError(
                f"Given {self.verb!r} names player {self.player + 1}; "
                f"players are numbered from 1")
        if self.player and shape != "create_player":
            raise SpecCaseError(
                f"Given {self.verb!r} is not a per-player step, so it cannot "
                f"name player {self.player + 1}")

    def Describe(self) -> str:
        shape = GIVEN_KIND[self.verb]
        if shape == "create_player" and self.player:
            # Only when it is not the default seat. `Describe` is what a failure
            # message reads back to the author, and someone who wrote `my hand
            # is` should not be answered about "player 1".
            return f"player {self.player + 1} {self.verb} is {', '.join(self.cards)}"
        if shape in ("create", "create_player"):
            return f"{self.verb} is {', '.join(self.cards)}"
        if shape == "card":
            return f"{self.cards[0]} {self.verb}"
        if shape == "card_value":
            return f"{self.cards[0]} {self.verb} {self.value}"
        if shape == "card_named_value":
            return f"{self.cards[0]} has {self.value} {self.name!r} {self.verb}"
        return f"{self.verb} {self.value}"

    def ToDict(self) -> Dict[str, Any]:
        return {"kind": "given", "verb": self.verb, "cards": list(self.cards),
                "value": self.value, "name": self.name, "player": self.player}


################################################################################
# Beats

@dataclass(frozen=True)
class WhenStep:
    """Answer the decision the engine is currently asking.

    `option` is the effect the player picks, written the way a rulebook would
    write it. The engine's own labels are identifiers (`Deal_4_damage_to_an_enemy`)
    derived from the card script rather than from printed text, so both sides are
    normalised before comparison -- see `resolve.NormaliseLabel`.

    `card` disambiguates when several options share a label, which is normal for
    the turn menu: one `play` entry per playable card, one `attack` per attacker.

    `payment` states how many resource icons the command spends. It does not
    name the cards or resource abilities used to make them; the runner chooses
    the first exact legal combination in engine order. `None` means use the
    ordinary payment planner, while `0` is an explicit choice to spend nothing.
    """

    option: str = ""
    card: str = ""
    targets: Tuple[str, ...] = ()
    payment: Optional[int] = None
    pass_priority: bool = False

    kind = "when"

    def __post_init__(self) -> None:
        if self.pass_priority:
            if self.option or self.card or self.targets or self.payment is not None:
                raise SpecCaseError(
                    "a pass step takes no option, card, targets or payment")
        elif not self.option and not self.card:
            raise SpecCaseError("a When step needs an option or a card")
        if self.payment is not None and self.payment < 0:
            raise SpecCaseError(
                f"a payment cannot be negative: {self.payment} resources")

    def Describe(self) -> str:
        if self.pass_priority:
            return "I pass"
        text = f"I choose {self.option!r}" if self.option else "I choose"
        if self.card:
            text += f" on {self.card!r}"
        if self.payment is not None:
            text += f" paying {self.payment} resources"
        if self.targets:
            text += f" targeting {', '.join(repr(t) for t in self.targets)}"
        return text

    def ToDict(self) -> Dict[str, Any]:
        return {"kind": "when", "option": self.option, "card": self.card,
                "targets": list(self.targets), "payment": self.payment,
                "pass_priority": self.pass_priority}


@dataclass(frozen=True)
class TargetsStep:
    """Assert which cards an offered option will accept.

    MARVEL-94. `PromptStep` pins the option set, and for a card that offers one
    action over several cards -- `AskChooseFace`, `AskDiscardFaces`, any
    `ForChoiceAbility` with a multi-target selector -- the option set is a
    *single row* and every card is a target of it. So the thing the printed text
    actually says, "look at the top 3 cards of your deck", was not assertable at
    all: the prompt table said `Futurist` and nothing about the three cards.

    That is the largest prompt family in the corpus, and the workaround it
    forced -- build a board where the wrong candidate is the only one and assert
    it survived -- costs a scenario each time and reads like full coverage when
    it is not.

    Compared as a set, like `PromptStep`, and for the same reason: a missing or
    extra legal target is behavior, the order the engine built them in is not.
    """

    option: str
    targets: Tuple[str, ...] = ()

    kind = "targets"

    def __post_init__(self) -> None:
        if not self.option:
            raise SpecCaseError("a legal-targets assertion needs an option")
        if not self.targets:
            raise SpecCaseError(
                f"a legal-targets assertion needs at least one card; "
                f"{self.option!r} names none. To say an option has no legal "
                f"target, name the card with 'I cannot choose'")

    def Describe(self) -> str:
        return (f"the legal targets for {self.option!r} are "
                f"{', '.join(repr(t) for t in self.targets)}")

    def ToDict(self) -> Dict[str, Any]:
        return {"kind": "targets", "option": self.option,
                "targets": list(self.targets)}


@dataclass(frozen=True)
class LimitStep:
    """Assert the most targets an offered option will take.

    The printed word is "up to": Ancestral Knowledge (01042) searches for "up to
    **3** different cards". `TargetsStep` pins *which* cards may be chosen and a
    `When` naming three of them pins that three is reachable, but neither says
    three is the ceiling. Offering a fourth is refused with `Play takes 1..3
    target(s)`, which is the engine being right and the transcript having no
    passing way to say so: the printed number was pinned from below only.

    The ceiling is `target_num_range[1]` at the decision, and it is the
    *effective* one -- `Selector.GetTargetRange` clamps it to the number of
    legal targets on the board. So the claim only bites on a board offering more
    candidates than the ceiling, which is exactly the board the printed number
    is about; with three candidates and a printed maximum of three, an engine
    that had lost the maximum entirely would answer 3 as well.

    **The sentence is "the target maximum for X is N", not "X takes at most N
    targets", deliberately.** This is an equality like every other `Then` in the
    vocabulary, and a ceiling of 2 does not satisfy it. The "at most" spelling
    promises an inequality that a clamped board would meet vacuously, and a step
    whose text means something looser than its check is exactly the kind of
    thing a second runner reimplements the other way. It also reads beside `the
    legal targets for "<option>" are`, which is the step it completes.
    """

    option: str
    maximum: int

    kind = "limit"

    def __post_init__(self) -> None:
        if not self.option:
            raise SpecCaseError("a target-maximum assertion needs an option")
        if self.maximum < 1:
            raise SpecCaseError(
                f"the target maximum for {self.option!r} is {self.maximum}; a "
                f"maximum below 1 is 'this option takes no target', which is "
                f"'I cannot choose' rather than a printed 'up to N'")

    def Describe(self) -> str:
        return f"the target maximum for {self.option!r} is {self.maximum}"

    def ToDict(self) -> Dict[str, Any]:
        return {"kind": "limit", "option": self.option, "maximum": self.maximum}


@dataclass(frozen=True)
class MinimumStep:
    """Assert the fewest targets an offered option will take.

    This is the floor counterpart to `LimitStep`. It reads
    `target_num_range[0]` from the live option, so it records the selector's
    *effective* floor rather than trying to recover its raw card-script range.
    For `range="All"`, that floor is the number of legal candidates currently
    on the board. A raw floor the board cannot satisfy produces no offered
    option at all, and is therefore unresolvable rather than a different
    minimum.

    The sentence says "is N" because this is equality. "At least N" would let
    a runner accept a floor that is too high and would give the future C#
    binding a looser contract than the Python implementation.
    """

    option: str
    minimum: int

    kind = "minimum"

    def __post_init__(self) -> None:
        if not self.option:
            raise SpecCaseError("a target-minimum assertion needs an option")
        if self.minimum < 1:
            raise SpecCaseError(
                f"the target minimum for {self.option!r} is {self.minimum}; a "
                f"minimum below 1 is an optional/no-target selector rather "
                f"than a printed target requirement")

    def Describe(self) -> str:
        return f"the target minimum for {self.option!r} is {self.minimum}"

    def ToDict(self) -> Dict[str, Any]:
        return {"kind": "minimum", "option": self.option,
                "minimum": self.minimum}


@dataclass(frozen=True)
class PromptStep:
    """Assert the engine is asking, and with exactly these options.

    One of the two assertions that earn a transcript its extra verbosity: it
    pins the *shape* of the question, which a batched format cannot express. The
    option set is state-dependent behavior -- a three-way printed choice offers
    two options when the third has no legal target -- so it is worth asserting.

    Compared as a set, not a sequence. A missing or extra option is a real
    behavioral change; the order the engine happens to build them in is not.
    """

    options: Tuple[str, ...] = ()

    kind = "prompt"

    def __post_init__(self) -> None:
        if not self.options:
            raise SpecCaseError("a prompt assertion needs at least one option")

    def Describe(self) -> str:
        return f"I am prompted to choose one of {', '.join(repr(o) for o in self.options)}"

    def ToDict(self) -> Dict[str, Any]:
        return {"kind": "prompt", "options": list(self.options)}


@dataclass(frozen=True)
class NoPromptStep:
    """Assert the resolution is over: no further mid-resolution question.

    The other assertion a transcript buys. `event_name` discriminates prompt
    kinds -- a mid-resolution ask is not the same thing as the turn menu coming
    back around -- so "the card finished resolving without asking me anything
    else" is checkable. A pre-loaded decision list has no equivalent.
    """

    kind = "no_prompt"

    def Describe(self) -> str:
        return "I am not prompted again"

    def ToDict(self) -> Dict[str, Any]:
        return {"kind": "no_prompt"}


@dataclass(frozen=True)
class NotOfferedStep:
    """Assert that one card-bound option is absent from the live decision.

    Affordability is a property of an option in the current game state, not of
    the cards in hand alone. Resource abilities in play, cost reductions,
    targets and other players can all change what can be paid. This assertion
    therefore observes the engine's decision instead of reimplementing payment
    rules in the spec runner.

    The card must resolve on the board. Otherwise a misspelled or absent card
    would make every option look absent and turn a vacuous claim into a pass.
    """

    option: str
    card: str

    kind = "not_offered"

    def __post_init__(self) -> None:
        if not self.option:
            raise SpecCaseError("a not-offered assertion needs an option")
        if not self.card:
            raise SpecCaseError(
                f"a not-offered assertion needs a card; {self.option!r} names none")

    def Describe(self) -> str:
        return f"I am not offered {self.option!r} on {self.card!r}"

    def ToDict(self) -> Dict[str, Any]:
        return {"kind": "not_offered", "option": self.option, "card": self.card}


@dataclass(frozen=True)
class CannotStep:
    """Assert the engine will not let this action name this card.

    The third thing a transcript can assert, and the only one that is about
    something *not* being possible. `PromptStep` pins which options are offered
    and `ThenStep` pins the board, and a restriction that shows up as neither
    slips past both.

    Guard is the case that forced it: "while this minion is engaged with you,
    you cannot attack the villain". The engine enforces it by emptying the
    option's legal targets, so `Attack` is still offered -- the option set is
    unchanged, no card's state has changed, and the restriction is invisible to
    every other assertion the format has. Stun works the same way: a stunned
    hero is still offered `Attack`, with `all_legal_targets` empty.

    An action can also be absent outright -- an alter-ego is offered no `Attack`
    at all. Both are "I cannot attack Rhino", which is what the rules text says,
    so this step passes when either holds: no matching option, or a matching
    option that will not take this card as a target.

    This is a claim about the decision the engine is asking *now*. It is not a
    claim about the board, so it cannot be evaluated from a captured state the
    way a `ThenStep` can; the policy checks it against the live decision.
    """

    option: str
    card: str
    # Display only, and not serialised. `I cannot attack "Rhino"` and
    # `I cannot choose "Futurist" targeting "Backflip"` are the same assertion
    # over the same fields, but echoing the second one back in the first one's
    # shape produces "I cannot Futurist 'Backflip'", which reads as a typo in
    # the failure message an author is trying to act on.
    verb: bool = True

    kind = "cannot"

    def __post_init__(self) -> None:
        if not self.option:
            raise SpecCaseError("a cannot assertion needs an action")
        if not self.card:
            raise SpecCaseError(
                f"a cannot assertion needs a card; {self.option!r} names none")

    def Describe(self) -> str:
        if self.verb:
            return f"I cannot {self.option} {self.card!r}"
        return f"I cannot choose {self.option!r} targeting {self.card!r}"

    def ToDict(self) -> Dict[str, Any]:
        return {"kind": "cannot", "option": self.option, "card": self.card}


COMPARISONS = ("==", "!=", ">=", "<=", ">", "<")


@dataclass(frozen=True)
class ThenStep:
    """One assertion over readable state.

    `subject` is a card reference, `me`/`player N`, `the main scheme`, or `game`.
    """

    subject: str
    prop: str
    value: Any
    op: str = "=="

    kind = "then"

    def __post_init__(self) -> None:
        if self.op not in COMPARISONS:
            raise SpecCaseError(
                f"unknown comparison {self.op!r}; use one of {', '.join(COMPARISONS)}")
        if not self.subject:
            raise SpecCaseError("a Then step needs a subject")
        if not self.prop:
            raise SpecCaseError("a Then step needs a property")

    def Describe(self) -> str:
        return f"{self.subject} {self.prop} {self.op} {Render(self.value)}"

    def ToDict(self) -> Dict[str, Any]:
        return {"kind": "then", "subject": self.subject, "prop": self.prop,
                "op": self.op, "value": self.value}


Beat = Union[WhenStep, PromptStep, NoPromptStep, NotOfferedStep, CannotStep,
             TargetsStep, MinimumStep, LimitStep, ThenStep]

ASSERTION_KINDS = ("prompt", "no_prompt", "not_offered", "cannot", "targets",
                   "minimum", "limit", "then")


def IsAction(beat: Beat) -> bool:
    return beat.kind == "when"


def Render(value: Any) -> str:
    """Values as an author wrote them, not as Python repr()s them."""
    if isinstance(value, bool):
        return "true" if value else "false"
    if value is None:
        return "none"
    return str(value)


################################################################################
# Case

@dataclass(frozen=True)
class SpecCase:
    """One decision path through one card's behavior, executable."""

    name: str
    scenario: str
    heroes: Tuple[str, ...]
    given: Tuple[GivenStep, ...] = ()
    beats: Tuple[Beat, ...] = ()
    seed: int = 1
    expert: bool = False
    # Decks that exist *before* `GameSetup()` runs, so a setup ability can see
    # them. Every `given` step is applied after setup returns, which is right
    # for a board a scenario is building and wrong for the deck a player
    # brought to the table -- see `SETUP_DECKS` and MARVEL-121.
    setup_player_deck: Tuple[str, ...] = ()
    setup_encounter_deck: Tuple[str, ...] = ()
    feature: str = ""
    # Provenance, used by the validation runner's quarantine.
    source_path: str = ""
    source_sha256: str = ""
    tags: Tuple[str, ...] = field(default=())

    def __post_init__(self) -> None:
        if not self.name:
            raise SpecCaseError("a case needs a name")
        if not self.scenario:
            raise SpecCaseError(f"case {self.name!r} needs a scenario")
        if not self.heroes:
            raise SpecCaseError(f"case {self.name!r} needs at least one hero")
        if not any(beat.kind in ASSERTION_KINDS for beat in self.beats):
            # An assertion-free case reports PASS while proving nothing.
            raise SpecCaseError(f"case {self.name!r} asserts nothing")

    @property
    def case_id(self) -> str:
        """Stable identity across runs: the feature it came from plus its name."""
        if self.feature:
            return f"{self.feature} :: {self.name}"
        return self.name

    @property
    def card_tags(self) -> Tuple[str, ...]:
        """`@card:01084` tags, so verdicts join to the card-text dataset by id."""
        return tuple(tag.split(":", 1)[1] for tag in self.tags
                     if tag.lower().startswith("card:"))

    def Actions(self) -> Tuple[WhenStep, ...]:
        return tuple(beat for beat in self.beats if isinstance(beat, WhenStep))

    def Assertions(self) -> Tuple[Beat, ...]:
        return tuple(beat for beat in self.beats if beat.kind in ASSERTION_KINDS)

    ############################################################################
    #
    def ToDict(self) -> Dict[str, Any]:
        return {
            "name": self.name,
            "feature": self.feature,
            "scenario": self.scenario,
            "heroes": list(self.heroes),
            "seed": self.seed,
            "expert": self.expert,
            "setup_player_deck": list(self.setup_player_deck),
            "setup_encounter_deck": list(self.setup_encounter_deck),
            "tags": list(self.tags),
            "source_path": self.source_path,
            "source_sha256": self.source_sha256,
            "given": [step.ToDict() for step in self.given],
            "beats": [beat.ToDict() for beat in self.beats],
        }

    def ToJson(self, *, indent: int | None = 2) -> str:
        return json.dumps(self.ToDict(), indent=indent, sort_keys=True)

    @staticmethod
    def FromDict(data: Dict[str, Any]) -> "SpecCase":
        try:
            return SpecCase(
                name=str(data["name"]),
                feature=str(data.get("feature", "")),
                scenario=str(data["scenario"]),
                heroes=tuple(str(x) for x in data["heroes"]),
                seed=int(data.get("seed", 1)),
                expert=bool(data.get("expert", False)),
                setup_player_deck=tuple(
                    str(x) for x in data.get("setup_player_deck", ())),
                setup_encounter_deck=tuple(
                    str(x) for x in data.get("setup_encounter_deck", ())),
                tags=tuple(str(x) for x in data.get("tags", ())),
                source_path=str(data.get("source_path", "")),
                source_sha256=str(data.get("source_sha256", "")),
                given=tuple(GivenFromDict(item) for item in data.get("given", ())),
                beats=tuple(BeatFromDict(item) for item in data.get("beats", ())),
            )
        except KeyError as exc:
            raise SpecCaseError(f"case is missing required field {exc}") from exc

    @staticmethod
    def FromJson(text: str) -> "SpecCase":
        return SpecCase.FromDict(json.loads(text))


def GivenFromDict(item: Dict[str, Any]) -> GivenStep:
    return GivenStep(
        verb=str(item["verb"]),
        cards=tuple(str(c) for c in item.get("cards", ())),
        value=int(item.get("value", 0)),
        name=str(item.get("name", "")),
        player=int(item.get("player", 0)),
    )


def BeatFromDict(item: Dict[str, Any]) -> Beat:
    kind = str(item.get("kind", ""))
    if kind == "when":
        return WhenStep(
            option=str(item.get("option", "")),
            card=str(item.get("card", "")),
            targets=tuple(str(t) for t in item.get("targets", ())),
            payment=(None if item.get("payment") is None
                     else int(item["payment"])),
            pass_priority=bool(item.get("pass_priority", False)),
        )
    if kind == "prompt":
        return PromptStep(options=tuple(str(o) for o in item.get("options", ())))
    if kind == "no_prompt":
        return NoPromptStep()
    if kind == "not_offered":
        return NotOfferedStep(option=str(item.get("option", "")),
                              card=str(item.get("card", "")))
    # `targets` has been serialisable and not loadable since MARVEL-94 added it:
    # `TargetsStep.ToDict` writes this kind and nothing here read it, so a JSON
    # case round trip raised "unknown beat kind 'targets'". Noticed while adding
    # `limit` beside it.
    if kind == "targets":
        return TargetsStep(option=str(item.get("option", "")),
                           targets=tuple(str(t) for t in item.get("targets", ())))
    if kind == "cannot":
        return CannotStep(option=str(item.get("option", "")),
                          card=str(item.get("card", "")))
    if kind == "limit":
        return LimitStep(option=str(item.get("option", "")),
                         maximum=int(item.get("maximum", 0)))
    if kind == "minimum":
        return MinimumStep(option=str(item.get("option", "")),
                           minimum=int(item.get("minimum", 0)))
    if kind == "then":
        return ThenStep(
            subject=str(item["subject"]),
            prop=str(item["prop"]),
            op=str(item.get("op", "==")),
            value=item["value"],
        )
    raise SpecCaseError(f"unknown beat kind {kind!r}")


def LoadJsonCases(text: str, *, source_path: str = "") -> List[SpecCase]:
    """One case or a list of them, from JSON."""
    data = json.loads(text)
    items: Sequence[Any] = data if isinstance(data, list) else [data]
    digest = SourceDigest(text)
    cases: List[SpecCase] = []
    for item in items:
        item = dict(item)
        item.setdefault("source_path", source_path)
        item.setdefault("source_sha256", digest)
        cases.append(SpecCase.FromDict(item))
    return cases


def SourceDigest(text: str) -> str:
    """Content hash of a scenario source, newline-normalised.

    The validation runner keys the trusted suite on this: edit the scenario and
    it drops out of the trusted set on the next run, with no way to pin it.
    """
    normalised = text.replace("\r\n", "\n").replace("\r", "\n")
    return hashlib.sha256(normalised.encode("utf-8")).hexdigest()
