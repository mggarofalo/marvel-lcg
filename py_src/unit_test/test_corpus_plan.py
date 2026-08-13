"""Which games a corpus run plays, and how a killed run picks up again.

The configuration space cannot be enumerated -- 108 scenarios against 63 heroes
against four player counts, before encounter sets -- so the sampling strategy is
the deliverable, and these tests are about that strategy rather than about the
games. See MARVEL-15.

Three properties carry the design:

- a plan is a **pure function** of (seed, sizes, inventory), which is what makes
  "regenerate the identical corpus" checkable without playing anything;
- the **floor** is a guarantee, not a target -- every scenario and every hero
  appears at least `floor` times even when that costs more games than were
  asked for, and the plan says so rather than quietly dropping coverage;
- heroes are drawn **least-used-first**, because a uniform draw over 63 heroes
  leaves the tail badly under-sampled at any realistic corpus size, and the tail
  is where port bugs hide.

Nothing here starts an engine or plays a game. `tools/corpus/generate.py` does
that, and what is testable about it without a corpus is its resume bookkeeping
and its reading of what a worker said on the way out (`TestCrashSignature*`,
MARVEL-97).
"""

import json
import os
import shutil
import tempfile
import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported. Nothing here boots it; the crash reporter
# is imported so the parser can be held against the format it actually emits.
import engine  # noqa: F401  pylint: disable=unused-import

from engine.device.manager.bot import crash
from engine.device.manager.bot.crash import CrashCollector, Occurrence
from tools.corpus import generate, inventory
from tools.corpus import signature as signature_module
from tools.corpus.inventory import Inventory, Scenario, Subset
from tools.corpus.plan import Build, Summarize


def Stock(scenarios=("rhino", "klaw", "ultron"), heroes=("spider_man", "thor")):
    return Inventory(scenarios=[Scenario(name) for name in scenarios],
                     heroes=list(heroes))


class TestInventory(unittest.TestCase):

    def test_expert_is_recognised_by_its_suffix(self):
        self.assertTrue(Scenario("rhino_expert").is_expert)
        self.assertFalse(Scenario("rhino").is_expert)

    def test_an_expert_scenario_knows_its_standard_form(self):
        self.assertEqual(Scenario("rhino_expert").standard, "rhino")

    def test_a_standard_scenario_is_its_own_standard_form(self):
        self.assertEqual(Scenario("rhino").standard, "rhino")

    def test_the_digest_moves_when_the_inventory_does(self):
        # A plan is only reproducible against the inventory it was drawn from:
        # add a scenario file and the same seed picks different games.
        self.assertNotEqual(Stock().Digest(),
                            Stock(scenarios=("rhino", "klaw")).Digest())

    def test_the_digest_does_not_move_otherwise(self):
        self.assertEqual(Stock().Digest(), Stock().Digest())

    def test_a_subset_keeps_only_what_was_named(self):
        narrow = Subset(Stock(), scenarios=["rhino"], heroes=["thor"])

        self.assertEqual(narrow.scenario_names, ["rhino"])
        self.assertEqual(narrow.heroes, ["thor"])

    def test_an_empty_subset_means_everything(self):
        self.assertEqual(Subset(Stock()).scenario_names, Stock().scenario_names)

    def test_an_unknown_name_is_dropped_rather_than_raising(self):
        # So `Check` can report every problem at once instead of the first.
        self.assertEqual(Subset(Stock(), scenarios=["nope"]).scenario_names, [])


class TestPlanIsPure(unittest.TestCase):

    def test_the_same_inputs_give_the_same_plan(self):
        a = Build(Stock(), seed=5, games=20)
        b = Build(Stock(), seed=5, games=20)

        self.assertEqual(a.Digest(), b.Digest())
        self.assertEqual([case.ToDict() for case in a.cases],
                         [case.ToDict() for case in b.cases])

    def test_a_different_seed_gives_a_different_plan(self):
        self.assertNotEqual(Build(Stock(), seed=5, games=20).Digest(),
                            Build(Stock(), seed=6, games=20).Digest())

    def test_a_different_inventory_gives_a_different_plan(self):
        self.assertNotEqual(
            Build(Stock(), seed=5, games=20).Digest(),
            Build(Stock(scenarios=("rhino", "klaw")), seed=5, games=20).Digest())

    def test_an_empty_inventory_is_refused(self):
        with self.assertRaises(ValueError):
            Build(Inventory([], []), seed=1, games=10)

    def test_no_usable_player_count_is_refused(self):
        with self.assertRaises(ValueError):
            Build(Stock(), seed=1, games=10, player_counts=[0, -1])


class TestSeeds(unittest.TestCase):

    def test_every_game_gets_its_own_seed(self):
        plan = Build(Stock(), seed=100, games=40)

        seeds = [seed for case in plan.cases
                 for seed in range(case.seed, case.seed + case.games)]

        self.assertEqual(len(seeds), len(set(seeds)))
        self.assertEqual(len(seeds), plan.games)

    def test_the_seeds_are_a_contiguous_run_from_the_plan_seed(self):
        plan = Build(Stock(), seed=100, games=40)

        self.assertEqual(
            sorted(seed for case in plan.cases
                   for seed in range(case.seed, case.seed + case.games)),
            list(range(100, 100 + plan.games)))

    def test_the_seed_base_can_be_moved_off_the_plan_seed(self):
        # So two corpora planned from the same seed can be kept apart.
        plan = Build(Stock(), seed=100, games=10, seed_base=9000)

        self.assertEqual(min(case.seed for case in plan.cases), 9000)


class TestTheFloorIsAGuarantee(unittest.TestCase):

    def Counts(self, plan):
        return plan.Counts()

    def test_every_scenario_appears_at_least_floor_times(self):
        plan = Build(Stock(), seed=3, games=30, floor=2)
        scenarios, _, _ = self.Counts(plan)

        self.assertEqual(len(scenarios), 3)
        self.assertGreaterEqual(min(scenarios.values()), 2)

    def test_every_hero_appears_at_least_floor_times(self):
        plan = Build(Stock(heroes=tuple(f"hero_{n}" for n in range(20))),
                     seed=3, games=30, floor=2)
        _, heroes, _ = self.Counts(plan)

        self.assertEqual(len(heroes), 20)
        self.assertGreaterEqual(min(heroes.values()), 2)

    def test_the_floor_beats_a_game_budget_that_is_too_small(self):
        # 40 scenarios cannot be covered by 5 games. Coverage wins, because a
        # corpus that silently dropped 35 scenarios to hit a game count is the
        # corpus this whole tool exists to avoid.
        plan = Build(Stock(scenarios=tuple(f"s{n}" for n in range(40))),
                     seed=3, games=5, floor=1)
        scenarios, _, _ = self.Counts(plan)

        self.assertEqual(len(scenarios), 40)
        self.assertGreater(plan.games, 5)

    def test_and_says_so_rather_than_only_doing_it(self):
        plan = Build(Stock(scenarios=tuple(f"s{n}" for n in range(40))),
                     seed=3, games=5, floor=1)

        self.assertTrue(plan.Warnings())
        self.assertIn("rather than the 5 requested", plan.Warnings()[0])

    def test_a_plan_that_fits_its_budget_warns_about_nothing(self):
        self.assertEqual(Build(Stock(), seed=3, games=60, floor=1).Warnings(), [])

    def test_the_hero_phase_only_runs_when_it_has_to(self):
        # Heroes ride along in the scenario sweep and mostly even out by
        # themselves. The top-up exists for runs too small for that.
        big = Build(Stock(scenarios=tuple(f"s{n}" for n in range(40))),
                    seed=3, games=60, floor=1)
        small = Build(Stock(scenarios=("rhino",),
                            heroes=tuple(f"hero_{n}" for n in range(20))),
                      seed=3, games=2, floor=1)

        self.assertEqual([c for c in big.cases if c.phase == "hero-coverage"], [])
        self.assertTrue([c for c in small.cases if c.phase == "hero-coverage"])


class TestHeroesAreDrawnLeastUsedFirst(unittest.TestCase):

    def test_every_hero_plays_once_before_any_plays_twice(self):
        heroes = tuple(f"hero_{n}" for n in range(12))
        plan = Build(Stock(scenarios=tuple(f"s{n}" for n in range(12)),
                           heroes=heroes),
                     seed=11, games=12, floor=1, player_counts=[2])
        _, counts, _ = plan.Counts()

        self.assertEqual(len(counts), 12)
        self.assertLessEqual(max(counts.values()) - min(counts.values()), 1)

    def test_ties_are_broken_randomly_rather_than_alphabetically(self):
        # A stable sort over a fixed list would hand out heroes in name order
        # for ever and put `hero_00` in every game.
        heroes = tuple(f"hero_{n:02d}" for n in range(12))
        first = [Build(Stock(heroes=heroes), seed=seed, games=1,
                       floor=0, player_counts=[1]).cases[0].heroes[0]
                 for seed in range(1, 15)]

        self.assertGreater(len(set(first)), 1)


class TestPlayerCounts(unittest.TestCase):

    def test_the_counts_are_cycled_rather_than_sampled(self):
        # Player count is the axis most likely to be systematically
        # under-covered by bad luck, so the distribution is made exact.
        plan = Build(Stock(scenarios=tuple(f"s{n}" for n in range(40)),
                           heroes=tuple(f"hero_{n}" for n in range(12))),
                     seed=3, games=40, floor=1)
        _, _, players = plan.Counts()

        self.assertEqual(sorted(players), [1, 2, 3, 4])
        self.assertLessEqual(max(players.values()) - min(players.values()), 1)

    def test_a_narrowed_set_is_respected(self):
        plan = Build(Stock(heroes=tuple(f"hero_{n}" for n in range(12))),
                     seed=3, games=20, player_counts=[1, 4])
        _, _, players = plan.Counts()

        self.assertEqual(sorted(players), [1, 4])

    def test_a_count_larger_than_the_roster_is_clamped(self):
        plan = Build(Stock(heroes=("thor",)), seed=3, games=4,
                     player_counts=[4])

        self.assertTrue(all(case.players == 1 for case in plan.cases))


class TestBatching(unittest.TestCase):

    def test_the_coverage_phases_play_one_game_per_case(self):
        # Their purpose is breadth. Batching there would multiply the floor by
        # the batch size.
        plan = Build(Stock(scenarios=tuple(f"s{n}" for n in range(20))),
                     seed=3, games=100, floor=1, games_per_case=4)

        self.assertTrue(all(case.games == 1 for case in plan.cases
                            if case.phase != "random"))

    def test_the_random_phase_batches(self):
        # Engine start is ~0.5s against ~0.2-2s for a game, so a process that
        # plays one game spends most of its life importing.
        plan = Build(Stock(), seed=3, games=100, floor=1, games_per_case=4)
        random_cases = [case for case in plan.cases if case.phase == "random"]

        self.assertTrue(random_cases)
        self.assertTrue(all(case.games <= 4 for case in random_cases))
        self.assertGreater(max(case.games for case in random_cases), 1)

    def test_the_last_batch_does_not_overshoot_the_budget(self):
        plan = Build(Stock(), seed=3, games=61, floor=1, games_per_case=4)

        self.assertEqual(plan.games, 61)


class TestCaseIdentity(unittest.TestCase):

    def test_the_id_is_built_from_what_decides_the_games(self):
        case = Build(Stock(), seed=3, games=10).cases[0]

        self.assertIn(case.scenario, case.id)
        self.assertIn(str(case.seed), case.id)

    def test_reordering_a_plan_does_not_invalidate_a_half_finished_corpus(self):
        # The id deliberately excludes `index`, so a plan whose cases move
        # still recognises what has already been played.
        plan = Build(Stock(), seed=3, games=10)
        ids = {case.id for case in plan.cases}
        shuffled = list(reversed(plan.cases))

        self.assertEqual({case.id for case in shuffled}, ids)

    def test_ids_are_unique_across_a_plan(self):
        plan = Build(Stock(), seed=3, games=200)

        self.assertEqual(len({case.id for case in plan.cases}), len(plan.cases))


class TestSummary(unittest.TestCase):

    def test_it_reports_coverage_and_the_digest(self):
        text = "\n".join(Summarize(Build(Stock(), seed=3, games=20)))

        self.assertIn("scenarios covered 3", text)
        self.assertIn("plan digest", text)

    def test_a_warning_is_part_of_the_summary(self):
        text = "\n".join(Summarize(
            Build(Stock(scenarios=tuple(f"s{n}" for n in range(40))),
                  seed=3, games=5)))

        self.assertIn("rather than the 5 requested", text)


class TestResume(unittest.TestCase):
    """What a killed run reads when it starts again."""

    def Write(self, lines):
        folder = tempfile.mkdtemp(prefix="corpus-resume-")
        self.addCleanup(shutil.rmtree, folder, True)
        path = os.path.join(folder, generate.PROGRESS_NAME)
        with open(path, "w", encoding="utf-8") as handle:
            handle.write(lines)
        return path

    def test_no_progress_file_is_not_an_error(self):
        self.assertEqual(generate.ReadProgress("/nonexistent/progress.jsonl"), {})

    def test_finished_cases_come_back_keyed_by_id(self):
        path = self.Write(json.dumps({"id": "a|b|1|1", "status": "ok"}) + "\n")

        self.assertEqual(list(generate.ReadProgress(path)), ["a|b|1|1"])

    def test_a_truncated_last_line_costs_exactly_one_case(self):
        # The file is appended to by a process that can be killed at any
        # moment, so a half-written last line is an expected state.
        path = self.Write(
            json.dumps({"id": "first", "status": "ok"}) + "\n"
            + json.dumps({"id": "second", "status": "ok"}) + "\n"
            + '{"id": "third", "stat')

        self.assertEqual(sorted(generate.ReadProgress(path)), ["first", "second"])

    def test_a_line_with_no_id_is_ignored(self):
        path = self.Write(json.dumps({"status": "ok"}) + "\n")

        self.assertEqual(generate.ReadProgress(path), {})

    def test_blank_lines_are_ignored(self):
        path = self.Write("\n\n" + json.dumps({"id": "a", "status": "ok"}) + "\n\n")

        self.assertEqual(list(generate.ReadProgress(path)), ["a"])


class TestThroughput(unittest.TestCase):

    def test_only_finished_games_count(self):
        outcomes = [
            {"games": 4, "seconds": 8.0, "status": "ok"},
            {"games": 4, "seconds": 8.0, "status": "failed"},
        ]
        timing = generate.Throughput(outcomes, wall_seconds=10.0, workers=2)

        self.assertEqual(timing["games"], 4)

    def test_per_worker_throughput_uses_cpu_time_not_wall_time(self):
        # The two answer different questions: wall says how long a corpus of
        # size N takes here, per-worker says what more machines would buy.
        outcomes = [{"games": 1, "seconds": 3600.0, "status": "ok"}] * 4
        timing = generate.Throughput(outcomes, wall_seconds=3600.0, workers=4)

        self.assertEqual(timing["games_per_hour"], 4.0)
        self.assertEqual(timing["games_per_hour_per_worker"], 1.0)

    def test_a_run_that_took_no_time_does_not_divide_by_zero(self):
        timing = generate.Throughput([], wall_seconds=0.0, workers=1)

        self.assertEqual(timing["games_per_hour"], 0)
        self.assertEqual(timing["games_per_hour_per_worker"], 0)


class TestWorkerCommand(unittest.TestCase):

    def Command(self, **kwargs):
        case = Build(Stock(), seed=3, games=10, **kwargs).cases[0]
        return case, generate.Command(case, "/out", [])

    def test_it_passes_the_whole_case(self):
        case, command = self.Command()

        self.assertIn("-bot_scenario", command)
        self.assertEqual(command[command.index("-bot_scenario") + 1], case.scenario)
        self.assertEqual(command[command.index("-bot_seed") + 1], str(case.seed))
        self.assertEqual(command[command.index("-bot_games") + 1], str(case.games))

    def test_heroes_are_separate_arguments(self):
        # Not one comma-joined string: `ListStr` reads consecutive values, and
        # a joined one would become a single hero name nothing can resolve.
        case = Build(Stock(), seed=3, games=40, player_counts=[2]).cases[0]
        command = generate.Command(case, "/out", [])

        start = command.index("-bot_heroes") + 1
        self.assertEqual(command[start:start + 2], list(case.heroes))

    def DeckFolders(self, command):
        start = command.index("-deck_folders") + 1
        folders = []
        for value in command[start:]:
            if value.startswith("-"):
                break
            folders.append(value)
        return folders

    def test_every_hero_the_plan_can_name_is_findable_from_the_command(self):
        """MARVEL-80. The plan sampled decks the engine could not find.

        `deck_folders` defaults to `["./deck/"]` alone; the generated decks live
        in `./deck/generated/`; and the plan has included them since the builder
        landed. A case whose hero was one of them died at step 0 with an
        `AssertionError` in `ReadJson` -- a crash report rather than a
        missing-deck message, which is how a whole tier of a planned corpus went
        ungenerated with nothing saying so.

        Stated as the property that actually matters rather than as a literal
        argument list: every hero the inventory can hand to a case must be
        reachable under the lookup the engine will actually perform. A deck
        folder added to one side and not the other then fails here, in
        milliseconds, instead of at step 0 of a long run.

        The lookup is modelled the way `FileManager` does it (`file/manager.py`
        around line 216): a **flat** join of the filename over
        `deck_folders + [starter_deck_folder]`. It does not recurse, which is
        the whole reason `./deck/` alone did not reach `./deck/generated/` --
        and a test that walked the tree recursively would pass on the broken
        command and prove nothing.
        """
        if not os.path.isdir("deck"):
            self.skipTest("run from py_src/")

        folders = self.DeckFolders(self.Command()[1]) + ["./deck/starter"]
        missing = [hero for hero in inventory.Read().heroes
                   if not any(os.path.isfile(os.path.join(folder, hero + ".json"))
                              for folder in folders)]
        self.assertEqual(missing, [],
                         f"the plan can name these and {folders} does not hold them")

    def test_the_generated_decks_are_among_them(self):
        """The regression this exists for, named.

        Without it the test above still passes on a tree with no generated decks
        on disk -- which is exactly the state the bug hid in.
        """
        if not os.path.isdir(inventory.GENERATED_HERO_FOLDER):
            self.skipTest("no generated decks on disk")

        generated = [h for h in inventory.Read().heroes
                     if h.startswith("generated_")]
        self.assertTrue(generated)

        folders = self.DeckFolders(self.Command()[1])
        for hero in generated:
            with self.subTest(hero=hero):
                self.assertTrue(
                    any(os.path.isfile(os.path.join(folder, hero + ".json"))
                        for folder in folders),
                    f"{hero} is in the plan and under none of {folders}")

    def test_extra_flags_come_last_so_they_win(self):
        case = Build(Stock(), seed=3, games=10).cases[0]
        command = generate.Command(case, "/out", ["-check_invariants"])

        self.assertEqual(command[-1], "-check_invariants")
        self.assertGreater(command.index("-check_invariants"),
                           command.index("-no_check_invariants"))


################################################################################
#
# What a failed case says about itself. See MARVEL-97.
#
# `generate.py` kept the last 600 characters of a worker's output as `detail`.
# The crash summary the worker prints is above that cut whenever a traceback
# precedes it, which is most of the time: of 98 failures in one 321-case run,
# 69 recorded nothing but the middle of a stack. The signature is parsed out
# and stored as a field now, because a field is bounded and a tail is not.

# Verbatim from a failed case in the 321-case run behind MARVEL-97, colour and
# log level included. The summary reaches us through the log, so neither is
# optional and neither is part of the report.
REAL_OUTPUT = (
    "\x1b[32m<I> Saved: ./crashes/bot-crash-timeout-stall-4083f456.json\x1b[0m\n"
    "\x1b[33m<W> 1 failure(s), 1 distinct signature(s)\x1b[0m\n"
    "\x1b[33m<W>   4083f456  timeout-stall        x1 in 1 game(s)  "
    "[seed 30 step 20000]  Game was cut short by bot_max_steps (20000)\x1b[0m\n"
    "\x1b[33m<W> Crash report: ./crashes/bot-crashes-black_widow_expert-"
    "nebula+falcon+adam_warlock+nick_fury-30-1.json\x1b[0m\n"
    "\n--- Engine Shutdown ---\n")


def Summary(lines, failures=None, signatures=None):
    """A worker's output ending in a crash summary of `lines`."""
    header = (f"<W> {failures if failures is not None else len(lines)} "
              f"failure(s), "
              f"{signatures if signatures is not None else len(lines)} "
              f"distinct signature(s)")
    return "\n".join(["<I> starting", header] + list(lines) + ["", "done"])


def Line(sig="4083f456", kind="timeout-stall", occurrences=1, games=1,
         where="seed 30 step 20000", title="Game was cut short"):
    return (f"<W>   {sig}  {kind:<20} x{occurrences} in {games} game(s)  "
            f"[{where}]  {title}")


class TestCrashSignatureParsing(unittest.TestCase):
    """The parser, against everything a worker can actually print."""

    def test_a_normal_line_yields_every_field(self):
        scan = signature_module.Parse(Summary([Line()]))

        self.assertEqual(scan.status, signature_module.PARSED)
        self.assertEqual(len(scan.entries), 1)
        entry = scan.entries[0]
        self.assertEqual(entry.signature, "4083f456")
        self.assertEqual(entry.kind, "timeout-stall")
        self.assertEqual(entry.seed, 30)
        self.assertEqual(entry.step, 20000)
        self.assertEqual(entry.title, "Game was cut short")
        self.assertEqual((entry.occurrences, entry.games), (1, 1))

    def test_the_log_prefix_and_colour_are_not_part_of_the_report(self):
        # Captured from a real run: this is the exact byte sequence the
        # manifest was throwing away.
        scan = signature_module.Parse(REAL_OUTPUT)

        self.assertEqual(scan.status, signature_module.PARSED)
        self.assertEqual([entry.signature for entry in scan.entries],
                         ["4083f456"])
        self.assertEqual(scan.entries[0].kind, "timeout-stall")
        self.assertEqual(scan.entries[0].step, 20000)
        self.assertEqual(scan.entries[0].title,
                         "Game was cut short by bot_max_steps (20000)")

    def test_several_signatures_are_all_kept_in_the_order_reported(self):
        # A case plays several games and can find several bugs. Keeping the
        # first would discard exactly what makes a case worth reconstructing,
        # and the child's order is already meaningful -- most occurrences
        # first, the hash breaking ties.
        text = Summary([
            Line(sig="976320f8", kind="engine-assert", occurrences=7,
                 where="seed 26 step 0", title="AssertionError at a.py:B"),
            Line(sig="4083f456", occurrences=2, games=2),
            Line(sig="0badbeef", kind="unhandled-exception",
                 where="seed 9 step 4", title="ValueError at c.py:D"),
        ], failures=10)

        scan = signature_module.Parse(text)

        self.assertEqual(scan.status, signature_module.PARSED)
        self.assertEqual([entry.signature for entry in scan.entries],
                         ["976320f8", "4083f456", "0badbeef"])
        self.assertEqual(scan.failures, 10)
        self.assertEqual(scan.signatures, 3)

    def test_a_run_that_reported_nothing_has_no_signatures(self):
        # A harness error, a bad hero name, a kill before the reporter ran.
        # This is a real state, not a parse failure, and the two must not look
        # the same to whoever reads the manifest.
        scan = signature_module.Parse(
            "Traceback (most recent call last):\n  File a\nAssertionError\n")

        self.assertEqual(scan.status, signature_module.NONE)
        self.assertEqual(scan.entries, [])
        self.assertIsNone(scan.signatures)

    def test_no_output_at_all_is_the_same_state(self):
        self.assertEqual(signature_module.Parse("").status,
                         signature_module.NONE)

    def test_a_truncated_line_is_reported_as_partial(self):
        # The child said there was one and we could not read it. If this came
        # back as "none" the parser could rot into always-nothing and look
        # exactly like the bug it replaced.
        text = Summary([Line()[:40]], failures=1, signatures=1)
        scan = signature_module.Parse(text)

        self.assertEqual(scan.status, signature_module.PARTIAL)
        self.assertEqual(scan.entries, [])
        self.assertEqual(scan.signatures, 1)

    def test_one_unreadable_line_among_several_is_still_partial(self):
        text = Summary([Line(sig="976320f8"), Line(sig="4083f456")[:30]],
                       failures=2, signatures=2)
        scan = signature_module.Parse(text)

        self.assertEqual(scan.status, signature_module.PARTIAL)
        self.assertEqual([entry.signature for entry in scan.entries],
                         ["976320f8"])

    def test_lines_with_no_header_above_them_are_partial(self):
        # Output cut at the top: what is here may not be all of it, and the
        # count that would say so is missing.
        scan = signature_module.Parse(Line() + "\n")

        self.assertEqual(scan.status, signature_module.PARTIAL)
        self.assertEqual(len(scan.entries), 1)
        self.assertIsNone(scan.signatures)

    def test_reading_more_lines_than_were_reported_is_partial(self):
        # Over-matching is as broken as under-matching, and only the header
        # can catch it.
        text = Summary([Line(sig="976320f8"), Line(sig="4083f456")],
                       failures=2, signatures=1)

        self.assertEqual(signature_module.Parse(text).status,
                         signature_module.PARTIAL)

    def test_unexpected_spacing_still_parses(self):
        # The pad between the fields is formatting, not meaning. A single
        # space, a tab, or a wall of them all describe the same failure.
        for spacing in ("  ", " ", "     ", "\t", " \t "):
            with self.subTest(spacing=repr(spacing)):
                text = Summary([
                    f"<W>{spacing}4083f456{spacing}timeout-stall{spacing}"
                    f"x1 in 1 game(s){spacing}[seed 30 step 20000]{spacing}"
                    f"Game was cut short"])
                scan = signature_module.Parse(text)

                self.assertEqual(scan.status, signature_module.PARSED)
                self.assertEqual(scan.entries[0].signature, "4083f456")
                self.assertEqual(scan.entries[0].title, "Game was cut short")

    def test_a_class_name_longer_than_the_pad_collapses_it(self):
        # `kind` is printed through a `:<20` pad, so the longest shipped class
        # name leaves one space and not the usual run of them.
        text = Summary([Line(kind="invariant-violation")])
        scan = signature_module.Parse(text)

        self.assertEqual(scan.entries[0].kind, "invariant-violation")

        longer = Summary([Line(kind="a-failure-class-nobody-has-invented-yet")])
        self.assertEqual(signature_module.Parse(longer).entries[0].kind,
                         "a-failure-class-nobody-has-invented-yet")

    def test_an_unknown_location_keeps_the_signature(self):
        # `FormatSummary` prints `[unknown]` when no occurrence was recorded.
        # The seed is what reproduces the game, so its absence is worth being
        # able to see -- but it is not a reason to lose the signature.
        scan = signature_module.Parse(Summary([Line(where="unknown")]))

        self.assertEqual(scan.status, signature_module.PARSED)
        self.assertEqual(scan.entries[0].signature, "4083f456")
        self.assertIsNone(scan.entries[0].seed)
        self.assertIsNone(scan.entries[0].step)

    def test_a_traceback_is_not_mistaken_for_a_signature(self):
        # Hex-looking words and bracketed text are ordinary in a stack; a
        # signature line is the whole shape or nothing.
        text = ("Traceback (most recent call last):\n"
                '  File "game/ability/factory/do_attack.py", line 4083, in Go\n'
                "    deadbeef = self.units[3]  # [seed 30 step 2]\n"
                "IndexError: list index out of range\n")

        self.assertEqual(signature_module.Parse(text).status,
                         signature_module.NONE)

    def test_a_later_summary_replaces_an_earlier_one(self):
        text = Summary([Line(sig="aaaaaaaa")]) + "\n" + Summary(
            [Line(sig="bbbbbbbb"), Line(sig="cccccccc")])
        scan = signature_module.Parse(text)

        self.assertEqual([entry.signature for entry in scan.entries],
                         ["bbbbbbbb", "cccccccc"])
        self.assertEqual(scan.status, signature_module.PARSED)


class TestTheSignatureSurvivesATailCut(unittest.TestCase):
    """The defect itself, pinned. See MARVEL-97.

    `detail` is the tail of `stdout + stderr`, so a traceback on stderr pushes
    the summary -- which the log wrote to stdout -- past the cut regardless of
    where the two happened in time. That is what put 69 of 98 failures in one
    run beyond reach, and no cap fixes it: a `FailedTrace` stack has no bound.
    """

    def Output(self):
        stack = "\n".join(
            f'  File "/long/absolute/path/py_src/engine/log/log.py", '
            f'line {number}, in FailedTrace'
            for number in range(20))
        return REAL_OUTPUT + f"Traceback (most recent call last):\n{stack}\n"

    def test_the_old_tail_loses_the_signature(self):
        # Not a test of the new code -- a demonstration that the input really
        # is shaped the way the fix assumes. If this ever stops being true the
        # test below is proving nothing.
        self.assertNotIn("4083f456", self.Output()[-600:])

    def test_parsing_the_whole_output_keeps_it(self):
        scan = signature_module.Parse(self.Output())

        self.assertEqual([entry.signature for entry in scan.entries],
                         ["4083f456"])


class TestTheParserTracksWhatTheChildPrints(unittest.TestCase):
    """Held against `crash.FormatSummary` itself, not against a copy of it.

    A parser tested only on hand-written fixtures agrees with the format it was
    written against forever, including after that format moves. This is the
    test that fails when someone edits the summary.
    """

    def Raised(self, exc):
        try:
            raise exc
        except BaseException as raised:      # noqa: BLE001
            return raised

    def Collector(self, **kwargs):
        collector = CrashCollector(**kwargs)
        collector.CaptureException(self.Raised(ValueError("boom")),
                                   Occurrence(seed=7, step=12))
        collector.CaptureException(self.Raised(TypeError("other")),
                                   Occurrence(seed=8, step=3))
        return collector

    def Parse(self, collector):
        return signature_module.Parse("\n".join(crash.FormatSummary(collector)))

    def test_every_signature_the_reporter_prints_is_read_back(self):
        collector = self.Collector()

        scan = self.Parse(collector)

        self.assertEqual(scan.status, signature_module.PARSED)
        self.assertEqual([entry.signature for entry in scan.entries],
                         [group.failure.signature
                          for group in collector.Groups()])
        self.assertEqual([entry.kind for entry in scan.entries],
                         [group.failure.kind for group in collector.Groups()])
        self.assertEqual([entry.title for entry in scan.entries],
                         [group.failure.title for group in collector.Groups()])
        # The seed is what regenerates the game, so it is the field the fix is
        # ultimately for. Read against the reporter's own order, which is by
        # occurrence count and not by the order they were captured in.
        self.assertEqual(sorted(entry.seed for entry in scan.entries), [7, 8])
        self.assertEqual([entry.seed for entry in scan.entries],
                         [group.minimal.seed for group in collector.Groups()])

    def test_a_reporter_that_hit_its_signature_cap_still_reads_as_complete(self):
        # The header counts what was *recorded*, so a capped report is not a
        # partial parse -- the missing signatures were never printed.
        collector = self.Collector(max_signatures=1)

        scan = self.Parse(collector)

        self.assertEqual(scan.status, signature_module.PARSED)
        self.assertEqual(len(scan.entries), 1)
        self.assertEqual(scan.failures, 2)

    def test_a_run_that_captured_nothing_reads_as_nothing(self):
        # `BotRunner` prints no summary at all in this case, so what is being
        # pinned is that a header claiming zero is still "no signature" and
        # never "a signature we could not read".
        scan = self.Parse(CrashCollector())

        self.assertEqual(scan.status, signature_module.NONE)
        self.assertEqual(scan.entries, [])


class TestTheOutcomeCarriesIt(unittest.TestCase):
    """What lands in `corpus-manifest.json`."""

    def Outcome(self, **kwargs):
        case = Build(Stock(), seed=3, games=10).cases[0]
        fields = dict(case=case, status="failed", exit_code=1, seconds=1.0,
                      folder="f", scenes=0, detail="tail of the output")
        fields.update(kwargs)
        return generate.Outcome(**fields)

    def test_detail_still_means_what_it_meant(self):
        # Callers that read `detail` keep working: it is still the tail, and
        # it is still the only thing there is when nothing was reported.
        record = self.Outcome().ToDict()

        self.assertEqual(record["detail"], "tail of the output")

    def test_the_signature_is_a_field_of_its_own(self):
        scan = signature_module.Parse(REAL_OUTPUT)

        record = self.Outcome(crash=scan).ToDict()

        self.assertEqual(record["crash"]["status"], "parsed")
        self.assertEqual(record["crash"]["entries"][0]["signature"], "4083f456")
        self.assertEqual(record["crash"]["entries"][0]["kind"], "timeout-stall")
        self.assertEqual(record["crash"]["entries"][0]["seed"], 30)
        self.assertEqual(record["crash"]["entries"][0]["step"], 20000)

    def test_a_case_that_reported_nothing_says_so(self):
        record = self.Outcome().ToDict()

        self.assertEqual(record["crash"]["status"], "none")
        self.assertEqual(record["crash"]["entries"], [])

    def test_the_record_survives_the_progress_file(self):
        # Outcomes reach the manifest through `progress.jsonl`, so a field
        # that cannot be written and read back is not recorded at all.
        scan = signature_module.Parse(REAL_OUTPUT)
        record = self.Outcome(crash=scan).ToDict()

        self.assertEqual(json.loads(json.dumps(record, sort_keys=True)), record)


class TestHowMuchWasExplained(unittest.TestCase):
    """The regression metric: failures the manifest cannot name."""

    def Record(self, status="failed", entries=(), scan_status="parsed"):
        return {"status": status,
                "crash": {"status": scan_status, "failures": len(entries),
                          "signatures": len(entries),
                          "entries": [{"signature": sig} for sig in entries]}}

    def test_a_failure_with_a_signature_is_explained(self):
        summary = generate.Explained([self.Record(entries=["aaaa"])])

        self.assertEqual(summary["failed"], 1)
        self.assertEqual(summary["with_signature"], 1)
        self.assertEqual(summary["unexplained"], 0)

    def test_a_failure_with_none_is_counted_as_unexplained(self):
        summary = generate.Explained(
            [self.Record(scan_status="none"), self.Record(entries=["aaaa"])])

        self.assertEqual(summary["unexplained"], 1)
        self.assertEqual(summary["with_signature"], 1)

    def test_signatures_are_counted_distinctly_across_the_run(self):
        summary = generate.Explained([
            self.Record(entries=["aaaa", "bbbb"]),
            self.Record(entries=["aaaa"]),
            # A case that exited 0 can still have captured a crash: capture
            # deliberately does not fail a run.
            self.Record(status="ok", entries=["cccc"]),
        ])

        self.assertEqual(summary["distinct_signatures"],
                         ["aaaa", "bbbb", "cccc"])
        self.assertEqual(summary["failed"], 2)

    def test_a_case_the_parser_could_not_read_is_counted_separately(self):
        summary = generate.Explained(
            [self.Record(scan_status="partial", entries=[])])

        self.assertEqual(summary["unparsed_cases"], 1)
        self.assertEqual(summary["unexplained"], 1)

    def test_an_old_manifest_record_still_summarises(self):
        # A resumed run reads `progress.jsonl` lines written before this
        # existed, so the field has to be optional on the way in.
        summary = generate.Explained([{"status": "failed"}, {"status": "ok"}])

        self.assertEqual(summary["unexplained"], 1)
        self.assertEqual(summary["distinct_signatures"], [])

    def test_the_report_says_how_many_were_named(self):
        manifest = {"scenes": 3, "ok": 1, "cases": 2, "failed": 1,
                    "timed_out": 0,
                    "timing": {"games_per_hour": 1, "workers": 1,
                               "games_per_hour_per_worker": 1},
                    "failures": generate.Explained(
                        [self.Record(entries=["aaaa"])])}

        lines = generate.Report(manifest)

        self.assertTrue(any("1/1 named" in line for line in lines), lines)

    def test_a_manifest_from_before_this_existed_still_reports(self):
        manifest = {"scenes": 3, "ok": 1, "cases": 2, "failed": 1,
                    "timed_out": 0,
                    "timing": {"games_per_hour": 1, "workers": 1,
                               "games_per_hour_per_worker": 1}}

        self.assertTrue(generate.Report(manifest))


if __name__ == "__main__":
    unittest.main()
