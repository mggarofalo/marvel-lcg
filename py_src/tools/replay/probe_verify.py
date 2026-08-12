"""Probe: does `-verify_replays` actually accept a good corpus and reject a bad one?

A gate that cannot fail is not a gate, and this project has already shipped two
that could not (MARVEL-43, MARVEL-65). Both looked correct in unit tests: what
was wrong was the path between the check and the process that runs it. So this
probe asks the question end to end, through the real command line, in fresh
processes -- the only way to see the exit code, the report and the engine's
device selection at the same time.

It plays bot games and then puts corpora past the verifier:

  clean       the scene as saved                       -> accepted, exit 0
  corrupt     one recorded step digest overwritten     -> rejected, exit 1
  truncated   the tail of the recording removed        -> incomplete, exit 1
  truncated   the same corpus, with -verify_allow_incomplete -> accepted, exit 0
  drifted     generated under -max_workers 3, verified without it -> exit 1
  drifted     the same corpus, verified with it                   -> exit 0
  drifted     the same corpus, with -verify_allow_config_drift    -> exit 0

The truncated pair is the one that hangs if the verify device is ever swapped
for an interactive one: a recording that ends before the game does makes the
engine ask for a decision nobody is there to make. That was MARVEL-28's original
symptom, so it is worth a probe rather than a comment.

The drifted trio is MARVEL-34, and it needs all three cases rather than the
first: a gate that fails whenever it is asked is as useless as one that never
does, and the honest path -- generate, then verify with the same engine -- has
to stay green or nobody will keep the gate. The first calibration of it did not:
`check_invariants` is forced on for `-device bot` and left off for a verifier,
so every corpus failed until that was recognised as invocation rather than
config.

Run:  python -m tools.replay.probe_verify
      python -m tools.replay.probe_verify --seed 7

Exit code 0 means the gate accepted what it should and rejected what it should.
"""

from __future__ import annotations

import argparse
import json
import os
import shutil
import subprocess
import sys
import tempfile
from typing import Any, Dict, List, NamedTuple

from tools.determinism.pinned_env import build_env

# Written into the save folder beside the scenes they describe. Neither is a
# scene, and the verifier is expected to skip both without being told to.
RUN_ARTEFACTS = ("bot-manifest-", "bot-coverage-")

# Long enough into the game that the corruption lands in ordinary play rather
# than setup, and short enough that a small game still reaches it.
CORRUPT_AT = 10


class Result(NamedTuple):
    exit_code: int
    report: Dict[str, Any]
    tail: str

    @property
    def ok(self) -> bool:
        return bool(self.report.get("ok"))

    @property
    def statuses(self) -> List[str]:
        return [str(case.get("status")) for case in self.report.get("cases", [])]


def Generate(folder: str, seed: int, extra: List[str] | None=None) -> str:
    """Play one bot game into `folder` and return the scene it saved."""
    os.makedirs(folder, exist_ok=True)
    proc = subprocess.run(
        [
            sys.executable, "main.py", "-bot",
            "-bot_seed", str(seed),
            "-bot_save_folder", folder.replace("\\", "/") + "/",
        ] + list(extra or []),
        capture_output=True, text=True, errors="replace",
        env=build_env(), cwd=os.getcwd(),
    )
    saved = [name for name in sorted(os.listdir(folder))
             if name.endswith(".json") and not name.startswith(RUN_ARTEFACTS)]
    if len(saved) != 1:
        raise RuntimeError(
            f"expected one saved scene in {folder}, found {saved}\n"
            f"exit {proc.returncode}\nstdout tail: {proc.stdout[-1200:]}")
    return os.path.join(folder, saved[0])


def Verify(folder: str, report_path: str, *, allow_incomplete: bool=False,
           extra: List[str] | None=None, quarantine: str="") -> Result:
    """Run the real command over `folder` and read what it said."""
    command = [
        sys.executable, "main.py", "-verify_replays",
        "-verify_folders", folder.replace("\\", "/") + "/",
        "-verify_report_file", report_path.replace("\\", "/"),
    ]
    if quarantine:
        command += ["-verify_quarantine_folder", quarantine.replace("\\", "/") + "/"]
    if allow_incomplete:
        command.append("-verify_allow_incomplete")
    command += list(extra or [])

    proc = subprocess.run(command, capture_output=True, text=True,
                          errors="replace", env=build_env(), cwd=os.getcwd())

    report: Dict[str, Any] = {}
    if os.path.isfile(report_path):
        with open(report_path, encoding="utf-8") as handle:
            report = json.load(handle)

    return Result(proc.returncode, report, (proc.stdout + proc.stderr)[-900:])


def CopyCorpus(source_scene: str, folder: str) -> str:
    os.makedirs(folder, exist_ok=True)
    target = os.path.join(folder, os.path.basename(source_scene))
    shutil.copyfile(source_scene, target)
    return target


def Corrupt(path: str, step: int) -> bool:
    """Overwrite one recorded step digest. The replay must notice."""
    with open(path, encoding="utf-8") as handle:
        scene = json.load(handle)
    inputs = scene.get("inputs") or []
    if len(inputs) <= step:
        return False
    inputs[step]["digest"] = "0" * 8
    with open(path, "w", encoding="utf-8") as handle:
        json.dump(scene, handle)
    return True


def Truncate(path: str, keep: int) -> bool:
    """Drop the tail of the recording, so the game outlives its inputs."""
    with open(path, encoding="utf-8") as handle:
        scene = json.load(handle)
    inputs = scene.get("inputs") or []
    if len(inputs) <= keep:
        return False
    scene["inputs"] = inputs[:keep]
    with open(path, "w", encoding="utf-8") as handle:
        json.dump(scene, handle)
    return True


def Check(label: str, holds: bool, detail: str="") -> int:
    print(f"{'PASS' if holds else 'FAIL'} {label}" + (f" -- {detail}" if detail and not holds else ""))
    return 0 if holds else 1


def main(argv: List[str] | None=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--seed", type=int, default=4242)
    parser.add_argument("--corrupt-at", type=int, default=CORRUPT_AT)
    args = parser.parse_args(argv)

    root = tempfile.mkdtemp(prefix="verify-probe-")
    failures = 0
    try:
        scene = Generate(os.path.join(root, "clean"), args.seed)
        with open(scene, encoding="utf-8") as handle:
            recorded = len(json.load(handle).get("inputs") or [])
        print(f"generated {os.path.basename(scene)} ({recorded} recorded steps)\n")

        # --- clean -------------------------------------------------------
        clean = Verify(os.path.join(root, "clean"),
                       os.path.join(root, "clean.json"))
        print(f"clean      exit {clean.exit_code}  ok={clean.report.get('ok')}  "
              f"total={clean.report.get('total')}  {clean.statuses}")
        failures += Check("a clean corpus is accepted",
                          clean.ok and clean.exit_code == 0, clean.tail)
        failures += Check("the run artefacts beside it were not counted as scenes",
                          clean.report.get("total") == 1,
                          f"total={clean.report.get('total')}")
        # The honest path: same engine, same flags. If this ever goes red the
        # drift gate is miscalibrated, not the corpus.
        failures += Check("its manifest reports no config drift",
                          clean.report.get("config_drifted") == 0,
                          f"config_drifted={clean.report.get('config_drifted')}\n"
                          + json.dumps(clean.report.get("manifests"), indent=2)[:900])

        # --- corrupt -----------------------------------------------------
        corrupt_folder = os.path.join(root, "corrupt")
        corrupted = CopyCorpus(scene, corrupt_folder)
        step = min(args.corrupt_at, max(0, recorded - 1))
        if not Corrupt(corrupted, step):
            print(f"FAIL the generated scene has too few steps to corrupt ({recorded})")
            return 1
        bad = Verify(corrupt_folder, os.path.join(root, "corrupt.json"))
        print(f"corrupt    exit {bad.exit_code}  ok={bad.report.get('ok')}  {bad.statuses}")
        failures += Check(f"a digest corrupted at step {step} is rejected",
                          not bad.ok and bad.exit_code == 1, bad.tail)
        failures += Check("the corrupted scene is reported as a failure, not as incomplete",
                          bad.statuses == ["fail"], str(bad.statuses))

        # --- truncated ---------------------------------------------------
        short_folder = os.path.join(root, "short")
        shortened = CopyCorpus(scene, short_folder)
        keep = max(1, recorded // 2)
        if not Truncate(shortened, keep):
            print(f"FAIL the generated scene has too few steps to truncate ({recorded})")
            return 1
        short = Verify(short_folder, os.path.join(root, "short.json"))
        print(f"truncated  exit {short.exit_code}  ok={short.report.get('ok')}  {short.statuses}")
        failures += Check("a truncated recording does not hang the process",
                          short.exit_code in (0, 1), f"exit {short.exit_code}")
        failures += Check("a truncated recording is reported as incomplete",
                          short.statuses == ["incomplete"], str(short.statuses) + "\n" + short.tail)
        failures += Check("a truncated recording fails the run by default",
                          not short.ok and short.exit_code == 1, short.tail)

        allowed = Verify(short_folder, os.path.join(root, "allowed.json"),
                         allow_incomplete=True)
        print(f"allowed    exit {allowed.exit_code}  ok={allowed.report.get('ok')}  "
              f"{allowed.statuses}")
        failures += Check("-verify_allow_incomplete accepts it",
                          allowed.ok and allowed.exit_code == 0, allowed.tail)

        # --- quarantine --------------------------------------------------
        # MARVEL-17: a replay that does not reproduce must leave an artefact
        # saying so. One that is silently dropped gets rediscovered as a C#
        # port bug months later.
        quarantine = os.path.join(root, "quarantine")
        held = Verify(corrupt_folder, os.path.join(root, "held.json"),
                      quarantine=quarantine)
        index_path = os.path.join(quarantine, "quarantine.json")
        index: Dict[str, Any] = {}
        if os.path.isfile(index_path):
            with open(index_path, encoding="utf-8") as handle:
                index = json.load(handle)
        print(f"quarantine exit {held.exit_code}  held={index.get('count')}")
        failures += Check("the corrupted scene is set aside with a reason",
                          index.get("count") == 1
                          and bool((index.get("cases") or [{}])[0].get("detail")),
                          json.dumps(index, indent=2)[:600])
        failures += Check("and a copy of it goes with the record",
                          (index.get("cases") or [{}])[0].get("copied") is True
                          and os.path.isfile(os.path.join(
                              quarantine, os.path.basename(corrupted))),
                          str(sorted(os.listdir(quarantine))))
        failures += Check("quarantining does not forgive the run",
                          not held.ok and held.exit_code == 1, held.tail)
        failures += Check("the corpus it was read from is left alone",
                          os.path.isfile(corrupted),
                          "the verifier moved its own input")

        clean_quarantine = os.path.join(root, "quarantine-clean")
        Verify(os.path.join(root, "clean"), os.path.join(root, "cq.json"),
               quarantine=clean_quarantine)
        clean_index_path = os.path.join(clean_quarantine, "quarantine.json")
        # "Nothing was quarantined" and "quarantining never ran" must not look
        # the same to whoever freezes the corpus.
        failures += Check("an empty quarantine is still written",
                          os.path.isfile(clean_index_path), clean_quarantine)

        # --- config drift ------------------------------------------------
        # A real second run under a real gameplay flag, rather than an edited
        # manifest: what is under test is the path from the command line
        # through the snapshot to the verifier's exit code.
        drift_folder = os.path.join(root, "drift")
        Generate(drift_folder, args.seed, ["-max_workers", "3"])

        drifted = Verify(drift_folder, os.path.join(root, "drift.json"))
        print(f"drifted    exit {drifted.exit_code}  ok={drifted.report.get('ok')}  "
              f"config_drifted={drifted.report.get('config_drifted')}  {drifted.statuses}")
        failures += Check("a corpus generated under different config is rejected",
                          not drifted.ok and drifted.exit_code == 1, drifted.tail)
        failures += Check("and the scenes themselves still replayed",
                          drifted.statuses == ["pass"], str(drifted.statuses))
        failures += Check("the drift names the variable that moved",
                          any(item["name"] == "max_workers"
                              for record in drifted.report.get("manifests", [])
                              for item in record.get("drift", [])),
                          json.dumps(drifted.report.get("manifests"), indent=2)[:900])

        matched = Verify(drift_folder, os.path.join(root, "matched.json"),
                         extra=["-max_workers", "3"])
        print(f"matched    exit {matched.exit_code}  ok={matched.report.get('ok')}  "
              f"config_drifted={matched.report.get('config_drifted')}")
        failures += Check("verifying it under the same config is accepted",
                          matched.ok and matched.exit_code == 0, matched.tail)

        waived = Verify(drift_folder, os.path.join(root, "waived.json"),
                        extra=["-verify_allow_config_drift"])
        print(f"waived     exit {waived.exit_code}  ok={waived.report.get('ok')}  "
              f"config_drifted={waived.report.get('config_drifted')}")
        failures += Check("-verify_allow_config_drift accepts it",
                          waived.ok and waived.exit_code == 0, waived.tail)
        failures += Check("and still says what drifted",
                          waived.report.get("config_drifted") == 1,
                          f"config_drifted={waived.report.get('config_drifted')}")

        # --- empty -------------------------------------------------------
        empty_folder = os.path.join(root, "empty")
        os.makedirs(empty_folder, exist_ok=True)
        empty = Verify(empty_folder, os.path.join(root, "empty.json"))
        print(f"empty      exit {empty.exit_code}  ok={empty.report.get('ok')}  "
              f"total={empty.report.get('total')}")
        failures += Check("verifying nothing is not the same as verifying everything",
                          not empty.ok and empty.exit_code == 1, empty.tail)

        print()
        return 1 if failures else 0
    finally:
        shutil.rmtree(root, ignore_errors=True)


if __name__ == "__main__":
    raise SystemExit(main())
