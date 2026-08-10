"""Probe: does an integrity error raised on a worker thread reach the run?

MARVEL-54 is a claim about plumbing, and the unit tests check it against a
`JobManager` standing on its own. That is exactly how MARVEL-32's first fix
passed while being wrong -- each layer was right and the path between them was
not. So this probe asks the question on the real path, in a real game.

`GameSession.GameSetup` calls `ControllerManager.WaitConnect`, which is the one
place ordinary play fans out onto `JobManager.RunProcesses` -- one threaded job
per controller, waited on with `WaitForAllJobsToComplete`. An exception raised
inside `InputDevice.WaitConnect` therefore happens on a pool thread, underneath
the `except Exception` in `Job.run_job` that this issue is about.

Two runs, and the difference between them is the whole finding:

  integrity   `FabricatedInputError` raised in the job -> the run must fail and
              save nothing
  ordinary    `ValueError` raised in exactly the same place -> the run must
              carry on and save its scene, because absorbing an ordinary
              failure is deliberate and must not regress

Against the pre-fix code the first run passes a whole game and saves it, which
is the bug: the error was logged on the worker thread and dropped.

Each run needs its own engine, so the probe re-invokes itself as a subprocess
per mode.

Run:  python -m tools.determinism.probe_threaded_integrity
      python -m tools.determinism.probe_threaded_integrity --seed 7

Exit code 0 means the integrity error stopped the run and the ordinary one
did not.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile
from typing import Any, Dict, List

from tools.determinism.pinned_env import build_env

RESULT_MARKER = "<<<RESULT>>>"

MODES = ("integrity", "ordinary")


def InstallRaise(mode: str, fired: List[str]) -> None:
    """Make the first `WaitConnect` raise, on the job thread that runs it."""
    from core.errors import EngineIntegrityError
    from engine.device.base.input import InputDevice

    class OrdinaryFailure(Exception):
        """Anything the engine is right to absorb."""

    class InjectedIntegrityError(EngineIntegrityError):
        """Stands in for `FabricatedInputError` and friends."""

    original = InputDevice.WaitConnect

    def RaiseOnce(self: Any) -> None:
        if not fired:
            fired.append(mode)
            if mode == "integrity":
                raise InjectedIntegrityError("injected on a WaitConnect job thread")
            raise OrdinaryFailure("injected on a WaitConnect job thread")
        return original(self)

    # `@final` is a type-checker instruction, not an interpreter one, and the
    # seam matters more than the decoration: `ControllerManager.WaitConnect`
    # calls this exact method from inside `JobManager.RunProcesses`.
    InputDevice.WaitConnect = RaiseOnce  # type: ignore[method-assign]


def RunChild(mode: str, folder: str, seed: int) -> Dict[str, Any]:
    """One bot game with the failure injected. Returns what came of it."""
    os.makedirs(folder, exist_ok=True)

    sys.argv = [
        "main.py", "-bot",
        "-bot_seed", str(seed),
        "-bot_save_folder", folder.replace("\\", "/") + "/",
    ]

    from engine import Engine

    if not Engine.Initialize():
        raise RuntimeError("engine failed to initialize")

    fired: List[str] = []
    InstallRaise(mode, fired)

    raised = ""
    try:
        Engine.EngineRun()
        run_ok = Engine.exit_code == 0
    except Exception as exc:
        # Reaching the top is a fine outcome for the integrity run: it is not
        # absorbed either way.
        raised = f"{type(exc).__name__}: {exc}"
        run_ok = False

    saved = [name for name in sorted(os.listdir(folder))
             if name.endswith(".json")
             and not name.startswith(("bot-manifest-", "bot-coverage-"))]

    return {"mode": mode, "fired": bool(fired), "run_ok": run_ok,
            "raised": raised, "saved": saved}


def Spawn(mode: str, seed: int) -> Dict[str, Any]:
    """Run one mode in its own engine and read back its result line."""
    folder = tempfile.mkdtemp(prefix=f"threaded-integrity-{mode}-")
    try:
        proc = subprocess.run(
            [sys.executable, "-m", "tools.determinism.probe_threaded_integrity",
             "--child", mode, "--child-folder", folder, "--seed", str(seed)],
            capture_output=True, text=True, errors="replace",
            env=build_env(), cwd=os.getcwd(),
        )
        for line in (proc.stdout + proc.stderr).splitlines():
            if line.startswith(RESULT_MARKER):
                return json.loads(line[len(RESULT_MARKER):])
        raise RuntimeError(
            f"the {mode} run produced no result line\n"
            f"exit {proc.returncode}\nstdout tail: {proc.stdout[-1200:]}\n"
            f"stderr tail: {proc.stderr[-800:]}")
    finally:
        shutil.rmtree(folder, ignore_errors=True)


def Check(label: str, holds: bool) -> int:
    print(f"{'PASS' if holds else 'FAIL'} {label}")
    return 0 if holds else 1


def main(argv: List[str] | None=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--seed", type=int, default=4242)
    parser.add_argument("--child", choices=MODES, default=None,
                        help=argparse.SUPPRESS)
    parser.add_argument("--child-folder", default=None, help=argparse.SUPPRESS)
    args = parser.parse_args(argv)

    if args.child:
        result = RunChild(args.child, args.child_folder or ".", args.seed)
        print(RESULT_MARKER + json.dumps(result))
        return 0

    results = {mode: Spawn(mode, args.seed) for mode in MODES}
    for mode in MODES:
        outcome = results[mode]
        print(f"{mode:9} fired={outcome['fired']}  ok={outcome['run_ok']}  "
              f"saved={outcome['saved'] or '(none)'}  "
              f"escaped={outcome['raised'] or '(none)'}")
    print()

    failures = 0
    for mode in MODES:
        if not results[mode]["fired"]:
            # A probe that never reached `WaitConnect` proves nothing.
            print(f"FAIL the {mode} run never reached WaitConnect; it tested nothing")
            return 1

    integrity = results["integrity"]
    failures += Check("an integrity error on a job thread failed the run",
                      not integrity["run_ok"])
    failures += Check("no scene was saved after it",
                      not integrity["saved"])

    ordinary = results["ordinary"]
    failures += Check("an ordinary exception on a job thread is still absorbed",
                      ordinary["run_ok"])
    failures += Check("and the game it interrupted still saved its scene",
                      bool(ordinary["saved"]))

    print()
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
