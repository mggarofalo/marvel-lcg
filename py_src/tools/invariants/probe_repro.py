"""Probe: does a violation actually produce a replay that reproduces it?

MARVEL-11 claims two things a unit test cannot check. Both are about what the
rest of the engine does to the checker, not about the rules themselves:

1. **The abort is not swallowed.** `InvariantViolation` is raised from inside
   `Controller.ChoiceOne`, which runs underneath `EffectInvoker`,
   `Message2.Send` and `Engine.EngineRun` -- all of which catch broadly so one
   bad card cannot end the game. `Log.OnCrash` re-raises `EngineIntegrityError`
   regardless of `Build.release`, but that is a claim about a code path, and the
   same claim was wrong once before (MARVEL-32, `probe_fabricated_input.py`).
2. **The dump is a repro.** The scene is written mid-game, holding steps
   `0..n-1` because `ChoiceOne` pushes step *n* only after it is answered. That
   should mean reloading it and replaying reaches exactly the failing step.

So this probe injects a rule that fires at a chosen step of an ordinary bot
game, then asks the engine both questions in order: did the run abort without
saving, and does the file it left behind trip the same rule at the same step.

Run:  python -m tools.invariants.probe_repro
      python -m tools.invariants.probe_repro --step 12 --seed 4242

Exit code 0 means the violation aborted the run *and* the repro reproduced it.
"""

from __future__ import annotations

import argparse
import os
import shutil
import sys
import tempfile
from typing import Any, List

RULE = "probe/injected"

# Late enough to be inside ordinary play rather than setup, so the abort has to
# travel back up through the ability and message dispatch handlers.
DEFAULT_STEP = 10


class Outcome:
    def __init__(self) -> None:
        self.fired_at: int = -1
        self.run_ok: bool | None = None
        self.raised = ""
        self.saved: List[str] = []
        self.repro = ""
        self.recorded_inputs = -1
        self.replay_fired_at = -1
        self.replay_raised = ""


def _install_rule(outcome: Outcome, step: int) -> None:
    """Replace the rule set with one that fires once, at `step`.

    Patching `Check` rather than corrupting the world keeps the probe about the
    plumbing: what the engine does with a violation, not whether a particular
    rule can be provoked.
    """
    from game.world import invariants

    def CheckAt(world: Any, progress: Any=None) -> List[Any]:
        step_id = world.controller_manager.replay.current_step_id
        if step_id != step:
            return []
        return [invariants.Violation(RULE, "probe", f"injected at step {step_id}")]

    invariants.Check = CheckAt  # type: ignore[assignment]


def _generate(folder: str, save_folder: str, seed: int, step: int,
              outcome: Outcome) -> None:
    """Play a bot game that trips the injected rule."""
    sys.argv = [
        "main.py", "-bot",
        "-bot_seed", str(seed),
        "-bot_save_folder", save_folder.replace("\\", "/") + "/",
        "-invariant_folder", folder.replace("\\", "/") + "/",
    ]

    from engine import Engine

    if not Engine.Initialize():
        raise RuntimeError("engine failed to initialize")

    _install_rule(outcome, step)

    try:
        Engine.EngineRun()
        outcome.run_ok = Engine.exit_code == 0
    except Exception as exc:
        # Reaching the top is a fine outcome -- it is not swallowed either way.
        outcome.raised = f"{type(exc).__name__}: {exc}"
        outcome.run_ok = False

    outcome.saved = [name for name in sorted(os.listdir(save_folder))
                     if name.endswith(".json") and not name.startswith("bot-manifest-")]

    repros = [name for name in sorted(os.listdir(folder)) if name.endswith(".json")]
    if repros:
        outcome.repro = os.path.join(folder, repros[0])
        outcome.fired_at = step


def _replay(outcome: Outcome, step: int) -> None:
    """Load the repro and replay it. The same rule must fire at the same step."""
    from engine import Engine
    from engine.lib import Json
    from game.test import Test
    from game.test.test_run import TestRun
    from game.world import invariants

    scene = Json.Load(outcome.repro)
    outcome.recorded_inputs = len(scene.get("inputs", []))

    reached: List[int] = []
    original = invariants.Check

    def Recording(world: Any, progress: Any=None) -> List[Any]:
        found = original(world, progress)
        if found:
            reached.append(world.controller_manager.replay.current_step_id)
        return found

    invariants.Check = Recording  # type: ignore[assignment]

    Test.is_in_test = True
    Test.test_cases = [outcome.repro]
    try:
        TestRun.Run(Engine.game, [outcome.repro], do_save=False)
    except Exception as exc:
        outcome.replay_raised = f"{type(exc).__name__}: {exc}"
    finally:
        TestRun.RunEnd(Engine.game, False, True)
        invariants.Check = original  # type: ignore[assignment]

    if reached:
        outcome.replay_fired_at = reached[0]


def main(argv: List[str] | None=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--seed", type=int, default=4242)
    parser.add_argument("--step", type=int, default=DEFAULT_STEP)
    args = parser.parse_args(argv)

    folder = tempfile.mkdtemp(prefix="invariant-repro-")
    save_folder = tempfile.mkdtemp(prefix="invariant-scenes-")
    outcome = Outcome()
    try:
        _generate(folder, save_folder, args.seed, args.step, outcome)
        if outcome.repro:
            _replay(outcome, args.step)

        print(f"rule injected at : step {args.step}")
        print(f"run reported ok  : {outcome.run_ok}")
        print(f"exception escaped: {outcome.raised or '(none)'}")
        print(f"scenes saved     : {outcome.saved or '(none)'}")
        print(f"repro written    : {os.path.basename(outcome.repro) if outcome.repro else '(none)'}")
        print(f"repro inputs     : {outcome.recorded_inputs}")
        print(f"replay fired at  : {outcome.replay_fired_at}")
        print(f"replay raised    : {outcome.replay_raised or '(none)'}")
        print()

        failures = 0

        if outcome.run_ok:
            failures += 1
            print("FAIL the run reported success after an invariant was violated")
        else:
            print("PASS the run reported failure")

        if outcome.saved:
            failures += 1
            print(f"FAIL a scene was saved despite the violation: {outcome.saved}")
        else:
            print("PASS no scene was saved")

        if not outcome.repro:
            print("FAIL no repro was written, so there is nothing to reproduce from")
            return 1
        print("PASS a repro was written")

        if outcome.recorded_inputs != args.step:
            failures += 1
            print(f"FAIL the repro holds {outcome.recorded_inputs} inputs, "
                  f"expected {args.step} (steps 0..{args.step - 1})")
        else:
            print(f"PASS the repro holds exactly steps 0..{args.step - 1}")

        if outcome.replay_fired_at != args.step:
            failures += 1
            print(f"FAIL replaying the repro fired at step "
                  f"{outcome.replay_fired_at}, expected {args.step}")
        else:
            print(f"PASS replaying the repro fired the same rule at step {args.step}")

        print()
        return 1 if failures else 0
    finally:
        shutil.rmtree(folder, ignore_errors=True)
        shutil.rmtree(save_folder, ignore_errors=True)


if __name__ == "__main__":
    raise SystemExit(main())
