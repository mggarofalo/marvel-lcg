"""Boot the engine without a human, far enough to produce per-step digests.

The engine blocks a thread inside `Controller.ChoiceOne` waiting for a
websocket or a keypress. `InputDevice.GetInput` is `@final`, but it delegates
to `DeviceManager.DoGetInput`, so a `DeviceManager` subclass is the supported
seam for driving the engine from code.

`NullDeviceManager` answers every prompt with the empty command -- "decline" /
"do nothing". That is not a bot and makes no attempt to play well; it exists so
the determinism harness has a driver today. When the real bot lands
(MARVEL-5), swap the `decide` callback and everything else here still applies.

The value of running at all is that a decline-only game still exercises setup
(deck shuffles, object id allocation, encounter deck construction), the whole
villain phase, and every forced ability -- which is where the ordering hazards
identified in `docs/determinism-audit.md` live.
"""

from __future__ import annotations

import hashlib
import json
import sys
from dataclasses import dataclass, field
from typing import Any, Callable, List, Sequence, Tuple


class StepBudgetExhausted(Exception):
    """Raised to unwind out of the engine once `max_steps` is reached."""


@dataclass
class Step:
    index: int
    player_id: int
    event_name: str
    digest: str


@dataclass
class RunResult:
    campaign: str
    heroes: List[str]
    seed: int
    steps: List[Step] = field(default_factory=list)
    object_index: dict[str, int] = field(default_factory=dict)
    game_over: bool = False
    error: str = ""

    def digest(self) -> str:
        """One hash covering every per-step digest, in order."""
        blob = "\n".join(f"{s.index}|{s.player_id}|{s.event_name}|{s.digest}" for s in self.steps)
        blob += "\n#objects " + json.dumps(self.object_index, sort_keys=True)
        return hashlib.sha256(blob.encode("utf-8")).hexdigest()

    def to_json(self) -> str:
        return json.dumps(
            {
                "campaign": self.campaign,
                "heroes": self.heroes,
                "seed": self.seed,
                "step_count": len(self.steps),
                "digest": self.digest(),
                "object_index": self.object_index,
                "game_over": self.game_over,
                "error": self.error,
                "steps": [
                    {"i": s.index, "p": s.player_id, "e": s.event_name, "digest": s.digest}
                    for s in self.steps
                ],
            },
            sort_keys=True,
        )


DecideFn = Callable[[int, Any], str]
"""(player_id, AskOptionPayload) -> a JSON CommandDescriptor string."""


def decline_everything(player_id: int, payload: Any) -> str:
    return "{}"


def _initialize_engine() -> None:
    """Replicates `Engine.Initialize` minus the device manager and the editor."""
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


def _build_device_manager(on_step: Callable[[int, Any], str]):
    from engine.device.base.input import InputDevice
    from engine.device.base.output import OutputDevice
    from engine.device.manager.base import DeviceManager

    class NullOutput(OutputDevice):
        def IsSyncReady(self) -> bool:
            return True

        def Render(self) -> None:
            pass

    class NullInput(InputDevice):
        def IsInputReady(self) -> bool:
            return True

        def IsConnect(self) -> bool:
            return True

    class NullDeviceManager(DeviceManager):
        def CreateDevices(self, controller):  # type: ignore[override]
            return NullOutput(controller, self), NullInput(controller, self)

        # No client to connect, no client to sync with. Both of the engine's
        # waits here are wall-clock bounded (see the audit), so they must be
        # short-circuited rather than left to time out.
        def DoWaitConnect(self, player_id, check):  # type: ignore[override]
            return

        def DoWaitSync(self, player_id, check):  # type: ignore[override]
            return

        def DoGetInput(self, data, player_id, check):  # type: ignore[override]
            return on_step(player_id, data)

        # Same reason `NullOutput.Render` is empty: there is no client, so the
        # WorldDescriptor built on every present is thrown away. The harness
        # runs the engine in hundreds of fresh processes, and the digests it
        # compares are built from the world rather than from render state, so
        # skipping the construction cannot move them. See MARVEL-29.
        def IsRenderNeeded(self):  # type: ignore[override]
            return False

    return NullDeviceManager()


def run_headless(
    campaign: str,
    heroes: Sequence[str],
    seed: int,
    *,
    max_steps: int = 2000,
    decide: DecideFn = decline_everything,
) -> RunResult:
    """Play one game with no human and return its per-step digest trace."""
    _initialize_engine()

    from engine import Engine
    from game.game import Game
    from game.scene.loader import SceneLoader
    from game.statistics.game_statistics import GameStatistics
    from game.test import Test

    result = RunResult(campaign=campaign, heroes=list(heroes), seed=seed)

    # `Test.IsInTesting()` suppresses the render/sync round trip and the
    # connect wait. Without it the engine tries to talk to a client.
    Test.is_in_test = True

    def on_step(player_id: int, payload: Any) -> str:
        world = Engine.game.world
        digest = world.render.CalculateDigest() if world else ""
        result.steps.append(
            Step(
                index=len(result.steps),
                player_id=player_id,
                event_name=payload.event_name,
                digest=digest,
            )
        )
        if len(result.steps) >= max_steps:
            raise StepBudgetExhausted()
        return decide(player_id, payload)

    statistics = GameStatistics()
    statistics.Load()
    Engine.statistics = statistics

    device_manager = _build_device_manager(on_step)
    Engine.device_manager = device_manager

    game = Game(statistics, device_manager)
    Engine.game = game

    scene = SceneLoader.NewScene(campaign, None, list(heroes), seed)
    game.session.SetScene(scene, "Load")

    try:
        if game.GameSetup():
            game.GameLoop()
    except StepBudgetExhausted:
        pass
    except Exception as exc:  # pragma: no cover - reported, not swallowed
        result.error = f"{type(exc).__name__}: {exc}"

    world = game.world
    if world is not None:
        result.object_index = dict(world.object_manager.index_dict)
        result.game_over = bool(world.is_game_over)

    return result


def _main(argv: List[str]) -> int:
    """`python -m tools.determinism.headless <campaign> <hero,hero> <seed> [max_steps]`

    Prints the run as one JSON object so a parent process can diff it.
    """
    if len(argv) < 4:
        print(_main.__doc__, file=sys.stderr)
        return 2
    campaign = argv[1]
    heroes = argv[2].split(",")
    seed = int(argv[3])
    max_steps = int(argv[4]) if len(argv) > 4 else 2000

    result = run_headless(campaign, heroes, seed, max_steps=max_steps)
    print("<<<RESULT>>>" + result.to_json())
    return 0


if __name__ == "__main__":
    raise SystemExit(_main(sys.argv))
