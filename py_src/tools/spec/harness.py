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
from typing import Any, Dict, Iterator, List, Optional, Sequence, Tuple

from tools.spec.assertions import AssertionResult
from tools.spec.case import GIVEN_KIND, GIVEN_VERBS, GivenStep, SpecCase
from tools.spec.policy import DecisionRecord, DescribeTrail, TranscriptPolicy
from tools.spec.resolve import AmbiguousCardRef, CardRefError, ResolveCard, ResolveFace
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


def BuildPuzzleScene(case: SpecCase) -> Any:
    """The scene dict `play_puzzle` builds, minus the pre-command strings.

    A puzzle scene deliberately has no encounter deck, no modular sets and no
    player deck: `Given` puts on the board exactly what the spec names, and
    nothing arrives that the spec did not ask for.
    """
    from engine.lib import Json
    from game.scene.scene import Scene

    scenario = LoadScenarioJson(case.scenario)
    heroes = [LoadHeroJson(name) for name in case.heroes]
    version = str(scenario.get("version", ""))

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
            "encounters": [],
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
                "player_deck": [],
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

    for position, step in enumerate(case.given, start=1):
        try:
            ApplyGivenStep(world, puzzle, step, packs)
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


def ApplyGivenStep(world: Any, puzzle: Any, step: GivenStep,
                   packs: Tuple[str, ...] = ()) -> None:
    kind, method_name = GIVEN_VERBS[step.verb]
    method = getattr(puzzle, method_name)

    if kind == "create":
        # These generate new cards, so every name has to become an id first.
        method(*[ResolveCardId(card, packs) for card in step.cards])
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

    method(face)


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


def RunCaseInternal(case: SpecCase, policy: TranscriptPolicy) -> CaseResult:
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

    try:
        scene = BuildPuzzleScene(case)
    except SetupError as exc:
        return CaseResult(case=case, outcome=OUTCOME_UNPLAYABLE, message=str(exc))

    game.session.SetScene(scene, "Replay")
    game.controller_manager.skip.SetSkipTo(0)

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
