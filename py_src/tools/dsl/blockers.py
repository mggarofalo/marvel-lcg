"""What stops a card script from being data, counted per construct.

MARVEL-92. The design rule in [migration.md](../../../docs/migration.md) is to build
the card DSL against the hardest cards, because the common ones fall out for
free and the tail is what turns a DSL back into a scripting language. Following
that rule needs a number that a design argument cannot supply on its own:
**which construct blocks how many cards, and how many does it block alone.**

    python -m tools.dsl.blockers                  # the census
    python -m tools.dsl.blockers --greedy         # what each node buys, in order
    python -m tools.dsl.blockers --blocker close  # every card a construct blocks
    python -m tools.dsl.blockers --card 44057     # one card's blockers
    python -m tools.dsl.blockers --out b.json     # the whole table

## What it measures, and what it does not

It walks the AST of every `cards/pack/**.py` and, inside the *handler* bodies
only, flags constructs that a tree of typed nodes cannot hold without a node
being designed for them. The ability envelope -- `AbilityFactory.X(...)`,
`.SetTarget(...)`, `.SetCost(...)`, `.LimitOncePerRound()` -- is not walked,
because it is already declarative; 22.9% of the statements in `GetAbilities`
live there. The condition lambdas among them are counted separately by
`EnvelopePredicates` and reported, rather than being silently treated as free.

**This is a static approximation and it errs in both directions.** A card flagged
`augassign` may well be a one-line sum that a `count` query expresses directly;
the flag says a naive `seq`/`if`/`forEach` tree cannot hold it, not that no
reasonable DSL can.

It errs the other way too, and that is the direction worth watching, because a
scanner that stops looking makes the corpus *look easier*. The first version of
this file flagged a `lambda` passed inside a handler but not a locally defined
function passed by name -- the same construct -- and reported 61 cards as
expressible that were not. `clean` is therefore not a guarantee: it means nothing
here objected, not that nothing could.

The number to trust is the *ranking* -- which constructs dominate -- rather than
any single card's verdict. Where the two disagree, the card is right and this
file is wrong.
"""

import argparse
import ast
import collections
import json
import os
import sys
from typing import Dict, List, Sequence, Set, Tuple

CARD_FOLDER = os.path.join("cards", "pack")

# The handler-local mutation methods. Reading a list is free; growing one
# across a loop is a `collect` node.
GROW = ("append", "extend", "add", "insert", "remove", "discard", "pop",
        "update", "clear", "sort", "reverse")

# Inline sub-abilities. These read as "choose a target, then do this" and are a
# node (`chooseTargets`), not dynamic registration -- the ability they build is
# static and only its target is decided at runtime. Splitting them out of the
# `factory-in-handler` count is the single biggest correction this file makes to
# a naive reading: 291 of the 354 scripts that call AbilityFactory inside a
# handler call nothing else.
INLINE_ABILITY = ("ForChoiceAbility", "ForChoiceAbility2", "ForChoiceAbility3",
                  "ForChoiceAbilityWithCost", "Otherwise")

# One line each, because these names end up in a design document.
BLOCKERS: Dict[str, str] = {
    "register": "installs an ability at runtime (Registers / RegisterTemp)",
    "factory-in-handler": "builds a non-inline ability inside a handler",
    "close": "a nested function closing over an enclosing handler's local",
    "grow": "grows or shrinks a local collection",
    "augassign": "accumulates into a local across a loop",
    "break": "leaves a loop early (first-match / any / all)",
    "comprehension": "a list/set/dict comprehension",
    "dyn-subscript": "indexes a literal table by a computed key",
    "isinstance": "dispatches on a runtime type",
    "class-def": "defines a class to hold card-local state",
    "while": "loops until a condition (unbounded)",
    "try": "catches, raises or uses a context manager",
    "string-build": "synthesises an identifier from a string",
    "unpack": "assigns to a tuple or a subscript",
    "slice": "slices a sequence",
    "callback": "passes a function as a value from inside a handler",
}

# Which node in the proposed DSL retires each blocker. `None` means nothing
# retires it and the card stays compiled. Kept next to the blocker so the two
# cannot drift apart in two documents.
RETIRED_BY: Dict[str, str] = {
    "close": "observe",
    "grow": "collect",
    "augassign": "count / sum query",
    "break": "any / all / firstWhere",
    "comprehension": "filter",
    "dyn-subscript": "lookup",
    "isinstance": "typed subject match",
    "class-def": "card-local counters",
    "string-build": "lookup",
    "unpack": "let",
    "slice": "take / drop",
    "callback": "effect subtree as a value",
    "factory-in-handler": "grantUntil",
    "register": "grantUntil",
    "while": "",
    "try": "",
}


################################################################################
#


class Scan(ast.NodeVisitor):
    """Flags on one card script.

    `GetAbilities` itself is the envelope and is skipped; every function nested
    inside it is a handler and is walked. Depth tracks nesting so that a
    function inside a handler -- the closure shape -- can be told apart from a
    handler itself.
    """

    def __init__(self) -> None:
        self.flags: Set[str] = set()
        self.depth = 0
        self.scopes: List[Set[str]] = []
        self.local_defs: List[Set[str]] = []

    # -- entry ---------------------------------------------------------------

    def Module(self, tree: ast.Module) -> Set[str]:
        for node in tree.body:
            if isinstance(node, ast.FunctionDef) and node.name == "GetAbilities":
                self.local_defs.append(LocalDefs(node))
                for child in node.body:
                    self.visit(child)
                self.local_defs.pop()
        return self.flags

    # -- scopes --------------------------------------------------------------

    def visit_FunctionDef(self, node: ast.FunctionDef) -> None:
        bound = Bound(node)
        if self.depth >= 1:
            # A function defined inside a handler closes over the handler when
            # it reads a name the handler bound and it does not bind itself.
            # Both halves matter: without the enclosing scopes it flags
            # nothing, and without subtracting `bound` it flags every inner
            # function that happens to reuse a name.
            enclosing: Set[str] = set()
            for scope in self.scopes:
                enclosing |= scope
            free = Free(node) - bound
            if free & enclosing:
                self.flags.add("close")
        self.depth += 1
        self.scopes.append(bound)
        self.local_defs.append(LocalDefs(node))
        for child in node.body:
            self.visit(child)
        self.local_defs.pop()
        self.scopes.pop()
        self.depth -= 1

    def visit_Lambda(self, node: ast.Lambda) -> None:
        if self.depth >= 1:
            self.flags.add("callback")
        self.generic_visit(node)

    def visit_ClassDef(self, node: ast.ClassDef) -> None:
        self.flags.add("class-def")
        self.generic_visit(node)

    # -- statements ----------------------------------------------------------

    def visit_While(self, node: ast.While) -> None:
        self.flags.add("while")
        self.generic_visit(node)

    def visit_Try(self, node: ast.Try) -> None:
        self.flags.add("try")
        self.generic_visit(node)

    def visit_With(self, node: ast.With) -> None:
        self.flags.add("try")
        self.generic_visit(node)

    def visit_Raise(self, node: ast.Raise) -> None:
        self.flags.add("try")
        self.generic_visit(node)

    def visit_Break(self, node: ast.Break) -> None:
        self.flags.add("break")

    def visit_Continue(self, node: ast.Continue) -> None:
        self.flags.add("break")

    def visit_AugAssign(self, node: ast.AugAssign) -> None:
        if self.depth >= 1:
            self.flags.add("augassign")
        self.generic_visit(node)

    def visit_Assign(self, node: ast.Assign) -> None:
        if self.depth >= 1:
            for target in node.targets:
                if isinstance(target, (ast.Tuple, ast.List)):
                    self.flags.add("unpack")
                elif isinstance(target, ast.Subscript):
                    self.flags.add("unpack")
        self.generic_visit(node)

    # -- expressions ---------------------------------------------------------

    def visit_ListComp(self, node: ast.ListComp) -> None:
        self.Comprehension(node)

    def visit_SetComp(self, node: ast.SetComp) -> None:
        self.Comprehension(node)

    def visit_DictComp(self, node: ast.DictComp) -> None:
        self.Comprehension(node)

    def visit_GeneratorExp(self, node: ast.GeneratorExp) -> None:
        self.Comprehension(node)

    def Comprehension(self, node: ast.AST) -> None:
        if self.depth >= 1:
            self.flags.add("comprehension")
        self.generic_visit(node)

    def visit_Subscript(self, node: ast.Subscript) -> None:
        if self.depth >= 1:
            if isinstance(node.slice, ast.Slice):
                self.flags.add("slice")
            elif not isinstance(node.slice, ast.Constant):
                # A computed index into a literal table is a `lookup`; a
                # computed index into a game collection is ordinary.
                if isinstance(node.value, (ast.Dict, ast.List, ast.Tuple)):
                    self.flags.add("dyn-subscript")
        self.generic_visit(node)

    def visit_Call(self, node: ast.Call) -> None:
        func = node.func
        if isinstance(func, ast.Name) and func.id == "isinstance" and self.depth >= 1:
            self.flags.add("isinstance")
        if self.depth >= 1:
            # A locally-defined function handed to something as an argument is
            # the same construct as a lambda in the same position, and both are
            # the `effect subtree as a value` node. Flagging only the lambda
            # form counted 61 cards clean that pass a named callback instead.
            local: Set[str] = set()
            for scope in self.local_defs:
                local |= scope
            for arg in list(node.args) + [k.value for k in node.keywords]:
                if isinstance(arg, ast.Name) and arg.id in local:
                    self.flags.add("callback")
        if isinstance(func, ast.Attribute):
            name = func.attr
            if self.depth >= 1:
                if name in ("Registers", "RegisterTemp", "UnRegister"):
                    self.flags.add("register")
                if name in ("format",):
                    self.flags.add("string-build")
                owner = func.value
                if isinstance(owner, ast.Name) and owner.id == "AbilityFactory":
                    if name not in INLINE_ABILITY:
                        self.flags.add("factory-in-handler")
                if name in GROW and isinstance(owner, ast.Name):
                    # A name the handler bound, mutated in place. This is a
                    # syntactic test and cannot see what the name refers to:
                    # `units = Worlds.GetOnFieldEnemies(effect)` followed by
                    # `units.remove(...)` counts, and should -- the DSL replaces
                    # it with `filter` either way. What it deliberately does not
                    # count is `player.hand.Add(...)`, a game action reached
                    # through an attribute.
                    if owner.id in self.Enclosing():
                        self.flags.add("grow")
        self.generic_visit(node)

    def visit_JoinedStr(self, node: ast.JoinedStr) -> None:
        if self.depth >= 1:
            self.flags.add("string-build")
        self.generic_visit(node)

    def Enclosing(self) -> Set[str]:
        out: Set[str] = set()
        for scope in self.scopes:
            out |= scope
        return out


def OwnBody(node: ast.AST):
    """Every node of this function, stopping at any nested function.

    `ast.walk` descends into nested `FunctionDef`s, which makes an inner
    function's locals look like the outer one's. That is not a detail: it made
    closure detection fire on mere name reuse, which flagged `to_counter_name`
    in Tic-Tac-Toe -- a function that closes over nothing.
    """
    nested = (ast.FunctionDef, ast.AsyncFunctionDef, ast.Lambda, ast.ClassDef)
    stack = list(getattr(node, "body", []))
    while stack:
        current = stack.pop()
        yield current
        # A nested function is yielded -- the enclosing scope does bind its
        # name -- but not entered. Entering it is what let one inner function
        # lend its locals to a sibling.
        if isinstance(current, nested):
            continue
        stack.extend(ast.iter_child_nodes(current))


def Bound(node: ast.AST) -> Set[str]:
    """Names this function binds: its parameters and its own assignments.

    Parameters are the half that was missing. `effect` and `message` are the
    commonest things an inner function captures in this corpus, and leaving
    them out meant the most ordinary closure in the tree went unflagged.
    """
    names: Set[str] = set()
    args = getattr(node, "args", None)
    if args is not None:
        for group in (args.posonlyargs, args.args, args.kwonlyargs):
            for arg in group:
                names.add(arg.arg)
        for extra in (args.vararg, args.kwarg):
            if extra is not None:
                names.add(extra.arg)
    for child in OwnBody(node):
        if isinstance(child, ast.Assign):
            for target in child.targets:
                for sub in ast.walk(target):
                    if isinstance(sub, ast.Name):
                        names.add(sub.id)
        elif isinstance(child, (ast.AugAssign, ast.AnnAssign)):
            if isinstance(child.target, ast.Name):
                names.add(child.target.id)
        elif isinstance(child, ast.For):
            for sub in ast.walk(child.target):
                if isinstance(sub, ast.Name):
                    names.add(sub.id)
        elif isinstance(child, (ast.FunctionDef, ast.ClassDef)):
            names.add(child.name)
    return names


def Free(node: ast.AST) -> Set[str]:
    """Every name this function reads, including inside functions it contains."""
    return {x.id for x in ast.walk(node)
            if isinstance(x, ast.Name) and isinstance(x.ctx, ast.Load)}


def LocalDefs(node: ast.AST) -> Set[str]:
    """Functions defined directly in this scope, by name."""
    return {child.name for child in getattr(node, "body", [])
            if isinstance(child, ast.FunctionDef)}


################################################################################
#


def CardFiles(folder: str=CARD_FOLDER) -> List[str]:
    out = []
    for root, _dirs, files in os.walk(folder):
        for name in sorted(files):
            if name.endswith(".py") and name != "__init__.py":
                out.append(os.path.join(root, name))
    return sorted(out)


def ScanFile(path: str) -> Set[str]:
    with open(path, encoding="utf-8") as handle:
        source = handle.read()
    return ScanSource(source)


def ScanSource(source: str) -> Set[str]:
    return Scan().Module(ast.parse(source))


def EnvelopePredicates(folder: str=CARD_FOLDER) -> Tuple[int, int, int]:
    """Lambdas in the envelope, and how many are more than a named predicate.

    The scanner does not walk the envelope, which means `conditions=[lambda ...]`
    costs a card nothing. That is right for `lambda e, m: Worlds.GetCrisisIcons(e)
    > 0` -- a printed condition with a name -- and wrong for the ones that walk a
    causal chain or fold a comprehension. Those are a second DSL surface, the
    condition language, and folding them into the card verdict would either
    overstate the corpus or hide the surface. So they are counted here and
    reported separately.

    Returns (scripts with an envelope lambda, lambdas, lambdas that are more
    than one call or one comparison).
    """
    scripts = lambdas = complex_ones = 0
    for path in CardFiles(folder):
        if not IsCardScript(path):
            continue
        with open(path, encoding="utf-8") as handle:
            tree = ast.parse(handle.read())
        found = 0
        for get in [x for x in tree.body
                    if isinstance(x, ast.FunctionDef) and x.name == "GetAbilities"]:
            handlers = {id(f) for f in ast.walk(get)
                        if isinstance(f, ast.FunctionDef) and f is not get}
            inside = {id(n) for f in ast.walk(get)
                      if isinstance(f, ast.FunctionDef) and id(f) in handlers
                      for n in ast.walk(f)}
            for node in ast.walk(get):
                if not isinstance(node, ast.Lambda) or id(node) in inside:
                    continue
                found += 1
                body = node.body
                if not isinstance(body, (ast.Call, ast.Compare, ast.Constant,
                                         ast.Name, ast.Attribute)):
                    complex_ones += 1
        if found:
            scripts += 1
            lambdas += found
    return scripts, lambdas, complex_ones


def IsCardScript(path: str) -> bool:
    """A card script is a module with a top-level `def GetAbilities`.

    Three files under `cards/pack/` are not: `endless/endless.py` is empty, and
    the two `campaign.py` modules define their handlers inside module-level
    helpers. Scanning them returns no flags -- which would have counted an empty
    file and two scenario-setup modules among the expressible cards. A census
    that quietly admits files it cannot see into the numerator is the exact
    failure this tool exists to argue against.
    """
    try:
        with open(path, encoding="utf-8") as handle:
            tree = ast.parse(handle.read())
    except (SyntaxError, OSError):
        return False
    return any(isinstance(node, ast.FunctionDef) and node.name == "GetAbilities"
               for node in tree.body)


def NotCardScripts(folder: str=CARD_FOLDER) -> List[str]:
    return [path for path in CardFiles(folder) if not IsCardScript(path)]


def Census(folder: str=CARD_FOLDER) -> Dict[str, Set[str]]:
    return {path: ScanFile(path) for path in CardFiles(folder)
            if IsCardScript(path)}


################################################################################
#


def Greedy(census: Dict[str, Set[str]]) -> List[Tuple[str, int, int]]:
    """Add nodes one at a time, most cards unblocked first.

    Answers the question the design actually has to settle: not "which blocker
    is most common" but "if the DSL grows by one node, which one buys the most,
    and where does the curve flatten". The point where it flattens is where the
    escape hatch belongs.

    Returns (node, cards this node alone clears, cards clear so far).

    **It walks nodes, not blockers.** `register` and `factory-in-handler` flag
    the identical 63 scripts, because `.Registers(AbilityFactory.WhenX(...))`
    trips both; both are retired by `grantUntil`. Scoring one blocker at a time
    made each score zero on its own, so the walk dropped `grantUntil` entirely
    and stopped 1.6 points short while the document claimed the node retired
    those cards. Grouping by node is what makes the curve mean what the header
    says.

    **The curve is a ceiling, not a forecast.** Naming a node against a blocker
    is a claim that one node covers the whole class, and this file cannot check
    that claim -- `observe` is asserted to cover every closure card on the
    strength of having been designed against four of them. Read the shape, not
    the endpoint.
    """
    nodes: Dict[str, Set[str]] = collections.defaultdict(set)
    for blocker, node in RETIRED_BY.items():
        if node:
            nodes[node].add(blocker)

    dirty = {path: set(flags) for path, flags in census.items() if flags}
    retired: Set[str] = set()
    order: List[Tuple[str, int, int]] = []
    cleared = 0
    while True:
        best, total = None, cleared
        for node, group in nodes.items():
            if group <= retired:
                continue
            freed = sum(1 for flags in dirty.values()
                        if not flags - retired - group)
            if freed > total:
                best, total = node, freed
        if best is None:
            break
        retired |= nodes[best]
        order.append((best, total - cleared, total))
        cleared = total
    return order


def Report(census: Dict[str, Set[str]], out=sys.stdout,
           skipped: Sequence[str]=()) -> None:
    total = len(census)
    clean = [p for p, flags in census.items() if not flags]
    print(f"card scripts: {total}", file=out)
    print(f"expressible with no new node: {len(clean)} "
          f"({100.0*len(clean)/total:.1f}%)", file=out)
    for path in skipped:
        print(f"  not counted (no GetAbilities): {path}", file=out)
    print(file=out)

    counts = collections.Counter()
    alone = collections.Counter()
    for flags in census.values():
        for flag in flags:
            counts[flag] += 1
        if len(flags) == 1:
            alone[next(iter(flags))] += 1

    print(f"{'blocker':<20} {'cards':>6} {'only':>6}  retired by", file=out)
    print("-" * 74, file=out)
    for flag, n in counts.most_common():
        node = RETIRED_BY.get(flag) or "-- stays compiled --"
        print(f"{flag:<20} {n:>6} {alone[flag]:>6}  {node}", file=out)
    print(file=out)
    print("'only' is the number of cards that blocker blocks by itself -- the "
          "cards\na node for it would unblock on its own.", file=out)

    scripts, lambdas, complex_ones = EnvelopePredicates()
    print(file=out)
    print(f"not counted above: {lambdas} condition lambdas in the envelope of "
          f"{scripts} scripts,\n{complex_ones} of them more than one call or "
          f"comparison. Those are the condition\nlanguage, a second DSL "
          f"surface this tool does not measure.", file=out)


def ReportGreedy(census: Dict[str, Set[str]], out=sys.stdout) -> None:
    total = len(census)
    clean = sum(1 for flags in census.values() if not flags)
    dirty = total - clean
    print(f"start: {clean} of {total} ({100.0*clean/total:.1f}%) carry no "
          f"blocker at all", file=out)
    print(file=out)
    print(f"{'+ node':<28} {'clears':>7} {'of ' + str(dirty):>8}   all cards",
          file=out)
    print("-" * 62, file=out)
    order = Greedy(census)
    for node, gain, cleared in order:
        print(f"{node:<28} {gain:>7} {cleared:>8}   "
              f"{100.0*(clean+cleared)/total:5.1f}%", file=out)

    taken: Set[str] = set()
    for node, _gain, _cleared in order:
        taken |= {b for b, n in RETIRED_BY.items() if n == node}
    remaining = [p for p, flags in census.items() if flags - taken]
    print(file=out)
    if remaining:
        print(f"{len(remaining)} cards are left, blocked by:", file=out)
        left = collections.Counter()
        for path in remaining:
            for flag in census[path] - taken:
                left[flag] += 1
        for flag, n in left.most_common():
            node = RETIRED_BY.get(flag) or "-- stays compiled --"
            print(f"  {flag:<20} {n:>4} cards   {node}", file=out)
        for path in sorted(remaining)[:10]:
            print(f"  {path}  [{', '.join(sorted(census[path] - taken))}]",
                  file=out)
    print(file=out)
    print("Every row after the first two is a claim that one node covers a "
          "whole\nconstruct class. None of those claims is checked here. The "
          "shape of the\ncurve is the finding; the endpoint is a ceiling.",
          file=out)


def Main(argv: Sequence[str]|None=None) -> int:
    parser = argparse.ArgumentParser(
        description="Which constructs stop a card script from being data")
    parser.add_argument("--folder", default=CARD_FOLDER)
    parser.add_argument("--greedy", action="store_true",
                        help="what each added node buys, best first")
    parser.add_argument("--blocker", help="list every card one blocker blocks")
    parser.add_argument("--card", help="one card, by id or path fragment")
    parser.add_argument("--out", help="write the whole table as JSON")
    args = parser.parse_args(argv)

    if not os.path.isdir(args.folder):
        print(f"{args.folder} not found -- run from py_src/", file=sys.stderr)
        return 2

    census = Census(args.folder)

    if args.card:
        hits = [p for p in census if args.card in p]
        if not hits:
            print(f"no card matching {args.card!r}", file=sys.stderr)
            return 1
        for path in sorted(hits):
            flags = sorted(census[path])
            print(f"{path}")
            if not flags:
                print("  expressible with no new node")
            for flag in flags:
                node = RETIRED_BY.get(flag) or "-- stays compiled --"
                print(f"  {flag:<20} {BLOCKERS[flag]:<50} {node}")
        return 0

    if args.blocker:
        hits = sorted(p for p, flags in census.items() if args.blocker in flags)
        print(f"{args.blocker}: {BLOCKERS.get(args.blocker, '?')}")
        print(f"{len(hits)} cards")
        for path in hits:
            print(f"  {path}")
        return 0

    if args.out:
        with open(args.out, "w", encoding="utf-8") as handle:
            json.dump({p: sorted(f) for p, f in census.items()}, handle,
                      indent=2, sort_keys=True)
        print(f"wrote {args.out}")

    Report(census, skipped=NotCardScripts(args.folder))
    if args.greedy:
        print()
        ReportGreedy(census)
    return 0


if __name__ == "__main__":
    sys.exit(Main())
