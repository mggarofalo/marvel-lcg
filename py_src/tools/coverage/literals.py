"""Card ids a card *script* names, which no deck or data file does.

`reach.py` joins the files the engine loads: starter decks, encounter sets,
scenarios. That map is only as complete as the data is, and one scenario builds
its decks in Python instead. `cards/pack/twc/07001a.py` opens a list of id
strings, hands each sub-list to `CardFactory.GenerateCards` or to a
`SetAsideDeck.Create`, and then sets `skip_create_encounter_deck`,
`skip_put_villain_into_play` and `no_use_obligation`, so the data-driven builder
at `game/world/world.py:231` never runs and the empty `villain`/`encounters`
arrays in `data/scenarios/the_wrecking_crew.json` cost the scenario nothing.

There is no metadata join that would recover those cards -- nothing asks
`cards.json` which set they belong to. The ids exist only as Python literals, so
reading the Python is the only way to find them. That is what this file does,
and it is MARVEL-98.

    python -m tools.coverage.literals            # what it finds, per script
    python -m tools.coverage.literals --ids      # just the ids

## The rule, exactly

An **AST walk**, not a regex, over every `cards/pack/**.py`. Two call shapes are
entry points, and only the one argument that holds ids is read:

| call | argument |
|---|---|
| `<anything>.GenerateCards(ids, deck, world)` | first positional, or `names=` |
| `<anything>.Create(effect, villain, ids)` | third positional, or `card_ids=` |

The receiver is not checked, because a static reader cannot know what
`GetThunderballEncounter(effect)` returns; the method name plus the argument
position plus the id shape is what identifies the call. A `.Create(effect,
villain)` with no third argument -- the eight other set-aside decks in the
corpus -- offers nothing to read and is passed over rather than guessed at.

The argument expression is then **resolved to the string constants it can hold**,
through the shapes that a literal table is written in:

- a list, tuple or set display, at any nesting depth
- a name bound to one of those, looked up through the enclosing function scopes
  and then the module
- a subscript of such a name -- `card_ids[index]` yields every id in the table,
  because the index is a loop variable and a static reader cannot say which row
  runs. Over-approximating **within a literal table the same file wrote** keeps
  the map on the side it is already on: an upper bound on reach, so what it
  calls unreachable really is
- `a + b`, a conditional expression, a starred argument
- the *values* of a dict display, and the element of a comprehension over one,
  which is how `cards/pack/sm/venom_goblin/27116a.py` maps a campaign log entry
  to the six Sinister Six villains

Only strings shaped like a card id survive: five digits with an optional letter
(`07005`, `01097a`), or the four-digits-and-a-slug form the challenge decks use
(`9999_two_for_one`). That filter is what stops a list of card *names* or deck
keys reaching the map, and it is why `Create`'s loose method name is safe.

## What it deliberately does not follow

Every one of these is a place the scanner returns nothing rather than guessing,
and each is a real shape in this corpus:

- **an id synthesised from a string.** `f"07{n:03d}"`, `"07" + suffix`,
  `str(number)`. Following it would mean evaluating the card script.
- **ids from a runtime query.** `cards/pack/endless/wild.py` passes
  `CardsDB.GetPapers(set_name="Rhino")` straight to `GenerateCards`. There is no
  literal there to find; that set is decided by the database at run time.
- **ids arriving as a parameter**, or through a helper the script calls. The walk
  is intra-procedural: a name is resolved to what an assignment in an enclosing
  *scope* bound it to, never to what a caller passed.
- **entry points other than the two above.** A maximal probe -- every id-shaped
  string constant anywhere in every card script, sound or not -- finds exactly
  one further card that nothing else reaches: `27175`, handed to
  `AbilityFactoryCampaign.ShuffleCardIntoDeck` in `cards/pack/sm/campaign.py`,
  which needs campaign mode. That is the measurement that says this rule's shape
  is not arbitrarily narrow: widening it to every literal in the tree would move
  reach by one card and admit an id like `01144`, which Ultron's own script names
  to talk about itself.

So this is a lower bound too, like the file it feeds. It is a lower bound with a
measured ceiling one card above it.
"""

from __future__ import annotations

import argparse
import ast
import os
import re
import sys
from typing import Dict, Iterable, List, Sequence, Set

CARD_FOLDER = os.path.join("cards", "pack")

# method name -> (index of the id argument, the keyword that names it).
#
# `GenerateCards(names, deck, world)` is `game/card/factory.py:20`;
# `SetAsideDeck.Create(by_effect, villain, card_ids)` is
# `game/deck/deck_aside.py:16`. Positions and keyword names come from those two
# signatures, so a signature change shows up here rather than in a silent miss.
ENTRY_POINTS: Dict[str, tuple] = {
    "GenerateCards": (0, "names"),
    "Create": (2, "card_ids"),
}

# `07005`, `01097a`, and the `9999_two_for_one` form the challenge decks use.
# Anything else in an id position is a name, a set key or a label, and admitting
# it would put non-cards into the reach map -- the same failure that keeping
# `encounter_sets` out of `SCENARIO_KEYS` avoids on the data side.
CARD_ID = re.compile(r"^\d{4,}[a-z]?(_[a-z0-9_]+)?$")

# A nested scope binds its own names. Walking into one while collecting an
# enclosing scope's bindings would let an inner function lend its locals to a
# sibling -- the same hazard `tools/dsl/blockers.py:OwnBody` documents.
NESTED = (ast.FunctionDef, ast.AsyncFunctionDef, ast.Lambda, ast.ClassDef)


################################################################################
# Resolving an expression to the ids it can hold


def OwnBody(node: ast.AST) -> Iterable[ast.AST]:
    """Every node of this scope, stopping at any nested scope."""
    stack = list(getattr(node, "body", []))
    while stack:
        current = stack.pop()
        yield current
        if isinstance(current, NESTED):
            continue
        stack.extend(ast.iter_child_nodes(current))


def Bindings(node: ast.AST) -> Dict[str, List[ast.expr]]:
    """name -> every expression this scope assigns to it.

    Every assignment, not the last one: a name written twice is resolved to the
    union, because which write reached the call is exactly the question a static
    reader cannot answer. Assignments inside an `if` or a `for` in this scope
    count -- the id table in `07001a.py` is at the top of a handler, but nothing
    says the next one will be.
    """
    found: Dict[str, List[ast.expr]] = {}
    for child in OwnBody(node):
        if isinstance(child, ast.Assign):
            for target in child.targets:
                if isinstance(target, ast.Name):
                    found.setdefault(target.id, []).append(child.value)
        elif isinstance(child, (ast.AnnAssign, ast.AugAssign)):
            if isinstance(child.target, ast.Name) and child.value is not None:
                found.setdefault(child.target.id, []).append(child.value)
    return found


def Strings(node: ast.expr | None, scopes: Sequence[Dict[str, List[ast.expr]]],
            seen: Set[int] | None=None) -> Set[str]:
    """Every string constant this expression can evaluate to, or none.

    `seen` is not defensive tidying: `names = names + ["07005"]` makes the
    binding table cyclic, and without it the walk does not terminate.
    """
    if node is None:
        return set()
    seen = set() if seen is None else seen
    if id(node) in seen:
        return set()
    seen.add(id(node))

    def Sub(child: ast.expr | None) -> Set[str]:
        return Strings(child, scopes, seen)

    def Union(children: Iterable[ast.expr | None]) -> Set[str]:
        found: Set[str] = set()
        for child in children:
            found |= Sub(child)
        return found

    if isinstance(node, ast.Constant):
        return {node.value} if isinstance(node.value, str) else set()
    if isinstance(node, (ast.List, ast.Tuple, ast.Set)):
        return Union(node.elts)
    if isinstance(node, ast.Dict):
        # The values, not the keys. `{"27094": "27158"}[x]` produces a value;
        # the keys are the lookup, which some *other* source named.
        return Union(node.values)
    if isinstance(node, ast.BinOp) and isinstance(node.op, ast.Add):
        return Sub(node.left) | Sub(node.right)
    if isinstance(node, ast.Subscript):
        # Deliberately ignores the index. `card_ids[index]` inside `for index in
        # range(4)` is the shape this file exists for, and every row of that
        # table is played by some game.
        return Sub(node.value)
    if isinstance(node, ast.Starred):
        return Sub(node.value)
    if isinstance(node, ast.IfExp):
        return Sub(node.body) | Sub(node.orelse)
    if isinstance(node, (ast.ListComp, ast.SetComp, ast.GeneratorExp)):
        return Sub(node.elt)
    if isinstance(node, ast.DictComp):
        return Sub(node.value)
    if isinstance(node, ast.Name):
        # Innermost scope that binds the name wins, which is what Python does.
        for scope in reversed(list(scopes)):
            if node.id in scope:
                return Union(scope[node.id])
        return set()
    # A call, an f-string, an attribute, a comparison: nothing literal to read.
    return set()


def Argument(call: ast.Call) -> ast.expr | None:
    """The argument of this call that holds card ids, if it is an entry point."""
    if not isinstance(call.func, ast.Attribute):
        return None
    entry = ENTRY_POINTS.get(call.func.attr)
    if entry is None:
        return None
    index, keyword = entry
    if len(call.args) > index:
        return call.args[index]
    for given in call.keywords:
        if given.arg == keyword:
            return given.value
    return None


class Scan(ast.NodeVisitor):
    """The ids one card script names, with a scope stack for resolving them."""

    def __init__(self) -> None:
        self.scopes: List[Dict[str, List[ast.expr]]] = []
        self.found: Set[str] = set()

    def Module(self, tree: ast.Module) -> Set[str]:
        self.scopes.append(Bindings(tree))
        for node in tree.body:
            self.visit(node)
        self.scopes.pop()
        return {value for value in self.found if CARD_ID.match(value)}

    def visit_FunctionDef(self, node: ast.FunctionDef) -> None:
        self.scopes.append(Bindings(node))
        for child in node.body:
            self.visit(child)
        self.scopes.pop()

    visit_AsyncFunctionDef = visit_FunctionDef  # type: ignore[assignment]

    def visit_Call(self, node: ast.Call) -> None:
        self.found |= Strings(Argument(node), self.scopes)
        self.generic_visit(node)


################################################################################
# Over the card tree


def ScanSource(source: str) -> Set[str]:
    return Scan().Module(ast.parse(source))


def ScanFile(path: str) -> Set[str]:
    try:
        with open(path, encoding="utf-8") as handle:
            source = handle.read()
    except OSError:
        return set()
    try:
        return ScanSource(source)
    except SyntaxError:
        # A card script the engine could not `exec` either. It would fail far
        # more loudly there than a coverage tool needs to here.
        return set()


def CardFiles(folder: str=CARD_FOLDER) -> List[str]:
    found: List[str] = []
    for root, _dirs, files in os.walk(folder):
        for name in sorted(files):
            if name.endswith(".py"):
                found.append(os.path.join(root, name))
    return sorted(found)


def Label(path: str, folder: str=CARD_FOLDER) -> str:
    """`cards/pack/twc/07001a.py` -> `twc/07001a`, on any platform.

    A source name reaches `--out` and a plan digest, so it must not carry a
    backslash on Windows and a slash everywhere else.
    """
    relative = os.path.relpath(path, folder)
    if relative.endswith(".py"):
        relative = relative[:-len(".py")]
    return relative.replace(os.sep, "/")


def Census(folder: str=CARD_FOLDER) -> Dict[str, Set[str]]:
    """path -> the ids it names. Scripts that name none are left out."""
    found: Dict[str, Set[str]] = {}
    for path in CardFiles(folder):
        ids = ScanFile(path)
        if ids:
            found[path] = ids
    return found


def Main(argv: Sequence[str] | None=None) -> int:
    parser = argparse.ArgumentParser(
        description="Card ids that only a card script names")
    parser.add_argument("--folder", default=CARD_FOLDER)
    parser.add_argument("--ids", action="store_true",
                        help="print the ids alone, one per line")
    args = parser.parse_args(argv)

    if not os.path.isdir(args.folder):
        print(f"{args.folder} not found -- run from py_src/", file=sys.stderr)
        return 2

    census = Census(args.folder)
    everything: Set[str] = set()
    for ids in census.values():
        everything |= ids

    if args.ids:
        for card_id in sorted(everything):
            print(card_id)
        return 0

    for path in sorted(census):
        print(f"{Label(path, args.folder):<32} {len(census[path]):>4} id(s)")
        print(f"    {' '.join(sorted(census[path]))}")
    print()
    print(f"{len(census)} script(s), {len(everything)} distinct id(s)")
    return 0


if __name__ == "__main__":
    sys.exit(Main())
