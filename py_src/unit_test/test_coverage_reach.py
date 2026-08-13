"""What a game setup could bring into play, and how a round is aimed at it.

Coverage reports say what a corpus *did* reach. `tools/coverage/reach.py` says
what it *could*, and the gap between them is what coverage-directed generation
steers by. A card no shipped setup contains is not a sampling failure that more
games would fix, which is the distinction MARVEL-16's "genuinely unreachable
remainder" turns on.

The measurement these tests protect: **3659 of 3781 scripted cards (96.8%) are
reachable**, leaving 122 that self-play cannot get to at any corpus size. It was
3447 (91.2%) before MARVEL-80 generated decks for the cards no shipped deck
names, and 3617 (95.7%) before MARVEL-98 read the ids out of the card scripts.

The map is a **lower bound**, and it has been badly wrong twice. It omitted
`player_deck`, the aspect-and-basic half of a starter deck and 25 of its 40
cards, which understated reachability by 756 and would have shipped as a
confident 71%. Then it missed the Wrecking Crew's whole encounter deck, which is
a Python list in a card script rather than anything in `data/`.

Both times what caught it was a corpus resolving cards the map called
unreachable, and the second time that check took weeks to act on because it only
*printed*. It is a gate now: `TestTheCrossCheckIsAGate` pins that an unexplained
card fails, and `TestEveryIdBearingKeyIsRead` is the guard that would have caught
the first defect before any corpus existed.
"""

import contextlib
import io
import json
import os
import shutil
import tempfile
import unittest

from tools.corpus.inventory import Inventory, Scenario
from tools.corpus.plan import Build, Directed
from tools.coverage import reach as reach_module


def Map(*sources):
    return reach_module.Map(sources=[
        reach_module.Source(kind, name, set(cards))
        for kind, name, cards in sources])


class TestIds(unittest.TestCase):

    def test_a_plain_id_comes_through(self):
        self.assertEqual(reach_module.Ids(["01094"]), {"01094"})

    def test_a_comma_joined_id_is_split(self):
        # `"01097a,01097b"` is one entry naming a two-sided card. A card that is
        # only ever the back of another is reached through the front, so the
        # split is meaning rather than tidying.
        self.assertEqual(reach_module.Ids(["01097a,01097b"]), {"01097a", "01097b"})

    def test_whitespace_and_blanks_are_dropped(self):
        self.assertEqual(reach_module.Ids([" 01094 , ", ""]), {"01094"})

    def test_a_missing_key_is_not_an_error(self):
        self.assertEqual(reach_module.Ids(None), set())

    def test_a_non_string_entry_is_skipped(self):
        self.assertEqual(reach_module.Ids([1, {"a": 1}, "01094"]), {"01094"})


class TestMap(unittest.TestCase):

    def setUp(self):
        self.reach = Map(
            ("hero", "thor", ["a", "b"]),
            ("hero", "hulk", ["b", "c"]),
            ("scenario", "rhino", ["x", "y"]),
        )

    def test_reachable_is_the_union(self):
        self.assertEqual(self.reach.reachable, {"a", "b", "c", "x", "y"})

    def test_a_card_knows_what_would_bring_it_in(self):
        self.assertEqual([s.name for s in self.reach.Sources("b")],
                         ["thor", "hulk"])

    def test_unreachable_is_what_nothing_names(self):
        self.assertEqual(self.reach.Unreachable(["a", "z"]), ["z"])

    def test_yield_ranks_by_how_much_is_still_wanted(self):
        self.assertEqual(self.reach.Yield("hero", {"c"}),
                         [("hulk", 1), ("thor", 0)])

    def test_yield_breaks_ties_by_name_so_a_plan_reproduces(self):
        self.assertEqual(self.reach.Yield("hero", set()),
                         [("hulk", 0), ("thor", 0)])


class TestReadFolder(unittest.TestCase):

    def setUp(self):
        self.folder = tempfile.mkdtemp(prefix="reach-")
        self.addCleanup(shutil.rmtree, self.folder, True)

    def Write(self, name, document):
        with open(os.path.join(self.folder, name), "w", encoding="utf-8") as handle:
            handle.write(document if isinstance(document, str)
                         else json.dumps(document))

    def test_it_collects_the_named_keys(self):
        self.Write("thor.json", {"hero": ["06001a,06001b"], "hero_deck": ["06002"],
                                 "ignored": ["99999"]})

        sources = reach_module.ReadFolder([self.folder], ("hero", "hero_deck"),
                                          "hero")

        self.assertEqual(len(sources), 1)
        self.assertEqual(sources[0].name, "thor")
        self.assertEqual(sources[0].cards, {"06001a", "06001b", "06002"})

    def test_a_file_that_will_not_parse_names_no_cards(self):
        # The engine fails on it far more loudly than this needs to.
        self.Write("broken.json", "{not json")

        self.assertEqual(reach_module.ReadFolder([self.folder], ("hero",), "hero"),
                         [])

    def test_a_missing_folder_is_not_an_error(self):
        self.assertEqual(
            reach_module.ReadFolder(["/nonexistent"], ("hero",), "hero"), [])

    def test_results_are_sorted_by_filename(self):
        self.Write("z.json", {"hero": ["1"]})
        self.Write("a.json", {"hero": ["2"]})

        sources = reach_module.ReadFolder([self.folder], ("hero",), "hero")

        self.assertEqual([source.name for source in sources], ["a", "z"])


class TestTheShippedData(unittest.TestCase):
    """Against the real `deck/` and `data/`, which is where the number lives."""

    @classmethod
    def setUpClass(cls):
        cls.reach = reach_module.Build()

    def test_every_kind_is_found(self):
        for kind in reach_module.KINDS:
            self.assertTrue(self.reach.Of(kind), kind)

    def test_kinds_names_every_kind_the_map_holds(self):
        # `Describe` and `Document` both walk `KINDS`. A kind added to `Build`
        # and not to `KINDS` would count toward `reachable` while vanishing from
        # the report and the JSON map -- the generated decks were exactly that
        # for the JSON until MARVEL-98 -- and the two would disagree with no
        # test failing.
        self.assertEqual({source.kind for source in self.reach.sources},
                         set(reach_module.KINDS))

    def test_there_are_sixty_three_starter_decks(self):
        self.assertEqual(len(self.reach.Of("hero")), 63)

    def test_a_known_hero_card_is_reachable_through_its_deck(self):
        # Thor's identity, from `deck/starter/thor.json`.
        self.assertIn("thor", [s.name for s in self.reach.Sources("06001a")])

    def test_a_known_villain_is_reachable_through_its_scenario(self):
        self.assertIn("rhino", [s.name for s in self.reach.Sources("01094")])

    def test_most_of_the_universe_is_reachable(self):
        # Measured at 96.8%. Held as a range rather than an exact number so a
        # card pack can be added without breaking the suite; what must not
        # happen silently is the *shape* moving, which means a source folder or
        # a key stopped being read.
        from engine.profile import coverage_report
        try:
            universe = coverage_report.LoadUniverse().cards
        except coverage_report.DatasetMissing:
            self.skipTest("datasets/cards/cards.json is not built")

        share = len(self.reach.reachable & set(universe)) / len(universe)

        self.assertGreater(share, 0.85,
            "reachability was measured at 91.2%; a big drop means a source "
            "folder or an id-bearing key stopped being read, which looks "
            "exactly like a smaller universe")
        self.assertLess(share, 1.0,
            "a remainder is expected -- these are the cards handed to "
            "hand-authored puzzle tests")

    def test_the_aspect_half_of_a_starter_deck_is_read(self):
        # `player_deck` holds 25 of a starter deck's 40 cards. Omitting it
        # understated reachability by 756 and is the reason this file exists in
        # its current shape.
        self.assertIn("player_deck", reach_module.HERO_KEYS)
        # `01050` Hulk is an Aggression ally: in no hero's own cards, in
        # several heroes' aspect halves.
        carriers = [source.name for source in self.reach.Sources("01050")]
        self.assertTrue(carriers, "the aspect pool is not being read at all")
        self.assertTrue(
            all(source.kind == "hero" for source in self.reach.Sources("01050")),
            carriers)

    def test_set_names_are_not_read_as_card_ids(self):
        # `encounter_sets` and `modular_sets` name other *files*, which are read
        # as sources in their own right. Feeding them to `Ids` would put set
        # names into the card id set and inflate reachability with things that
        # are not cards.
        for key in ("encounter_sets", "modular_sets"):
            self.assertNotIn(key, reach_module.SCENARIO_KEYS)
        self.assertNotIn("bomb_scare", self.reach.reachable)


class TestEveryIdBearingKeyIsRead(unittest.TestCase):
    """No source file may grow a key of card ids that nothing reads.

    This is the guard for the defect that shaped this module. `player_deck`
    was simply not in `HERO_KEYS`, and a missing key does not look like a bug --
    it looks like a smaller universe, and every number downstream is quietly
    wrong in the safe-sounding direction. Nothing failed; the map just said 71%
    when the answer was 91%.

    Two keys are known to hold something other than card ids and are listed as
    such rather than skipped silently, so adding a third is a decision.
    """

    # Hold the *names* of other files, which are read as sources in their own
    # right. Reading them as card ids would inflate reachability with set names.
    NOT_CARD_IDS = {"encounter_sets", "modular_sets"}

    def Keys(self, folders, ignore=()):
        """Every key in those folders whose value is a list of strings."""
        import glob

        found = set()
        for folder in folders:
            for path in glob.glob(os.path.join(folder, "*.json")):
                try:
                    with open(path, encoding="utf-8") as handle:
                        document = json.load(handle)
                except (OSError, ValueError):
                    continue
                if not isinstance(document, dict):
                    continue
                for key, value in document.items():
                    if isinstance(value, list) and any(
                            isinstance(item, str) for item in value):
                        found.add(key)
        return found - set(ignore) - self.NOT_CARD_IDS

    def Check(self, folders, read):
        missed = self.Keys(folders) - set(read)
        self.assertEqual(missed, set(),
            f"these keys hold card ids and nothing reads them: {sorted(missed)}. "
            "A missed key understates reachability without failing anything.")

    def test_hero_decks(self):
        self.Check(reach_module.HERO_FOLDERS, reach_module.HERO_KEYS)

    def test_encounter_sets(self):
        self.Check(reach_module.ENCOUNTER_FOLDERS, reach_module.ENCOUNTER_KEYS)

    def test_scenarios(self):
        self.Check(reach_module.SCENARIO_FOLDERS, reach_module.SCENARIO_KEYS)

    def test_the_check_would_have_caught_the_defect_it_exists_for(self):
        # Without `player_deck`, `test_hero_decks` must fail. A guard nobody has
        # seen fail is a guard nobody knows works.
        read = [key for key in reach_module.HERO_KEYS if key != "player_deck"]

        with self.assertRaises(AssertionError):
            self.Check(reach_module.HERO_FOLDERS, read)


class TestCardScriptsAreTheirOwnKind(unittest.TestCase):
    """The Wrecking Crew's encounter deck is Python, not data (MARVEL-98).

    `data/scenarios/the_wrecking_crew.json` has an empty `villain` and
    `encounters`; the four encounter decks are id lists in
    `cards/pack/twc/07001a.py`, handed to `CardFactory.GenerateCards` and
    `SetAsideDeck.Create`. A map built only from files calls 42 cards unreachable
    that every game of that scenario plays.

    The rule that finds them is `tools/coverage/literals.py` and is tested in
    `unit_test/test_coverage_literals.py`. What is tested here is the join: that
    those ids reach the map, and that they stay **separately attributable**.
    """

    @classmethod
    def setUpClass(cls):
        cls.reach = reach_module.Build()
        cls.files_only = reach_module.Build(scripts=False)

    def test_a_card_only_a_script_names_is_reachable(self):
        # `07005` Held Hostage: in no deck, set or scenario file.
        sources = self.reach.Sources("07005")
        self.assertEqual([source.name for source in sources], ["twc/07001a"])
        self.assertEqual([source.kind for source in sources], ["script-literal"])

    def test_without_the_scanner_that_card_is_unreachable(self):
        # The defect, reproduced. A guard nobody has seen fail is a guard nobody
        # knows works, and this is the one that moved 42 cards.
        self.assertNotIn("07005", self.files_only.reachable)

    def test_the_scanner_moved_the_whole_encounter_deck(self):
        from engine.profile import coverage_report
        try:
            universe = set(coverage_report.LoadUniverse().cards)
        except coverage_report.DatasetMissing:
            self.skipTest("datasets/cards/cards.json is not built")

        moved = (self.reach.reachable - self.files_only.reachable) & universe

        # 42 scripted cards, measured. A range, so a pack can be added; what
        # must not happen silently is this going to nothing, which is what a
        # scanner that stopped matching would look like.
        self.assertGreater(len(moved), 30)
        self.assertTrue(all(card_id.startswith("07") for card_id in moved),
                        sorted(moved))

    def test_it_is_not_folded_into_the_shipped_ceiling(self):
        # The same rule the generated decks are kept under: a single merged
        # number makes it impossible to say whether reach moved because the game
        # changed or because this tool got smarter.
        for kind in ("hero", "encounter-set", "scenario"):
            for source in self.reach.Of(kind):
                self.assertNotIn("07005", source.cards, source.name)

    def test_the_other_script_it_finds_adds_no_reach(self):
        # `sm/venom_goblin/27116a` maps a campaign log entry to the six Sinister
        # Six villains. They are the only non-`twc` cards the scanner picks up,
        # and every one of them is already named by a data file -- which is the
        # evidence that the twc shape is the outlier rather than the norm.
        for card_id in ("27158", "27159", "27160", "27161", "27162", "27163"):
            self.assertIn(card_id, self.files_only.reachable, card_id)

    def test_a_script_source_appears_in_the_json_map(self):
        from engine.profile import coverage_report
        try:
            universe = coverage_report.LoadUniverse()
        except coverage_report.DatasetMissing:
            self.skipTest("datasets/cards/cards.json is not built")

        document = reach_module.Document(self.reach, universe)

        self.assertIn("script-literal", document["sources"])
        self.assertIn("07005", document["sources"]["script-literal"]["twc/07001a"])


class TestTheCrossCheckIsAGate(unittest.TestCase):
    """A card played that the map calls unreachable now fails the command.

    It used to print. That is how 45 `twc` cards came to sit on the unreachable
    list while this file's own docstring described 26 of them as known: nothing
    failed, so nothing had to be done. The line between the two is not the
    number, it is the exit code.

    A hard invariant is not available -- `tough`, `stunned` and `confused` are
    created by the engine, named by no file, and played in almost every game. So
    the gate is an allowlist with a reason per entry: a known hole passes, and a
    new one fails.
    """

    def Corpus(self, resolved):
        """A folder holding one coverage artefact that resolved those cards."""
        folder = tempfile.mkdtemp(prefix="reach-corpus-")
        self.addCleanup(shutil.rmtree, folder, True)
        document = {"coverage_version": 1, "games": [{
            "seed": 1, "scenario": "x", "heroes": ["a"],
            "cards": {"present": sorted(resolved), "entered_play": {},
                      "resolved": {card_id: 1 for card_id in resolved}},
            "factories": {}, "triggers": {}, "stages": {}, "ability_types": {}}]}
        path = os.path.join(folder, "bot-coverage-x-a-1-1.json")
        with open(path, "w", encoding="utf-8") as handle:
            json.dump(document, handle)
        return folder

    def Universe(self):
        from engine.profile import coverage_report
        try:
            return coverage_report.LoadUniverse()
        except coverage_report.DatasetMissing:
            self.skipTest("datasets/cards/cards.json is not built")

    def AnUnreachableCard(self, reach, universe):
        remaining = reach.Unreachable(universe.cards)
        remaining = [c for c in remaining if c not in reach_module.ENGINE_CREATED]
        self.assertTrue(remaining, "nothing is unreachable to test with")
        return remaining[0]

    # -- the rule on its own, with no corpus on disk --------------------------

    def test_an_allowlisted_card_is_explained(self):
        self.assertEqual(reach_module.Unexplained(["tough", "stunned"]), [])

    def test_anything_else_is_not(self):
        self.assertEqual(reach_module.Unexplained(["tough", "07005"]), ["07005"])

    def test_every_allowlist_entry_carries_a_reason(self):
        # The allowlist is the waiver, so an entry with no reason is a hand-list
        # of ids -- which is the shape MARVEL-98 was told not to produce.
        for card_id, reason in reach_module.ENGINE_CREATED.items():
            self.assertTrue(reason.strip(), card_id)
            self.assertGreater(len(reason), 20, card_id)

    def test_the_allowlist_is_only_the_engine_created_cards(self):
        # Three status cards. If this grows, someone widened the waiver, and
        # that is a decision rather than a detail.
        self.assertEqual(sorted(reach_module.ENGINE_CREATED),
                         ["confused", "stunned", "tough"])

    # -- end to end, over a corpus -------------------------------------------

    def test_a_corpus_that_only_played_allowlisted_cards_passes(self):
        universe = self.Universe()
        reach = reach_module.Build()

        check = reach_module.CrossCheck(
            reach, universe, self.Corpus(["tough", "stunned", "07005"]))

        self.assertTrue(check.ok, check.lines)

    def test_a_corpus_that_played_something_unexplained_fails(self):
        universe = self.Universe()
        reach = reach_module.Build()
        surprise = self.AnUnreachableCard(reach, universe)

        check = reach_module.CrossCheck(
            reach, universe, self.Corpus(["tough", surprise]))

        self.assertFalse(check.ok)
        self.assertEqual(check.unexplained, [surprise])
        self.assertTrue(any("FAIL" in line for line in check.lines), check.lines)

    def test_the_twc_deck_would_have_failed_before_the_scanner(self):
        # The defect this issue is, run as a test: the same corpus that passes
        # against the map fails against the file-only one.
        universe = self.Universe()
        corpus = self.Corpus(["tough", "07005", "07020"])

        self.assertTrue(reach_module.CrossCheck(
            reach_module.Build(), universe, corpus).ok)
        self.assertEqual(reach_module.CrossCheck(
            reach_module.Build(scripts=False), universe, corpus).unexplained,
            ["07005", "07020"])

    def test_a_corpus_with_no_artefacts_is_a_failure(self):
        # "The gate found no hole" and "the gate never ran" must not look alike.
        # `-verify_replays` settles an empty folder the same way.
        universe = self.Universe()
        folder = tempfile.mkdtemp(prefix="reach-empty-")
        self.addCleanup(shutil.rmtree, folder, True)

        check = reach_module.CrossCheck(reach_module.Build(), universe, folder)

        self.assertFalse(check.ok)

    def test_the_command_exits_non_zero(self):
        universe = self.Universe()
        surprise = self.AnUnreachableCard(reach_module.Build(), universe)
        buffer = io.StringIO()

        with contextlib.redirect_stdout(buffer):
            code = reach_module.main(["--corpus", self.Corpus(["tough", surprise])])

        self.assertEqual(code, 1, buffer.getvalue())
        self.assertIn(surprise, buffer.getvalue())

    def test_the_command_exits_zero_when_every_hole_is_explained(self):
        self.Universe()
        buffer = io.StringIO()

        with contextlib.redirect_stdout(buffer):
            code = reach_module.main(["--corpus", self.Corpus(["tough"])])

        self.assertEqual(code, 0, buffer.getvalue())


class TestDirectedPlanning(unittest.TestCase):
    """Greedy set cover: aim a round at what nothing has played."""

    def Inventory(self, scenarios, heroes):
        return Inventory(scenarios=[Scenario(n) for n in scenarios],
                         heroes=list(heroes))

    def test_the_highest_yield_scenario_is_chosen_first(self):
        reach = Map(("scenario", "poor", ["p"]),
                    ("scenario", "rich", ["a", "b", "c"]),
                    ("hero", "thor", ["h"]))
        plan = Build(self.Inventory(["poor", "rich"], ["thor"]),
                     seed=1, games=1, floor=0, player_counts=[1],
                     targets=["a", "b", "c", "p"], reach=reach)

        self.assertEqual(plan.cases[0].scenario, "rich")
        self.assertEqual(plan.cases[0].phase, "coverage-directed")

    def test_heroes_are_chosen_for_what_they_carry(self):
        reach = Map(("scenario", "rhino", ["x"]),
                    ("hero", "empty", []),
                    ("hero", "carrier", ["want"]))
        plan = Build(self.Inventory(["rhino"], ["empty", "carrier"]),
                     seed=1, games=1, floor=0, player_counts=[1],
                     targets=["want"], reach=reach)

        self.assertEqual(plan.cases[0].heroes, ("carrier",))

    def test_a_seat_with_nothing_left_to_bring_falls_back_to_least_used(self):
        # Not to whatever `Yield` lists first: ties there break alphabetically,
        # so a directed round would otherwise seat one hero in every game and
        # stop covering heroes at all. Measured before the fix: one hero in 36
        # of 60 games.
        reach = Map(("scenario", "rhino", []),
                    *[("hero", f"hero_{n:02d}", []) for n in range(8)])
        plan = Build(self.Inventory(["rhino"], [f"hero_{n:02d}" for n in range(8)]),
                     seed=1, games=8, floor=0, player_counts=[2],
                     targets=["unreachable-by-anything"], reach=reach)

        _, heroes, _ = plan.Counts()
        self.assertGreater(len(heroes), 2)

    def test_it_stops_when_nothing_more_can_be_aimed_at(self):
        # The plateau, at plan level: more games would still be games, they
        # would just no longer be directed. The caller fills the rest at random.
        reach = Map(("scenario", "rhino", ["a"]), ("hero", "thor", []))
        plan = Build(self.Inventory(["rhino"], ["thor"]),
                     seed=1, games=20, floor=0, player_counts=[1],
                     targets=["a"], reach=reach)

        directed = [case for case in plan.cases if case.phase == "coverage-directed"]
        self.assertEqual(len(directed), 1)
        self.assertTrue([case for case in plan.cases if case.phase == "random"])

    def test_no_targets_means_no_directed_phase(self):
        plan = Build(self.Inventory(["rhino"], ["thor"]),
                     seed=1, games=4, floor=0, targets=[], reach=Map())

        self.assertEqual(
            [case for case in plan.cases if case.phase == "coverage-directed"], [])

    def test_a_directed_plan_is_still_reproducible(self):
        reach = Map(("scenario", "rhino", ["a"]), ("scenario", "klaw", ["b"]),
                    ("hero", "thor", ["c"]), ("hero", "hulk", ["d"]))
        args = dict(seed=4, games=6, floor=0, targets=["a", "b", "c", "d"],
                    reach=reach)

        self.assertEqual(
            Build(self.Inventory(["rhino", "klaw"], ["thor", "hulk"]), **args).Digest(),
            Build(self.Inventory(["rhino", "klaw"], ["thor", "hulk"]), **args).Digest())

    def test_directed_cases_still_count_toward_the_hero_floor(self):
        # Heroes picked for what they carry are still heroes played. Not
        # counting them would make a directed round report a floor it had not
        # met.
        reach = Map(("scenario", "rhino", []), ("hero", "carrier", ["want"]),
                    ("hero", "other", []))
        inventory = self.Inventory(["rhino"], ["carrier", "other"])
        plan = Build(inventory, seed=1, games=6, floor=0, player_counts=[1],
                     targets=["want"], reach=reach)

        directed = [case for case in plan.cases
                    if case.phase == "coverage-directed"]
        self.assertEqual([case.heroes for case in directed], [("carrier",)])
        # The random fill that follows draws least-used-first, and it can only
        # do that if the directed case was banked.
        self.assertEqual(
            [case.heroes for case in plan.cases
             if case.phase == "random"][0], ("other",))

    def test_the_budget_is_respected(self):
        reach = Map(*[("scenario", f"s{n}", [f"card_{n}"]) for n in range(50)],
                    ("hero", "thor", []))
        plan = Build(self.Inventory([f"s{n}" for n in range(50)], ["thor"]),
                     seed=1, games=5, floor=0, player_counts=[1],
                     targets=[f"card_{n}" for n in range(50)], reach=reach)

        self.assertEqual(plan.games, 5)


class TestDirectedDirectly(unittest.TestCase):
    """`Directed` returns how much of the budget it used, so the caller can
    fill the rest."""

    def Builder(self, inventory):
        import random
        from tools.corpus.plan import Builder

        return Builder(random.Random(1), inventory, seed_base=0,
                       player_counts=[1])

    def test_a_zero_budget_plays_nothing(self):
        inventory = Inventory([Scenario("rhino")], ["thor"])
        builder = self.Builder(inventory)

        used = Directed(builder, inventory, ["a"],
                        Map(("scenario", "rhino", ["a"])), budget=0)

        self.assertEqual(used, 0)
        self.assertEqual(builder.cases, [])

    def test_it_reports_what_it_used(self):
        inventory = Inventory([Scenario("rhino"), Scenario("klaw")], ["thor"])
        builder = self.Builder(inventory)

        used = Directed(builder, inventory, ["a", "b"],
                        Map(("scenario", "rhino", ["a"]),
                            ("scenario", "klaw", ["b"]),
                            ("hero", "thor", [])), budget=10)

        self.assertEqual(used, 2)
        self.assertEqual(len(builder.cases), 2)


################################################################################
#

class TestResolvedShareCountsBothSets(unittest.TestCase):
    """The cross-check's ratio must not mix populations.

    A card this map calls unreachable and the corpus played anyway is a hole in
    the map, reported on its own line. It belongs to neither side of "how much
    of what is reachable did the corpus resolve", and putting it in the
    numerator alone read 82.4% where the answer was 81.1% -- with 44 such cards
    out of 3,617. The failure mode is silent and always flattering, which is the
    one direction a coverage figure must never drift.
    """

    def test_a_card_played_but_called_unreachable_counts_on_neither_side(self):
        share = reach_module.ResolvedShare(resolved={"a", "b", "x"},
                                           reachable={"a", "b", "c", "d"})
        self.assertEqual(share, 0.5)

    def test_the_share_cannot_exceed_one(self):
        # The shape the old arithmetic would have printed above 100%: a map with
        # more holes than reachable cards.
        share = reach_module.ResolvedShare(resolved={"a", "x", "y", "z"},
                                           reachable={"a", "b"})
        self.assertLessEqual(share, 1.0)
        self.assertEqual(share, 0.5)

    def test_resolving_everything_reachable_is_one(self):
        self.assertEqual(
            reach_module.ResolvedShare({"a", "b"}, {"a", "b"}), 1.0)

    def test_an_empty_map_does_not_divide_by_zero(self):
        self.assertEqual(reach_module.ResolvedShare({"a"}, set()), 0.0)


if __name__ == "__main__":
    unittest.main()
