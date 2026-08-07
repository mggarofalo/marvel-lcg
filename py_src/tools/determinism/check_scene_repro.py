"""Run the same bot game twice and compare the scene files it writes.

This is the acceptance test for MARVEL-27: the same seed under the same policy
must produce a byte-identical *hashable payload* across runs and across
machines, or a content-addressed corpus manifest cannot exist.

Two things are compared, and the difference between them is the point:

  payload   the scene minus `Scene.PROVENANCE_KEYS` and the checksum -- what a
            corpus manifest hashes. This must always match.
  file      the raw bytes on disk. These match too when the bot saves
            deterministically (`bot_deterministic_save`, on by default), which
            is what keeps a machine fingerprint out of the repository.

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
from typing import List, Tuple

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
    saved = [name for name in sorted(os.listdir(folder)) if name.endswith(".json")]
    if len(saved) != 1:
        raise RuntimeError(
            f"expected one saved scene in {folder}, found {saved}\n"
            f"exit {proc.returncode}\nstdout tail: {proc.stdout[-1200:]}\n"
            f"stderr tail: {proc.stderr[-800:]}"
        )
    return os.path.join(folder, saved[0])


def _digests(path: str) -> Tuple[str, str, str]:
    """(file digest, payload digest, file name) for one saved scene."""
    from engine.lib import Json, Ver

    Ver.Initialize()
    from game.scene.scene import Scene

    with open(path, "rb") as handle:
        raw = handle.read()

    data = Json.Load(path)
    payload = Scene.HashablePayload(data)

    return (
        hashlib.sha256(raw).hexdigest(),
        hashlib.sha256(payload.encode("utf-8")).hexdigest(),
        os.path.basename(path),
    )


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--scenario", default="rhino")
    parser.add_argument("--heroes", default="spider_man",
                        help="comma separated")
    parser.add_argument("--seed", type=int, default=4242)
    parser.add_argument("--no-deterministic-save", action="store_true",
                        help="save with the wall-clock metadata, as a human "
                             "save does -- payloads must still match, files "
                             "must not be expected to")
    args = parser.parse_args(argv)

    heroes = args.heroes.split(",")
    deterministic_save = not args.no_deterministic_save

    print(f"{args.scenario} / {'+'.join(heroes)} / seed {args.seed}, "
          f"deterministic save {'on' if deterministic_save else 'OFF'}\n")

    root = tempfile.mkdtemp(prefix="scene-repro-")
    try:
        results = []
        for label in ("A", "B"):
            path = _run_bot(os.path.join(root, label), args.scenario, heroes,
                            args.seed, deterministic_save)
            results.append(_digests(path))
            print(f"run {label}  {results[-1][2]}\n"
                  f"      file    {results[-1][0][:32]}\n"
                  f"      payload {results[-1][1][:32]}")
    finally:
        shutil.rmtree(root, ignore_errors=True)

    (file_a, payload_a, name_a), (file_b, payload_b, name_b) = results
    failures = 0
    print()

    if name_a != name_b:
        failures += 1
        print(f"FAIL file names differ: {name_a} vs {name_b}")

    if payload_a == payload_b:
        print("PASS hashable payloads are identical")
    else:
        failures += 1
        print("FAIL hashable payloads differ -- the corpus cannot be "
              "content-addressed")

    if file_a == file_b:
        print("PASS files are byte-identical")
        if not deterministic_save:
            print("     Unexpected: a non-deterministic save should carry a "
                  "playtime that differs between runs.")
    elif deterministic_save:
        failures += 1
        print("FAIL files differ despite a deterministic save")
    else:
        print("     files differ, as expected without a deterministic save")

    print()
    return 1 if failures else 0


if __name__ == "__main__":
    raise SystemExit(main())
