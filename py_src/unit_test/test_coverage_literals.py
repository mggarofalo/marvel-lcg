"""Card ids that live in a card script rather than in a data file.

`tools/coverage/literals.py` is the rule that finds them, and MARVEL-98 is what
it exists for: `data/scenarios/the_wrecking_crew.json` has an empty `villain` and
`encounters`, and its whole encounter deck is a Python list in
`cards/pack/twc/07001a.py`. A 321-case corpus played 45 cards the reach map
called unreachable, and 44 of them were those.

**A rule, not a match.** The point of these tests is that the scanner survives
the file being written differently -- reordered, reflowed, aliased, built by
concatenation, held in a tuple -- because a scanner that only reads one file's
current formatting is a hand-list with extra steps. Each mutation below is
applied to the *real* `07001a.py` and must yield the same 46 ids.

The other half is what it refuses. A scanner that guesses at computed ids would
put cards into the reach map that no game brings in, and the map's whole value is
that what it calls unreachable really is. So the shapes it will not follow are
tested as deliberately as the ones it will.
"""

import ast
import os
import textwrap
import unittest

from tools.coverage import literals


TWC = os.path.join("cards", "pack", "twc", "07001a.py")


def Scan(source):
    return literals.ScanSource(textwrap.dedent(source))


################################################################################
# The rule, on written-out shapes


class TestTheEntryPoints(unittest.TestCase):
    """Which calls are read, and which argument of them."""

    def test_a_literal_list_reaching_generate_cards(self):
        self.assertEqual(
            Scan('CardFactory.GenerateCards(["07005", "07006"], deck, world)'),
            {"07005", "07006"})

    def test_the_ids_argument_of_a_set_aside_deck(self):
        # `SetAsideDeck.Create(by_effect, villain, card_ids)`: the third
        # positional argument, and nothing else on the call.
        self.assertEqual(
            Scan('deck.Create(effect, villain, ["07020", "07021"])'),
            {"07020", "07021"})

    def test_generate_cards_by_keyword(self):
        self.assertEqual(Scan('CardFactory.GenerateCards(names=["07005"])'),
                         {"07005"})

    def test_create_by_keyword(self):
        self.assertEqual(
            Scan('deck.Create(effect, villain, card_ids=["07020"])'),
            {"07020"})

    def test_a_create_with_no_ids_offers_nothing(self):
        # Eight other scripts call `.Create(effect, villain)` and let the deck
        # fill itself. There is nothing to read, and guessing is what would put
        # cards into the map that no game brings in.
        self.assertEqual(
            Scan('GetTheCollection(effect).Create(effect, villain, '
                 'type=DeckType.AsideDeck)'), set())

    def test_the_receiver_is_not_checked(self):
        # A static reader cannot know what `GetThunderballEncounter(effect)`
        # returns. The method name, the argument position and the id shape are
        # what identify the call -- which is why the id shape has to be strict.
        self.assertEqual(
            Scan('GetThunderballEncounter(effect).Create(e, v, ["07020"])'),
            {"07020"})

    def test_another_method_with_a_list_of_ids_is_not_an_entry_point(self):
        # `AbilityFactory.BeginGameWithSetAside([...])` names ids too, and every
        # one of them is already in a deck file. Adding entry points widens the
        # claim this tool makes; the docstring records the measurement that says
        # widening buys one card.
        self.assertEqual(
            Scan('AbilityFactory.BeginGameWithSetAside(["09032", "09033"])'),
            set())

    def test_an_argument_in_the_wrong_position_is_not_read(self):
        self.assertEqual(
            Scan('CardFactory.GenerateCards(deck, ["07005"], world)'), set())


class TestResolution(unittest.TestCase):
    """Getting from the argument expression to the ids it can hold."""

    def test_a_name_bound_in_the_same_scope(self):
        self.assertEqual(Scan("""
            def handler(effect, message):
                ids = ["07005", "07006"]
                CardFactory.GenerateCards(ids, deck, world)
            """), {"07005", "07006"})

    def test_a_name_bound_in_an_enclosing_scope(self):
        self.assertEqual(Scan("""
            def outer():
                ids = ["07005"]
                def inner():
                    CardFactory.GenerateCards(ids, deck, world)
            """), {"07005"})

    def test_a_name_bound_at_module_level(self):
        self.assertEqual(Scan("""
            IDS = ["07005"]
            def handler(effect, message):
                CardFactory.GenerateCards(IDS, deck, world)
            """), {"07005"})

    def test_the_innermost_binding_wins(self):
        self.assertEqual(Scan("""
            ids = ["07005"]
            def handler(effect, message):
                ids = ["07006"]
                CardFactory.GenerateCards(ids, deck, world)
            """), {"07006"})

    def test_a_name_written_twice_resolves_to_both(self):
        # Which write reached the call is the question a static reader cannot
        # answer, so both count. Over-approximating inside one file's own
        # literal table keeps the map on the side it is already on.
        self.assertEqual(Scan("""
            def handler(effect, message):
                ids = ["07005"]
                if something:
                    ids = ["07006"]
                CardFactory.GenerateCards(ids, deck, world)
            """), {"07005", "07006"})

    def test_a_table_indexed_by_a_loop_variable(self):
        # This is the Wrecking Crew shape exactly, and the reason a subscript
        # resolves to the whole table: `index` runs 0..3 and every row is dealt.
        self.assertEqual(Scan("""
            def handler(effect, message):
                table = [["07005"], ["07020"], ["07035"], ["07049"]]
                for index in range(4):
                    CardFactory.GenerateCards(table[index], deck, world)
            """), {"07005", "07020", "07035", "07049"})

    def test_a_table_indexed_by_a_constant(self):
        self.assertEqual(Scan("""
            def handler(effect, message):
                table = [["07005"], ["07020"]]
                CardFactory.GenerateCards(table[0], deck, world)
            """), {"07005", "07020"})

    def test_a_tuple_reads_like_a_list(self):
        self.assertEqual(Scan('CardFactory.GenerateCards(("07005",), d, w)'),
                         {"07005"})

    def test_concatenated_lists(self):
        self.assertEqual(Scan("""
            def handler(effect, message):
                CardFactory.GenerateCards(["07005"] + ["07006"], deck, world)
            """), {"07005", "07006"})

    def test_a_conditional_expression_takes_both_branches(self):
        self.assertEqual(Scan("""
            def handler(effect, message):
                CardFactory.GenerateCards(
                    ["07005"] if expert else ["07006"], deck, world)
            """), {"07005", "07006"})

    def test_a_starred_argument(self):
        self.assertEqual(Scan("""
            def handler(effect, message):
                rows = [["07005"]]
                CardFactory.GenerateCards(*rows, deck, world)
            """), {"07005"})

    def test_a_dict_lookup_reads_the_values(self):
        # `cards/pack/sm/venom_goblin/27116a.py`: a campaign log entry is mapped
        # to the Sinister Six villain it unlocks. The values are what reaches
        # the call; the keys are the lookup, and some other source names those.
        self.assertEqual(Scan("""
            def handler(effect, message):
                found = CampaignLog.GetList("Victory", effect)
                ids = [{"27094": "27158", "27095": "27159"}[x] for x in found]
                CardFactory.GenerateCards(ids, None, effect.world)
            """), {"27158", "27159"})

    def test_a_cyclic_binding_terminates(self):
        # `ids = ids + [...]` makes the binding table cyclic. Without the seen
        # set this walk does not return at all.
        self.assertEqual(Scan("""
            def handler(effect, message):
                ids = ["07005"]
                ids = ids + ["07006"]
                CardFactory.GenerateCards(ids, deck, world)
            """), {"07005", "07006"})


class TestWhatItRefuses(unittest.TestCase):
    """Shapes the scanner deliberately returns nothing for.

    Each one is a real construct in this corpus. Following any of them would
    mean evaluating the card script, and a wrong entry here does not look like a
    bug -- it looks like a card that is reachable and never gets played.
    """

    def test_an_id_built_from_a_format_string(self):
        self.assertEqual(Scan("""
            def handler(effect, message):
                ids = [f"070{n:02d}" for n in range(5, 17)]
                CardFactory.GenerateCards(ids, deck, world)
            """), set())

    def test_an_id_built_by_concatenating_strings(self):
        self.assertEqual(
            Scan('CardFactory.GenerateCards(["07" + "005"], deck, world)'),
            set())

    def test_ids_from_a_runtime_query(self):
        # `cards/pack/endless/wild.py`. There is no literal to find: the
        # database decides that set when the game runs.
        self.assertEqual(Scan("""
            def handler(effect, message):
                sets = CardsDB.GetPapers(set_name="Rhino")
                CardFactory.GenerateCards(sets, deck, effect.world)
            """), set())

    def test_ids_arriving_as_a_parameter(self):
        # The walk is intra-procedural on purpose: a name resolves to what an
        # enclosing *scope* bound, never to what a caller passed.
        self.assertEqual(Scan("""
            def build(ids):
                CardFactory.GenerateCards(ids, deck, world)
            def handler(effect, message):
                build(["07005"])
            """), set())

    def test_a_string_that_is_not_shaped_like_a_card_id(self):
        # Names, set keys and labels turn up in these positions. Admitting them
        # would put non-cards into the reach map, which is the same failure that
        # keeping `encounter_sets` out of `SCENARIO_KEYS` avoids on the data
        # side.
        self.assertEqual(
            Scan('deck.Create(e, v, ["Wrecker", "bomb_scare", "07005"])'),
            {"07005"})

    def test_the_challenge_id_shape_is_still_a_card(self):
        # `9999_two_for_one` is how the challenge decks name theirs, so the
        # filter cannot simply be five digits.
        self.assertEqual(
            Scan('deck.Create(e, v, ["9999_two_for_one"])'),
            {"9999_two_for_one"})


################################################################################
# Against the real card tree


class TestTheRealTree(unittest.TestCase):

    @classmethod
    def setUpClass(cls):
        if not os.path.isdir(literals.CARD_FOLDER):
            raise unittest.SkipTest("run from py_src/")
        cls.census = literals.Census()

    def test_the_wrecking_crew_scheme_is_found(self):
        found = {literals.Label(path) for path in self.census}
        self.assertIn("twc/07001a", found)

    def test_it_finds_the_whole_encounter_deck(self):
        ids = self.census[TWC]
        # Four villains' encounter decks, 46 distinct ids, none of them named by
        # any deck, set or scenario file.
        self.assertEqual(len(ids), 46)
        for card_id in ("07005", "07020", "07035", "07059"):
            self.assertIn(card_id, ids)

    def test_the_villain_ids_are_not_swept_in(self):
        # The four villains are in the scenario's `set_aside`, and the script
        # reaches them by name through `Worlds.AsideDeck(...).FindCard`. Only
        # what passes through the two entry points is read.
        self.assertNotIn("07002", self.census[TWC])

    def test_almost_nothing_else_in_the_corpus_does_this(self):
        # The finding, not an incidental count: two scripts out of ~3800 build a
        # deck from ids the data does not hold. If a third appears, this is the
        # test that says so, and the reach map will already have grown it.
        self.assertEqual(sorted(literals.Label(p) for p in self.census),
                         ["sm/venom_goblin/27116a", "twc/07001a"])

    def test_a_script_that_names_no_ids_is_not_a_source(self):
        for path in self.census:
            self.assertTrue(self.census[path], path)

    def test_labels_are_slash_separated_on_every_platform(self):
        for path in self.census:
            self.assertNotIn("\\", literals.Label(path))


################################################################################
# The mutations: a rule, not a match


class Mutation(ast.NodeTransformer):
    """Rewrites of the real script that must not change what is found."""

    def __init__(self, kind):
        self.kind = kind
        self.lifted = []

    def visit_List(self, node):
        self.generic_visit(node)
        if self.kind == "reorder":
            return ast.List(elts=list(reversed(node.elts)), ctx=node.ctx)
        if self.kind == "tuple":
            return ast.Tuple(elts=node.elts, ctx=node.ctx)
        if self.kind == "concat" and len(node.elts) > 3:
            half = len(node.elts) // 2
            return ast.BinOp(left=ast.List(elts=node.elts[:half], ctx=node.ctx),
                             op=ast.Add(),
                             right=ast.List(elts=node.elts[half:], ctx=node.ctx))
        return node

    def visit_Constant(self, node):
        if self.kind == "computed" and isinstance(node.value, str) \
                and literals.CARD_ID.match(node.value):
            # Every id becomes `"07" + "005"`, which is the one thing this
            # scanner promises not to read.
            return ast.BinOp(left=ast.Constant(value=node.value[:2]),
                             op=ast.Add(),
                             right=ast.Constant(value=node.value[2:]))
        return node

    def visit_Assign(self, node):
        self.generic_visit(node)
        if self.kind != "alias":
            return node
        targets = [t.id for t in node.targets if isinstance(t, ast.Name)]
        if "card_ids" in targets and isinstance(node.value, (ast.List, ast.Tuple)):
            # Lift the table to module level and leave an alias behind, which is
            # the reformatting a maintainer is most likely to reach for.
            self.lifted.append(ast.Assign(
                targets=[ast.Name(id="LIFTED_TABLE", ctx=ast.Store())],
                value=node.value))
            node.value = ast.Name(id="LIFTED_TABLE", ctx=ast.Load())
        return node


def Mutate(source, kind):
    tree = ast.parse(source)
    mutation = Mutation(kind)
    tree = mutation.visit(tree)
    tree.body = mutation.lifted + tree.body
    ast.fix_missing_locations(tree)
    return ast.unparse(tree)


class TestReformattingTheSource(unittest.TestCase):
    """The same ids come out however the table is written.

    A scanner that matches one file's exact formatting is a hand-list, and the
    thing MARVEL-98 was told not to produce is a hand-list of the twc ids.
    """

    @classmethod
    def setUpClass(cls):
        if not os.path.exists(TWC):
            raise unittest.SkipTest("run from py_src/")
        with open(TWC, encoding="utf-8") as handle:
            cls.source = handle.read()
        cls.expected = literals.ScanSource(cls.source)

    def Check(self, kind):
        self.assertEqual(literals.ScanSource(Mutate(self.source, kind)),
                         self.expected, kind)

    def test_reflowed_onto_single_lines(self):
        # `ast.unparse` collapses every one of those four 15-line lists onto one
        # line. An AST walk cannot tell the difference; a regex over lines can.
        self.assertEqual(literals.ScanSource(ast.unparse(ast.parse(self.source))),
                         self.expected)

    def test_reordered(self):
        self.Check("reorder")

    def test_held_in_tuples(self):
        self.Check("tuple")

    def test_split_by_concatenation(self):
        self.Check("concat")

    def test_lifted_to_a_module_level_alias(self):
        self.Check("alias")

    def test_computed_ids_are_deliberately_not_followed(self):
        # The negative control for all of the above: the same file with every id
        # synthesised from two string halves yields nothing at all. This is the
        # boundary of the rule, stated as a test rather than as a promise.
        self.assertEqual(literals.ScanSource(Mutate(self.source, "computed")),
                         set())


if __name__ == "__main__":
    unittest.main()
