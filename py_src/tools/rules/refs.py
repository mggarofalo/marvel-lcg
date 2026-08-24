"""Look a rule up, and find everything that points at it.

    python -m tools.rules.refs rr:damage           # the rule, and what references it
    python -m tools.rules.refs rr:damage --tree    # follow the references outward
    python -m tools.rules.refs --roots             # rules nothing references
    python -m tools.rules.refs --cycles            # references must not loop

Run from `py_src/`.

## The data is one-way; the reverse index is not data

References flow in exactly one direction: **an exception names the rule it
overrides, and a base rule names nothing.** `rr:damage` references nothing.
`rr:overkill` references `rr:damage`. Something else may reference
`rr:overkill`. It is a directed acyclic graph and it is authored that way, once
per edge.

"What references damage?" is then a *retrieval* question, answered by scanning
at query time -- never stored. Storing it would mean the same relationship
written down twice, in two files, able to disagree; and the moment they
disagree there is no way to tell which is right.

This module is that retrieval, and the model a tooltip UI would use: open a
rule, see every rule that references it, open one of those, see the same thing
again, outward as far as the reader wants to go.

## Why cycles are an error rather than a shape

A cycle means two rules each claim priority over the other, which is not a
statement about the game -- it is two authors disagreeing. `--cycles` is a
gate, not a report.
"""

from __future__ import annotations

import argparse
import collections
import json
import os
import sys
from typing import Dict, Iterable, List, Sequence, Set

RR_INDEX = os.path.join("..", "datasets", "rules-reference", "index.json")
PACK_INDEX = os.path.join("..", "datasets", "rules-packs", "index.json")
GRAPH = os.path.join("..", "datasets", "rules-graph.json")


def load(*paths: str, graph: str = GRAPH) -> Dict[str, Dict]:
    """Every rule in the corpus, both tiers, keyed by id, with its edges.

    The indexes are *generated* -- rebuilt from the PDFs on every harvest and
    carrying no reference field at all. The edges are *authored*, and live in
    one file of their own. Keeping them apart is what makes the authoring
    survive a re-harvest; storing an authored edge inside a file that gets
    destroyed and rewritten would lose it on the next refresh, silently.
    """
    rules: Dict[str, Dict] = {}
    for path in paths:
        if not os.path.exists(path):
            continue
        with open(path, encoding="utf-8") as handle:
            for record in json.load(handle).get("entries", []):
                rules[record["id"]] = dict(record, references=[])

    if os.path.exists(graph):
        with open(graph, encoding="utf-8") as handle:
            for rule_id, edge in json.load(handle).get("edges", {}).items():
                if rule_id in rules:
                    rules[rule_id]["references"] = list(edge.get("references") or [])
                    rules[rule_id]["why"] = edge.get("why", "")
    return rules


def outgoing(rules: Dict[str, Dict], rule_id: str) -> List[str]:
    return list((rules.get(rule_id) or {}).get("references") or [])


def incoming(rules: Dict[str, Dict], rule_id: str) -> List[str]:
    """Computed, never stored — see the module docstring."""
    return sorted(other for other, record in rules.items()
                  if rule_id in (record.get("references") or []))


def cycles(rules: Dict[str, Dict]) -> List[List[str]]:
    """Every reference cycle, as a list of ids in order."""
    found: List[List[str]] = []
    colour: Dict[str, int] = collections.defaultdict(int)
    stack: List[str] = []

    def walk(node: str) -> None:
        colour[node] = 1
        stack.append(node)
        for nxt in outgoing(rules, node):
            if nxt not in rules:
                continue
            if colour[nxt] == 1:
                found.append(stack[stack.index(nxt):] + [nxt])
            elif colour[nxt] == 0:
                walk(nxt)
        stack.pop()
        colour[node] = 2

    for node in sorted(rules):
        if colour[node] == 0:
            walk(node)
    return found


def _describe(rules: Dict[str, Dict], rule_id: str) -> str:
    record = rules.get(rule_id) or {}
    return f"{rule_id}  {record.get('fragment', record.get('title', ''))[:78]}"


def _tree(rules: Dict[str, Dict], rule_id: str, depth: int,
          seen: Set[str], indent: str = "") -> None:
    if depth <= 0:
        return
    for other in incoming(rules, rule_id):
        marker = " (seen)" if other in seen else ""
        print(f"{indent}  ← {_describe(rules, other)}{marker}")
        if other not in seen:
            seen.add(other)
            _tree(rules, other, depth - 1, seen, indent + "    ")


def main(argv: Sequence[str] | None = None) -> int:
    parser = argparse.ArgumentParser(
        description="Follow the one-way rule reference graph.")
    parser.add_argument("rule_id", nargs="?")
    parser.add_argument("--rr", default=RR_INDEX)
    parser.add_argument("--packs", default=PACK_INDEX)
    parser.add_argument("--tree", action="store_true",
                        help="follow incoming references outward")
    parser.add_argument("--depth", type=int, default=3)
    parser.add_argument("--roots", action="store_true",
                        help="rules nothing references")
    parser.add_argument("--cycles", action="store_true")
    args = parser.parse_args(argv)

    rules = load(args.rr, args.packs)
    if not rules:
        print("no rules corpus found", file=sys.stderr)
        return 2

    if args.cycles:
        found = cycles(rules)
        if not found:
            print(f"no reference cycles across {len(rules)} rule(s)")
            return 0
        for cycle in found:
            print("  cycle: " + " → ".join(cycle))
        return 1

    if args.roots:
        referenced = {target for record in rules.values()
                      for target in (record.get("references") or [])}
        roots = sorted(set(rules) - referenced)
        print(f"{len(roots)} rule(s) nothing references:\n")
        for rule_id in roots[:60]:
            print(f"  {_describe(rules, rule_id)}")
        if len(roots) > 60:
            print(f"\n  ... and {len(roots) - 60} more")
        return 0

    if not args.rule_id:
        edges = sum(len(r.get("references") or []) for r in rules.values())
        print(f"{len(rules)} rules, {edges} reference(s).")
        print("Pass a rule id, or --roots / --cycles.")
        return 0

    if args.rule_id not in rules:
        print(f"unknown rule {args.rule_id!r}", file=sys.stderr)
        return 1

    record = rules[args.rule_id]
    print(f"{args.rule_id}  —  {record.get('title', '')}")
    if record.get("fragment"):
        print(f"\n  {record['fragment']}")

    out = outgoing(rules, args.rule_id)
    print(f"\nreferences ({len(out)}) — authored, one-way:")
    for other in out:
        print(f"  → {_describe(rules, other)}")
    if record.get("why"):
        print(f"      because: {record['why']}")

    back = incoming(rules, args.rule_id)
    print(f"\nreferenced by ({len(back)}) — computed, never stored:")
    if args.tree:
        _tree(rules, args.rule_id, args.depth, {args.rule_id})
    else:
        for other in back:
            print(f"  ← {_describe(rules, other)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
