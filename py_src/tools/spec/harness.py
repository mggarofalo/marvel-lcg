"""Run one `SpecCase` against the Python engine and report what happened.

The shape of a run:

1. Boot the engine once per process (`EnsureEngine`) -- card database, config,
   job and task managers. Roughly half a second, and a suite pays it once.
2. Build a **puzzle scene**: villain, main scheme and identities, and nothing
   else. `GameServerNewGame.play_puzzle` builds the same thing for the web
   client. A puzzle starts with no encounter deck and no player deck, so the
   board contains only what the spec asks for.
3. `GameSetup()`, then apply the `Given` steps through `RunPuzzle`.
4. `GameLoop()`, with `ScriptedPolicy` answering decisions. It plays the `When`
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

from tools.spec.assertions import AssertionResult, EvaluateAll
from tools.spec.case import GIVEN_KIND, GIVEN_VERBS, GivenStep, SpecCase
from tools.spec.policy import DecisionRecord, DescribeTrail, ScriptedPolicy
from tools.spec.resolve import CardRefError, ResolveCard, ResolveFace
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

def LoadScenarioJson(name: str) -> Dict[str, Any]:
    from engine.file import FileManager
    from engine.lib import Json

    path = FileManager.FindJsonPath("Campaign", name, nullable=True)
    if not path:
        raise SetupError(f"scenario {name!r} not found under data/scenarios/")
    with FileManager.OpenFile(path, read=True) as file:
        return Json.Loads(file.Read())


def LoadHeroJson(name: str) -> Dict[str, Any]:
    from engine.file import FileManager
    from engine.lib import Json

    path = FileManager.FindJsonPath("Hero", name, nullable=True)
    if not path:
        raise SetupError(f"hero {name!r} not found under deck/")
    with FileManager.OpenFile(path, read=True) as file:
        return Json.Loads(file.Read())


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

def ApplyGiven(world: Any, steps: Sequence[GivenStep]) -> None:
    """Apply the setup commands, in order, through `RunPuzzle`."""
    from game.puzzle.puzzle import RunPuzzle

    puzzle = RunPuzzle(world)

    for position, step in enumerate(steps, start=1):
        try:
            ApplyGivenStep(world, puzzle, step)
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

CARD_ID = re.compile(r"^\d{5}[a-z]?$", re.IGNORECASE)


def ApplyGivenStep(world: Any, puzzle: Any, step: GivenStep) -> None:
    kind, method_name = GIVEN_VERBS[step.verb]
    method = getattr(puzzle, method_name)

    if kind == "create":
        # Card ids only -- these generate new cards rather than finding them.
        method(*step.cards)
        return

    if kind == "value":
        method(step.value)
        return

    face = ResolveOrCreateFace(world, puzzle, step)

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


def ResolveOrCreateFace(world: Any, puzzle: Any, step: GivenStep) -> Any:
    """The card a `Given` names, generating it when the verb is one that may.

    Only `in_play` and `revealed` create, and only from a bare card id. Every
    other verb must name a card already on the board, so a typo'd name is an
    error rather than a silently conjured card sitting in the aside deck --
    which is what `RunPuzzle.FindOrCreateFace` does today.
    """
    ref = step.cards[0]
    try:
        return ResolveFace(world, ref)
    except CardRefError:
        if step.verb not in CREATING_VERBS or not CARD_ID.match(ref.strip()):
            raise
    return puzzle.CreateCard(ref.strip())


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

    policy = ScriptedPolicy(steps=tuple(case.when), max_decisions=max_decisions)

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


def RunCaseInternal(case: SpecCase, policy: ScriptedPolicy) -> CaseResult:
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

        ApplyGiven(world, case.given)
        game.GameLoop()
    except SetupError as exc:
        return CaseResult(case=case, outcome=OUTCOME_UNPLAYABLE, message=str(exc),
                          decisions=policy.records)
    except Exception as exc:
        return CaseResult(case=case, outcome=OUTCOME_ERROR,
                          message=f"{type(exc).__name__}: {exc}",
                          decisions=policy.records)

    return Judge(case, policy, game)


def Judge(case: SpecCase, policy: ScriptedPolicy, game: Any) -> CaseResult:
    """Turn a completed run into a result."""
    state = policy.state
    if state is None:
        # The game finished without the policy reaching a free decision. That
        # is still a board worth asserting on -- a scenario may legitimately end
        # the game -- so snapshot whatever the engine finished with.
        world = game.world
        if world is not None:
            state = Capture(world)

    if policy.failure:
        return CaseResult(
            case=case, outcome=OUTCOME_UNPLAYABLE, state=state,
            decisions=policy.records,
            message=policy.failure + "\n" + DescribeTrail(policy.records))

    if not policy.completed:
        unplayed = policy.steps[policy.index]
        return CaseResult(
            case=case, outcome=OUTCOME_UNPLAYABLE, state=state,
            decisions=policy.records,
            message=(f"the game ended with When step {policy.index + 1} unplayed "
                     f"({unplayed.Describe()})\n" + DescribeTrail(policy.records)))

    if state is None:
        return CaseResult(case=case, outcome=OUTCOME_ERROR, decisions=policy.records,
                          message="no board state was captured")

    results = EvaluateAll(state, case.then)
    failures = [result for result in results if not result.passed]

    if not failures:
        return CaseResult(case=case, outcome=OUTCOME_PASS, assertions=results,
                          decisions=policy.records, state=state)

    # A Then that names a card or property this game does not have describes a
    # different game, not a different outcome.
    outcome = OUTCOME_UNPLAYABLE if any(f.unresolvable for f in failures) else OUTCOME_ASSERTION
    return CaseResult(case=case, outcome=outcome, assertions=results,
                      decisions=policy.records, state=state)


def RunCases(cases: Sequence[SpecCase], *, max_decisions: int = 200) -> List[CaseResult]:
    return [RunCase(case, max_decisions=max_decisions) for case in cases]
