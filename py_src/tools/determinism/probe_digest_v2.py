"""Probe: what the v2 state digest costs, and what v1 could not see.

Every quantitative claim in `docs/state-digest-v2.md` comes from here, so the
claims can be re-checked against a moved tree instead of being taken on trust.
It reimplements v1 exactly as `docs/state-digest-contract.md` specifies it --
the engine no longer contains v1 -- and runs both over the same live worlds.

What it measures:

  coverage    how many of the world's cards each format describes
  payload     bytes per step, raw and gzip -9, and the implied corpus cost
  blind       transitions where a card's state changed and v1's value did not,
              which is the D2 collision made concrete rather than argued
  sentinel    cards in play whose v1 value landed in the -1..-4 sentinel range,
              i.e. D3 firing
  hidden      face-down cards in play that contributed their real attributes to
              v1's sum, i.e. D8 firing

The driver is `run_headless`, whose policy declines every decision. That is a
short game, but the numbers that matter here are per-step and per-card, and the
card count is fixed at setup. Deep-game payload is measured separately from real
bot scenes -- see "How the measurements were taken" in the v2 document.

Run:  python -m tools.determinism.probe_digest_v2
"""

from __future__ import annotations

import gzip
import json
import sys
from dataclasses import dataclass, field
from typing import Any, Dict, List, Tuple

from tools.determinism.headless import run_headless
from tools.determinism.pinned_env import is_pinned

# Enough variety that a conclusion is not a property of one scenario. Each entry
# is (campaign, heroes, seed).
BOARDS: List[Tuple[str, List[str], int]] = [
    ("rhino", ["spider_man"], 12345),
    ("rhino", ["spider_man"], 7),
    ("klaw", ["she_hulk"], 2026),
    ("klaw", ["captain_marvel", "she_hulk"], 99),
    ("ultron", ["black_panther"], 4242),
]

# MARVEL-4 sized the corpus at 10,000 games of a few hundred steps each. Used
# only to turn a per-step byte count into a number anyone can weigh.
CORPUS_GAMES = 10_000
CORPUS_STEPS_PER_GAME = 300


################################################################################
# v1, reimplemented from its specification


def V1CardValue(card: Any) -> int:
    """`docs/state-digest-contract.md`, "Which cards appear, and as what"."""
    if card.IsInHand():
        return -2
    if card.IsInDeck() and card.area.GetAll()[-1] == card.face:
        return -3
    if card.IsInDeck() and card.area.GetAll()[0] == card.face:
        return -4
    if card.IsOnField() or card.area.flags.is_status_area:
        info = card.face.GetInfoDict()
        crc = {
            'is_exhaust': int(not card.state.is_ready),
            'traits': card.face.GetTraitsTotalCount(),
        } | info
        crc = {
            key: value for key, value in crc.items()
            if value != 0 and key not in ('curr_ally_limit', 'curr_restricted_limit')
        }
        return sum(crc.values())
    return -1


def V1Digest(world: Any) -> Dict[int, int]:
    values: Dict[int, int] = {}
    for object_id, card in world.object_manager.card_dict.items():
        if object_id == 0:
            continue
        value = V1CardValue(card)
        if value != -1:
            values[object_id] = value
    return values


def V1Serialize(values: Dict[int, int]) -> str:
    return str(values).replace(' ', '')


################################################################################
# Sampling


@dataclass
class Sample:
    v1: Dict[int, int]
    v2: Dict[int, Dict[str, Any]]
    total_cards: int
    sentinel_collisions: List[Tuple[int, int]] = field(default_factory=list)
    hidden_contributors: List[int] = field(default_factory=list)

    @property
    def v1_text(self) -> str:
        return V1Serialize(self.v1)

    @property
    def v2_text(self) -> str:
        from game.world import digest
        return digest.Serialize({
            "v": digest.DIGEST_VERSION,
            "cards": list(self.v2.values()),
        })


def _Sample(world: Any) -> Sample:
    from game.world import digest

    v1 = V1Digest(world)
    document = digest.BuildDocument(world)
    v2 = {record["id"]: record for record in document["cards"]}

    sample = Sample(v1=v1, v2=v2, total_cards=len(world.object_manager.card_dict))
    for object_id, card in world.object_manager.card_dict.items():
        in_play = card.IsOnField() or card.area.flags.is_status_area
        if not in_play:
            continue
        value = v1.get(object_id)
        # D3: a card in play whose fields sum into the sentinel range is
        # indistinguishable from a card in hand or at a pile boundary.
        if value is not None and value < 0:
            sample.sentinel_collisions.append((object_id, value))
        # D8: v1 populated `self.crc` before the face-up guard.
        if not card.state.is_face_up and value not in (None, 0):
            sample.hidden_contributors.append(object_id)
    return sample


def Collect(campaign: str, heroes: List[str], seed: int, max_steps: int) -> List[Sample]:
    samples: List[Sample] = []

    def decide(player_id: int, payload: Any) -> str:
        from engine import Engine
        world = Engine.game.world
        if world is not None:
            samples.append(_Sample(world))
        return "{}"

    run_headless(campaign, heroes, seed, max_steps=max_steps, decide=decide)
    return samples


################################################################################
# Analysis


def Blind(samples: List[Sample]) -> Dict[str, int]:
    """How each one-card, one-step change fared under v1.

    A change is one card between two consecutive steps whose v2 record moved.
    Four outcomes:

      saw             v1's integer moved too
      missed_fields   the card's *fields* changed and v1's integer did not.
                      This is D2 -- the sum cancelled -- and it is the number
                      that says whether the collision argument is real
      missed_position the card moved within a pile, which v1 tracked only as
                      top / bottom / elsewhere
      outside_v1      v1 omitted the card from the digest entirely
    """
    counts = {"saw": 0, "missed_fields": 0, "missed_position": 0, "outside_v1": 0}
    for before, after in zip(samples, samples[1:]):
        for object_id, record in after.v2.items():
            previous = before.v2.get(object_id)
            if previous == record:
                continue
            if object_id not in before.v1 or object_id not in after.v1:
                counts["outside_v1"] += 1
            elif before.v1[object_id] != after.v1[object_id]:
                counts["saw"] += 1
            elif previous is not None and previous.get("fields") != record.get("fields"):
                counts["missed_fields"] += 1
            else:
                counts["missed_position"] += 1
    return counts


def Bytes(texts: List[str]) -> Tuple[float, float]:
    """(mean raw bytes per step, mean gzip -9 bytes per step over the whole trace)."""
    if not texts:
        return 0.0, 0.0
    raw = sum(len(text.encode("utf-8")) for text in texts)
    blob = "\n".join(texts).encode("utf-8")
    return raw / len(texts), len(gzip.compress(blob, 9)) / len(texts)


def Corpus(per_step_bytes: float) -> float:
    """Gigabytes the digest column alone would add to the MARVEL-4 corpus."""
    return per_step_bytes * CORPUS_STEPS_PER_GAME * CORPUS_GAMES / 1e9


################################################################################
#


def _Report(results: Dict[str, List[Sample]]) -> Dict[str, Any]:
    every = [sample for samples in results.values() for sample in samples]
    if not every:
        raise SystemExit("no samples: every board ended before its first decision")

    v1_raw, v1_gz = Bytes([sample.v1_text for sample in every])
    v2_raw, v2_gz = Bytes([sample.v2_text for sample in every])

    changes = {"saw": 0, "missed_fields": 0, "missed_position": 0, "outside_v1": 0}
    for samples in results.values():
        for key, value in Blind(samples).items():
            changes[key] += value

    return {
        "boards": {
            name: {
                "steps": len(samples),
                "cards": samples[0].total_cards if samples else 0,
                "v1_cards": len(samples[0].v1) if samples else 0,
            }
            for name, samples in results.items()
        },
        "steps": len(every),
        "coverage": {
            "cards_mean": sum(s.total_cards for s in every) / len(every),
            "v1_mean": sum(len(s.v1) for s in every) / len(every),
            "v2_mean": sum(len(s.v2) for s in every) / len(every),
        },
        "payload_bytes_per_step": {
            "v1_raw": round(v1_raw, 1),
            "v1_gzip": round(v1_gz, 1),
            "v2_raw": round(v2_raw, 1),
            "v2_gzip": round(v2_gz, 1),
            "raw_ratio": round(v2_raw / v1_raw, 1) if v1_raw else 0,
            "gzip_ratio": round(v2_gz / v1_gz, 1) if v1_gz else 0,
        },
        "corpus_gb_added": {
            "v1": round(Corpus(v1_gz), 3),
            "v2": round(Corpus(v2_gz), 3),
            "games": CORPUS_GAMES,
            "steps_per_game": CORPUS_STEPS_PER_GAME,
        },
        "changes": changes,
        "sentinel_collisions": sorted({
            pair for sample in every for pair in sample.sentinel_collisions
        }),
        "hidden_contributors": sorted({
            object_id for sample in every for object_id in sample.hidden_contributors
        }),
    }


def _main(argv: List[str]) -> int:
    if not is_pinned():
        print("warning: not running under the pinned environment "
              "(see tools/determinism/pinned_env.py)", file=sys.stderr)

    max_steps = int(argv[1]) if len(argv) > 1 else 60

    results: Dict[str, List[Sample]] = {}
    for campaign, heroes, seed in BOARDS:
        name = f"{campaign}/{'+'.join(heroes)}/{seed}"
        print(f"running {name} ...", file=sys.stderr)
        try:
            results[name] = Collect(campaign, heroes, seed, max_steps)
        except Exception as exc:  # reported, not swallowed
            print(f"  skipped: {type(exc).__name__}: {exc}", file=sys.stderr)

    print(json.dumps(_Report(results), indent=2, sort_keys=True))
    return 0


if __name__ == "__main__":
    raise SystemExit(_main(sys.argv))
