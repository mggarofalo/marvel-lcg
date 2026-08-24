"""MARVEL-164 -- could a client have expressed what the bot actually did?

`tools/affordances/census.py` measured what the engine offers, and MARVEL-161
kept eight of its fourteen fields. Keeping eight is a bet: that the six dropped
carry nothing a player needs, and that the eight carry everything. The corpus
can settle it, because it recorded 200,000 inputs a bot actually chose.

    For every recorded step: the input the corpus holds must be expressible
    using only the `Prompt` the affordance model would have carried.

A failure means one of two things and both are worth knowing -- the affordance
derivation missed a legal action, or the bot took an illegal one.

## Three levels, and the third is the point

    1  the effect      the option the bot chose is in the offered list
    2  the targets     every target it chose is in `TargetRequest.Legal`, and
                       the count is inside min/max
    3  the resources   every generator it spent is in `CostOption.Sources`

**Level 1 is nearly free and nearly redundant.** The replay itself resolves the
recorded effect against the offered list, so a step where the option was missing
would already fail as a divergence. It is measured anyway because "nearly" is
doing work: the engine resolves against `effect_list`, and the model carries a
*projection* of it.

**Level 3 is where the value is.** Only 7.3% of priced affordances have a
single way to pay, so a generator set that is narrower than reality passes
levels 1 and 2 and still leaves a client unable to express a legal payment. The
engine resolves a recorded resource against
`checker.cost_for_different_target.GetAllPayEffects()` -- the effect's own list.
The model carries `target_payment` instead. Whether those two agree is exactly
what nobody has checked.

## What this does not answer

Whether the offered list is *complete* -- whether some legal action the bot
never took is missing from it. The corpus records choices, not the sets they
came from, so nothing derived from it can answer that. It is a spec question.

## Run

    python -m tools.affordances.verify ~/Source/marvel-lcg-corpus --per-shard 1
    python -m tools.affordances.verify <shards> --only rhino --scenes 3
    python -m tools.affordances.verify <shards> --per-shard 1 --json out.json
"""

from __future__ import annotations

import argparse
import collections
import json
import os
import re
import sys
from typing import Any, Dict, List, Optional, Sequence, Tuple

from tools.replay import observe

# `c143 01095` -> 143, `e53 Play c35 32016` -> 53. The recording writes an
# object id and then whatever was readable at the time, which is why the tail
# is not parsed.
_ID = re.compile(r"^([ce])(\d+)\b")


def _ObjectId(text: str) -> int:
    """The object id a recorded reference starts with, or -1."""
    match = _ID.match(str(text))
    return int(match.group(2)) if match else -1


# `e56 Special c19 51010` -> the card the option hangs off, 19.
_ANCHOR = re.compile(r"\bc(\d+)\b")


def _Anchor(command: str) -> int:
    """The anchor card in a recorded effect reference, or -1."""
    match = _ANCHOR.search(str(command))
    return int(match.group(1)) if match else -1


class Verdict:

    KEEP_FAILURES = 8

    def __init__(self) -> None:
        self.scenes = 0
        self.steps = 0

        # How each recorded input was classified.
        self.choices = 0
        self.declines = 0
        self.debug = 0
        self.unrecorded = 0

        self.effect_ok = 0
        self.effect_by_id = 0
        # Resolved by `(anchor, verb)` after the recorded effect id missed.
        self.effect_by_anchor = 0
        self.id_drift: collections.Counter = collections.Counter()
        self.drift_note: Dict[str, Any] = {}

        self.with_targets = 0
        self.targets_ok = 0
        self.counted = 0
        self.count_ok = 0
        self.grouped = 0
        self.grouped_ok = 0
        # Grouped selections whose size is outside the flat min/max. Not a
        # failure: it is the measurement that says min/max cannot be obeyed
        # alongside a group.
        self.grouped_disagrees = 0

        self.with_resources = 0
        self.resources_ok = 0

        # A decline is only expressible if the prompt said declining was legal.
        self.decline_offered = 0

        self.verbs: collections.Counter = collections.Counter()
        self.kinds: collections.Counter = collections.Counter()

        # Two fields MARVEL-161 kept on the strength of the mechanism rather
        # than the evidence: neither appeared once in the 6,351 options its
        # census sampled. This is a larger and differently-drawn sample -- real
        # corpus games rather than bot games -- so it is worth counting them
        # while the options are already rendered.
        self.offered_options = 0
        self.illegal_offered = 0
        self.illegal_reason: collections.Counter = collections.Counter()
        self.trait_constrained = 0
        self.note: Dict[str, Any] = {}
        self.failures: List[Dict[str, Any]] = []
        self.errors: List[str] = []

    @staticmethod
    def id_drift_key(recorded: int, offered: int) -> int:
        """By how much the recorded effect id missed."""
        return recorded - offered

    def Fail(self, detail: Dict[str, Any]) -> None:
        if len(self.failures) < self.KEEP_FAILURES:
            self.failures.append(detail)

    @property
    def ok(self) -> bool:
        return (self.effect_ok == self.choices
                and self.targets_ok == self.with_targets
                and self.count_ok == self.counted
                and self.grouped_ok == self.grouped
                and self.resources_ok == self.with_resources
                and self.decline_offered == self.declines
                and not self.errors)


def Project(descriptor: Any) -> Dict[str, Any]:
    """One rendered option, reduced to what the affordance model carries.

    Deliberately a *projection*: it reads only the eight fields MARVEL-161 kept
    and never falls back to the `Effect` behind them. Reading the effect would
    make this measure the engine's own resolution, which is already known to
    work -- the replay depends on it.
    """
    payment: Dict[int, List[int]] = {}
    for target, entry in (descriptor.target_payment or {}).items():
        generators: List[int] = []
        for slot in (getattr(entry, "payment", None) or []):
            generators.extend(int(key) for key in slot)
        payment[int(target)] = generators

    low, high = (list(descriptor.target_num_range) + [0, 0])[:2]
    return {
        "id": descriptor.id,
        "verb": descriptor.name,
        "anchor": descriptor.bind_id,
        "legal": list(descriptor.all_legal_targets or []),
        "min": low,
        "max": high,
        "groups": [list(group) for group in (descriptor.target_groups or [])],
        "payment": payment,
        "illegal": descriptor.failure_reason or "",
        "traits": list(descriptor.target_must_include_traits or []),
    }


def _CheckTargets(option: Dict[str, Any], chosen: List[int],
                  verdict: Verdict, where: Dict[str, Any]) -> None:
    verdict.with_targets += 1

    outside = [target for target in chosen if target not in option["legal"]]
    if outside:
        verdict.Fail({**where, "why": "targets outside TargetRequest.Legal",
                      "chosen": chosen, "legal": option["legal"]})
    else:
        verdict.targets_ok += 1

    if option["groups"]:
        # Grouped: min/max is deliberately not checked, and that is a finding
        # rather than a concession. See `Verdict.grouped_disagrees`.
        verdict.grouped += 1
        if any(set(chosen) <= set(group) for group in option["groups"]):
            verdict.grouped_ok += 1
        else:
            verdict.Fail({**where, "why": "selection is not inside any target group",
                          "chosen": chosen, "groups": option["groups"]})
        if not option["min"] <= len(chosen) <= option["max"]:
            verdict.grouped_disagrees += 1
            verdict.note.setdefault("grouped_disagrees", {**where,
                                    "chosen": chosen,
                                    "range": [option["min"], option["max"]],
                                    "groups": option["groups"]})
        return

    verdict.counted += 1
    if option["min"] <= len(chosen) <= option["max"]:
        verdict.count_ok += 1
    else:
        verdict.Fail({**where, "why": "target count outside min/max",
                      "chosen": chosen,
                      "range": [option["min"], option["max"]]})


def _CheckResources(option: Dict[str, Any], spent: List[int], chosen: List[int],
                    verdict: Verdict, where: Dict[str, Any]) -> None:
    verdict.with_resources += 1

    # A cost may be priced per target or not at all, and the recording does not
    # say which entry it paid against -- so the offer is the union. That is the
    # right reading for the question being asked: a client picks a target first
    # and then pays, so anything in any entry was offerable.
    offered = {generator for generators in option["payment"].values()
               for generator in generators}
    outside = [generator for generator in spent if generator not in offered]
    if outside:
        verdict.Fail({**where, "why": "resources outside CostOption.Sources",
                      "spent": spent, "offered": sorted(offered),
                      "targets": chosen})
    else:
        verdict.resources_ok += 1


def Verify(root: str, *, only: Sequence[str] = (), scenes: int = 0,
           per_shard: int = 0, max_steps: int = 0) -> Verdict:
    verdict = Verdict()

    for scene, text in observe.scenes(root, only=only, limit=scenes,
                                      per_shard=per_shard):
        verdict.scenes += 1

        def on_step(obs: observe.Observation) -> None:
            verdict.steps += 1
            recorded = obs.recorded
            if recorded is None:
                verdict.unrecorded += 1
                return

            verdict.kinds[obs.kind or "(none)"] += 1
            where = {"scene": os.path.basename(obs.scene), "step": obs.step,
                     "event": recorded.event, "input": recorded.effect.id}

            command = str(recorded.effect.id)
            if command.startswith(":") or command.startswith("Puzzle."):
                verdict.debug += 1
                return

            options = [Project(effect.Render(obs.by_effect, obs.player_id))
                       for effect in obs.effects]
            for option in options:
                verdict.offered_options += 1
                if option["illegal"]:
                    verdict.illegal_offered += 1
                    verdict.illegal_reason[option["illegal"][:70]] += 1
                if option["traits"]:
                    verdict.trait_constrained += 1

            if not command:
                # Declining. Expressible only if the prompt said so, and that
                # is `Prompt.Cancellable` -- a property of the prompt, which is
                # why no option's target range can stand in for it.
                verdict.declines += 1
                if obs.cancellable:
                    verdict.decline_offered += 1
                else:
                    verdict.Fail({**where, "why": "declined a prompt that was not cancellable",
                                  "kind": obs.kind,
                                  "options": [option["verb"] for option in options]})
                return

            verdict.choices += 1
            verdict.verbs[observe._Verb(command)] += 1

            wanted = _ObjectId(command)
            match = next((option for option in options if option["id"] == wanted), None)
            if match is not None:
                verdict.effect_ok += 1
                verdict.effect_by_id += 1
            else:
                # The effect object id did not match. That is not the same as
                # the option being absent: effect ids are allocated per session
                # and a recording is a different session, which is why the
                # engine re-resolves through `CommandDescriptor.FindNewEffectId`
                # rather than trusting the number it wrote down.
                #
                # `(anchor, verb)` is what the model carries, and whether it is
                # enough is the question -- so it is tried, and counted
                # separately rather than folded into the first number.
                verb, anchor = observe._Verb(command), _Anchor(command)
                candidates = [option for option in options
                              if option["anchor"] == anchor and option["verb"] == verb]
                if len(candidates) == 1:
                    verdict.effect_ok += 1
                    verdict.effect_by_anchor += 1
                    verdict.id_drift[verdict.id_drift_key(wanted, candidates[0]["id"])] += 1
                    verdict.drift_note.setdefault(observe._Trigger(recorded.event), where)
                    match = candidates[0]
                else:
                    verdict.Fail({**where, "why": "the chosen effect is not in the offered list",
                                  "offered": [(option["id"], option["anchor"],
                                               option["verb"]) for option in options],
                                  "by": f"anchor c{anchor} verb {verb!r} matched "
                                        f"{len(candidates)} options"})
                    return

            chosen = [_ObjectId(target) for target in recorded.effect.targets]
            if chosen:
                _CheckTargets(match, chosen, verdict, where)

            spent = [_ObjectId(resource) for resource in recorded.effect.resources]
            if spent:
                _CheckResources(match, spent, chosen, verdict, where)

        result = observe.replay_text(scene, text, on_step, max_steps=max_steps)
        if result.error:
            verdict.errors.append(f"{scene}: {result.error}")
        # A scene that diverged is not evidence about affordances: the states
        # the options were computed from were not the recorded ones.
        if not result.completed:
            verdict.errors.append(f"{scene}: the replay did not complete")

    return verdict


def Report(verdict: Verdict) -> List[str]:
    def share(count: int, total: int) -> str:
        return f"{count}/{total} ({100.0 * count / total:.1f}%)" if total else "0/0"

    lines = [
        f"scenes            {verdict.scenes}",
        f"steps             {verdict.steps}",
        f"  choices         {verdict.choices}",
        f"  declines        {verdict.declines}",
        f"  debug commands  {verdict.debug}",
        f"  past the recording {verdict.unrecorded}",
        "",
        f"1  effect in the offered list   {share(verdict.effect_ok, verdict.choices)}",
        f"   ...by its recorded id        {share(verdict.effect_by_id, verdict.choices)}",
        f"   ...by (anchor, verb)         {share(verdict.effect_by_anchor, verdict.choices)}",
        f"2  targets in Legal             {share(verdict.targets_ok, verdict.with_targets)}",
        f"   count inside min/max         {share(verdict.count_ok, verdict.counted)}",
        f"   selection inside a group     {share(verdict.grouped_ok, verdict.grouped)}",
        f"   ...whose size min/max forbids {share(verdict.grouped_disagrees, verdict.grouped)}",
        f"3  resources in Sources         {share(verdict.resources_ok, verdict.with_resources)}",
        "",
        f"   declining was offered        {share(verdict.decline_offered, verdict.declines)}",
        "",
        f"options rendered              {verdict.offered_options}",
        f"  carrying a failure reason   {share(verdict.illegal_offered, verdict.offered_options)}",
        f"  constrained by a trait      {share(verdict.trait_constrained, verdict.offered_options)}",
        "",
        "prompt kinds:",
    ]
    for kind, count in verdict.kinds.most_common():
        lines.append(f"  {kind:<24} {count}")
    lines.append("")
    lines.append(f"verbs exercised: {len(verdict.verbs)}")
    for verb, count in verdict.verbs.most_common(12):
        lines.append(f"  {verb or '(none)':<24} {count}")

    for failure in verdict.failures:
        lines.append("")
        lines.append(f"FAILED {failure['scene']} step {failure['step']}: {failure['why']}")
        for key, value in failure.items():
            if key not in ("scene", "step", "why"):
                lines.append(f"    {key}: {value}")

    for reason, count in verdict.illegal_reason.most_common(8):
        lines.append(f"  illegal: {reason} ({count})")

    if verdict.id_drift:
        lines.append("")
        lines.append("recorded effect id minus offered effect id, where they differed:")
        for delta, count in verdict.id_drift.most_common(10):
            lines.append(f"  {delta:+d} on {count} steps")
        for trigger, where in verdict.drift_note.items():
            lines.append(f"  first seen at {trigger}: {where['input']} "
                         f"({where['scene']} step {where['step']})")

    if "grouped_disagrees" in verdict.note:
        lines.append("")
        lines.append("A grouped selection whose size the flat range forbids:")
        for key, value in verdict.note["grouped_disagrees"].items():
            lines.append(f"    {key}: {value}")

    for error in verdict.errors:
        lines.append(f"ERROR {error}")

    return lines


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Verify affordance completeness against the frozen corpus (MARVEL-164).")
    parser.add_argument("shards", help="the corpus shard directory")
    parser.add_argument("--only", nargs="*", default=(), help="shard names")
    parser.add_argument("--scenes", type=int, default=0)
    parser.add_argument("--per-shard", type=int, default=0)
    parser.add_argument("--max-steps", type=int, default=0)
    parser.add_argument("--json", default="")
    args = parser.parse_args(list(argv) if argv is not None else None)

    verdict = Verify(args.shards, only=args.only, scenes=args.scenes,
                     per_shard=args.per_shard, max_steps=args.max_steps)

    for line in Report(verdict):
        print(line)

    if args.json:
        with open(args.json, "w", encoding="utf-8") as handle:
            json.dump({key: value for key, value in vars(verdict).items()
                       if not key.startswith("_")},
                      handle, indent=2, sort_keys=True, default=dict)

    return 0 if verdict.ok else 1


if __name__ == "__main__":
    sys.exit(main())
