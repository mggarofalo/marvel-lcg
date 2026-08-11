"""Boot the engine without a human, far enough to produce per-step digests.

The engine blocks a thread inside `Controller.ChoiceOne` waiting for a
websocket or a keypress. `InputDevice.GetInput` is `@final`, but it delegates
to `DeviceManager.DoGetInput`, so a `DeviceManager` subclass is the supported
seam for driving the engine from code.

`NullDeviceManager` supplies the device; `decide` supplies the answers. Two are
available and the difference is not a detail:

  `decline_everything`  the empty command for every prompt. Still exercises
                        setup (deck shuffles, object id allocation, encounter
                        deck construction), the whole villain phase and every
                        forced ability -- where the ordering hazards in
                        `docs/determinism-audit.md` live.
  `PolicyDriver`        answers from a real `BotPolicy`, so the harness plays
                        cards. Roughly doubles the step count and opens the
                        response windows a decline-only game never reaches.

The decline-only driver was the only one for a while, and it quietly bounded
what the harness could prove: `probe_forced_selection` found 60 forced-ability
batches and not one with a second candidate, so the first-player tie-break --
where MARVEL-39 and MARVEL-40 both live -- was never reached, and no digest
evidence about either fix meant anything. `build_decide("first")` reaches it.
See MARVEL-69.
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


class PolicyDriver:
    """Answer decisions from a `BotPolicy` instead of declining them.

    This module shipped with `decline_everything` because no bot existed yet,
    and said to swap the callback once one did. This is that swap, and it is
    not cosmetic: a decline-only game never plays a card, so it never opens the
    response windows where two forced abilities meet on one message. That is why
    `probe_forced_selection` reported 60 batches and zero with more than one
    candidate -- the tie-break MARVEL-39 and MARVEL-40 both live inside was
    never reached, and every digest-based argument about them was vacuous. See
    MARVEL-69.

    The policy sees exactly what it sees under the real bot device: an
    `AskOptionPayload` parsed into a `BotDecision`. `attempt` is tracked here
    the same way `BotDeviceManager.SupplyInput` tracks it, because a policy that
    is being corrected must be told so -- `FirstLegalPolicy` walks down its
    option list on `attempt` alone.
    """

    def __init__(self, policy: Any) -> None:
        self.policy = policy
        self.attempt_key: Tuple[int, int] = (-1, -1)
        self.attempt = 0

    def __call__(self, player_id: int, payload: Any) -> str:
        from engine import Engine
        from engine.device.manager.bot.command import BotCommand
        from engine.device.manager.bot.policy import BotDecision, BotOptionParser

        game = Engine.game
        step_id = game.controller_manager.replay.current_step_id

        key = (player_id, step_id)
        if key != self.attempt_key:
            self.attempt_key = key
            self.attempt = 0
        else:
            self.attempt += 1

        decision = BotDecision(
            player_id    = player_id,
            step_id      = step_id,
            attempt      = self.attempt,
            event_name   = payload.event_name,
            ability_type = payload.ability_type,
            prompt_text  = payload.prompt_text,
            can_cancel   = payload.show_cancel,
            options      = BotOptionParser.Parse(payload.options_json),
            replay_input = payload.replay_input,
            world        = game.world,
        )
        return BotCommand.ToJson(self.policy.Choose(decision))


def build_decide(policy_name: str, policy_seed: int=0) -> DecideFn:
    """`decline`, or any name `BotPolicyFactory` knows (`first`, `random`)."""
    if policy_name == "decline":
        return decline_everything

    from engine.device.manager.bot.policies import BotPolicyFactory

    policy = BotPolicyFactory.Create(policy_name, policy_seed)
    policy.OnGameStart(policy_seed)
    return PolicyDriver(policy)


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
    policy = argv[5] if len(argv) > 5 else "decline"
    policy_seed = int(argv[6]) if len(argv) > 6 else 0

    result = run_headless(campaign, heroes, seed, max_steps=max_steps,
                          decide=build_decide(policy, policy_seed))
    print("<<<RESULT>>>" + result.to_json())
    return 0


if __name__ == "__main__":
    raise SystemExit(_main(sys.argv))
