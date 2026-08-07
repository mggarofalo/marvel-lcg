"""Probe: can a timed-out wait still get a fabricated decline into a saved file?

MARVEL-32 claims a headless run cannot record an input the policy never made.
The unit tests check each layer of that claim in isolation, which is exactly
how the first version of the fix passed while being wrong: `ChoiceOne` runs
underneath `EffectInvoker.ResolveSelfInternal` and `Message2.Send`, both of
which catch broadly and report through `Log.OnCrash` -- and `Log.OnCrash`
re-raises only when `Build.release` is false, which `build.py` hardcodes true.
So `FabricatedInputError` was raised, swallowed, and the game carried on and
saved.

This probe reproduces the scenario the issue actually describes: a machine
under load stalls one in-game decision past the timeout, and then recovers. A
guard that only samples the timer at fixed points sees zero every time it
looks.

Run:  python -m tools.determinism.probe_fabricated_input
      python -m tools.determinism.probe_fabricated_input --event WhenPlayerInTurn

Exit code 0 means the fabricated input was refused: nothing was saved.
"""

from __future__ import annotations

import argparse
import os
import shutil
import sys
import tempfile
from typing import Any, Callable, List

# How long the stalled decision blocks for. Only has to exceed nothing.
STALL_SECONDS = 0.5

# The default targets an in-game decision rather than setup or mulligan,
# because the swallowing handlers sit on the ability and message dispatch path.
DEFAULT_EVENT = "WhenPlayerInTurn"


class Outcome:
    def __init__(self) -> None:
        self.stalled_at = ""
        self.raised = ""
        self.run_ok: bool | None = None
        self.saved: List[str] = []
        self.manifests: List[str] = []


def _install_stall(outcome: Outcome, event_name: str) -> None:
    """Make one decision on `event_name` wait past a timeout, then recover."""
    from engine.device.manager.base import DeviceManager

    original = DeviceManager.DoGetInput
    stalled: List[bool] = []

    def StallOnce(self: Any, data: Any, player_id: int, check: Callable[[], bool]):
        if not stalled and data.event_name == event_name:
            stalled.append(True)
            outcome.stalled_at = f"{data.event_name} (player {player_id})"
            self.timer.UpdateMaxTimeout(STALL_SECONDS)
            try:
                # Never answer, so the wait runs to genuine expiry.
                return original(self, data, player_id, lambda: False)
            finally:
                # The disturbance clears itself. Every later sample of the
                # timer reads zero, which is the whole point.
                self.timer.UpdateMaxTimeout(0)
        return original(self, data, player_id, check)

    DeviceManager.DoGetInput = StallOnce  # type: ignore[method-assign]


def _run(folder: str, seed: int, event_name: str) -> Outcome:
    outcome = Outcome()

    sys.argv = [
        "main.py", "-bot",
        "-bot_seed", str(seed),
        "-bot_save_folder", folder.replace("\\", "/") + "/",
    ]

    from engine import Engine

    if not Engine.Initialize():
        raise RuntimeError("engine failed to initialize")

    _install_stall(outcome, event_name)

    try:
        Engine.EngineRun()
        outcome.run_ok = Engine.exit_code == 0
    except Exception as exc:  # the refusal reaching the top is a fine outcome
        outcome.raised = f"{type(exc).__name__}: {exc}"
        outcome.run_ok = False

    for name in sorted(os.listdir(folder)):
        if name.startswith("bot-manifest-"):
            outcome.manifests.append(name)
        elif name.endswith(".json"):
            outcome.saved.append(name)

    return outcome


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--seed", type=int, default=4242)
    parser.add_argument("--event", default=DEFAULT_EVENT)
    args = parser.parse_args(argv)

    folder = tempfile.mkdtemp(prefix="fabricated-input-")
    try:
        outcome = _run(folder, args.seed, args.event)
    finally:
        shutil.rmtree(folder, ignore_errors=True)

    print(f"stalled decision : {outcome.stalled_at or '(never reached)'}")
    print(f"run reported ok  : {outcome.run_ok}")
    print(f"exception escaped: {outcome.raised or '(none)'}")
    print(f"scenes saved     : {outcome.saved or '(none)'}")
    print(f"manifests written: {outcome.manifests or '(none)'}")
    print()

    failures = 0

    if not outcome.stalled_at:
        # A probe that never reached the decision proves nothing either way.
        print(f"FAIL no {args.event} decision was reached; the probe tested nothing")
        return 1

    if outcome.saved:
        failures += 1
        print("FAIL a scene containing a fabricated decline was saved")
    else:
        print("PASS no scene was saved")

    if outcome.run_ok:
        failures += 1
        print("FAIL the run reported success after fabricating an input")
    else:
        print("PASS the run reported failure")

    print()
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
