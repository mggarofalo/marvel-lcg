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

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from game.selector.selector_rule import SelectorRule

PY_SRC = Path(__file__).resolve().parent.parent
GAME = PY_SRC / "game"
CLIENT = PY_SRC / "public" / "js" / "marvel" / "effect.ts"

# Attribute names that hold the rule under test. `SelectorRule.select_rule` is
# the resolved one and `raw_select_rule` is what the card script passed;
# `SelectorRange` copies the resolved one under the same name.
RULE_ATTRS = frozenset({"select_rule", "raw_select_rule"})


def Declared():
    """Every rule name the engine declares, from the `Literal`s themselves."""
    names = set()
    for literal in (SelectorRule.RULE_BASE,
                    SelectorRule.RULE_WITH_PARAM,
                    SelectorRule.RULE_FOR_UI):
        names.update(get_args(literal))
    return names


DECLARED = Declared()


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
    """

    def __init__(self):
        self.names = set()
        self.prefixes = set()
        self.attributes = set()

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
                    self.names.add(text)
        self.generic_visit(node)

    def visit_Call(self, node):
        func = node.func
        if isinstance(func, ast.Attribute) and func.attr == "startswith" and \
                self.IsRuleExpression(func.value):
            for argument in node.args:
                text = self.StringOf(argument)
                if text:
                    self.prefixes.add(text)
        self.generic_visit(node)

    def visit_If(self, node):
        self.attributes.update(TestedAttributes(node.test))
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
    """Declared rules the engine dispatches on, anywhere under `root`."""
    scan = RuleBranchScan()
    for path in sorted(Path(root).rglob("*.py")):
        scan.visit(ast.parse(path.read_text(encoding="utf-8"), filename=str(path)))

    found = set()
    for rule in DECLARED:
        if not rule:
            # The empty rule is "no rule". There is nothing to branch on and
            # nothing to enforce; it is classified below rather than scanned.
            continue
        if rule in scan.names:
            found.add(rule)
        elif any(rule.startswith(prefix) for prefix in scan.prefixes):
            found.add(rule)
        elif SnakeCase(rule) in scan.attributes:
            found.add(rule)
    return found


# --------------------------------------------------------------------------
# The reviewed table (MARVEL-118)
# --------------------------------------------------------------------------

# `PYTHON` -- the engine enforces it, and `BranchedOnRules` must find the
# branch. `UI_ONLY` -- the engine deliberately does not, the browser does, and
# `effect.ts` must contain the name. `NO_RULE` -- reserved for the empty string,
# and asserted to hold nothing else, so "there is no rule here" cannot be used
# to wave a real one through.
PYTHON = "python"
UI_ONLY = "ui-only"
NO_RULE = "no-rule"

REVIEWED_SELECT_RULES = {
    "": (NO_RULE,
         "the absence of a rule. Every selector that names no rule carries "
         "this, and there is nothing to enforce"),

    # RULE_BASE
    "DifferentCards": (PYTHON,
        "`SelectorRule.AfterSelectTargets` rejects a selection holding two "
        "faces with one title, via `FacesCounter.GetDifferentCardsCount`, and "
        "`EffectChecker.UpdateLegalTargets` refuses to offer an effect whose "
        "minimum cannot be met by distinct titles. MARVEL-118 -- before that "
        "this rule was enforced only by `effect.ts`"),
    "DifferentType": (PYTHON,
        "`EffectChecker.UpdateLegalTargets` refuses the effect when the legal "
        "pool holds fewer distinct card types than the minimum. NOTE: that is "
        "a feasibility check on the *pool*; nothing validates the chosen set, "
        "which `effect.ts` does. See the module docstring of this file and the "
        "MARVEL-118 report -- the gap is recorded, not fixed here"),
    "MustIncludeTraits": (PYTHON,
        "`EffectChecker.UpdateLegalTargets` refuses the effect when the legal "
        "pool cannot cover the required traits. Same caveat as `DifferentType`: "
        "the pool is checked, the selection is not"),

    # RULE_WITH_PARAM -- never passed as `select_rule=`; `SelectorRule.__init__`
    # derives the name from the matching `combined_*` argument, so the argument
    # is the rule and the branch on it is the enforcement.
    "CombinedResourceCost": (PYTHON,
        "`SelectorRule.Process` drops a pool that cannot reach the minimum and "
        "`AfterSelectTargets` bounds the chosen set both ways, on "
        "`FacesCounter.GetPrintedCost`"),
    "CombinedPrintedCost": (PYTHON,
        "same two sites, and -- worth knowing -- the same `GetPrintedCost` "
        "call, so this rule and `CombinedResourceCost` are behaviourally one "
        "rule under two names, exactly as `effect.ts` treats them"),
    "CombinedResourceIcons": (PYTHON,
        "`Process` and `AfterSelectTargets`, on "
        "`FacesCounter.GetPrintedResourcesIcon`. `effect.ts` returns true for "
        "this one unconditionally, so here the browser is the lenient side. No "
        "card in `cards/pack/` passes `combined_resource_icons`, and the upper "
        "bound in `AfterSelectTargets` reads index 0 where the other two read "
        "index 1 -- both reported under MARVEL-118, neither changed here"),

    # RULE_FOR_UI
    "VillainAndMinionsEngagedSamePlayer": (PYTHON,
        "`AfterSelectTargets` takes this branch through "
        "`startswith('VillainAndMinions')` and rejects a selection whose "
        "minions are not all engaged with one player"),
    "VillainAndMinionsEngagedWithYou": (PYTHON,
        "the same `startswith` branch, which is why it is credited. It is a "
        "weaker check than the browser's: `effect.ts` requires the engaged "
        "player to be *you* and requires exactly one villain, and Python "
        "requires neither. Reported under MARVEL-118, not changed here"),
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
        claimed = {rule for rule, (side, _) in REVIEWED_SELECT_RULES.items()
                   if side == PYTHON}

        self.assertEqual(
            BranchedOnRules(), claimed,
            "the rules the engine dispatches on and the rules claimed as "
            "enforced in Python disagree. A rule listed as `PYTHON` with no "
            "branch is the MARVEL-118 bug again: declared, rendered, passed by "
            "cards, and implemented by nothing but `effect.ts`. A rule the "
            "engine branches on but that is listed `UI_ONLY` is the entry "
            "being wrong.")

    def test_ui_only_rules_are_really_enforced_by_the_client(self):
        # "Deliberately UI-only" has to be a claim about code that exists,
        # or it is indistinguishable from the rule nobody wrote.
        client = CLIENT.read_text(encoding="utf-8")
        for rule, (side, reason) in sorted(REVIEWED_SELECT_RULES.items()):
            if side != UI_ONLY:
                continue
            with self.subTest(rule=rule):
                self.assertIn(
                    f'"{rule}"', client,
                    f"{rule} is listed as enforced by the client, but "
                    f"{CLIENT.name} never names it")
                self.assertTrue(reason.strip(),
                                f"{rule} is listed UI-only with no reason")

    def test_only_the_empty_rule_may_claim_there_is_no_rule(self):
        no_rule = {rule for rule, (side, _) in REVIEWED_SELECT_RULES.items()
                   if side == NO_RULE}
        self.assertEqual(no_rule, {""})

    def test_every_entry_states_a_reason(self):
        for rule, (_, reason) in sorted(REVIEWED_SELECT_RULES.items()):
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
        self.assertEqual(set(), self.ScanOf(
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
