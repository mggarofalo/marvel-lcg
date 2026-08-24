"""What the engine actually offers at a prompt, measured.

MARVEL-161 proposes replacing the prompt's list of option strings with a list of
affordances anchored to board objects. `docs/presentation-layer.md` sketched the
shape as five fields:

    Affordance { Id, Kind, AnchorId, Label, Legality }

This measures what the engine already has to say, so the shape can be checked
against it rather than guessed. The corpus cannot answer the question -- it
records the input that was *chosen*, never the set it was chosen from -- so this
plays games instead, through the same headless harness the determinism probes
use, and censuses the payload at every prompt.

## What a prompt is

`Controller.ChoiceOne` renders each candidate `Effect` into an `EffectDescriptor`
and hands the batch to the device as an `AskOptionPayload`. That payload is the
affordance list in all but name, and it already has two levels:

    prompt   event_name, ability_type, prompt_text, show_cancel
    option   id, name, bind_id, bind_player_id, all_legal_targets,
             target_num_range, target_payment, select_rule, select_rule_param,
             target_groups, target_must_include_traits, failure_reason,
             is_search, pay_size_is_effect

Fourteen fields per option, against the proposal's five. The question this answers
is which of the fourteen carry information often enough to belong on the wire,
and which are vestigial.

Run:
    python -m tools.affordances.census --games 12
    python -m tools.affordances.census --games 40 --json census.json

Each game boots the engine, so this takes seconds per game rather than
milliseconds. It is a tool, not a unit test.
"""

from __future__ import annotations

import argparse
import collections
import json
import sys
from typing import Any, Dict, Iterator, List, Sequence, Tuple

# Campaign/hero pairs to sample. Spread across factions and villain shapes so
# the census is not a portrait of one scenario's ability set.
BOARDS: Tuple[Tuple[str, Tuple[str, ...]], ...] = (
    ("rhino", ("spider_man",)),
    ("klaw", ("captain_marvel",)),
    ("ultron", ("iron_man", "black_panther")),
    ("rhino", ("she_hulk", "ms_marvel")),
    ("crossbones", ("captain_america",)),
    ("absorbing_man", ("thor",)),
)


class Census:

    def __init__(self) -> None:
        self.games = 0
        self.prompts = 0
        self.options = 0

        self.options_per_prompt: collections.Counter = collections.Counter()
        self.event_name: collections.Counter = collections.Counter()
        self.ability_type: collections.Counter = collections.Counter()
        self.verb: collections.Counter = collections.Counter()
        self.show_cancel: collections.Counter = collections.Counter()

        # Per-option field usage: how often each says something.
        self.informative: collections.Counter = collections.Counter()
        self.targets_offered: collections.Counter = collections.Counter()
        self.payment_options: collections.Counter = collections.Counter()
        self.anchor_is_self: collections.Counter = collections.Counter()
        self.failure_reason: collections.Counter = collections.Counter()
        self.select_rule: collections.Counter = collections.Counter()

    def Prompt(self, payload: Any) -> None:
        self.prompts += 1
        self.event_name[getattr(payload, "event_name", "") or "(none)"] += 1
        self.ability_type[getattr(payload, "ability_type", "") or "(none)"] += 1
        self.show_cancel[bool(getattr(payload, "show_cancel", False))] += 1

        try:
            options = json.loads(getattr(payload, "options_json", "") or "[]")
        except json.JSONDecodeError:
            return
        self.options_per_prompt[len(options)] += 1

        for option in options:
            self.options += 1
            self.verb[option.get("name") or "(none)"] += 1
            self._Option(option)

    def _Option(self, option: Dict[str, Any]) -> None:
        targets = option.get("all_legal_targets") or []
        payment = option.get("target_payment") or {}
        low, high = (option.get("target_num_range") or [0, 0])[:2]

        self.targets_offered[len(targets)] += 1
        self.anchor_is_self[option.get("bind_id") in targets] += 1

        if option.get("failure_reason"):
            self.failure_reason[option["failure_reason"][:60]] += 1
        if option.get("select_rule"):
            self.select_rule[option["select_rule"]] += 1

        for entry in payment.values():
            self.payment_options[len(entry.get("payment") or [])] += 1

        # "Informative" means the field would change what a client draws. A
        # field that is always empty does not need to be on the wire.
        checks = {
            "id": option.get("id") is not None,
            "name": bool(option.get("name")),
            "bind_id": option.get("bind_id", -1) >= 0,
            "bind_player_id": option.get("bind_player_id", -1) >= 0,
            "all_legal_targets": bool(targets),
            "target_num_range": (low, high) != (0, 0),
            "target_payment": bool(payment),
            "select_rule": bool(option.get("select_rule")),
            "select_rule_param": tuple(option.get("select_rule_param") or ()) not in ((), (0, 0)),
            "target_groups": bool(option.get("target_groups")),
            "target_must_include_traits": bool(option.get("target_must_include_traits")),
            "failure_reason": bool(option.get("failure_reason")),
            "is_search": bool(option.get("is_search")),
            "pay_size_is_effect": bool(option.get("pay_size_is_effect")),
        }
        for field, informative in checks.items():
            if informative:
                self.informative[field] += 1


def Play(census: Census, campaign: str, heroes: Sequence[str], seed: int,
         max_steps: int) -> None:
    from tools.determinism import headless

    decide = headless.build_decide("mixed", policy_seed=seed)

    def peek(player_id: int, payload: Any) -> str:
        census.Prompt(payload)
        return decide(player_id, payload)

    headless.run_headless(campaign, heroes, seed, max_steps=max_steps, decide=peek)
    census.games += 1


def _Table(counter: collections.Counter, total: int, limit: int,
           key: Any = str) -> Iterator[str]:
    for name, count in counter.most_common(limit):
        share = 100.0 * count / total if total else 0.0
        yield f"  {key(name):<46} {count:>8,}  {share:5.1f}%"


def Report(census: Census, limit: int) -> str:
    out: List[str] = []
    add = out.append
    add(f"games   {census.games:>8,}")
    add(f"prompts {census.prompts:>8,}")
    add(f"options {census.options:>8,}")

    add("\n## Per-option fields that say something")
    add(f"  {'field':<46} {'options':>8}  share")
    out.extend(_Table(census.informative, census.options, 20))

    add("\n## Options per prompt")
    add(f"  {'options':<46} {'prompts':>8}  share")
    out.extend(_Table(census.options_per_prompt, census.prompts, limit,
                      key=lambda n: f"{n} option(s)"))

    add("\n## Legal targets offered per option")
    add(f"  {'targets':<46} {'options':>8}  share")
    out.extend(_Table(census.targets_offered, census.options, limit,
                      key=lambda n: f"{n} target(s)"))

    add("\n## Ways to pay, per priced option")
    priced = sum(census.payment_options.values())
    add(f"  {'payments':<46} {'costs':>8}  share")
    out.extend(_Table(census.payment_options, priced, limit,
                      key=lambda n: f"{n} payment(s)"))

    add("\n## Prompt context: event_name")
    add(f"  {'event':<46} {'prompts':>8}  share")
    out.extend(_Table(census.event_name, census.prompts, limit))

    add("\n## Prompt context: ability_type")
    out.extend(_Table(census.ability_type, census.prompts, limit))

    add("\n## Prompt context: show_cancel")
    out.extend(_Table(census.show_cancel, census.prompts, limit,
                      key=lambda flag: "cancellable" if flag else "not cancellable"))

    add("\n## Verbs")
    add(f"  {'verb':<46} {'options':>8}  share")
    out.extend(_Table(census.verb, census.options, limit))
    add(f"  ({len(census.verb)} distinct)")

    add("\n## select_rule")
    ruled = sum(census.select_rule.values())
    out.extend(_Table(census.select_rule, ruled, limit))
    add(f"  ({len(census.select_rule)} distinct, on {ruled:,} options)")

    add("\n## failure_reason -- options offered that cannot be taken")
    failed = sum(census.failure_reason.values())
    out.extend(_Table(census.failure_reason, failed, limit))
    add(f"  ({len(census.failure_reason)} distinct, on {failed:,} options)")

    return "\n".join(out)


def _main(argv: Sequence[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--games", type=int, default=12)
    parser.add_argument("--max-steps", type=int, default=400)
    parser.add_argument("--seed", type=int, default=20260824)
    parser.add_argument("--limit", type=int, default=25)
    parser.add_argument("--json")
    args = parser.parse_args(argv)

    census = Census()
    for index in range(args.games):
        campaign, heroes = BOARDS[index % len(BOARDS)]
        seed = args.seed + index
        try:
            Play(census, campaign, heroes, seed, args.max_steps)
        except Exception as error:  # noqa: BLE001
            # One board failing to boot must not lose the census of the rest;
            # a partial measurement is still a measurement, and a silent skip
            # is not.
            print(f"  [{index}] {campaign}/{'+'.join(heroes)} seed {seed}: "
                  f"{type(error).__name__}: {error}", file=sys.stderr)
            continue
        print(f"  [{index}] {campaign}/{'+'.join(heroes)} seed {seed}: "
              f"{census.prompts:,} prompts", file=sys.stderr, flush=True)

    print(Report(census, args.limit))

    if args.json:
        payload = {
            "games": census.games,
            "prompts": census.prompts,
            "options": census.options,
            "informative": dict(census.informative),
            "options_per_prompt": {str(k): v for k, v in census.options_per_prompt.items()},
            "targets_offered": {str(k): v for k, v in census.targets_offered.items()},
            "payment_options": {str(k): v for k, v in census.payment_options.items()},
            "event_name": dict(census.event_name),
            "ability_type": dict(census.ability_type),
            "verb": dict(census.verb),
            "select_rule": dict(census.select_rule),
            "failure_reason": dict(census.failure_reason),
        }
        with open(args.json, "w", encoding="utf-8", newline="\n") as handle:
            json.dump(payload, handle, indent=2, sort_keys=True)
        print(f"\nwrote {args.json}", file=sys.stderr)

    return 0


if __name__ == "__main__":
    raise SystemExit(_main(sys.argv[1:]))
