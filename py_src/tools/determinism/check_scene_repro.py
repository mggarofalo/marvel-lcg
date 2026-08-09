"""Run the same bot game twice and compare the scene files it writes.

This is the acceptance test for MARVEL-27: the same seed under the same policy
must produce a byte-identical *hashable payload* across runs and across
machines, or a content-addressed corpus manifest cannot exist.

Three things are compared, and the difference between them is the point:

  payload   the scene minus `PROVENANCE_KEYS` and the checksum -- what a corpus
            manifest hashes. This must always match, in either save mode.
  file      the raw bytes on disk. These match too when the bot saves
            deterministically (`bot_deterministic_save`, on by default).
  ambient   which of `AMBIENT_KEYS` the file carries. None, under a
            deterministic save -- that is what keeps a machine fingerprint out
            of the repository and what makes byte equality hold on someone
            else's machine, which a single-host run cannot demonstrate.

The `--no-deterministic-save` control checks that the ambient metadata comes
back, not that the two files differ. `playtime` is written to one decimal
place, so a game that finishes in under a second can round the same way twice.

Run:
    python -m tools.determinism.check_scene_repro
    python -m tools.determinism.check_scene_repro --seed 99 --scenario klaw \
        --heroes she_hulk,captain_marvel
    python -m tools.determinism.check_scene_repro --no-deterministic-save
"""

from __future__ import annotations

import argparse
import hashlib
import os
import shutil
import subprocess
import sys
import tempfile
from typing import List, NamedTuple, Tuple

from tools.determinism.pinned_env import build_env


def _run_bot(folder: str, scenario: str, heroes: List[str], seed: int,
             deterministic_save: bool) -> str:
    """Play one game into `folder` in a fresh process and return the file."""
    os.makedirs(folder, exist_ok=True)
    # Bool config variables are set by presence: `-x` turns one on, `-no_x`
    # turns it off. There is no `-x false`.
    save_flag = "-bot_deterministic_save" if deterministic_save else "-no_bot_deterministic_save"
    proc = subprocess.run(
        [
            sys.executable, "main.py",
            "-bot",
            "-bot_scenario", scenario,
            "-bot_heroes", *heroes,
            "-bot_seed", str(seed),
            "-bot_save_folder", folder.replace("\\", "/") + "/",
            save_flag,
        ],
        capture_output=True,
        text=True,
        env=build_env(),
        cwd=os.getcwd(),
        errors="replace",
    )
    # The bot writes its own per-run artefacts into the save folder beside the
    # scenes they describe: a manifest (MARVEL-32) and a coverage report
    # (MARVEL-13). Neither is a scene and neither is compared here.
    RUN_ARTEFACTS = ("bot-manifest-", "bot-coverage-")
    saved = [name for name in sorted(os.listdir(folder))
             if name.endswith(".json") and not name.startswith(RUN_ARTEFACTS)]
    if len(saved) != 1:
        raise RuntimeError(
            f"expected one saved scene in {folder}, found {saved}\n"
            f"exit {proc.returncode}\nstdout tail: {proc.stdout[-1200:]}\n"
            f"stderr tail: {proc.stderr[-800:]}"
        )
    return os.path.join(folder, saved[0])


class Saved(NamedTuple):
    name: str
    file_digest: str
    payload_digest: str
    ambient: Tuple[str, ...]
    """Which of `AMBIENT_KEYS` the file actually carries."""


def _inspect(path: str) -> Saved:
    from engine.lib import Json, Ver

    Ver.Initialize()
    from game.scene import scene as scene_module

    with open(path, "rb") as handle:
        raw = handle.read()

    data = Json.Load(path)
    payload = scene_module.Scene.HashablePayload(data)
    metadata = data.get("metadata") or {}

    return Saved(
        name=os.path.basename(path),
        file_digest=hashlib.sha256(raw).hexdigest(),
        payload_digest=hashlib.sha256(payload.encode("utf-8")).hexdigest(),
        ambient=tuple(key for key in scene_module.AMBIENT_KEYS if key in metadata),
    )


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--scenario", default="rhino")
    parser.add_argument("--heroes", default="spider_man",
                        help="comma separated")
    parser.add_argument("--seed", type=int, default=4242)
    parser.add_argument("--no-deterministic-save", action="store_true",
                        help="save the way a human save does, as a control: "
                             "the ambient metadata must come back and the "
                             "payload must be unaffected by it")
    args = parser.parse_args(argv)

    heroes = args.heroes.split(",")
    deterministic_save = not args.no_deterministic_save

    print(f"{args.scenario} / {'+'.join(heroes)} / seed {args.seed}, "
          f"deterministic save {'on' if deterministic_save else 'OFF'}\n")

    root = tempfile.mkdtemp(prefix="scene-repro-")
    try:
        runs: List[Saved] = []
        for label in ("A", "B"):
            path = _run_bot(os.path.join(root, label), args.scenario, heroes,
                            args.seed, deterministic_save)
            runs.append(_inspect(path))
            print(f"run {label}  {runs[-1].name}\n"
                  f"      file    {runs[-1].file_digest[:32]}\n"
                  f"      payload {runs[-1].payload_digest[:32]}\n"
                  f"      ambient {', '.join(runs[-1].ambient) or '(none)'}")
    finally:
        shutil.rmtree(root, ignore_errors=True)

    a, b = runs
    failures = 0
    print()

    if a.name != b.name:
        failures += 1
        print(f"FAIL file names differ: {a.name} vs {b.name}")

    if a.payload_digest == b.payload_digest:
        print("PASS hashable payloads are identical")
    else:
        failures += 1
        print("FAIL hashable payloads differ -- the corpus cannot be "
              "content-addressed")

    if deterministic_save:
        # Both halves are asserted. Byte equality is the goal; the absence of
        # the ambient keys is what makes it hold on a different machine on a
        # different day, which this run cannot itself demonstrate.
        if a.file_digest == b.file_digest:
            print("PASS files are byte-identical")
        else:
            failures += 1
            print("FAIL files differ despite a deterministic save")

        if a.ambient or b.ambient:
            failures += 1
            print(f"FAIL ambient metadata was written anyway: "
                  f"{', '.join(sorted(set(a.ambient) | set(b.ambient)))}")
        else:
            print("PASS no wall-clock or machine metadata in the file")
    else:
        # The control. Do NOT assert that the files differ: `playtime` is
        # written to one decimal place, and a game that finishes in under a
        # second can round to the same value twice. What must hold is that the
        # ambient metadata is present -- that is what proves the deterministic
        # mode is doing something rather than the two paths being identical.
        missing = sorted(
            {key for run in runs for key in ("sign", "time", "playtime")
             if key not in run.ambient}
        )
        if missing:
            failures += 1
            print(f"FAIL a human-style save did not write: {', '.join(missing)}")
        else:
            print("PASS ambient metadata is written, as a human save does")

        if a.file_digest == b.file_digest:
            print("     files happen to match: playtime rounded to the same "
                  "tenth of a second in both runs")
        else:
            print("     files differ, as a human save normally does")

    print()
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
