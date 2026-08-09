"""Probe: are Change Form effect ids stable when the allocator moves?

`PlayerSetup.register_change_form` gathers identity faces into a `set` and
registers one Change Form ability per entry, so set iteration order decides the
order `Effect` object ids are allocated. Those ids are written into saved
scenes. `docs/determinism-audit.md` (F4) measured the failure while landing
MARVEL-38: two of three bot scenes differed, and the whole diff was `e6`/`e7`
swapping between `01001a` and `01001b`.

Per-step digests cannot see this. `CalculateDigest` is keyed on *card* object
ids, which come off a separate counter, so effect-id drift never reaches it --
that is audit finding F5. Running `check_runs` after the fix therefore proves
nothing about it either way, which is exactly why this probe exists.

What it does instead is what F4's evidence did, deliberately: boot the engine
several times with different amounts of incidental allocation before setup, and
compare the `(effect object id, printed card id)` pairs the Change Form
abilities were given. Identity-hashed set order is a function of allocation
history, so a perturbation is the cheapest way to make a latent reordering fire.

  stable    every perturbation produced the same pairs -- exit 0
  UNSTABLE  a perturbation reordered them -- exit 1, and the orders are printed

Against the pre-MARVEL-33 code this reports UNSTABLE. Note that plain
`sorted(identities)` does not fix it either: `CardFace.__lt__` orders by card,
and an identity's two faces share one card, so they tie and the stable sort
keeps address order. See `PlayerSetup.IdentityOrder`.

Run:  python -m tools.determinism.probe_change_form_order
      python -m tools.determinism.probe_change_form_order --matrix wide
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

# How much incidental allocation to do before the engine boots. Stands in for
# anything that shifts the allocator: an extra log line, a longer card name,
# one more player. `probe_hash_order` shows 0 and 7 landing the same way and 64
# and 5000 landing differently, so the set spans both sides of that boundary.
PERTURBATIONS = (0, 7, 64, 5000)


def _CollectChangeFormEffects(world: Any) -> List[List[Any]]:
    """`[effect object id, printed card id]` for every Change Form ability.

    Read off the live world rather than off a saved scene so the probe does not
    depend on the save format, and sorted by effect id because the question is
    which id each *ability* received -- not what order the cards were visited
    in.
    """
    found: List[List[Any]] = []
    for card in world.object_manager.card_dict.values():
        for effect in card.GetEffects():
            if effect.ability.IsFunction("Change Form"):
                found.append([effect.object_id, effect.this.paper.card_id])
    return sorted(found)


def _child(campaign: str, heroes: List[str], seed: int, perturb: int) -> int:
    # Before the engine is imported, so the allocation history the identity set
    # is hashed against is already different by the time the set is built.
    junk = ["x" * (i % 97) for i in range(perturb)]
    del junk

    from tools.determinism.headless import run_headless

    # Setup is all this needs: Change Form abilities are registered in
    # `PlayerSetup.SetupPlayerAbility`, long before the first decision.
    result = run_headless(campaign, heroes, seed, max_steps=1)

    from engine import Engine

    world = Engine.game.world
    payload: Dict[str, Any] = {
        "effects": _CollectChangeFormEffects(world) if world else [],
        "error": result.error,
    }
    print(MARKER + json.dumps(payload))
    return 0


def _spawn(campaign: str, heroes: List[str], seed: int, perturb: int) -> Dict[str, Any]:
    proc = subprocess.run(
        [
            sys.executable,
            "-m",
            "tools.determinism.probe_change_form_order",
            "--child",
            campaign,
            ",".join(heroes),
            str(seed),
            str(perturb),
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
        f"no result from {campaign}/{heroes}/{seed} perturb={perturb}\n"
        f"stderr tail: {proc.stderr[-800:]}"
    )


def _Format(effects: List[List[Any]]) -> str:
    return " ".join(f"e{object_id}={card_id}" for object_id, card_id in effects)


def main(argv: List[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--matrix", choices=("smoke", "wide"), default="smoke")
    args = parser.parse_args(argv)

    cases = MATRIX_SMOKE if args.matrix == "smoke" else MATRIX_WIDE

    unstable = 0
    barren = 0
    for campaign, heroes, seed in cases:
        label = f"{campaign} / {'+'.join(heroes)} / seed {seed}"
        orders: Dict[str, List[int]] = {}
        count = 0
        for perturb in PERTURBATIONS:
            stats = _spawn(campaign, list(heroes), seed, perturb)
            if stats["error"]:
                print(f"{label}\n    engine raised: {stats['error']}")
            count = len(stats["effects"])
            orders.setdefault(_Format(stats["effects"]), []).append(perturb)

        if count == 0:
            # Nothing was registered, so the case proves nothing. Say so rather
            # than let it read as a pass.
            print(f"{label}\n    no Change Form ability registered -- proves nothing")
            barren += 1
            continue

        if len(orders) == 1:
            print(f"{label}\n    {count} ability(s), stable across {len(PERTURBATIONS)} perturbations")
            print(f"        {next(iter(orders))}")
        else:
            unstable += 1
            print(f"{label}\n    {count} ability(s), UNSTABLE -- {len(orders)} distinct orderings")
            for order, perturbs in orders.items():
                print(f"        perturb={','.join(str(p) for p in perturbs):<12} {order}")

    print()
    if unstable:
        print(f"{unstable} case(s) reordered under perturbation -- effect ids are not deterministic")
        return 1
    if barren == len(cases):
        print("No case registered a Change Form ability -- this run does not exercise the order.")
        return 1
    print(f"all {len(cases) - barren} case(s) with Change Form abilities held their ids")
    return 0


if __name__ == "__main__":
    if len(sys.argv) > 2 and sys.argv[1] == "--child":
        raise SystemExit(
            _child(sys.argv[2], sys.argv[3].split(","), int(sys.argv[4]), int(sys.argv[5]))
        )
    raise SystemExit(main())
