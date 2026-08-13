"""The blocker census (MARVEL-92).

This tool's output is quoted in a design argument, so the tests that matter are
the ones that stop it drifting into flattery. Two failure modes, and they pull in
opposite directions:

**Silent under-counting.** If the scanner stops walking part of the tree, every
card looks expressible and the DSL looks easier than it is. This is the one that
actually happened: the first version flagged a `lambda` passed inside a handler
but not a locally-defined function passed by name -- the same construct -- and
counted 61 cards clean that were not. Every corpus-level assertion here is
therefore **two-sided**; a lower bound on the clean share would have got safer as
the scanner got more broken.

**Counting the envelope.** `AbilityFactory.X(...).SetTarget(...)` is already
declarative and lambdas passed to `conditions=` there are mostly named printed
predicates. Flagging those would report the corpus as far harder than it is. The
ones that are *not* named predicates are counted by `EnvelopePredicates` and
reported separately rather than folded in either direction.
"""

import ast
import io
import os
import unittest

from tools.dsl import blockers

CARDS = os.path.join("cards", "pack")


def Wrap(body: str) -> str:
    """A card script whose handler contains `body`."""
    lines = "\n".join("        " + line for line in body.splitlines())
    return (
        "def GetAbilities():\n"
        "    def handler(effect, message):\n"
        f"{lines}\n"
        "    return [AbilityFactory.WhenThisRevealed(None, handler)]\n"
    )


################################################################################
#

class TestFlags(unittest.TestCase):
    """One construct per blocker, so a scanner regression names itself."""

    def Flags(self, body: str):
        return blockers.ScanSource(Wrap(body))

    def test_a_plain_handler_carries_nothing(self):
        self.assertEqual(self.Flags(
            "player = effect.GetInitiator()\n"
            "for unit in player.GetEngagedMinions():\n"
            "    if unit.HasTrait('DRONE'):\n"
            "        this.DealDamage([unit], 2, effect)\n"), set())

    def test_while(self):
        self.assertIn("while", self.Flags("while x > 0:\n    x -= 1\n"))

    def test_try(self):
        self.assertIn("try", self.Flags("raise ValueError()\n"))

    def test_growing_a_local_collection(self):
        self.assertIn("grow", self.Flags(
            "found = []\n"
            "for unit in effect.targets:\n"
            "    found.append(unit)\n"))

    def test_a_grow_method_reached_through_an_attribute_is_not_a_blocker(self):
        """`player.discard_pile.add(card)` is a game action, not bookkeeping."""
        self.assertNotIn("grow", self.Flags(
            "player = effect.GetInitiator()\n"
            "player.discard_pile.add(effect.targets[0])\n"))

    def test_an_unbound_name_is_not_treated_as_a_local_collection(self):
        """The half of the `grow` rule the attribute test never reaches.

        Without the enclosing-scope check, any call named `add`/`remove` on a
        bare name would flag -- including names the handler never bound.
        """
        self.assertNotIn("grow", self.Flags("Registry.add(effect)\n"))

    def test_accumulating_across_a_loop(self):
        self.assertIn("augassign", self.Flags(
            "total = 0\n"
            "for unit in effect.targets:\n"
            "    total += 2\n"))

    def test_a_closure_over_an_enclosing_local(self):
        self.assertIn("close", self.Flags(
            "seen = set()\n"
            "def inner():\n"
            "    seen.add(1)\n"
            "inner()\n"))

    def test_a_closure_over_an_enclosing_parameter(self):
        """The commonest closure in the corpus, and it was going unflagged.

        Handler parameters were not collected as bound names, so a nested
        function capturing `effect` -- which almost all of them do -- looked
        like it closed over nothing. 60 cards were counted clean on that basis.
        """
        self.assertIn("close", self.Flags(
            "def inner(targets):\n"
            "    this.DealDamage(targets, 2, effect)\n"
            "initiator.ChooseAbilities(effect, inner)\n"))

    def test_a_nested_function_that_only_reuses_a_name_is_not_a_closure(self):
        """Tic-Tac-Toe's `to_counter_name`, which closes over nothing.

        The first version collected enclosing locals with `ast.walk`, which
        descends into nested functions, so an inner function's own locals looked
        like the outer one's and mere name reuse flagged as capture.
        """
        self.assertNotIn("close", self.Flags(
            "counter_name = 'a'\n"
            "def pure(place):\n"
            "    counter_name = 'counter_' + place\n"
            "    return counter_name\n"
            "this.PlaceCounters(pure('b'), effect)\n"))

    def test_one_inner_function_does_not_lend_its_locals_to_a_sibling(self):
        """`ast.walk` descends into nested functions; scope analysis must not.

        Collecting the handler's locals with `ast.walk` pulls in every name
        bound inside the functions it contains, so a *sibling* reading a name of
        its own looks like it captured one of the handler's. Subtracting the
        inner function's own bindings hides most of this; it does not hide the
        sibling case, which is why the walk has to stop at the boundary.
        """
        self.assertNotIn("close", self.Flags(
            "def a(place):\n"
            "    counter = place\n"
            "    return counter\n"
            "def b():\n"
            "    return counter\n"
            "this.PlaceCounters(a('x'), effect)\n"))

    def test_a_local_function_passed_by_name_is_the_same_as_a_lambda(self):
        """The bug that made the headline number wrong.

        Both forms are the `effect subtree as a value` node. Flagging only the
        lambda counted 61 cards clean that pass a named callback instead.
        """
        by_name = self.Flags(
            "def action(targets):\n"
            "    this.DealDamage(targets, 2, effect)\n"
            "initiator.ChooseAbilities(effect, "
            "AbilityFactory.ForChoiceAbility('', action))\n")
        by_lambda = self.Flags(
            "initiator.ChooseAbilities(effect, AbilityFactory.ForChoiceAbility("
            "'', lambda targets: this.DealDamage(targets, 2, effect)))\n")
        self.assertIn("callback", by_name)
        self.assertIn("callback", by_lambda)

    def test_runtime_registration(self):
        self.assertIn("register", self.Flags(
            "this.effect.Registers(AbilityFactory.WhenUnitBeDefeated("
            "AbilityType.Temp0, 'This', action))\n"))

    def test_an_inline_choice_ability_is_not_dynamic_registration(self):
        """`ForChoiceAbility` is "choose a target, then do this".

        The ability it builds is static -- only its target is runtime -- so it
        is a node, not an escape hatch. Counting these as dynamic registration
        would multiply the apparent size of the problem several times over.
        """
        flags = self.Flags(
            "initiator.ChooseAbilities(effect, "
            "AbilityFactory.ForChoiceAbility('', 'x').SetTarget(Enemy))\n")
        self.assertNotIn("factory-in-handler", flags)
        self.assertNotIn("register", flags)

    def test_a_non_inline_factory_call_is_a_blocker(self):
        self.assertIn("factory-in-handler", self.Flags(
            "AbilityFactory.WhenUnitWouldAttack(AbilityType.Temp0, None, 'a')\n"))

    def test_break(self):
        self.assertIn("break", self.Flags(
            "for unit in effect.targets:\n"
            "    break\n"))

    def test_isinstance(self):
        self.assertIn("isinstance", self.Flags(
            "if isinstance(message, Message.WhenCardRevealed):\n"
            "    pass\n"))

    def test_comprehension(self):
        self.assertIn("comprehension", self.Flags(
            "ids = [x.card_id for x in effect.targets]\n"))

    def test_slice(self):
        self.assertIn("slice", self.Flags(
            "top = effect.targets[0:2]\n"))

    def test_unpack(self):
        self.assertIn("unpack", self.Flags(
            "first, second = effect.targets\n"))

    def test_synthesising_an_identifier(self):
        self.assertIn("string-build", self.Flags(
            "name = f'tic_tac_toe_{place}'\n"))

    def test_indexing_a_literal_table_by_a_computed_key(self):
        self.assertIn("dyn-subscript", self.Flags(
            "name = {'Y': 'y', 'R': 'r'}[color]\n"))

    def test_class_def(self):
        self.assertIn("class-def", blockers.ScanSource(
            "def GetAbilities():\n"
            "    class Buff_1(Buff):\n"
            "        pass\n"
            "    return []\n"))


################################################################################
#

class TestTheEnvelopeIsNotWalked(unittest.TestCase):
    """The declarative half of every card must not be counted against it."""

    def test_a_condition_lambda_in_the_envelope_is_not_flagged(self):
        source = (
            "def GetAbilities():\n"
            "    def handler(effect, message):\n"
            "        this.DealDamage(effect.targets, 2, effect)\n"
            "    return [AbilityFactory.WhenInYourPlayTurn(\n"
            "        AbilityType.HeroAction,\n"
            "        handler,\n"
            "        conditions=[lambda effect, message:\n"
            "            Worlds.GetCrisisIcons(effect) > 0],\n"
            "    ).SetTarget(Enemy).SetCost(Cost('1')).LimitOncePerRound()]\n")
        self.assertEqual(blockers.ScanSource(source), set())

    def test_a_literal_table_in_the_envelope_is_not_flagged(self):
        source = (
            "def GetAbilities():\n"
            "    names = ['07005', '07006']\n"
            "    return [AbilityFactory.BeginGameWithSetAside(names)]\n")
        self.assertEqual(blockers.ScanSource(source), set())

    def test_the_envelope_predicates_are_counted_somewhere(self):
        """Not walking them is a choice, and a silent choice is a hidden claim.

        `conditions=` lambdas cost a card nothing in the census. Most are named
        printed conditions; some walk a causal chain. They are the condition
        language -- a second DSL surface -- and the tool has to say so rather
        than let the reader assume the envelope is free.
        """
        if not os.path.isdir(CARDS):
            self.skipTest("run from py_src/")
        scripts, lambdas, complex_ones = blockers.EnvelopePredicates()
        self.assertGreater(scripts, 0)
        self.assertGreaterEqual(lambdas, scripts)
        self.assertGreater(complex_ones, 0,
                           "if none are complex the caveat can be dropped")

        out = io.StringIO()
        blockers.Report(blockers.Census(), out=out)
        self.assertIn("condition lambdas in the envelope", out.getvalue())


################################################################################
#

class TestKnownCards(unittest.TestCase):
    """Verdicts reached by reading the card, pinned against the scanner."""

    @classmethod
    def setUpClass(cls):
        if not os.path.isdir(CARDS):
            raise unittest.SkipTest("run from py_src/")

    def Flags(self, relative: str):
        return blockers.ScanFile(os.path.join(CARDS, relative))

    def test_spectrum_is_expressible_as_it_stands(self):
        # 53018: forEach over the cards used to pay, read a printed resource,
        # modify one object. Nothing a node tree cannot hold.
        self.assertEqual(self.Flags(os.path.join("falcon", "53018.py")), set())

    def test_master_molds_children_is_expressible_as_it_stands(self):
        # 32117: `if the query is empty then A else forEach(query) B`.
        self.assertEqual(self.Flags(
            os.path.join("mut_gen", "master_mold", "32117.py")), set())

    def test_promised_prosperity_needs_scoped_observation(self):
        # 24005b prints "each player who was not dealt a card *this way*", and
        # implements it by watching a foreign sub-ability it invoked.
        flags = self.Flags(os.path.join("hood", "the_hood", "24005b.py"))
        self.assertIn("register", flags)
        self.assertIn("close", flags)

    def test_hail_hydra_prints_the_same_phrase_and_needs_nothing(self):
        """The control that keeps the 24005b argument honest.

        03030 prints "Each player who was **not attacked this way**" -- the same
        causal wording -- and needs no watcher, because it performs the attacks
        itself and can see the result. So the demand in 24005b is not the
        phrase; it is that the deal happens inside a foreign ability that
        reports nothing back. Without this test the doc's claim reads as though
        the printed text alone forces a general observation primitive.
        """
        flags = self.Flags(
            os.path.join("cap", "captain_america_nemesis", "03030.py"))
        self.assertNotIn("register", flags)

    def test_stephen_strange_installs_abilities_on_cards_it_does_not_own(self):
        # 09001b seeds five Invocation cards and installs a zone-redirect on
        # each. This is the escape hatch, and it should stay visible.
        self.assertIn("register", self.Flags(
            os.path.join("drs", "doctor_strange", "09001b.py")))

    def test_tic_tac_toe_does_not_close_over_anything(self):
        # The correction in docs/card-dsl.md rests on this: `to_counter_name`
        # is a pure function of its argument, not a closure.
        self.assertNotIn("close", self.Flags(
            os.path.join("deadpool", "44057.py")))

    def test_luck_be_a_lady_is_the_only_unbounded_loop_in_the_corpus(self):
        census = blockers.Census()
        looping = sorted(p for p, flags in census.items() if "while" in flags)
        self.assertEqual(looping, [os.path.join(
            CARDS, "next_evol", "domino", "40041.py")])


################################################################################
#

class TestCensus(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        if not os.path.isdir(CARDS):
            raise unittest.SkipTest("run from py_src/")
        cls.census = blockers.Census()

    def test_every_flag_raised_is_a_documented_blocker(self):
        """A flag with no line of prose is a flag nobody can act on."""
        raised = set()
        for flags in self.census.values():
            raised |= flags
        self.assertEqual(raised - set(blockers.BLOCKERS), set())

    def test_every_blocker_says_what_retires_it_or_that_nothing_does(self):
        self.assertEqual(set(blockers.BLOCKERS), set(blockers.RETIRED_BY))

    def test_a_file_that_is_not_a_card_script_is_excluded_and_named(self):
        """`endless.py` is empty; the two `campaign.py` modules have no
        `GetAbilities`, so nothing in them is walked. Scanning them returns no
        flags, which would have counted an empty file and two scenario-setup
        modules among the expressible cards.
        """
        skipped = blockers.NotCardScripts()
        self.assertIn(os.path.join(CARDS, "endless", "endless.py"), skipped)
        for path in skipped:
            self.assertNotIn(path, self.census)

    def test_the_clean_share_is_pinned_from_both_sides(self):
        """A lower bound alone gets *safer* as the scanner gets more broken.

        Under-counting drives the clean share toward 1.0, which is the failure
        this file exists to catch, so the upper bound is the load-bearing half.
        Both are loose enough to survive a card being edited.
        """
        clean = sum(1 for flags in self.census.values() if not flags)
        share = clean / len(self.census)
        self.assertGreater(share, 0.70)
        self.assertLess(share, 0.84)

    def test_the_greedy_walk_terminates_and_never_goes_backwards(self):
        order = blockers.Greedy(self.census)
        self.assertTrue(order)
        seen, last = set(), 0
        for node, gain, cleared in order:
            self.assertNotIn(node, seen)
            seen.add(node)
            self.assertGreater(gain, 0)
            self.assertGreaterEqual(cleared, last)
            last = cleared

    def test_the_cumulative_column_is_the_running_sum_of_the_clears_column(self):
        """The two columns are printed side by side and mean different things.

        Swapping them changes every figure quoted from this table and nothing
        else would notice.
        """
        running = 0
        for _node, gain, cleared in blockers.Greedy(self.census):
            running += gain
            self.assertEqual(running, cleared)

    def test_the_walk_scores_nodes_rather_than_blockers(self):
        """`grantUntil` retires two blockers that always travel together.

        `.Registers(AbilityFactory.WhenX(...))` trips both `register` and
        `factory-in-handler`, so scoring one blocker at a time gave each a gain
        of zero and dropped the node from the curve -- while the design document
        claimed that node retired those cards. Both statements could not be
        true.
        """
        nodes = [node for node, _gain, _cleared
                 in blockers.Greedy(self.census)]
        self.assertIn("grantUntil", nodes)

    def test_permanent_foreign_installation_is_two_cards(self):
        """The size of the escape hatch, which is the whole design question.

        Seven scripts call `.Registers()`. Five call it on *themselves*, with
        `AbilityType.Temp0`, and tear it down again with `Effects.UnRegister` --
        that is scoped observation, not installation. Only two install an
        ability onto a card the source does not own, and those two are the
        compiled-code carve-out. If that set grows, the DSL is not holding the
        line and this test should be the thing that says so.
        """
        foreign = []
        for path, flags in self.census.items():
            if "register" not in flags:
                continue
            with open(path, encoding="utf-8") as handle:
                tree = ast.parse(handle.read())
            for node in ast.walk(tree):
                if (isinstance(node, ast.Call)
                        and isinstance(node.func, ast.Attribute)
                        and node.func.attr == "Registers"
                        and ast.unparse(node.func.value) != "this.effect"):
                    foreign.append(path)
                    break
        self.assertEqual(sorted(foreign), [
            os.path.join(CARDS, "drs", "doctor_strange", "09001b.py"),
            os.path.join(CARDS, "twc", "07001a.py"),
        ])
