"""Which `game/operate/` helpers definitely stop and ask the player something.

`scripts.PlayerChoiceApi` sees only the prompts a card script names itself. A
card that reaches one through a helper -- `Search.Collection`,
`Players.DiscardHeroActionAttachment` -- looked like a card that asks nothing,
and the spec campaign shards on exactly that (MARVEL-114).

## Why this is a *must* analysis and not reachability

The obvious fix is a transitive closure: mark every helper from which a prompt is
reachable, then credit any card that calls one. Measured, that marks 43 of the
232 methods in `game/operate/` and moves **498** card scripts into
`interactive`. Nearly all of it is wrong, because the prompts sit behind guards:

    Faces.DiscardAll        prompts only when `simultaneous=True`, and **no**
                            call site in `cards/pack/` passes it -- 244 of 244
                            leave it at the default. 211 flips, all false.
    Worlds.FindMainScheme   prompts only when the board holds more than one
                            main scheme.
    Filter.One              prompts only when a tie has to be broken.

`player_choice_calls` is the cross-language contract the C# port is built
against, so "this card asks" and "this card does not ask" are equally load
bearing and a false positive is as much a lie as a false negative. Over-crediting
would also destroy the one job `--tier interactive` has, which is to be the list
an author walks.

So the rule is an **under-approximation**: a helper is credited only when the
prompt is reached on *every* path through it, given the arguments the call site
actually passes. Anything the analysis cannot decide is not credited. The cards
that genuinely *may* ask -- the board-conditional population -- are deliberately
left out, and `unit_test/test_card_dataset.py` pins the list of guarded prompt
sites so that a helper added later cannot join them unnoticed.

## What it can decide

* **Complementary branches.** `Players.DiscardHeroActionAttachment` prompts in
  the `if may:` arm and again in the `else:` arm, so it always prompts. The
  outcome sets of the two arms are unioned rather than approximated away, which
  is what makes that fall out instead of being missed.
* **Early returns.** `Players.DiscardResourceIconFromHand` reaches a prompt
  under no enclosing `if` at all -- and is still not credited, because two
  guarded `return`s sit above it. A guard analysis that only reads enclosing
  `if` tests calls this one unconditional, which is how it was first measured
  and it was wrong.
* **Keyword forwarding.** `Search.PlayerCard(may=True)` calls
  `Search.SearchForCard(..., may=may)` calls `Search.SearchForCards(...)` calls
  `SearchInternal.SearchForCardsInternal(...)`, where `may` being true excludes
  the one branch that does not prompt. Literals and bound parameter names
  propagate along that chain; a rename, an expression, or `**kwargs` does not,
  and stops the propagation rather than guessing.

## Why the scope is `game/operate/`

That layer is namespaced by class, so `Faces.DiscardAll` in a card script
resolves to exactly one definition with no import tracking. Nothing else a card
script calls by qualified name reaches a prompt --
`unit_test/test_card_dataset.py` checks that rather than assuming it, because
the whole defect was a helper layer nobody was looking at.
"""

from __future__ import annotations

import ast
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Sequence, Set, Tuple

OPERATE_ROOT = Path("game/operate")

# Where a path can end up. `PROMPT` is absorbing: once a path has prompted,
# what it does afterwards cannot un-ask the question, so it is never followed
# further.
PROMPT, FALL, RETURN, BREAK, CONTINUE = (
    "prompt", "fall", "return", "break", "continue")

# A prompt written inside one of these is *declared*, not performed: the engine
# calls it back later, if at all. Crediting it would be reachability again.
_DEFERRED = (ast.Lambda, ast.FunctionDef, ast.AsyncFunctionDef, ast.ClassDef)

# Short-circuit and conditional expressions evaluate their operands
# conditionally, so a prompt inside one is not on every path.
_CONDITIONAL_EXPR = (ast.IfExp, ast.BoolOp)

# Values worth propagating through a call chain. Everything else -- a list, a
# call, an attribute, an f-string -- is unknown, which is not the same as false
# and is treated as undecidable rather than guessed.
_PROPAGATED = (bool, str, int, type(None))

# Depth cap for mutual recursion between helpers. The operate layer nests about
# four deep; this is a backstop, not a tuning parameter.
_MAX_DEPTH = 24

MethodKey = Tuple[str, str]
Env = Dict[str, Any]


class _Unknown:
    """Not a value. Distinct from `None`, which is a value a parameter can take."""

    def __repr__(self) -> str:  # pragma: no cover - debugging aid
        return "<unknown>"


UNKNOWN = _Unknown()


################################################################################
# Reading the helper layer


def _IndexMethods(root: Path) -> Dict[MethodKey, ast.FunctionDef]:
    """`(Class, Method) -> its definition`, over every module in `game/operate/`."""
    directory = root / OPERATE_ROOT
    if not directory.is_dir():
        raise FileNotFoundError(
            f"{directory} is missing. The helper layer moved; the indirect "
            f"player-choice rule in {__name__} has to move with it."
        )
    methods: Dict[MethodKey, ast.FunctionDef] = {}
    for path in sorted(directory.glob("*.py")):
        if path.name == "__init__.py":
            continue
        tree = ast.parse(path.read_text(encoding="utf-8"), filename=str(path))
        for node in ast.walk(tree):
            if not isinstance(node, ast.ClassDef):
                continue
            for member in node.body:
                if isinstance(member, (ast.FunctionDef, ast.AsyncFunctionDef)):
                    methods[(node.name, member.name)] = member
    return methods


################################################################################
# Literals, bindings and three-valued guards


def _Literal(node: ast.AST) -> Tuple[bool, Any]:
    if isinstance(node, ast.Constant) and isinstance(node.value, _PROPAGATED):
        return True, node.value
    return False, None


def _ValueOf(node: ast.AST, env: Env) -> Any:
    """The literal this expression denotes under `env`, or `UNKNOWN`."""
    known, value = _Literal(node)
    if known:
        return value
    if isinstance(node, ast.Name) and node.id in env:
        return env[node.id]
    return UNKNOWN


def Bind(function: ast.FunctionDef, call: ast.Call, env: Env) -> Env:
    """The callee's parameter bindings for one call site, evaluated in `env`.

    Defaults first, then whatever the call site pins down. An argument the
    analysis cannot read *removes* the binding rather than leaving the default
    in place -- a parameter passed an unknown expression is unknown, and
    silently keeping its default is how a guard gets evaluated against a value
    the caller never passed.
    """
    args = function.args
    positional = [a.arg for a in args.posonlyargs] + [a.arg for a in args.args]
    bound: Env = {}

    if args.defaults:
        for name, default in zip(positional[len(positional) - len(args.defaults):],
                                 args.defaults):
            known, value = _Literal(default)
            if known:
                bound[name] = value
    for keyword_only, default in zip(args.kwonlyargs, args.kw_defaults):
        if default is None:
            continue
        known, value = _Literal(default)
        if known:
            bound[keyword_only.arg] = value

    for index, argument in enumerate(call.args):
        if isinstance(argument, ast.Starred) or index >= len(positional):
            continue
        value = _ValueOf(argument, env)
        if value is UNKNOWN:
            bound.pop(positional[index], None)
        else:
            bound[positional[index]] = value

    accepted = set(positional) | {a.arg for a in args.kwonlyargs}
    for keyword in call.keywords:
        if keyword.arg is None or keyword.arg not in accepted:
            continue
        value = _ValueOf(keyword.value, env)
        if value is UNKNOWN:
            bound.pop(keyword.arg, None)
        else:
            bound[keyword.arg] = value
    return bound


def Truth(node: ast.AST, env: Env) -> Optional[bool]:
    """`True`, `False`, or `None` for "the analysis cannot say".

    Three-valued on purpose. `None` is what stops a board-state guard --
    `if len(schemes) == 1`, `if not original_faces` -- from being read as
    either answer.
    """
    if isinstance(node, ast.Constant):
        return bool(node.value)
    if isinstance(node, ast.Name):
        return bool(env[node.id]) if node.id in env else None
    if isinstance(node, ast.UnaryOp) and isinstance(node.op, ast.Not):
        inner = Truth(node.operand, env)
        return None if inner is None else (not inner)
    if isinstance(node, ast.BoolOp):
        values = [Truth(value, env) for value in node.values]
        if isinstance(node.op, ast.And):
            if any(value is False for value in values):
                return False
            return True if all(value is True for value in values) else None
        if any(value is True for value in values):
            return True
        return False if all(value is False for value in values) else None
    if isinstance(node, ast.Compare) and len(node.ops) == 1:
        operator = node.ops[0]
        left = _ValueOf(node.left, env)
        right = _ValueOf(node.comparators[0], env)
        if left is UNKNOWN or right is UNKNOWN:
            return None
        if isinstance(operator, ast.Eq):
            return left == right
        if isinstance(operator, ast.NotEq):
            return left != right
        if isinstance(operator, ast.Is):
            return left is right
        if isinstance(operator, ast.IsNot):
            return left is not right
        return None
    return None


################################################################################
# The analysis


class HelperPrompts:
    """Does calling `Class.Method(...)` with these arguments always ask?

    One instance reads the helper layer once and answers per call site. Results
    are memoised on `(method, bindings)`, which is what keeps a whole pack scan
    cheap -- 3,457 card scripts resolve to a few dozen distinct bindings.
    """

    def __init__(self, root: Path, prompt_api: Iterable[str]) -> None:
        self.methods = _IndexMethods(root)
        self.prompt_api: Set[str] = set(prompt_api)
        self.classes: Set[str] = {name for name, _ in self.methods}
        self._answers: Dict[Tuple[MethodKey, Tuple[Tuple[str, Any], ...]], bool] = {}

    # -- public ------------------------------------------------------------

    def AlwaysPrompts(self, key: MethodKey, env: Env) -> bool:
        return self._Method(key, env, ())

    def DefaultEnv(self, key: MethodKey) -> Env:
        """The bindings a call passing nothing but positional objects would give."""
        return Bind(self.methods[key], ast.Call(func=ast.Name(id="f"), args=[],
                                                keywords=[]), {})

    def CallSites(self, tree: ast.AST) -> List[Tuple[MethodKey, ast.Call]]:
        """Every `Class.Method(...)` in this tree that names a helper we index."""
        sites: List[Tuple[MethodKey, ast.Call]] = []
        for node in ast.walk(tree):
            if not isinstance(node, ast.Call):
                continue
            func = node.func
            if not (isinstance(func, ast.Attribute)
                    and isinstance(func.value, ast.Name)):
                continue
            key = (func.value.id, func.attr)
            if key in self.methods:
                sites.append((key, node))
        return sites

    def Credited(self, tree: ast.AST) -> List[str]:
        """The helpers this script calls that are certain to prompt.

        Qualified names, sorted, deduplicated. A script calling the same helper
        twice with different arguments is credited if *either* call always asks.
        """
        credited = {f"{key[0]}.{key[1]}"
                    for key, call in self.CallSites(tree)
                    if self.AlwaysPrompts(key, Bind(self.methods[key], call, {}))}
        return sorted(credited)

    def PromptSites(self) -> Dict[MethodKey, List[str]]:
        """Helper methods whose own body performs a prompt, and which prompts.

        The syntactic fact underneath the analysis, exposed because the guard in
        `unit_test/test_card_dataset.py` needs an oracle the analysis did not
        produce. Deferred bodies are excluded here for the same reason they are
        excluded everywhere else: a prompt inside a callback is not this
        method's prompt.
        """
        found: Dict[MethodKey, List[str]] = {}
        for key, function in self.methods.items():
            names = sorted(self._DirectPrompts(function))
            if names:
                found[key] = names
        return found

    def _DirectPrompts(self, function: ast.FunctionDef) -> Set[str]:
        names: Set[str] = set()

        def Walk(node: ast.AST) -> None:
            for child in ast.iter_child_nodes(node):
                if isinstance(child, _DEFERRED):
                    continue
                if isinstance(child, ast.Call):
                    called = child.func
                    name = (called.attr if isinstance(called, ast.Attribute)
                            else called.id if isinstance(called, ast.Name) else "")
                    if name in self.prompt_api:
                        names.add(name)
                Walk(child)

        for statement in function.body:
            # The test has to be on the statement itself as well as on its
            # children: a handler defined at the top of a method body is a child
            # of nothing, and walking into it would count the engine's later
            # callback as this method's own prompt.
            if isinstance(statement, _DEFERRED):
                continue
            Walk(statement)
        return names

    # -- the fixpoint ------------------------------------------------------

    def _Method(self, key: MethodKey, env: Env,
                stack: Tuple[MethodKey, ...]) -> bool:
        if key in stack or len(stack) >= _MAX_DEPTH:
            return False
        memo = (key, tuple(sorted(env.items(), key=lambda item: item[0])))
        cached = self._answers.get(memo)
        if cached is not None:
            return cached
        # Seeded false so a cycle reached through this method answers "cannot
        # say" rather than recursing; the real answer overwrites it below.
        self._answers[memo] = False
        answer = self._Block(self.methods[key].body, env, stack + (key,)) == {PROMPT}
        self._answers[memo] = answer
        return answer

    def _Block(self, body: Sequence[ast.stmt], env: Env,
               stack: Tuple[MethodKey, ...]) -> Set[str]:
        """Where the paths through a statement list end up.

        `{PROMPT}` alone means every one of them asked. Statements are followed
        only along the `FALL` edge, so a guarded `return` above a prompt keeps
        `RETURN` in the set and the block is not credited.
        """
        outcomes: Set[str] = set()
        reaches_end = True
        for statement in body:
            reached = self._Statement(statement, env, stack)
            outcomes |= (reached - {FALL})
            if FALL not in reached:
                reaches_end = False
                break
        if reaches_end:
            outcomes.add(FALL)
        return outcomes

    def _Loop(self, node: ast.AST, body: Set[str], env: Env,
              stack: Tuple[MethodKey, ...], always_enters: bool) -> Set[str]:
        outcomes: Set[str] = set()
        if PROMPT in body:
            outcomes.add(PROMPT)
        if RETURN in body:
            outcomes.add(RETURN)
        if BREAK in body:
            # A `break` leaves the loop, not the function -- the mistake worth
            # naming, because reading it as an exit hides the prompt below every
            # search loop in `SearchInternal`.
            outcomes.add(FALL)
        orelse = getattr(node, "orelse", None)
        if orelse:
            outcomes |= self._Block(orelse, env, stack)
        if not always_enters:
            outcomes.add(FALL)
        if not outcomes:
            # `while True:` with no break and no return: control never leaves,
            # so no path through it prompts and none falls out either.
            outcomes.add(RETURN)
        return outcomes

    def _Statement(self, node: ast.stmt, env: Env,
                   stack: Tuple[MethodKey, ...]) -> Set[str]:
        if isinstance(node, _DEFERRED):
            return {FALL}
        if isinstance(node, ast.Return):
            return {PROMPT} if self._Expression(node.value, env, stack) else {RETURN}
        if isinstance(node, ast.Raise):
            return {RETURN}
        if isinstance(node, ast.Break):
            return {BREAK}
        if isinstance(node, ast.Continue):
            return {CONTINUE}
        if isinstance(node, ast.If):
            if self._Expression(node.test, env, stack):
                return {PROMPT}
            decided = Truth(node.test, env)
            if decided is True:
                return self._Block(node.body, env, stack)
            if decided is False:
                return self._Block(node.orelse, env, stack) if node.orelse else {FALL}
            taken = self._Block(node.body, env, stack)
            other = self._Block(node.orelse, env, stack) if node.orelse else {FALL}
            return taken | other
        if isinstance(node, ast.While):
            if self._Expression(node.test, env, stack):
                return {PROMPT}
            return self._Loop(node, self._Block(node.body, env, stack), env, stack,
                              Truth(node.test, env) is True)
        if isinstance(node, (ast.For, ast.AsyncFor)):
            if self._Expression(node.iter, env, stack):
                return {PROMPT}
            return self._Loop(node, self._Block(node.body, env, stack), env, stack,
                              False)
        if isinstance(node, (ast.With, ast.AsyncWith)):
            for item in node.items:
                if self._Expression(item.context_expr, env, stack):
                    return {PROMPT}
            return self._Block(node.body, env, stack)
        if isinstance(node, (ast.Try, ast.Match, getattr(ast, "TryStar", ast.Try))):
            # An exception can leave from anywhere inside, so nothing in here is
            # on every path. Undecidable, which means not credited.
            return {FALL, RETURN}
        for child in ast.iter_child_nodes(node):
            if isinstance(child, _DEFERRED + _CONDITIONAL_EXPR):
                continue
            if self._Expression(child, env, stack):
                return {PROMPT}
        return {FALL}

    def _Expression(self, node: Optional[ast.AST], env: Env,
                    stack: Tuple[MethodKey, ...]) -> bool:
        """Does evaluating this expression certainly reach a prompt?"""
        if node is None:
            return False
        for child in ast.iter_child_nodes(node):
            if isinstance(child, _DEFERRED + _CONDITIONAL_EXPR):
                continue
            if self._Expression(child, env, stack):
                return True
        if isinstance(node, ast.Call):
            func = node.func
            if isinstance(func, ast.Attribute):
                if (isinstance(func.value, ast.Name)
                        and (func.value.id, func.attr) in self.methods):
                    key = (func.value.id, func.attr)
                    return self._Method(key, Bind(self.methods[key], node, env),
                                        stack)
                return func.attr in self.prompt_api
            if isinstance(func, ast.Name):
                return func.id in self.prompt_api
        return False
