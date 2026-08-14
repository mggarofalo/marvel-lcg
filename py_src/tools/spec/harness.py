"""Run one `SpecCase` against the Python engine and report what happened.

The shape of a run:

1. Boot the engine once per process (`EnsureEngine`) -- card database, config,
   job and task managers. Roughly half a second, and a suite pays it once.
2. Build a **puzzle scene**: villain, main scheme and identities, and nothing
   else. `GameServerNewGame.play_puzzle` builds the same thing for the web
   client. A puzzle starts with no encounter deck and no player deck, so the
   board contains only what the spec asks for.
3. `GameSetup()`, then apply the `Given` steps through `RunPuzzle`.
4. `GameLoop()`, with `TranscriptPolicy` answering decisions. It plays the
   steps and halts on the first free decision afterwards.
5. Evaluate the `Then` steps against the captured state.

Given steps call `RunPuzzle` methods directly rather than going through
`PuzzleHelper.Exec`. That skips an `exec()` per command and an
`exec(f"c{c} = ...")` per card per command, which is what makes thousands of
cases practical, and it keeps the harness off the engine's `exec`-based path.

Runs are deterministic: a fixed seed per case, a policy with no randomness, and
no wall clock anywhere in the loop.
"""

from __future__ import annotations

import contextlib
import io
import re
from dataclasses import dataclass, field
from typing import Any, Dict, Iterator, List, Optional, Sequence, Set, Tuple

from tools.spec.assertions import AssertionResult
from tools.spec.case import GIVEN_KIND, GIVEN_VERBS, GivenStep, SpecCase
from tools.spec.policy import DecisionRecord, DescribeTrail, TranscriptPolicy
from tools.spec.resolve import (AmbiguousCardRef, CardRefError, MarkEngineBaseline,
                                ResolveCard, ResolveFace)
from tools.spec.state import Capture, StateView

CATEGORY_NAME = "SPEC"

# Outcomes the harness itself can tell apart. The validation runner (MARVEL-21)
# maps these onto its PASS / FAIL-spec-wrong / FAIL-engine-suspected verdicts.
OUTCOME_PASS = "PASS"
OUTCOME_ASSERTION = "ASSERTION"      # ran cleanly, a Then disagreed
OUTCOME_UNPLAYABLE = "UNPLAYABLE"    # a Given or When could not be applied
OUTCOME_ERROR = "ERROR"              # the engine raised


class SetupError(Exception):
    """A `Given` step that could not be applied to this board."""


@dataclass
class CaseResult:
    case: SpecCase
    outcome: str
    assertions: List[AssertionResult] = field(default_factory=list)
    decisions: List[DecisionRecord] = field(default_factory=list)
    state: Optional[StateView] = None
    message: str = ""
    engine_log: str = ""
    """The engine's own game log for this case -- the play-by-play, for triage."""

    @property
    def passed(self) -> bool:
        return self.outcome == OUTCOME_PASS

    def Failures(self) -> List[AssertionResult]:
        return [result for result in self.assertions if not result.passed]

    def Describe(self) -> str:
        head = f"{self.outcome:<10} {self.case.case_id}"
        lines = [head]
        if self.message:
            lines.append(f"     {self.message}")
        for result in self.Failures():
            lines.append("     " + result.Describe().replace("\n", "\n     ").strip())
        return "\n".join(lines)


################################################################################
# Engine output
#
# The engine narrates every game to stdout. That narration is the best triage
# material there is -- it says what the engine thought it was doing -- but it
# drowns a suite run, so it is captured per case and printed only on request.

ANSI = re.compile(r"\x1b\[[0-9;]*[A-Za-z]")
ENGINE_LOG_LINES = 200


@contextlib.contextmanager
def CaptureEngineOutput() -> Iterator[io.StringIO]:
    buffer = io.StringIO()

    from engine.log import Log
    # `LogHelper.Print` appends to this forever and nothing ever clears it
    # (`Log.Setup`'s reset is commented out). Over thousands of cases that is a
    # steadily growing string; the harness never saves a crash log, so dropping
    # it per case costs nothing.
    Log.all_log_text = ""

    with contextlib.redirect_stdout(buffer):
        yield buffer


def TailLog(buffer: io.StringIO, limit: int = ENGINE_LOG_LINES) -> str:
    text = ANSI.sub("", buffer.getvalue()).replace("\r", "\n")
    lines = [line for line in text.splitlines() if line.strip()]
    if len(lines) > limit:
        lines = [f"... {len(lines) - limit} earlier line(s)"] + lines[-limit:]
    return "\n".join(lines)


################################################################################
# Engine bootstrap

_ENGINE_READY = False


def EnsureEngine() -> None:
    """Bring the engine up once per process.

    Mirrors `Engine.Initialize` minus the device manager and the editor, the
    same way `tools/determinism/headless.py` does. Must run with `py_src/` as
    the working directory -- every data path in the engine is relative to it.
    """
    global _ENGINE_READY
    if _ENGINE_READY:
        return

    from engine.lib import Ver
    Ver.Initialize()

    from engine.config import ConfigVariables
    ConfigVariables.Initialize()

    from engine.job import JobManager
    from engine.task import TaskManager
    JobManager.Initialize()
    TaskManager.Initialize()

    from engine.user.user_info import UserInfo
    UserInfo.Initialize()

    from engine.lib import ImageCreator, TransText
    TransText.Initialize()
    ImageCreator.Initialize()

    from cards.database import CardsDB
    CardsDB.Initialize()

    # Suppresses the render/sync round trip and the connect wait; without it the
    # engine tries to talk to a client that is not there.
    from game.test import Test
    Test.is_in_test = True

    _ENGINE_READY = True


################################################################################
# Scene

_JSON_CACHE: Dict[Tuple[str, str], Dict[str, Any]] = {}


def LoadJson(kind: str, name: str, missing: str) -> Dict[str, Any]:
    """A scenario or hero definition, read once per process.

    A suite runs thousands of cases over a handful of scenarios; re-reading the
    same two files for every one of them is pure overhead.
    """
    key = (kind, name)
    if key in _JSON_CACHE:
        return _JSON_CACHE[key]

    from engine.file import FileManager
    from engine.lib import Json

    path = FileManager.FindJsonPath(kind, name, nullable=True)
    if not path:
        raise SetupError(missing)
    with FileManager.OpenFile(path, read=True) as file:
        data = Json.Loads(file.Read())
    _JSON_CACHE[key] = data
    return data


def LoadScenarioJson(name: str) -> Dict[str, Any]:
    return LoadJson("Campaign", name,
                    f"scenario {name!r} not found under data/scenarios/")


def LoadHeroJson(name: str) -> Dict[str, Any]:
    return LoadJson("Hero", name, f"hero {name!r} not found under deck/")


def PreferredPacks(case: SpecCase) -> Tuple[str, ...]:
    """Set prefixes this scenario is plausibly about.

    Printed names collide across packs -- five cards are called "Nick Fury" --
    so a bare name needs a tie-break. The scenario already says which game it is
    playing, and a scenario about the core-set Rhino means the core-set Shocker.
    `@card:` tags are honoured too, for a scenario that reaches outside its set.
    """
    prefixes: List[str] = []

    def Add(card_id: str) -> None:
        prefix = str(card_id).strip()[:2]
        if prefix.isdigit() and prefix not in prefixes:
            prefixes.append(prefix)

    for tag in case.card_tags:
        Add(tag)
    try:
        for card_id in LoadScenarioJson(case.scenario).get("villain", []):
            Add(card_id)
        for hero in case.heroes:
            for card_id in LoadHeroJson(hero).get("hero", []):
                Add(card_id)
    except SetupError:
        pass
    return tuple(prefixes)


# A deck that exists before `GameSetup()` runs, rather than one a `Given`
# stacks afterwards. MARVEL-121.
#
# ## Why a second spelling exists at all
#
# `RunCaseInternal` applies every `Given` *after* `GameSetup()` returns, and
# that is the right order for almost everything a scenario says: a board is
# built by putting cards on it, and the engine has to have finished dealing
# before there is a board to put them on.
#
# It is the wrong order for exactly one thing -- a **setup ability**, which
# fires during setup and reads a deck that a `Given` has not stocked yet. The
# engine sends `Message.WhenCardSetup` from two places inside `GameSetup()`:
# `world.py` step 12 for every main scheme and villain, and step 16 for every
# identity. Any ability hanging off either one runs against the empty decks a
# puzzle scene is built with.
#
# Measured against `datasets/cards/cards.json`: **49 cards** carry a setup
# ability that searches a zone the puzzle scene blanks -- 37 main schemes, 5
# alter egos, 4 challenges, 2 Civil War leaders and 1 support. Three of them
# are in the core set (01040b T'Challa, 01116a Underground Distribution,
# 01137a The Crimson Cowl), and all three had the gap written into a spec file
# header as prose because there was no way to say it in a scenario.
#
# ## Why the existing steps were not simply moved
#
# The obvious fix is to make `my deck is` and `the encounter deck is` mean
# "this is the deck", full stop, and put them in the scene. That was measured
# before it was rejected: routing both verbs into the scene turns **102 of the
# 411 currently passing scenarios red and fixes none of them**, because
#
#   - cards the *scene* creates are allocated before `MarkEngineBaseline`, so
#     every `"<card> #N"` ordinal over them is refused (MARVEL-42),
#   - `player_setup.SelectIdentity` shuffles the player deck at setup step 6,
#     which destroys the top-first order `my deck is` promises (MARVEL-82),
#   - a card with the printed Setup keyword sitting in the deck now enters play
#     at step 11 and changes the board out from under the transcript.
#
# So the two orders are both real and a scenario has to be able to pick. These
# steps are additive: a scenario that does not write one is byte-for-byte the
# scenario it was.
#
# ## What the `at setup` spelling costs
#
# **Order is not preserved.** `player.player_deck.Shuffle(rule)` runs at setup
# step 6 and the encounter deck is shuffled the same way, so unlike
# `my deck is` these are a *set* of cards, not a stack. That is what a real
# game does, and it is why the step is for setup abilities -- which search --
# rather than for pinning what gets drawn next. Stack the draw order with
# `my deck is` in the same scenario if a beat needs it.
#
# **The cards cannot be named by ordinal.** They are allocated during setup,
# before `MarkEngineBaseline`, so they are the engine's cards and not the
# scenario's, exactly like the two Rhinos. Name them by printed name or id.
#
# **A one-card deck can end the game during setup.** `SelectorEnd.DoShuffle`
# asserts the deck is non-empty, so a hero whose setup ability searches its own
# deck and shuffles afterwards raises when the search emptied it. Give a
# searching hero at least two cards. That is an engine bug rather than a
# harness one and is reported as its own finding.
SETUP_DECKS = ("setup_player_deck", "setup_encounter_deck")


def BuildPuzzleScene(case: SpecCase) -> Any:
    """The scene dict `play_puzzle` builds, minus the pre-command strings.

    A puzzle scene deliberately has no encounter deck, no modular sets and no
    player deck: `Given` puts on the board exactly what the spec names, and
    nothing arrives that the spec did not ask for.

    The two exceptions are the `at setup` decks, and they are exceptions
    because of *when* they have to exist rather than what they contain. See
    `SETUP_DECKS`.
    """
    from engine.lib import Json
    from game.scene.scene import Scene

    scenario = LoadScenarioJson(case.scenario)
    heroes = [LoadHeroJson(name) for name in case.heroes]
    version = str(scenario.get("version", ""))
    packs = PreferredPacks(case)
    setup_player_deck = [ResolveCardId(ref, packs)
                         for ref in case.setup_player_deck]
    setup_encounter_deck = [ResolveCardId(ref, packs)
                            for ref in case.setup_encounter_deck]

    data = {
        "version": version,
        "metadata": {
            "seed": case.seed,
            "comment": case.name,
            "cover": "",
            "is_puzzle": True,
        },
        "campaign": {
            "version": version,
            "name": scenario.get("name", case.scenario),
            "villain": list(scenario.get("villain", [])),
            "expert": bool(case.expert),
            "schemes": list(scenario.get("schemes", [])),
            "set_aside": [],
            "encounters": setup_encounter_deck,
            "encounter_sets": [],
            "modular_sets": [],
        },
        "players": [
            {
                "version": version,
                "name": f"Player {index}",
                "hero": list(hero.get("hero", [])),
                "hero_deck": [],
                "obligations": [],
                "nemesis_set": [],
                "set_aside": [],
                # Seat 1 only. `my deck at setup is` is first person, like every
                # other deck-stocking step, and there is no per-seat form of it
                # yet -- see `SETUP_DECKS`.
                "player_deck": setup_player_deck if index == 0 else [],
            }
            for index, hero in enumerate(heroes)
        ],
        "puzzle": [],
    }

    scene = Json.LoadsAs(Json.Dumps(data), Scene)
    scene.UpdateVersion()
    return scene


################################################################################
# Given

def ApplyGiven(world: Any, case: SpecCase) -> None:
    """Apply the setup commands, in order, through `RunPuzzle`."""
    from game.puzzle.puzzle import RunPuzzle

    puzzle = RunPuzzle(world)
    packs = PreferredPacks(case)

    # Everything allocated from here on exists because the scenario asked for
    # it. That is what lets `#N` mean "the Nth copy the scenario created".
    MarkEngineBaseline(world)

    # Cards a creating step has already taken, so a second step naming the same
    # card is caught however it spells it.
    created: Set[int] = set()

    for position, step in enumerate(case.given, start=1):
        try:
            ApplyGivenStep(world, puzzle, step, packs, created)
        except SetupError:
            raise
        except CardRefError as exc:
            raise SetupError(f"Given step {position} ({step.Describe()}): {exc}") from exc
        except Exception as exc:
            raise SetupError(
                f"Given step {position} ({step.Describe()}) failed: "
                f"{type(exc).__name__}: {exc}") from exc


# Verbs that may bring a card into the game rather than only finding one. A
# scenario has to be able to say "a Hydra Mercenary is in play" without first
# naming a zone to put it in.
CREATING_VERBS = ("in_play", "revealed")

CARD_ID = re.compile(r"^\d{5}[a-z]?(,\d{5}[a-z]?)*$", re.IGNORECASE)

_NAME_INDEX: Dict[str, List[str]] = {}


CARD_DATASET = "../datasets/cards/cards.json"


def NameIndex() -> Dict[str, List[str]]:
    """Printed name -> the card ids that carry it.

    A scenario names cards the way the card does. `CardFactory.GenerateCard`
    wants an id, so something has to bridge the two, and that belongs in the
    runner rather than in the scenario -- otherwise every spec would be written
    against ids and become unreadable.

    Names come from `datasets/cards/`, not from the engine's own
    `data/cards.json`. The two disagree about 51 names: some are engine typos
    ("Sinister Synchonization"), and 21141/21142 have each other's names
    outright. Indexing the engine's copy would quietly resolve a correctly
    spelled scenario to the wrong card, which is exactly what MARVEL-19 built
    the dataset to prevent.

    The index is restricted to cards the engine can actually generate, so a name
    the dataset knows but the engine does not fails as "no card is named that"
    rather than at `CardFactory`.
    """
    global _NAME_INDEX
    if _NAME_INDEX:
        return _NAME_INDEX

    from cards.database import CardsDB

    index: Dict[str, List[str]] = {}
    for card_id, name in PrintedNames().items():
        if card_id not in CardsDB.papers:
            continue
        index.setdefault(name.casefold(), []).append(card_id)
    _NAME_INDEX = index
    return index


def PrintedNames() -> Dict[str, str]:
    """card id -> printed name, from the spec-authoring dataset."""
    import json
    import os

    if not os.path.exists(CARD_DATASET):
        raise SetupError(
            f"{CARD_DATASET} is missing; run `python -m tools.cards.extract` "
            f"from py_src/ to build the card dataset")

    with open(CARD_DATASET, "r", encoding="utf-8") as handle:
        data = json.load(handle)

    names: Dict[str, str] = {}
    for card in data.get("cards", []):
        # Unique cards are printed with a leading bullet that is not part of the
        # name anyone would type.
        name = str(card.get("name", "")).lstrip("* ").strip()
        card_id = str(card.get("card_id", ""))
        if name and card_id:
            names[card_id] = name
    return names


def ResolveCardId(text: str, packs: Tuple[str, ...] = ()) -> str:
    """A card id from what the scenario wrote, which may be a printed name."""
    written = text.strip().strip('"')
    if CARD_ID.match(written):
        return written

    found = NameIndex().get(written.casefold())
    if not found:
        raise SetupError(
            f"no card is named {written!r}; write the printed name or a card id")
    if len(found) == 1:
        return found[0]

    narrowed = [card_id for card_id in found if card_id[:2] in packs]
    if len(narrowed) == 1:
        return narrowed[0]

    candidates = narrowed or found
    raise SetupError(
        f"{written!r} is the printed name of {len(candidates)} cards "
        f"({', '.join(sorted(candidates))}); use the card id to say which")


# Zones a scenario writes top-first: the first card named is the top of the
# pile, and so the next one drawn, revealed or looked at.
#
# The top is the only end the game has a name for. Effects say "look at the top
# card of the encounter deck", "put this card on top of your deck", "reveal the
# top card"; nothing in the game addresses the bottom, and a deck is shuffled
# before play anyway, so the bottom is not a position a scenario has any reason
# to describe. A literal read bottom-first would make an author reverse the line
# in their head to answer the only question the rules ever ask of a deck.
#
# `my hand is` is deliberately absent. A hand has no top -- its order decides
# nothing but which copy is `#1`, and that is creation order, which is written
# order either way.
#
# **Order does not survive a shuffle.** These stack a scene, they do not pin the
# deck for the rest of the game: an encounter deck that runs out is reshuffled
# from its discard pile, and after that the order is the RNG's. A scenario that
# reaches a reshuffle must not depend on what comes next.
TOP_FIRST_ZONES = frozenset({
    "player_deck", "player_discard", "player_set_aside",
    "encounter_deck", "encounter_discard",
})


def StackTopFirst(world: Any, before: "frozenset[int]") -> None:
    """Restack the cards a creating step just made, first-written on top.

    Two orderings have to run the same way here and the engine's list runs them
    opposite ways, which is the whole reason this function exists:

      **Creation order** decides `#N` -- `FindCards` returns matches in
      object-id order, so `"Hydra Mercenary #1"` is the first one the scenario
      *wrote*. That is a promise the format makes (MARVEL-42) and it has to
      keep, so the cards are created in written order and left that way.

      **Deck position** decides what is drawn -- `Deck.GetTop` is `cards[-1]`,
      so the engine's top is the *end* of its list, and creating in written
      order puts the first card written at the bottom.

    So the cards are made in written order and then restacked. Reversing the
    list handed to `RunPuzzle` instead would fix the draw order and silently
    redefine `#1` as the last card written, which is a worse bug than the one it
    fixes: a scenario would keep passing while meaning something else.

    Each card is moved to the bottom in written order, so the first one written
    ends up on top. `Insert` is the supported move -- it takes the card out of
    the list before putting it back -- and every side effect it has is a no-op
    for a card already sitting in this deck.

    See MARVEL-82.
    """
    made = [world.object_manager.card_dict[object_id]
            for object_id in sorted(set(world.object_manager.card_dict) - before)]
    for card in made:
        area = card.area
        if area is None:
            # A creating step whose cards did not land in a zone is not
            # something to paper over silently -- but it is also not this
            # function's business to diagnose, and the run will fail on its own.
            continue
        area.Insert(0, card)


def SeatOf(world: Any, index: int) -> Any:
    """The player in seat `index`, 0-based, or a `SetupError` saying how many.

    **Seat order, not turn order.** `world.players` is rotated by one at the end
    of every round to pass the first player token and loses a player outright on
    elimination, so it is not a stable way to name a seat;
    `const_seat_order_players` is built once and never reordered.
    """
    seats = world.const_seat_order_players
    if index >= len(seats):
        raise SetupError(
            f"this game has {len(seats)} player(s), so there is no player "
            f"{index + 1}; add one with 'the heroes are \"<a>\", \"<b>\"'")
    return seats[index]


# `create_player` verb -> the deck attribute on `Player` it stocks. Kept beside
# `SeatOf` rather than in `GIVEN_VERBS` alone so the indirection is readable:
# the catalogue says which sentence, `GIVEN_VERBS` says which deck, and this is
# where a name is turned into the object.
def PlayerZone(player: Any, attribute: str) -> Any:
    zone = getattr(player, attribute, None)
    if zone is None:
        raise SetupError(f"a player has no {attribute!r} deck")
    return zone


def ApplyGivenStep(world: Any, puzzle: Any, step: GivenStep,
                   packs: Tuple[str, ...] = (),
                   created: "Set[int]|None" = None) -> None:
    kind, method_name = GIVEN_VERBS[step.verb]

    if kind == "create_player":
        # One seat's own zone. `RunPuzzle`'s four helpers all stock
        # `GetFirstPlayer()`, so the second player's deck is unreachable through
        # them (MARVEL-101); the call they make is this one.
        from game.card.factory import CardFactory

        area = PlayerZone(SeatOf(world, step.player), method_name)
        before = frozenset(world.object_manager.card_dict)
        for card in step.cards:
            CardFactory.GenerateCard(ResolveCardId(card, packs), area, world)
        if step.verb in TOP_FIRST_ZONES:
            StackTopFirst(world, before)
        return

    method = getattr(puzzle, method_name)

    if kind == "create":
        # These generate new cards, so every name has to become an id first.
        before = frozenset(world.object_manager.card_dict)
        method(*[ResolveCardId(card, packs) for card in step.cards])
        if step.verb in TOP_FIRST_ZONES:
            StackTopFirst(world, before)
        return

    if kind == "value":
        method(step.value)
        return

    face = ResolveOrCreateFace(world, puzzle, step, packs)

    if kind == "card_value":
        method(face, step.value)
        return

    if kind == "card_named_value":
        method(face, step.name, step.value)
        return

    # kind == "card": several of these are toggles in `RunPuzzle`, so a spec
    # that says "is stunned" about an already-stunned card would un-stun it.
    # Given is declarative; make it so.
    if step.verb in ("stunned", "confused", "tough"):
        if StatusPresent(face, step.verb):
            return
        method(face)
        return

    if step.verb in ("hero_form", "alter_ego_form"):
        ChangeToForm(puzzle, face, step.verb)
        return

    if step.verb in CREATING_VERBS:
        RejectDuplicateCreation(face, step, created)

    method(face)


def RejectDuplicateCreation(face: Any, step: GivenStep,
                            created: "Set[int]|None") -> None:
    """Refuse a creating step that would act on a card twice (MARVEL-42).

    Given is declarative, so a second `"Hydra Mercenary" is in play` resolves to
    the card the first one created. The scenario then runs with **one** minion
    while reading as though it has two, which is the silent-wrong-pass this
    harness exists to refuse.

    Both creating verbs need this, for different reasons. `PutIntoPlay` on a
    card already in play quietly does nothing. `Reveal` is worse: it re-runs the
    whole reveal pipeline -- `WhenCardWouldReveal`, `WhenPlayerRevealCard`, the
    revealing-area move -- so a repeat double-fires reveal triggers rather than
    no-opping.

    An author who writes the line twice means two copies. The way to get them is
    to create both and address them by ordinal, which stays legal after the
    first one moves because ordinals track provenance, not zone:

        Given the encounter deck is "Hydra Mercenary", "Hydra Mercenary"
        And "Hydra Mercenary #1" is in play
        And "Hydra Mercenary #2" is in play
    """
    def refuse(why: str) -> None:
        raise SetupError(
            f"{step.Describe()}: {why}. If you meant a second copy, create both "
            f"and name them by ordinal — the encounter deck is \"{face.name}\", "
            f"\"{face.name}\" then \"{face.name} #1\" / \"{face.name} #2\".")

    object_id = face.card.object_id

    if created is not None and object_id in created:
        refuse(f"an earlier Given already used {face.name}, so this step "
               f"repeats it rather than adding a copy")

    # Catches a card the scenario setup already put on the board, which no
    # earlier Given would have recorded.
    if step.verb == "in_play" and face.card.IsOnField():
        refuse(f"{face.name} is already in play, so this step changes nothing")

    if created is not None:
        created.add(object_id)


def ResolveOrCreateFace(world: Any, puzzle: Any, step: GivenStep,
                        packs: Tuple[str, ...] = ()) -> Any:
    """The card a `Given` names, generating it when the verb is one that may.

    Only `in_play` and `revealed` create, and only from a bare card id that
    matches nothing yet. Every other verb must name a card already on the
    board, so a typo'd name is an error rather than a silently conjured card
    sitting in the aside deck -- which is what `RunPuzzle.FindOrCreateFace`
    does today.

    An *ambiguous* ref never creates. "This id already means two cards" and
    "this id means nothing yet" are opposite problems, and answering the first
    one by manufacturing a third copy would be the silent first-match this
    module exists to refuse.
    """
    ref = step.cards[0]
    try:
        return ResolveFace(world, ref)
    except AmbiguousCardRef:
        raise
    except CardRefError:
        if step.verb not in CREATING_VERBS:
            raise
        # A creating verb may name a card that is not on the board yet, by
        # printed name or by id. Anything that is neither stays an error.
        card_id = ResolveCardId(ref, packs)
    return puzzle.CreateCard(card_id)


def StatusPresent(face: Any, verb: str) -> bool:
    from game.card.face.attribute.can_status import CanStatus

    if not CanStatus.IsType(face):
        raise SetupError(f"{face.name} cannot hold statuses")
    return {
        "stunned": face.IsStunned,
        "confused": face.IsConfused,
        "tough": face.IsTough,
    }[verb]()


def ChangeToForm(puzzle: Any, face: Any, verb: str) -> None:
    """Flip an identity to hero or alter-ego, only if it is not there already."""
    from game.card.face.card_type import Hero, Identity

    if not Identity.IsType(face):
        raise SetupError(f"{face.name} is not an identity, so it has no form to change")

    wants_hero = verb == "hero_form"
    if bool(Hero.IsType(face)) == wants_hero:
        return
    puzzle.ChangeForm(face, "Identity")


################################################################################
# Run

def RunCase(case: SpecCase, *, max_decisions: int = 200) -> CaseResult:
    """Play one case. Never raises for a failing spec -- that is a result."""
    EnsureEngine()

    policy = TranscriptPolicy(beats=tuple(case.beats), max_decisions=max_decisions)

    with CaptureEngineOutput() as buffer:
        result = RunCaseInternal(case, policy)
        result.engine_log = TailLog(buffer)

    if result.outcome == OUTCOME_PASS:
        # The engine catches exceptions raised while broadcasting a message,
        # logs them and keeps playing (see `game/message/message.py`). A case
        # that "passed" over a swallowed traceback has proved nothing.
        internal = InternalError(result.engine_log)
        if internal:
            result.outcome = OUTCOME_ERROR
            result.message = f"the engine logged an error during the run: {internal}"

    return result


def InternalError(engine_log: str) -> str:
    """The first engine-logged failure in a captured log, if any."""
    from engine.log import Log

    if not Log.HasError(error=True):
        return ""
    for line in engine_log.splitlines():
        stripped = line.strip()
        if stripped.startswith("<F>") or stripped.startswith("<E>"):
            return stripped
    return "see the engine log"


def NewGameForCase(case: SpecCase, policy: TranscriptPolicy) -> Any:
    """A `Game` wired to `policy`, with the case's puzzle scene loaded.

    Stops short of `GameSetup()` so callers that want the world without playing
    the transcript -- the harness itself, and tests that inspect setup -- share
    one definition of how a case becomes a game.
    """
    from engine import Engine
    from engine.device.manager.bot.manager import BotDeviceManager
    from game.game import Game
    from game.statistics.game_statistics import GameStatistics

    statistics = GameStatistics()
    statistics.Load()
    Engine.statistics = statistics

    device_manager = BotDeviceManager(policy)
    Engine.device_manager = device_manager

    game = Game(statistics, device_manager)
    Engine.game = game

    scene = BuildPuzzleScene(case)
    game.session.SetScene(scene, "Replay")
    game.controller_manager.skip.SetSkipTo(0)
    return game


def RunCaseInternal(case: SpecCase, policy: TranscriptPolicy) -> CaseResult:
    try:
        game = NewGameForCase(case, policy)
    except SetupError as exc:
        return CaseResult(case=case, outcome=OUTCOME_UNPLAYABLE, message=str(exc))

    try:
        if not game.GameSetup():
            world = game.world
            reason = world.game_over.reason if world else "unknown"
            return CaseResult(
                case=case, outcome=OUTCOME_UNPLAYABLE,
                message=f"the game ended during setup: {reason}")

        world = game.world
        if world is None:
            return CaseResult(case=case, outcome=OUTCOME_ERROR,
                              message="the engine produced no world")

        ApplyGiven(world, case)
        game.GameLoop()
    except SetupError as exc:
        return CaseResult(case=case, outcome=OUTCOME_UNPLAYABLE, message=str(exc),
                          decisions=policy.records)
    except Exception as exc:
        return CaseResult(case=case, outcome=OUTCOME_ERROR,
                          message=f"{type(exc).__name__}: {exc}",
                          decisions=policy.records)

    return Judge(case, policy, game)


def Judge(case: SpecCase, policy: TranscriptPolicy, game: Any) -> CaseResult:
    """Turn a completed run into a result.

    The policy has already evaluated every beat as it reached it -- assertions
    between decisions cannot be judged anywhere else -- so this only has to
    settle anything the game ended before reaching, and decide the outcome.
    """
    world = game.world
    if not policy.halted:
        # The game ended without the policy reaching its stop, so nothing has
        # judged the tail of the transcript yet.
        policy.Finish(world)

    state = policy.state
    if state is None and world is not None:
        state = Capture(world)

    if policy.failure:
        return CaseResult(
            case=case, outcome=OUTCOME_UNPLAYABLE, state=state,
            assertions=policy.results, decisions=policy.records,
            message=policy.failure + "\n" + DescribeTrail(policy.records))

    results = policy.results
    failures = [result for result in results if not result.passed]

    if not failures:
        if not results:
            return CaseResult(case=case, outcome=OUTCOME_ERROR,
                              decisions=policy.records, state=state,
                              message="the transcript asserted nothing")
        return CaseResult(case=case, outcome=OUTCOME_PASS, assertions=results,
                          decisions=policy.records, state=state)

    # A Then that names a card or property this game does not have describes a
    # different game, not a different outcome.
    outcome = OUTCOME_UNPLAYABLE if any(f.unresolvable for f in failures) else OUTCOME_ASSERTION
    return CaseResult(case=case, outcome=outcome, assertions=results,
                      decisions=policy.records, state=state,
                      message="" if policy.halted else
                              "the game ended before the transcript finished")


def RunCases(cases: Sequence[SpecCase], *, max_decisions: int = 200) -> List[CaseResult]:
    return [RunCase(case, max_decisions=max_decisions) for case in cases]
