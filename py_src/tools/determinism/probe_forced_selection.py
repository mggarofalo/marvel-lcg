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

**`multi > 0` is necessary and not sufficient, and that is not a hypothetical.**
Until MARVEL-95 the only batch this probe ever counted was two `AbilityType.Temp0`
cleanups on a single face -- the engine taking two continuous modifiers back off,
labelled `Temp #1` and `Temp #2`, with interchangeable answers and no order for
anyone to choose. It counted as `multi` and proved nothing. MARVEL-95 stopped
asking about those and this probe went straight to zero, which is how the
artefact was identified at all. The current defaults reach a genuine one: two
`ForcedInterrupt` abilities on two different cards, 04072 Experimental Weapons
and 04149 Weapon Master.

So read `multi` with the batch's contents in hand. A count is evidence that a
*prompt* happened, not that a rules question was asked.

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


def _child(campaign: str, heroes: List[str], seed: int, max_steps: int,
           policy: str, policy_seed: int) -> int:
    from tools.determinism.headless import (_initialize_engine, build_decide,
                                            run_headless)

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

    result = run_headless(campaign, heroes, seed, max_steps=max_steps,
                          decide=build_decide(policy, policy_seed))

    payload = counters.AsDict()
    payload["steps"] = len(result.steps)
    payload["error"] = result.error
    print(MARKER + json.dumps(payload))
    return 0


def _spawn(campaign: str, heroes: List[str], seed: int, max_steps: int,
           policy: str, policy_seed: int) -> Dict[str, Any]:
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
            policy,
            str(policy_seed),
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
    # Wide by default, unlike the other probes: the smoke pair provably cannot
    # reach a multi-candidate batch even with a card-playing policy (measured
    # at 59 batches, largest 1), so a bare run of this probe on smoke would
    # always report that it exercises neither fix. See MARVEL-69.
    parser.add_argument("--matrix", choices=("smoke", "wide"), default="wide")
    # 400 steps, `first`, seed 0 was the default until MARVEL-95, and the one
    # multi-candidate batch it ever found was not a tie-break at all: two
    # `AbilityType.Temp0` cleanups on one face, labelled `Temp #1` and
    # `Temp #2`, whose answers were interchangeable. MARVEL-95 stopped asking
    # about those, and this probe went to zero -- which is the measurement that
    # says MARVEL-69's evidence had been resting on an artefact.
    #
    # These defaults reach a real one: 04072 Experimental Weapons and 04149
    # Weapon Master, two `ForcedInterrupt` abilities on two different cards,
    # under `crossbones / black_widow+doctor_strange+hulk / seed 20260806`.
    # Reproducible across runs, and `unit_test/test_forced_order_prompt.py`
    # pins the shape independently of self-play finding it.
    parser.add_argument("--max-steps", type=int, default=800)
    # A decline-only driver never plays a card, so it never opens a response
    # window where two forced abilities meet. See MARVEL-69.
    parser.add_argument("--policy", default="random",
                        choices=("decline", "first", "random"))
    parser.add_argument("--policy-seed", type=int, default=2)
    args = parser.parse_args(argv)

    cases = MATRIX_SMOKE if args.matrix == "smoke" else MATRIX_WIDE

    totals = Counters()
    for campaign, heroes, seed in cases:
        stats = _spawn(campaign, list(heroes), seed, args.max_steps,
                       args.policy, args.policy_seed)
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
            _child(sys.argv[2], sys.argv[3].split(","), int(sys.argv[4]),
                   int(sys.argv[5]), sys.argv[6], int(sys.argv[7]))
        )
    raise SystemExit(main())
