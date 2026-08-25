"""Emit the cross-language prompt test vectors.

Writes `datasets/digest/prompts.json`, the acceptance fixture for the *other*
half of the fold's return value.

`datasets/digest/vectors.json` records the board at every step and says nothing
about what the player was being asked. A port can reproduce all seven recorded
boards of `rhino / spider_man / 12345` while asking entirely the wrong
questions, because declining a prompt that should never have been offered leaves
the same board as declining the right one. This is the fixture that closes that
gap:

    (state, input) -> (state, Prompt?, GameEvent[])
                       ^^^^^  ^^^^^^^
                    vectors  this file

The two are emitted from the same three games with the same seeds and the same
declining policy, so step *n* here is the prompt that was open at step *n*
there. Read them together.

## What a prompt is, in the Python engine

`Controller.ChoiceOne` renders each candidate `Effect` into an
`EffectDescriptor` and hands the batch to the device as an `AskOptionPayload`.
That payload is the prompt, and `DeviceManager.DoGetInput` supplies the seat it
was put to. The projection below is `tools.affordances.verify.Project` widened
to every field the C# records carry -- twelve of the payload's fourteen. The two
it drops, `select_rule_param` and `pay_size_is_effect`, are the two MARVEL-161
dropped after the census, and they are dropped here for the same reason rather
than as a second, independent decision.

## Three things this measures that were previously assumed

**`ability_type` is `TimingPriority.name`, and that enum has twelve members** --
`Rule`, `Statistics`, `Constant`, `Status`, `ForcedInterrupt`, `Interrupt`,
`Boost`, `ForcedResponse`, `Response`, `Normal`, `Consequential`, `End`. The C#
`PromptKind` has four, drawn from a census that observed exactly four. That is a
sample, not a domain; `kinds` in the output records which ones these games
actually reach so the gap is visible rather than inferred.

**An affordance has one string, not two.** The C# `Affordance` carries both
`Verb` and `Label`; the payload carries one `name` and there is no second
source. `verb` is emitted alone. A port that fills `Label` from anywhere but
`name` is inventing it.

**A prompt's own label is `prompt_text`,** which is the engine's console line and
does carry leading newlines and turn numbers -- `"\\n--- Spider-Man's Turn (1)
---"`. It is emitted verbatim, whitespace included, because the alternative is
for two implementations to normalise it differently.

Run:
    python -m tools.digest.emit_prompts
    python -m tools.digest.emit_prompts --check   # non-zero if the file is stale

The output is byte-stable: same code, same engine, same file, no timestamps.
Like `emit_vectors`, it boots the engine and plays, so it takes seconds rather
than milliseconds and does not belong in the fast unit tier.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
from typing import Any, Dict, List, Optional, Sequence

from tools import fixtures
from tools.determinism.pinned_env import is_pinned
from tools.digest.emit_vectors import CASES

OUTPUT = os.path.join("..", "datasets", "digest", "prompts.json")


def Targets(option: Dict[str, Any]) -> Optional[Dict[str, Any]]:
    """The `TargetRequest`, or `None` when the affordance takes no targets.

    Absent rather than empty: the C# type is nullable and 13.5% of sampled
    options have nothing to choose. `Change_Form` is the case to keep in mind --
    empty candidates, empty groups, a `[0, 0]` range -- and a client that
    rendered it as "select between 0 and 0 things" would be wrong in a way an
    absent request cannot be.
    """
    legal = list(option.get("all_legal_targets") or [])
    groups = [list(group) for group in (option.get("target_groups") or [])]
    low, high = (list(option.get("target_num_range") or []) + [0, 0])[:2]
    traits = list(option.get("target_must_include_traits") or [])
    rule = option.get("select_rule") or ""
    search = bool(option.get("is_search"))

    if not legal and not groups and not traits and not rule and (low, high) == (0, 0):
        return None

    return {
        "legal": legal,
        "min": int(low),
        "max": int(high),
        "groups": groups,
        "must_include_traits": traits,
        "rule": rule,
        "is_search": search,
    }


def Costs(option: Dict[str, Any]) -> List[Dict[str, Any]]:
    """The `CostOption` list, one per priced target, ascending by target.

    `target_payment` is keyed by target id as a *string*, because it arrived
    through JSON. The key is an object id and is emitted as an integer; sorting
    on the string would order 10 before 2 and make the fixture depend on how
    many objects happened to exist.
    """
    payment = option.get("target_payment") or {}
    costs: List[Dict[str, Any]] = []
    for target in sorted(payment, key=int):
        entry = payment[target] or {}
        sources: List[Dict[str, str]] = []
        for slot in (entry.get("payment") or []):
            # One `{effect: letters}` mapping per generator, in offer order.
            # Not sorted: the engine's order is the order a client lists them
            # in, and 22.1% of priced affordances offer five of these.
            for effect, generates in slot.items():
                sources.append({"effect": int(effect), "generates": generates})

        costs.append({
            "target": int(target),
            "cost": str(entry.get("cost") or ""),
            "rule": list(entry.get("rule") or []),
            "or_cost": str(entry.get("or_cost") or ""),
            "or_rule": list(entry.get("or_rule") or []),
            "sources": sources,
        })

    return costs


def Affordance(option: Dict[str, Any]) -> Dict[str, Any]:
    """One rendered option, reduced to what the affordance model carries."""
    return {
        "id": option.get("id"),
        "verb": option.get("name") or "",
        "anchor_id": option.get("bind_id", -1),
        "anchor_player": option.get("bind_player_id", -1),
        "targets": Targets(option),
        "costs": Costs(option),
        # `""` becomes `null` so it maps onto a nullable C# string without the
        # port having to decide whether an empty reason means "legal" or "the
        # engine forgot to say".
        "illegal": option.get("failure_reason") or None,
    }


def PromptOf(player_id: int, payload: Any) -> Dict[str, Any]:
    """One `AskOptionPayload` plus the seat it was put to, projected."""
    options = json.loads(getattr(payload, "options_json", "") or "[]")
    return {
        "player": player_id,
        "kind": getattr(payload, "ability_type", "") or "",
        "trigger": getattr(payload, "event_name", "") or "",
        "label": getattr(payload, "prompt_text", "") or "",
        # The payload says whether the engine will refuse a cancel, which is
        # the negation of what a client needs. `show_cancel` is already that
        # negation, so it travels under the name the C# record uses.
        "cancellable": bool(getattr(payload, "show_cancel", False)),
        "affordances": [Affordance(option) for option in options],
    }


def _Run(campaign: str, heroes: Sequence[str], seed: int, max_steps: int) -> List[Dict[str, Any]]:
    """Every prompt of one headless game, in order, declining each."""
    from tools.determinism.headless import run_headless

    prompts: List[Dict[str, Any]] = []

    def decide(player_id: int, payload: Any) -> str:
        prompts.append(PromptOf(player_id, payload))
        return "{}"

    run_headless(campaign, heroes, seed, max_steps=max_steps, decide=decide)
    return prompts


def Build() -> Dict[str, Any]:
    from engine.lib import Ver

    cases: List[Dict[str, Any]] = []
    kinds: Dict[str, int] = {}
    verbs: Dict[str, int] = {}

    # The same cases as `emit_vectors`, imported rather than repeated: step *n*
    # of a case here has to be the prompt open at step *n* of the same case
    # there, and two hand-kept lists would eventually disagree about that.
    for campaign, heroes, seed, max_steps, _full in CASES:
        prompts = _Run(campaign, heroes, seed, max_steps)
        for prompt in prompts:
            kinds[prompt["kind"]] = kinds.get(prompt["kind"], 0) + 1
            for affordance in prompt["affordances"]:
                verbs[affordance["verb"]] = verbs.get(affordance["verb"], 0) + 1

        cases.append({
            "campaign": campaign,
            "heroes": list(heroes),
            "seed": seed,
            "max_steps": max_steps,
            "steps": len(prompts),
            "prompts": prompts,
        })

    return {
        "contract": "docs/presentation-layer.md",
        "generated_by": "py_src/tools/digest/emit_prompts.py",
        "engine_build": str(Ver.version),
        "note": "step n here is the prompt open at step n of vectors.json.",
        "policy": "decline",
        # Which of `TimingPriority`'s twelve members and which verbs these games
        # actually reach. A port checks its own coverage against this instead of
        # against the four-member enum, which is a sample of a larger domain.
        "kinds": dict(sorted(kinds.items())),
        "verbs": dict(sorted(verbs.items())),
        "cases": cases,
    }


def Render(document: Dict[str, Any]) -> str:
    return json.dumps(document, indent=2, sort_keys=True) + "\n"


def _main(argv: Sequence[str]) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true",
                        help="exit non-zero if the checked-in file is stale")
    args = parser.parse_args(list(argv))

    if not is_pinned():
        print("warning: not running under the pinned environment "
              "(see tools/determinism/pinned_env.py)", file=sys.stderr)

    rendered = Render(Build())

    if args.check:
        verdict = fixtures.Compare(rendered, OUTPUT)
        if verdict != fixtures.FRESH:
            print(fixtures.Explain(verdict, OUTPUT,
                                   "python -m tools.digest.emit_prompts"),
                  file=sys.stderr)
            return 1
        print(f"{OUTPUT} is up to date")
        return 0

    os.makedirs(os.path.dirname(OUTPUT), exist_ok=True)
    with open(OUTPUT, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(rendered)
    print(f"wrote {OUTPUT}")
    return 0


if __name__ == "__main__":
    raise SystemExit(_main(sys.argv[1:]))
