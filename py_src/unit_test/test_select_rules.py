"""Every declared select rule is enforced somewhere, and we know where.

`SelectorRule` declares its rules as three `Literal` unions. A card script names
one, `Effect.GetDescriptor` renders it, and the web client's `check_select_rule`
(`public/js/marvel/effect.ts`) enforces it by disabling the OK button. Whether
*Python* enforces it is a separate question, and until MARVEL-118 nobody had
asked it: `"DifferentCards"` was declared, passed by `01042` and by every
`TeamUp` target, rendered into the descriptor, and implemented by nothing but
the browser. The bot, the spec harness, the corpus generator and the C# port
could all choose a pair a human clicking in the browser could not.

A rule declared-but-unimplemented looks exactly like a rule that works, which is
why it survived. So the population is derived from the source and compared, for
equality, against a table somebody had to write a sentence in:

  * `DECLARED` comes from `typing.get_args` on the `Literal`s themselves, so a
    new rule appears the moment it is declared.
  * `BranchedOnRules` finds the rules the engine actually *dispatches on* --
    a comparison against a `select_rule` expression, a `startswith` prefix test
    on one, or a truth test on the rule's own dedicated attribute. It looks for
    a branch, not for the name: `"DifferentCards"` appeared in `game/` twice
    before this issue (declared in `RULE_BASE`, passed by `selector_target.py`)
    and a scan for the literal would have called it implemented.
  * `REVIEWED_SELECT_RULES` says, per rule, which side enforces it and why.

MARVEL-127 is why the scan reports a *site* rather than a bare name. "The
engine branches on this rule somewhere" was the whole claim, and two rules
passed it while enforcing half of what they say:

  * **Feasibility** asks whether the *pool* could satisfy the rule, so an
    impossible effect is never offered. `EffectChecker.UpdateLegalTargets` and
    `SelectorRule.Process` are the two sites that do it.
  * **Selection** asks whether the *chosen set* does. `AfterSelectTargets` is
    the only site that does it, and it is the one that decides whether a rule
    binds a player who is not clicking in the browser.

`MustIncludeTraits` and `DifferentType` had the first and not the second, which
is indistinguishable from a working rule if all you ask is "is it named in a
branch". So the table names the sites, the scan finds them, and the comparison
is per site -- a rule that gains a feasibility check and no selection check is
now `FEASIBILITY_ONLY` in writing, or the test fails.

Shaped after `REVIEWED_ABSORBERS` in `test_integrity_errors.py` and
`REVIEWED_GUARDED_PROMPTS` in `test_card_dataset.py`, and for their reason: the
comparison is an equality, so it fails in both directions. Declaring a rule
without an entry fails. Claiming Python enforces one it does not fails. Claiming
a rule is deliberately UI-only fails unless the browser really does enforce it
-- the `effect.ts` side is checked too, so "UI-only" is a claim about code that
exists rather than a shrug.

What it does *not* do is pin today's list as a snapshot. Nothing here names a
count or a card id, and the tests fail on the addition of a rule rather than on
a change to any card.
"""

import ast
import re
import unittest
from pathlib import Path
from typing import get_args
from unittest.mock import patch

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from game.selector.selector_rule import SelectorRule
from game.card.face.base import Villain
from game.card.face.card_type import Minion
from game.effect.effect_failure import EffectFailure
from game.operate.faces_counter import FacesCounter
from game.selector.factory import Select

PY_SRC = Path(__file__).resolve().parent.parent
GAME = PY_SRC / "game"
CLIENT = PY_SRC / "public" / "js" / "marvel" / "effect.ts"

# Attribute names that hold the rule under test. `SelectorRule.select_rule` is
# the resolved one and `raw_select_rule` is what the card script passed;
# `SelectorRange` copies the resolved one under the same name.
RULE_ATTRS = frozenset({"select_rule", "raw_select_rule"})

# The two enforcement sites, keyed by the *outermost* enclosing function -- the
# outermost rather than the innermost so that renaming a nested helper does not
# move a site. `UpdateLegalTargets` holds its branches in a nested
# `get_all_legal_targets`, and that name is deliberately not written here.
FEASIBILITY = "feasibility"
SELECTION = "selection"

SITE_FUNCTIONS = {
    "UpdateLegalTargets": FEASIBILITY,   # EffectChecker: is the pool capable?
    "Process": FEASIBILITY,              # SelectorRule: drop an incapable pool
    "AfterSelectTargets": SELECTION,     # SelectorRule: is the choice legal?
}


def Declared():
    """Every rule name the engine declares, from the `Literal`s themselves."""
    names = set()
    for literal in (SelectorRule.RULE_BASE,
                    SelectorRule.RULE_WITH_PARAM,
                    SelectorRule.RULE_FOR_UI):
        names.update(get_args(literal))
    return names


DECLARED = Declared()

# The attribute-truth-test shape (`if self.combined_resource_cost:`) is only
# evidence for `RULE_WITH_PARAM`, because only there does `SelectorRule` derive
# the rule name *from* the attribute. Applied to the other groups it reads any
# same-named attribute anywhere in `game/` as enforcement, and there is one:
# `CostRule.different_type` is "spend resources of different types", a resource
# rule with no relationship to the `DifferentType` *select* rule, and
# `cost.py:149` and `resources.py:117` both branch on it. That credited the
# select rule with two branches it does not have. Harmless while the scan only
# asked *whether* a rule was branched on -- `DifferentType` was independently
# found in `EffectChecker` -- and not harmless once it reports *where*.
DECLARED_WITH_PARAM = frozenset(get_args(SelectorRule.RULE_WITH_PARAM))


def SnakeCase(name):
    """`CombinedResourceCost` -> `combined_resource_cost`."""
    return re.sub(r"(?<!^)(?=[A-Z])", "_", name).lower()


class RuleBranchScan(ast.NodeVisitor):
    """Rule names, and rule-name prefixes, the engine branches on.

    Three shapes, because the three `Literal` groups are dispatched three
    different ways and all three are enforcement:

      * `selector.selector_rule.select_rule == "DifferentType"` -- a comparison
        against a `select_rule` expression. `RULE_BASE` uses this.
      * `self.select_rule.startswith('VillainAndMinions')` -- a prefix test on
        one. `RULE_FOR_UI` uses this, and the prefix covers both members.
      * `if self.combined_resource_cost:` -- a truth test on the rule's own
        attribute. `RULE_WITH_PARAM` uses this, because `SelectorRule.__init__`
        derives the rule name *from* that attribute, so the attribute is the
        rule.

    A rule name that merely appears -- declared, passed as an argument, written
    in a comment -- is not a branch and is deliberately not collected.

    Each is recorded against the enforcement *site* it was found in -- see
    `SITE_FUNCTIONS` and the module docstring. A branch outside both sites is
    recorded under `None`, which no table entry can claim, so a rule enforced
    somewhere unexpected fails the comparison rather than passing as either.
    """

    def __init__(self):
        self.names = {}
        self.prefixes = {}
        self.attributes = {}
        self.functions = []

    def Site(self):
        """The outermost enclosing function that is an enforcement site."""
        for name in self.functions:
            if name in SITE_FUNCTIONS:
                return SITE_FUNCTIONS[name]
        return None

    def Record(self, table, value):
        table.setdefault(self.Site(), set()).add(value)

    def visit_FunctionDef(self, node):
        self.functions.append(node.name)
        self.generic_visit(node)
        self.functions.pop()

    visit_AsyncFunctionDef = visit_FunctionDef

    @staticmethod
    def IsRuleExpression(node):
        return isinstance(node, ast.Attribute) and node.attr in RULE_ATTRS

    @staticmethod
    def StringOf(node):
        if isinstance(node, ast.Constant) and isinstance(node.value, str):
            return node.value
        return None

    def visit_Compare(self, node):
        operands = [node.left] + list(node.comparators)
        if any(self.IsRuleExpression(x) for x in operands):
            for operand in operands:
                text = self.StringOf(operand)
                if text is not None:
                    self.Record(self.names, text)
        self.generic_visit(node)

    def visit_Call(self, node):
        func = node.func
        if isinstance(func, ast.Attribute) and func.attr == "startswith" and \
                self.IsRuleExpression(func.value):
            for argument in node.args:
                text = self.StringOf(argument)
                if text:
                    self.Record(self.prefixes, text)
        self.generic_visit(node)

    def visit_If(self, node):
        for attribute in TestedAttributes(node.test):
            self.Record(self.attributes, attribute)
        self.generic_visit(node)


def TestedAttributes(test):
    """`self.x` attribute names used as a truth test in `test`.

    Walks `and`/`or`/`not` so `if self.a or self.b:` counts as both. A bare
    attribute is a truth test; one inside a comparison or a call is not, and is
    left to the other two shapes.
    """
    if isinstance(test, ast.Attribute):
        return {test.attr}
    if isinstance(test, ast.BoolOp):
        found = set()
        for value in test.values:
            found |= TestedAttributes(value)
        return found
    if isinstance(test, ast.UnaryOp) and isinstance(test.op, ast.Not):
        return TestedAttributes(test.operand)
    return set()


def BranchedOnRules(root=GAME):
    """Declared rules the engine dispatches on, and where.

    Returns `{rule: frozenset(sites)}` for every declared rule branched on
    anywhere under `root`. A rule with no branch at all is absent, so the
    mapping's keys are still "the rules Python enforces" and its values are the
    MARVEL-127 half: *which* question each branch answers.
    """
    scan = RuleBranchScan()
    for path in sorted(Path(root).rglob("*.py")):
        scan.visit(ast.parse(path.read_text(encoding="utf-8"), filename=str(path)))

    found = {}
    for rule in DECLARED:
        if not rule:
            # The empty rule is "no rule". There is nothing to branch on and
            # nothing to enforce; it is classified below rather than scanned.
            continue
        sites = set()
        for site, names in scan.names.items():
            if rule in names:
                sites.add(site)
        for site, prefixes in scan.prefixes.items():
            if any(rule.startswith(prefix) for prefix in prefixes):
                sites.add(site)
        if rule in DECLARED_WITH_PARAM:
            for site, attributes in scan.attributes.items():
                if SnakeCase(rule) in attributes:
                    sites.add(site)
        if sites:
            found[rule] = frozenset(sites)
    return found


# --------------------------------------------------------------------------
# The reviewed table (MARVEL-118)
# --------------------------------------------------------------------------

# `ENFORCED` -- the engine validates the *chosen set*, so the rule binds every
# player and not only one clicking in the browser. `FEASIBILITY_ONLY` -- the
# engine branches on the rule but only ever asks whether the pool could satisfy
# it, which is what MARVEL-127 found two of; the browser is then the sole
# enforcer of the actual selection, exactly as in the MARVEL-118 bug.
# `UI_ONLY` -- the engine deliberately does not enforce it, the browser does,
# and `effect.ts` must contain the name. `NO_RULE` -- reserved for the empty
# string, and asserted to hold nothing else, so "there is no rule here" cannot
# be used to wave a real one through.
#
# The state is *derived* from the sites rather than believed: a rule claiming
# `ENFORCED` with no `SELECTION` site fails, and so does one claiming
# `FEASIBILITY_ONLY` that has grown a selection check. Nothing is
# `FEASIBILITY_ONLY` today, and that is a result rather than a design -- both
# members were fixed by MARVEL-127 and the state stays so the next one has a
# name to be written down under.
ENFORCED = "enforced"
FEASIBILITY_ONLY = "feasibility-only"
UI_ONLY = "ui-only"
NO_RULE = "no-rule"

REVIEWED_SELECT_RULES = {
    "": (NO_RULE, frozenset(),
         "the absence of a rule. Every selector that names no rule carries "
         "this, and there is nothing to enforce"),

    # RULE_BASE
    "DifferentCards": (ENFORCED, frozenset({FEASIBILITY, SELECTION}),
        "`SelectorRule.AfterSelectTargets` rejects a selection holding two "
        "faces with one title, via `FacesCounter.GetDifferentCardsCount`, and "
        "`EffectChecker.UpdateLegalTargets` refuses to offer an effect whose "
        "minimum cannot be met by distinct titles. MARVEL-118 -- before that "
        "this rule was enforced only by `effect.ts`"),
    "DifferentType": (ENFORCED, frozenset({FEASIBILITY, SELECTION}),
        "`UpdateLegalTargets` refuses the effect when the legal pool holds "
        "fewer distinct card types than the minimum, and since MARVEL-127 "
        "`AfterSelectTargets` refuses a chosen set with a repeated type, via "
        "`FacesCounter.GetDifferentTypesCount`. Until then only the pool was "
        "checked and its single user (`45017`) hand-rolled the pairing in a "
        "`check_again_fn` -- so the rule looked enforced and enforced nothing"),
    "MustIncludeTraits": (ENFORCED, frozenset({FEASIBILITY, SELECTION}),
        "`UpdateLegalTargets` refuses the effect when the legal pool cannot "
        "cover the required traits, and since MARVEL-127 `AfterSelectTargets` "
        "strikes each chosen card's traits off the required list and refuses a "
        "selection that leaves any standing. That second half is the live one: "
        "`SelectorFactory.Alliance` builds every Alliance card's cost from a "
        "`CardFinder(traits=...)` whose traits are an OR, so two AVENGERs "
        "satisfied a pool that had to cover AVENGER *and* GUARDIAN"),

    # RULE_WITH_PARAM -- never passed as `select_rule=`; `SelectorRule.__init__`
    # derives the name from the matching `combined_*` argument, so the argument
    # is the rule and the branch on it is the enforcement.
    "CombinedResourceCost": (ENFORCED, frozenset({FEASIBILITY, SELECTION}),
        "`SelectorRule.Process` drops a pool that cannot reach the minimum and "
        "`AfterSelectTargets` bounds the chosen set both ways, on "
        "`FacesCounter.GetPrintedCost`. This remains a distinct descriptor "
        "name because shipped card scripts emit it and clients consume it"),
    "CombinedPrintedCost": (ENFORCED, frozenset({FEASIBILITY, SELECTION}),
        "same two sites and the same `GetPrintedCost` call. MARVEL-128 kept "
        "both names as compatibility aliases: both have shipped callers, are "
        "rendered to clients and saved inputs, and `effect.ts` accepts both"),
    "CombinedResourceIcons": (ENFORCED, frozenset({FEASIBILITY, SELECTION}),
        "`Process` and `AfterSelectTargets`, on "
        "`FacesCounter.GetPrintedResourcesIcon`. `effect.ts` returns true for "
        "this one unconditionally, so here the browser is the lenient side. "
        "`Players.DiscardResourceIconFromHand` supplies it for three shipped "
        "cards; MARVEL-128 fixed its upper-bound check to read index 1"),

    # RULE_FOR_UI
    "VillainAndMinionsEngagedSamePlayer": (ENFORCED, frozenset({SELECTION}),
        "`AfterSelectTargets` takes this branch through "
        "`startswith('VillainAndMinions')`, requires exactly one villain, and "
        "requires every minion engaged with the selected player. Selection "
        "only, and correctly so: the player group is chosen from the pool"),
    "VillainAndMinionsEngagedWithYou": (ENFORCED, frozenset({SELECTION}),
        "the same `startswith` branch. Since MARVEL-128 it matches the browser "
        "and the four printed cards: exactly one Villain (including a Leader) "
        "and only minions engaged with `Select.GetYou(effect)`. The target "
        "helper already builds that pool, and this selection check also binds "
        "non-browser inputs"),
}


class TestEverySelectRuleIsAccountedFor(unittest.TestCase):
    """The guard.

    Fails on the *addition of a rule*, which is the event that has to be
    caught -- a rule nobody implemented looks exactly like one that works, and
    the browser will happily enforce it for the one player who is a browser.
    """

    def test_every_declared_rule_has_been_reviewed(self):
        self.assertEqual(
            set(REVIEWED_SELECT_RULES), DECLARED,
            "the set of declared select rules moved. Every member of "
            "`SelectorRule.RULE_BASE`, `RULE_WITH_PARAM` and `RULE_FOR_UI` "
            "needs an entry in `REVIEWED_SELECT_RULES` saying which side "
            "enforces it and why. See MARVEL-118.")

    def test_rules_claimed_for_python_are_the_rules_python_branches_on(self):
        claimed = {rule: sites for rule, (_, sites, _) in
                   REVIEWED_SELECT_RULES.items() if sites}

        self.assertEqual(
            BranchedOnRules(), claimed,
            "the rules the engine dispatches on -- and where it dispatches on "
            "them -- disagree with the table. A rule claimed with a branch it "
            "does not have is the MARVEL-118 bug again: declared, rendered, "
            "passed by cards, and implemented by nothing but `effect.ts`. A "
            "rule claiming a `SELECTION` site it does not have is MARVEL-127: "
            "the pool is checked, the chosen set is not, and the rule binds "
            "nobody who is not clicking in the browser.")

    def test_the_state_of_a_rule_follows_from_where_it_is_enforced(self):
        """The third state, and the reason it is derived rather than declared.

        `ENFORCED` is a claim about the *selection* being validated. Before
        MARVEL-127 both `DifferentType` and `MustIncludeTraits` were written
        down as enforced in Python, truthfully -- the engine did branch on them
        -- and the word covered a rule that checked the pool and let any
        selection through. Deriving the word from the sites is what stops the
        next one reading the same.
        """
        for rule, (state, sites, _) in sorted(REVIEWED_SELECT_RULES.items()):
            with self.subTest(rule=rule):
                if not rule:
                    expected = NO_RULE
                elif SELECTION in sites:
                    expected = ENFORCED
                elif sites:
                    expected = FEASIBILITY_ONLY
                else:
                    expected = UI_ONLY
                self.assertEqual(
                    state, expected,
                    f"{rule} is written down as {state!r} but its enforcement "
                    f"sites {sorted(sites)} say {expected!r}")

    def test_no_entry_claims_an_unknown_enforcement_site(self):
        # `RuleBranchScan` files a branch found outside both sites under
        # `None`, so an entry could only match it by naming `None` here. The
        # sites are a closed set for the same reason the step catalogue is.
        for rule, (_, sites, _) in sorted(REVIEWED_SELECT_RULES.items()):
            with self.subTest(rule=rule):
                self.assertLessEqual(set(sites), {FEASIBILITY, SELECTION})

    def test_ui_only_rules_are_really_enforced_by_the_client(self):
        # "Deliberately UI-only" has to be a claim about code that exists,
        # or it is indistinguishable from the rule nobody wrote.
        client = CLIENT.read_text(encoding="utf-8")
        for rule, (state, _, reason) in sorted(REVIEWED_SELECT_RULES.items()):
            if state != UI_ONLY:
                continue
            with self.subTest(rule=rule):
                self.assertIn(
                    f'"{rule}"', client,
                    f"{rule} is listed as enforced by the client, but "
                    f"{CLIENT.name} never names it")
                self.assertTrue(reason.strip(),
                                f"{rule} is listed UI-only with no reason")

    def test_a_feasibility_only_rule_is_still_enforced_by_the_client(self):
        # The half-enforced state is not a licence to stop caring: whatever
        # Python declines to check about the selection, the browser is then the
        # only thing checking, and that has to be true rather than assumed.
        client = CLIENT.read_text(encoding="utf-8")
        for rule, (state, _, reason) in sorted(REVIEWED_SELECT_RULES.items()):
            if state != FEASIBILITY_ONLY:
                continue
            with self.subTest(rule=rule):
                self.assertIn(
                    f'"{rule}"', client,
                    f"{rule} checks only the pool in Python and "
                    f"{CLIENT.name} does not check the selection either, so "
                    f"nothing enforces it anywhere")
                self.assertTrue(reason.strip(),
                                f"{rule} is listed feasibility-only with no reason")

    def test_only_the_empty_rule_may_claim_there_is_no_rule(self):
        no_rule = {rule for rule, (state, _, _) in REVIEWED_SELECT_RULES.items()
                   if state == NO_RULE}
        self.assertEqual(no_rule, {""})

    def test_every_entry_states_a_reason(self):
        for rule, (_, _, reason) in sorted(REVIEWED_SELECT_RULES.items()):
            with self.subTest(rule=rule):
                self.assertTrue(reason.strip(), f"{rule} has no stated reason")

    def test_the_scan_finds_something(self):
        # Everything above is vacuously true of a scan that walks no files or
        # matches no shape.
        self.assertTrue(BranchedOnRules())

    def test_the_scan_ignores_a_rule_name_that_is_only_passed_along(self):
        """The shape that hid MARVEL-118 for as long as it hid.

        `selector_target.py` passes `"DifferentCards"` as a value and
        `selector_rule.py` declares it in a `Literal`. Neither is a branch, and
        a scan that counted either would have reported the unimplemented rule
        as implemented.

        The fourth line is the one that keeps the `select_rule` filter honest: a
        comparison against some *other* expression is not a select-rule branch
        either, however suggestive the string beside it looks.
        """
        # A real declared name, because the scan only ever reports declared
        # ones -- a made-up name would make this pass for the wrong reason.
        self.assertEqual({}, self.ScanOf(
            'RULE = Literal["", "DifferentCards"]\n'
            'MAP = {"TeamUp": ((2, 2), "DifferentCards")}\n'
            'def f(x):\n'
            '    if x.card_name == "DifferentCards":\n'
            '        return Select(select_rule="DifferentCards")\n'))

    def test_the_scan_sees_each_of_the_three_enforcement_shapes(self):
        self.assertIn("DifferentType", self.ScanOf(
            'if selector.selector_rule.select_rule == "DifferentType":\n'
            '    pass\n'))
        self.assertIn("VillainAndMinionsEngagedWithYou", self.ScanOf(
            "if not self.select_rule.startswith('VillainAndMinions'):\n"
            '    pass\n'))
        self.assertIn("CombinedResourceIcons", self.ScanOf(
            'if self.combined_resource_icons:\n'
            '    pass\n'))

    def test_an_unrelated_attribute_of_the_same_name_is_not_enforcement(self):
        """`CostRule.different_type`, which is not `DifferentType`.

        "Spend two resources of different types" and "choose two cards of
        different types" are unrelated rules that snake-case to one attribute
        name, and `game/element/cost.py` and `game/element/resources.py` both
        branch on the resource one. The attribute shape is evidence only for
        `RULE_WITH_PARAM`, where the rule name is *derived* from the attribute;
        anywhere else it is a name collision.
        """
        found = self.ScanOf(
            'def Pay(self):\n'
            '    if self.rule.different_type:\n'
            '        pass\n')
        self.assertNotIn("DifferentType", found)

        # ...and the shape still works for the group it was written for.
        found = self.ScanOf(
            'def Process(self):\n'
            '    if self.combined_resource_cost:\n'
            '        pass\n')
        self.assertEqual(found["CombinedResourceCost"], frozenset({FEASIBILITY}))

    def test_a_branch_is_filed_under_the_function_that_holds_it(self):
        """The MARVEL-127 half: *where* a rule is checked is the finding.

        The same comparison in two methods means two different things, and
        before this the scan could not tell them apart.
        """
        feasibility = self.ScanOf(
            'def UpdateLegalTargets(self):\n'
            '    if self.select_rule == "DifferentType":\n'
            '        pass\n')
        self.assertEqual(feasibility["DifferentType"], frozenset({FEASIBILITY}))

        selection = self.ScanOf(
            'def AfterSelectTargets(self):\n'
            '    if self.select_rule == "DifferentType":\n'
            '        pass\n')
        self.assertEqual(selection["DifferentType"], frozenset({SELECTION}))

    def test_a_nested_helper_does_not_move_the_site(self):
        """`UpdateLegalTargets` holds its branches in a nested function.

        The site is taken from the outermost enclosing function that names one,
        so renaming the helper is not a silent reclassification -- which is the
        failure mode of writing the helper's name into `SITE_FUNCTIONS`.
        """
        found = self.ScanOf(
            'def UpdateLegalTargets(self):\n'
            '    def whatever_this_helper_is_called(selector):\n'
            '        if selector.selector_rule.select_rule == "DifferentCards":\n'
            '            pass\n')
        self.assertEqual(found["DifferentCards"], frozenset({FEASIBILITY}))

    def test_a_branch_outside_both_sites_is_not_credited_to_either(self):
        # It is still *found* -- suppressing it would let a rule enforced in
        # some third place read as enforced nowhere -- but it is filed under a
        # site no table entry may claim, so it fails the comparison loudly.
        found = self.ScanOf(
            'def SomewhereElse(self):\n'
            '    if self.select_rule == "DifferentCards":\n'
            '        pass\n')
        self.assertEqual(found["DifferentCards"], frozenset({None}))

    @staticmethod
    def ScanOf(source):
        import tempfile
        with tempfile.TemporaryDirectory() as tmp:
            (Path(tmp) / "sample.py").write_text(source, encoding="utf-8")
            return BranchedOnRules(Path(tmp))


class TestDifferentCardsIsEnforced(unittest.TestCase):
    """The rule itself, against stand-ins.

    `AfterSelectTargets` reads exactly two things off a target -- its `name` and
    whether it is a `Minion` -- so a fake that provides `name` fails on the rule
    rather than on scenario setup. Same reasoning as `test_invariants.py`.

    This is the half the static scan cannot do. The scan proves a branch on the
    rule exists; these prove the branch decides the right way, in both
    directions, which is what stops the next reader from reading a branch that
    is present and wrong as a rule that works.
    """

    class FakeFace:
        def __init__(self, name):
            self.name = name

    class FakeFailures:
        def __init__(self):
            self.reasons = []

        def Set(self, player, text):
            self.reasons.append(text)

    class FakeEffect:
        def __init__(self):
            self.initiator = None
            self.failures = None

    def Effect(self):
        effect = self.FakeEffect()
        effect.failures = self.FakeFailures()
        return effect

    def test_two_copies_of_one_title_are_refused(self):
        rule = SelectorRule(select_rule="DifferentCards")
        effect = self.Effect()
        targets = [self.FakeFace("Panther Claws"), self.FakeFace("Panther Claws")]

        self.assertFalse(rule.AfterSelectTargets(effect, targets, (1, 3)))
        self.assertTrue(effect.failures.reasons)

    def test_distinct_titles_are_allowed(self):
        rule = SelectorRule(select_rule="DifferentCards")
        effect = self.Effect()
        targets = [self.FakeFace("Panther Claws"), self.FakeFace("Tactical Genius")]

        self.assertTrue(rule.AfterSelectTargets(effect, targets, (1, 3)))

    def test_different_objects_are_not_enough(self):
        # The whole point of the FAQ the `01042` script quotes: "different"
        # means a different printed title, not a different object. Two distinct
        # `CardFace` instances sharing a title are one card.
        first = self.FakeFace("Panther Claws")
        second = self.FakeFace("Panther Claws")
        self.assertIsNot(first, second)

        rule = SelectorRule(select_rule="DifferentCards")
        self.assertFalse(rule.AfterSelectTargets(self.Effect(), [first, second], (1, 3)))

    def test_a_single_target_is_always_allowed(self):
        rule = SelectorRule(select_rule="DifferentCards")
        targets = [self.FakeFace("Panther Claws")]
        self.assertTrue(rule.AfterSelectTargets(self.Effect(), targets, (1, 3)))

    def test_a_selector_without_the_rule_does_not_get_it(self):
        # The rule has to be opt-in, or every "choose 2 cards" in the game
        # silently gains a restriction it was never printed with.
        rule = SelectorRule()
        targets = [self.FakeFace("Panther Claws"), self.FakeFace("Panther Claws")]
        self.assertTrue(rule.AfterSelectTargets(self.Effect(), targets, (1, 3)))


class TestMustIncludeTraitsIsEnforced(unittest.TestCase):
    """The live half of MARVEL-127.

    `SelectorFactory.Alliance` gives every Alliance-keyword card a `(2, 2)`
    selector over `CardFinder(traits=["AVENGER", "GUARDIAN"], ...)`, and that
    finder's traits are an **OR** -- so the pool is "friends who are either",
    the feasibility check passes on any board holding one of each, and nothing
    then asked what the player actually picked. Two AVENGERs paid a cost
    printed as one AVENGER and one GUARDIAN, through `cost_func.py` and a path
    the bot reaches.

    Stand-ins for the same reason as the class above: `AfterSelectTargets`
    reads exactly one thing off a target here -- `FindHasTrait` -- so a fake
    that answers it fails on the rule rather than on scenario setup.
    """

    class FakeFace:
        def __init__(self, *traits):
            self.name = "-".join(traits) or "no traits"
            self.traits = frozenset(traits)

        def FindHasTrait(self, *traits):
            # The engine's own contract: the subset of what was asked for that
            # this face actually carries.
            return [x for x in traits if x in self.traits]

    def Rule(self, *traits):
        return SelectorRule(select_rule="MustIncludeTraits",
                            target_must_include_traits=list(traits))

    def Effect(self):
        return TestDifferentCardsIsEnforced().Effect()

    def test_two_avengers_do_not_pay_an_avenger_and_a_guardian(self):
        # The exploit, stated as a test.
        rule = self.Rule("AVENGER", "GUARDIAN")
        effect = self.Effect()
        targets = [self.FakeFace("AVENGER"), self.FakeFace("AVENGER")]

        self.assertFalse(rule.AfterSelectTargets(effect, targets, (2, 2)))
        self.assertTrue(effect.failures.reasons)

    def test_one_of_each_pays(self):
        rule = self.Rule("AVENGER", "GUARDIAN")
        targets = [self.FakeFace("AVENGER"), self.FakeFace("GUARDIAN")]

        self.assertTrue(rule.AfterSelectTargets(self.Effect(), targets, (2, 2)))

    def test_the_order_the_traits_are_chosen_in_does_not_matter(self):
        rule = self.Rule("AVENGER", "GUARDIAN")
        targets = [self.FakeFace("GUARDIAN"), self.FakeFace("AVENGER")]

        self.assertTrue(rule.AfterSelectTargets(self.Effect(), targets, (2, 2)))

    def test_one_card_carrying_both_traits_covers_both(self):
        # `effect.ts` strikes every matching trait off the required list for
        # each chosen card, so a single AVENGER GUARDIAN satisfies the pair.
        # Python has to agree, or the two enforcers disagree in the other
        # direction and a legal browser click becomes an illegal engine input.
        rule = self.Rule("AVENGER", "GUARDIAN")
        targets = [self.FakeFace("AVENGER", "GUARDIAN"), self.FakeFace("XMEN")]

        self.assertTrue(rule.AfterSelectTargets(self.Effect(), targets, (2, 2)))

    def test_a_missing_trait_is_refused_however_many_cards_are_chosen(self):
        rule = self.Rule("AVENGER", "GUARDIAN")
        targets = [self.FakeFace("AVENGER")] * 5

        self.assertFalse(rule.AfterSelectTargets(self.Effect(), targets, (5, 5)))

    def test_a_selector_without_the_rule_does_not_get_it(self):
        # `SelectorRule.__init__` drops `target_must_include_traits` unless the
        # rule is named, so this cannot be enforced by accident.
        rule = SelectorRule(target_must_include_traits=["AVENGER", "GUARDIAN"])
        targets = [self.FakeFace("AVENGER"), self.FakeFace("AVENGER")]

        self.assertTrue(rule.AfterSelectTargets(self.Effect(), targets, (2, 2)))


class TestDifferentTypeIsEnforced(unittest.TestCase):
    """The other half of MARVEL-127, and the one that was invisible.

    `DifferentType`'s single user (`45017`, Suit Up) passes a `check_again_fn`
    that hand-rolls the one-ally-one-upgrade pairing, so the rule enforcing
    nothing cost nothing and showed up nowhere. That callback stays -- it also
    checks `CanAttachTo`, which no select rule can express -- so these tests
    are what says the rule itself works.

    `FacesCounter.GetDifferentTypesCount` keys on `type(face)`, and card faces
    are the final concrete classes (`Ally`, `Upgrade`, ... all carry
    `FinalType`, whose `type_name` *is* `__class__.__name__`), so distinct fake
    classes are the honest stand-in for distinct card types.
    """

    class FakeAlly:
        name = "an ally"

    class FakeUpgrade:
        name = "an upgrade"

    def Effect(self):
        return TestDifferentCardsIsEnforced().Effect()

    def test_two_cards_of_one_type_are_refused(self):
        rule = SelectorRule(select_rule="DifferentType")
        effect = self.Effect()
        targets = [self.FakeAlly(), self.FakeAlly()]

        self.assertFalse(rule.AfterSelectTargets(effect, targets, (2, 2)))
        self.assertTrue(effect.failures.reasons)

    def test_two_cards_of_different_types_are_allowed(self):
        rule = SelectorRule(select_rule="DifferentType")
        targets = [self.FakeAlly(), self.FakeUpgrade()]

        self.assertTrue(rule.AfterSelectTargets(self.Effect(), targets, (2, 2)))

    def test_distinct_objects_of_one_type_are_still_one_type(self):
        first, second = self.FakeAlly(), self.FakeAlly()
        self.assertIsNot(first, second)

        rule = SelectorRule(select_rule="DifferentType")
        self.assertFalse(rule.AfterSelectTargets(self.Effect(), [first, second], (2, 2)))

    def test_a_single_target_is_always_allowed(self):
        rule = SelectorRule(select_rule="DifferentType")
        self.assertTrue(rule.AfterSelectTargets(self.Effect(), [self.FakeAlly()], (1, 1)))

    def test_a_selector_without_the_rule_does_not_get_it(self):
        rule = SelectorRule()
        targets = [self.FakeAlly(), self.FakeAlly()]
        self.assertTrue(rule.AfterSelectTargets(self.Effect(), targets, (2, 2)))


class TestDifferentCardsFeasibility(unittest.TestCase):
    """The other half: an effect that cannot reach its minimum is not offered.

    `EffectChecker.UpdateLegalTargets` already refused a `DifferentType`
    selector whose pool held too few distinct types, and MARVEL-118 gives
    `DifferentCards` the same treatment: a pool of four cards that are all the
    same title cannot answer "choose 2 different cards", so the option should
    never appear rather than appear and then be refused.

    Reached with stand-ins for the same reason as the class above. The branch is
    not reachable from any *shipped* card today -- the only two `DifferentCards`
    selectors are `01042` (minimum 1, where the rule is vacuous) and `TeamUp`
    (minimum 2, whose pool holds one unit per named character and so cannot hold
    a repeated title) -- so a bot game cannot cover it and this is the only thing
    that can.
    """

    class FakeFlags:
        is_statistics = False
        is_delay_ability = False

    class FakeAbility:
        def __init__(self, selectors):
            self.flags = TestDifferentCardsFeasibility.FakeFlags()
            self.selectors = selectors
            self.is_label_attack = False
            self.is_label_thwart = False

    class FakeRule:
        def __init__(self, select_rule):
            self.select_rule = select_rule
            self.target_must_include_traits = []

    class FakeSelector:
        def __init__(self, rule, targets, target_range):
            self.selector_rule = rule
            self.condition = None
            self.is_optional = False
            self.target_text = None
            self._targets = targets
            self._range = target_range

        def GetAllLegalTargets(self, effect, referential_effect=None):
            return self._targets

        def GetTargetRange(self, effect, all_legal_targets):
            return self._range

    class FakeContext:
        def __init__(self):
            self.all_legal_targets = []
            self.target_range = (0, 0)

    class FakeGameRule:
        v16_confuse_stun = False

    class FakeWorld:
        def __init__(self):
            self.rule = TestDifferentCardsFeasibility.FakeGameRule()

    class FakeEffect:
        def __init__(self, ability):
            self.ability = ability
            self.bind_message = None
            self.context = TestDifferentCardsFeasibility.FakeContext()
            self.world = TestDifferentCardsFeasibility.FakeWorld()
            self.initiator = None

    def Checker(self, select_rule, names, target_range):
        from game.effect.effect_checker import EffectChecker

        faces = [TestDifferentCardsIsEnforced.FakeFace(name) for name in names]
        selector = self.FakeSelector(self.FakeRule(select_rule), faces, target_range)
        effect = self.FakeEffect(self.FakeAbility([selector]))
        return EffectChecker(effect)

    def test_a_pool_of_one_title_cannot_answer_choose_two_different(self):
        checker = self.Checker("DifferentCards",
                               ["Panther Claws"] * 4, (2, 2))
        self.assertFalse(checker.UpdateLegalTargets())

    def test_a_pool_with_two_titles_can(self):
        checker = self.Checker("DifferentCards",
                               ["Panther Claws", "Panther Claws", "Tactical Genius"],
                               (2, 2))
        self.assertTrue(checker.UpdateLegalTargets())

    def test_a_minimum_of_one_is_answered_by_any_pool(self):
        # `01042` itself: "up to 3", so the minimum is 1 and no pool holding
        # anything at all can fail this. The check must not start refusing it.
        checker = self.Checker("DifferentCards",
                               ["Panther Claws", "Panther Claws"], (1, 3))
        self.assertTrue(checker.UpdateLegalTargets())

    def test_a_selector_without_the_rule_is_not_filtered(self):
        checker = self.Checker("", ["Panther Claws"] * 4, (2, 2))
        self.assertTrue(checker.UpdateLegalTargets())


class TestCombinedValueRules(unittest.TestCase):
    """The two MARVEL-128 findings among parameterised rules.

    Resource-icon selection used the lower bound twice, turning every range
    into an exact value. The two printed-cost spellings really are aliases, but
    both are shipped client-facing descriptor names, so their compatibility and equal
    behaviour are pinned rather than one name being silently removed.
    """

    def Effect(self):
        return TestDifferentCardsIsEnforced().Effect()

    def test_resource_icons_accept_a_total_between_distinct_bounds(self):
        rule = SelectorRule(combined_resource_icons=(1, 3))
        with patch.object(FacesCounter, "GetPrintedResourcesIcon", return_value=2):
            self.assertTrue(rule.AfterSelectTargets(self.Effect(), [object()], (1, 3)))

    def test_resource_icons_reject_a_total_above_the_upper_bound(self):
        rule = SelectorRule(combined_resource_icons=(1, 3))
        with patch.object(FacesCounter, "GetPrintedResourcesIcon", return_value=4):
            self.assertFalse(rule.AfterSelectTargets(self.Effect(), [object()], (1, 3)))

    def test_printed_cost_aliases_keep_their_descriptor_names(self):
        resource = SelectorRule(combined_resource_cost=(1, 3))
        printed = SelectorRule(combined_printed_cost=(1, 3))

        self.assertEqual(resource.GetRuleAndParam(),
                         ("CombinedResourceCost", (1, 3)))
        self.assertEqual(printed.GetRuleAndParam(),
                         ("CombinedPrintedCost", (1, 3)))

    def test_printed_cost_aliases_make_the_same_decisions(self):
        rules = [SelectorRule(combined_resource_cost=(1, 3)),
                 SelectorRule(combined_printed_cost=(1, 3))]

        for total, expected in ((0, False), (1, True), (2, True),
                                (3, True), (4, False)):
            with self.subTest(total=total), \
                    patch.object(FacesCounter, "GetPrintedCost", return_value=total):
                actual = [rule.AfterSelectTargets(self.Effect(), [object()], (1, 3))
                          for rule in rules]
                self.assertEqual(actual, [expected, expected])


class TestVillainAndMinionRules(unittest.TestCase):
    """Python must enforce the same choice the browser and printed cards do."""

    class FakeFace:
        def __init__(self, kind, engaged_player=None):
            self.kind = kind
            self.engaged_player = engaged_player
            self.name = kind

        def GetEngagedPlayer(self):
            return self.engaged_player

    class FakeContext:
        def __init__(self, legal_targets):
            self.all_legal_targets = legal_targets

    def Check(self, rule_name, targets, you=None, legal_targets=None):
        effect = TestDifferentCardsIsEnforced().Effect()
        effect.context = self.FakeContext(
            targets if legal_targets is None else legal_targets)
        rule = SelectorRule(select_rule=rule_name)
        with patch.object(Villain, "IsType",
                          side_effect=lambda face: face.kind in {"villain", "leader"}), \
                patch.object(Minion, "IsType",
                             side_effect=lambda face: face.kind == "minion"), \
                patch.object(Select, "GetYou", return_value=you):
            return rule.AfterSelectTargets(effect, targets, (0, 99)), effect

    def test_with_you_requires_one_villain(self):
        you = object()
        minion = self.FakeFace("minion", you)

        accepted, _ = self.Check("VillainAndMinionsEngagedWithYou", [minion], you)
        self.assertFalse(accepted)

        accepted, _ = self.Check(
            "VillainAndMinionsEngagedWithYou",
            [self.FakeFace("villain"), self.FakeFace("villain"), minion],
            you,
        )
        self.assertFalse(accepted)

    def test_with_you_rejects_a_minion_engaged_with_someone_else(self):
        you = object()
        targets = [self.FakeFace("villain"),
                   self.FakeFace("minion", object())]

        accepted, effect = self.Check(
            "VillainAndMinionsEngagedWithYou", targets, you)

        self.assertFalse(accepted)
        self.assertIn(EffectFailure.EngagedDifferentPlayer,
                      effect.failures.reasons)

    def test_with_you_accepts_one_villain_and_your_minions(self):
        you = object()
        targets = [self.FakeFace("villain"),
                   self.FakeFace("minion", you),
                   self.FakeFace("minion", you)]

        accepted, _ = self.Check(
            "VillainAndMinionsEngagedWithYou", targets, you)

        self.assertTrue(accepted)

    def test_with_you_requires_every_minion_engaged_with_you(self):
        you = object()
        villain = self.FakeFace("villain")
        first = self.FakeFace("minion", you)
        second = self.FakeFace("minion", you)

        accepted, _ = self.Check(
            "VillainAndMinionsEngagedWithYou", [villain, first], you,
            legal_targets=[villain, first, second],
        )

        self.assertFalse(accepted)

    def test_a_leader_counts_as_the_one_villain(self):
        you = object()
        targets = [self.FakeFace("leader"), self.FakeFace("minion", you)]

        accepted, _ = self.Check(
            "VillainAndMinionsEngagedWithYou", targets, you)

        self.assertTrue(accepted)

    def test_same_player_rule_still_allows_another_players_minions(self):
        another_player = object()
        targets = [self.FakeFace("villain"),
                   self.FakeFace("minion", another_player)]

        accepted, _ = self.Check(
            "VillainAndMinionsEngagedSamePlayer", targets)

        self.assertTrue(accepted)

    def test_same_player_rule_requires_that_players_whole_group(self):
        player = object()
        villain = self.FakeFace("villain")
        first = self.FakeFace("minion", player)
        second = self.FakeFace("minion", player)

        accepted, _ = self.Check(
            "VillainAndMinionsEngagedSamePlayer", [villain, first],
            legal_targets=[villain, first, second],
        )

        self.assertFalse(accepted)

    def test_same_player_rule_does_not_require_another_players_group(self):
        chosen_player = object()
        other_player = object()
        villain = self.FakeFace("villain")
        chosen = self.FakeFace("minion", chosen_player)
        other = self.FakeFace("minion", other_player)

        accepted, _ = self.Check(
            "VillainAndMinionsEngagedSamePlayer", [villain, chosen],
            legal_targets=[villain, chosen, other],
        )

        self.assertTrue(accepted)

    def test_same_player_rule_allows_villain_alone_when_no_minion_exists(self):
        villain = self.FakeFace("villain")

        accepted, _ = self.Check(
            "VillainAndMinionsEngagedSamePlayer", [villain],
            legal_targets=[villain],
        )

        self.assertTrue(accepted)


class TestDifferentCardsCount(unittest.TestCase):
    """`FacesCounter.GetDifferentCardsCount` counts titles, not objects."""

    def Counter(self):
        from game.operate.faces_counter import FacesCounter
        return FacesCounter

    def test_repeats_of_one_title_count_once(self):
        faces = [TestDifferentCardsIsEnforced.FakeFace("Panther Claws")] * 3
        self.assertEqual(self.Counter().GetDifferentCardsCount(faces), 1)

    def test_distinct_objects_sharing_a_title_count_once(self):
        faces = [TestDifferentCardsIsEnforced.FakeFace("Panther Claws"),
                 TestDifferentCardsIsEnforced.FakeFace("Panther Claws")]
        self.assertEqual(self.Counter().GetDifferentCardsCount(faces), 1)

    def test_distinct_titles_each_count(self):
        faces = [TestDifferentCardsIsEnforced.FakeFace("Panther Claws"),
                 TestDifferentCardsIsEnforced.FakeFace("Tactical Genius"),
                 TestDifferentCardsIsEnforced.FakeFace("Panther Claws")]
        self.assertEqual(self.Counter().GetDifferentCardsCount(faces), 2)

    def test_nothing_is_zero(self):
        self.assertEqual(self.Counter().GetDifferentCardsCount([]), 0)


if __name__ == "__main__":
    unittest.main()
