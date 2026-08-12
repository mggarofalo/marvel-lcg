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
that, and what is testable about it without a corpus is its resume bookkeeping.
"""

import json
import os
import shutil
import tempfile
import unittest

from tools.corpus import generate
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

    def test_extra_flags_come_last_so_they_win(self):
        case = Build(Stock(), seed=3, games=10).cases[0]
        command = generate.Command(case, "/out", ["-check_invariants"])

        self.assertEqual(command[-1], "-check_invariants")
        self.assertGreater(command.index("-check_invariants"),
                           command.index("-no_check_invariants"))


if __name__ == "__main__":
    unittest.main()
