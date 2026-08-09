"""Probe: what shapes of forced-ability batch does self-play actually reach?

`EventManager.SelectForcedEffect` decides which of several simultaneously
initiating forced abilities goes first. Two defects lived in it, and neither
moves a per-step digest in a decline-only run, so `check_runs` staying green
across the fix is ambiguous on its own -- it could mean the fix is a no-op here,
or it could mean the situation never arose. This probe distinguishes them.

It counts, for every call:

  batches        calls that reached the selection at all
  multi          calls with two or more candidates -- where the choice matters
  with_delay     calls where a delay ability sat in the batch alongside a
                 candidate. This is MARVEL-39's trigger: the delay abilities
                 were filtered out of the offered faces but not out of the list
                 the chosen face was indexed back into
  misaligned     calls where the old index arithmetic would have resolved a
                 *different* effect than the one chosen. The defect firing,
                 counted directly rather than inferred from `with_delay`
  same_card      calls where every candidate sits on one card. This is
                 MARVEL-40's trigger: the engine skipped the first player's
                 order choice entirely for these

`multi = 0` means the run proves nothing about either fix and the digest
evidence must not be read as coverage. `misaligned > 0` means MARVEL-39 was
live in this run, not latent.

Run:  python -m tools.determinism.probe_forced_selection
      python -m tools.determinism.probe_forced_selection --matrix wide
"""

from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
from typing import Any, Dict, List

from tools.determinism.check_runs import MATRIX_SMOKE, MATRIX_WIDE
from tools.determinism.pinned_env import build_env

MARKER = "<<<PROBE>>>"

FIELDS = ("batches", "multi", "with_delay", "misaligned", "same_card", "largest")


class Counters:
    def __init__(self) -> None:
        self.counts = {name: 0 for name in FIELDS}

    def Record(self, forced_effects: List[Any], chosen_index: int) -> None:
        """`chosen_index` is the index the first player picked among candidates."""
        candidates = [x for x in forced_effects if not x.ability.flags.is_delay_ability]
        if not candidates:
            return

        self.counts["batches"] += 1
        self.counts["largest"] = max(self.counts["largest"], len(candidates))

        if len(candidates) < 2:
            return
        self.counts["multi"] += 1

        faces = [x.this for x in candidates]
        if all(face.card == faces[0].card for face in faces):
            self.counts["same_card"] += 1
            return

        if len(candidates) != len(forced_effects):
            self.counts["with_delay"] += 1
            # What the pre-MARVEL-39 code did: index the *unfiltered* list with
            # a position in the filtered one. Compare like for like, guarding
            # the overrun that arithmetic could also produce.
            if chosen_index >= len(forced_effects):
                self.counts["misaligned"] += 1
            elif forced_effects[chosen_index] is not candidates[chosen_index]:
                self.counts["misaligned"] += 1

    def AsDict(self) -> Dict[str, int]:
        return dict(self.counts)


def _child(campaign: str, heroes: List[str], seed: int, max_steps: int) -> int:
    from tools.determinism.headless import _initialize_engine, run_headless

    _initialize_engine()

    from game.event.manager import EventManager

    counters = Counters()
    select = EventManager.SelectForcedEffect

    def Instrumented(forced_effects, ask_first_player):
        # Wrap the prompt so the probe learns which candidate was chosen without
        # restating the selection rule -- the production method still decides.
        chosen_index = 0

        def Watch(faces):
            nonlocal chosen_index
            face = ask_first_player(faces)
            chosen_index = faces.index(face) if face is not None else 0
            return face

        effect = select(forced_effects, Watch)
        counters.Record(forced_effects, chosen_index)
        return effect

    EventManager.SelectForcedEffect = staticmethod(Instrumented)  # type: ignore[assignment]

    result = run_headless(campaign, heroes, seed, max_steps=max_steps)

    payload = counters.AsDict()
    payload["steps"] = len(result.steps)
    payload["error"] = result.error
    print(MARKER + json.dumps(payload))
    return 0


def _spawn(campaign: str, heroes: List[str], seed: int, max_steps: int) -> Dict[str, Any]:
    proc = subprocess.run(
        [
            sys.executable,
            "-m",
            "tools.determinism.probe_forced_selection",
            "--child",
            campaign,
            ",".join(heroes),
            str(seed),
            str(max_steps),
        ],
        capture_output=True,
        text=True,
        env=build_env(),
        cwd=os.getcwd(),
        errors="replace",
    )
    for line in proc.stdout.splitlines():
        if line.startswith(MARKER):
            return json.loads(line[len(MARKER):])
    raise RuntimeError(
        f"no result from {campaign}/{heroes}/{seed}\n"
        f"stderr tail: {proc.stderr[-800:]}"
    )


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--matrix", choices=("smoke", "wide"), default="smoke")
    parser.add_argument("--max-steps", type=int, default=400)
    args = parser.parse_args(argv)

    cases = MATRIX_SMOKE if args.matrix == "smoke" else MATRIX_WIDE

    totals = Counters()
    for campaign, heroes, seed in cases:
        stats = _spawn(campaign, list(heroes), seed, args.max_steps)
        for name in FIELDS:
            if name == "largest":
                totals.counts[name] = max(totals.counts[name], stats[name])
            else:
                totals.counts[name] += stats[name]
        label = f"{campaign} / {'+'.join(heroes)} / seed {seed}"
        print(
            f"{label}\n"
            f"    {stats['steps']:>4} steps  "
            f"{stats['batches']:>6} batches  "
            f"{stats['multi']:>4} multi  "
            f"{stats['with_delay']:>3} with_delay  "
            f"{stats['misaligned']:>3} misaligned  "
            f"{stats['same_card']:>3} same_card  "
            f"largest {stats['largest']}"
        )
        if stats["error"]:
            print(f"    engine raised: {stats['error']}")

    counts = totals.AsDict()
    print(
        f"\ntotal: {counts['batches']} batches, {counts['multi']} with two or more "
        f"candidates (largest {counts['largest']})\n"
        f"       {counts['with_delay']} carried a delay ability, of which "
        f"{counts['misaligned']} would have resolved the wrong effect (MARVEL-39)\n"
        f"       {counts['same_card']} were all on one card, skipping the "
        f"first-player choice (MARVEL-40)"
    )
    if counts["multi"] == 0:
        print("\nNo multi-candidate batch was reached -- this run exercises neither fix.")
        return 1
    return 0


if __name__ == "__main__":
    if len(sys.argv) > 2 and sys.argv[1] == "--child":
        raise SystemExit(
            _child(sys.argv[2], sys.argv[3].split(","), int(sys.argv[4]), int(sys.argv[5]))
        )
    raise SystemExit(main())
