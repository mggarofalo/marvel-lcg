"""Replay corpus scenes in this process and watch every decision go past.

`main.py -verify_replays` replays a corpus and answers one question: did each
step reproduce its recorded digest? That is the right question for the corpus
gate and the wrong one for MARVEL-163 and MARVEL-164, which need to see the
*engine* at each step -- its areas, the effects it offered, the input the
recording answered with -- none of which survives into a digest.

So this drives the same replay in-process and hands each step to a callback.

## The seam

`Controller.ChoiceOne` is the only place the engine stops and asks. It is also
where the per-step digest is taken, so a snapshot made here is the state that
digest describes, by construction rather than by argument. Wrapping it gives a
callback four things a digest cannot carry:

    world      real objects, so an area is an identity rather than a name
    effects    what the engine offered, before anything chose
    recorded   what the corpus answered with, if the recording still has a step
    message    the timing point the decision opened at

The wrapper mirrors `ChoiceOne`'s two early returns. A call that the engine
would drop -- no effects, or a game already over -- is not a step, and counting
it would put the callback's step numbering out of step with the recording's.

## What this does not change

Nothing. The replay is the ordinary one: the same `VerifyDeviceManager`, the
same digest comparison at every step, the same verdict. If a scene diverges,
`Log.HasError` trips and `Replay` says so. An observation harness that quietly
disabled the oracle it observes would be worse than no harness.

Reads shards in place; a scene is written to a temporary file only because
`SceneLoader.Load` takes a path.
"""

from __future__ import annotations

import contextlib
import os
import tempfile
from dataclasses import dataclass, field
from typing import Any, Callable, Dict, Iterator, List, Optional, Sequence, Tuple

from tools.corpus.expand import read_shard, shard_paths


@dataclass
class Observation:
    """One decision, as the engine posed it."""

    scene: str
    step: int
    world: Any
    player_id: int
    effects: Sequence[Any]
    by_effect: Any
    message: Any
    recorded: Any = None

    @property
    def trigger(self) -> str:
        """The timing point that opened this, e.g. `WhenPlayerChooseAbility`.

        Read from the recording rather than the message, so it is the same
        string `tools/events/census.py` counted and the same one an event
        carries. Empty once the recording runs out.
        """
        if self.recorded is None:
            return ""
        return _Trigger(self.recorded.event)

    @property
    def verb(self) -> str:
        """What the recording chose, e.g. `Play`, `Attack`, `Choose`."""
        if self.recorded is None:
            return ""
        return _Verb(self.recorded.effect.id)


@dataclass
class SceneResult:
    scene: str
    steps: int = 0
    completed: bool = False
    error: str = ""


def _Trigger(event_name: str) -> str:
    """`m217 WhenPlayerChooseAbility` -> `WhenPlayerChooseAbility`."""
    parts = str(event_name).split(" ", 1)
    return parts[1] if len(parts) == 2 else str(event_name)


def _Verb(effect_id: Any) -> str:
    """`e1 Choose c1 32001b` -> `Choose`. `e1 c1 32001b` -> `""`.

    The same split `tools/events/census.py` uses. A debug command (`:...`) and
    a decline (`""`) both come back empty, which is correct: neither names a
    verb.
    """
    text = str(effect_id)
    if not text or text.startswith(":") or text.startswith("Puzzle."):
        return ""
    parts = text.split(" ")
    if len(parts) >= 2 and not parts[1].startswith("c"):
        return parts[1]
    return ""


def initialize() -> None:
    """Boot the engine far enough to load a scene. Idempotent."""
    global _initialized
    if _initialized:
        return

    from tools.determinism.headless import _initialize_engine

    _initialize_engine()
    _initialized = True


_initialized = False


@contextlib.contextmanager
def _hooked(on_choice: Callable[[Any, Sequence[Any], Any, Any], None]) -> Iterator[None]:
    """Wrap `Controller.ChoiceOne` for the duration of a replay."""
    from engine.controller.controller import Controller

    original = Controller.ChoiceOne

    def wrapper(self, effect_list, by_effect, message, priority, is_forced):  # type: ignore[no-untyped-def]
        # `ChoiceOne`'s own two early returns, mirrored. Neither becomes a step
        # in the replay history, so neither may become an observation.
        if len(effect_list) != 0 and self.game.state.is_running:
            on_choice(self, effect_list, by_effect, message)
        return original(self, effect_list, by_effect, message, priority, is_forced)

    Controller.ChoiceOne = wrapper  # type: ignore[method-assign]
    try:
        yield
    finally:
        Controller.ChoiceOne = original  # type: ignore[method-assign]


def _new_game() -> Any:
    """A game wired to the verify device, the same one `-verify_replays` uses."""
    from engine import Engine
    from engine.device.manager.verify.manager import VerifyDeviceManager
    from game.game import Game
    from game.statistics.game_statistics import GameStatistics
    from game.test import Test

    statistics = GameStatistics()
    statistics.Load()
    Engine.statistics = statistics

    device_manager = VerifyDeviceManager()
    Engine.device_manager = device_manager

    game = Game(statistics, device_manager)
    Engine.game = game

    Test.is_in_test = True
    return game


def replay_text(scene: str, text: str,
                on_step: Callable[[Observation], None],
                *, max_steps: int = 0) -> SceneResult:
    """Replay one scene's text, calling `on_step` at every decision."""
    from engine.log import Log
    from game.test import Test
    from game.test.test_run import TestRun

    initialize()
    game = _new_game()
    result = SceneResult(scene=scene)

    class Budget(Exception):
        pass

    def on_choice(controller: Any, effects: Sequence[Any], by_effect: Any,
                  message: Any) -> None:
        replay = controller.manager.replay
        step = replay.replay_step_id
        recorded = (replay.replay_inputs[step]
                    if step < len(replay.replay_inputs) else None)
        on_step(Observation(
            scene=scene,
            step=step,
            world=message.world,
            player_id=controller.player_id,
            effects=effects,
            by_effect=by_effect,
            message=message,
            recorded=recorded,
        ))
        result.steps += 1
        if max_steps and result.steps >= max_steps:
            raise Budget()

    folder = tempfile.mkdtemp(prefix="observe-")
    path = os.path.join(folder, os.path.basename(scene))
    try:
        with open(path, "w", encoding="utf-8") as handle:
            handle.write(text)

        Test.test_cases = [path]

        # `Log.log_statistics` is process-global and `TestRun.Run` reads a
        # scene's verdict out of it, so without this the first scene to log an
        # error would fail every scene after it.
        Log.Setup()

        with _hooked(on_choice):
            try:
                result.completed = bool(TestRun.Run(game, [path], do_save=False))
            except Budget:
                result.completed = False
            except Exception as exc:  # reported, never swallowed
                result.error = f"{type(exc).__name__}: {exc}"
    finally:
        try:
            os.remove(path)
            os.rmdir(folder)
        except OSError:
            pass

    return result


def scenes(root: str, *, only: Sequence[str] = (), limit: int = 0,
           per_shard: int = 0) -> Iterator[Tuple[str, str]]:
    """`(scene path, scene text)` from the frozen shards, in a stable order.

    `per_shard` samples the same way `tools/events/census.py` does -- the first
    *n* of each shard by name, so a partial run still spans every campaign
    rather than exhausting the alphabet's first one.
    """
    produced = 0
    for path in shard_paths(root, only):
        bundle = read_shard(path)
        taken = 0
        for scene_path in sorted(bundle):
            if per_shard and taken >= per_shard:
                break
            yield scene_path, bundle[scene_path]
            taken += 1
            produced += 1
            if limit and produced >= limit:
                return
