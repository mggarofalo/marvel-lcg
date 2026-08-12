"""What a corpus records about its configuration, and what drift means.

The engine is deterministic for a given configuration and not across
configurations, so a corpus is only reproducible if the resolved config is known
and a verifier can tell when it has changed. `engine/config_record.py` does both;
these tests pin the cut it makes and the three ways two snapshots can differ.

The cut is the part worth arguing about, so most of this is about `IsCompared`.
It is a denylist rather than an allowlist on purpose -- a flag nobody classified
is compared, so it surfaces as noise rather than passing in silence -- and the
exclusions each stand for a reason the module docstring records. See MARVEL-34.
"""

import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from engine.config import ConfigVariable, ConfigVariables
from engine.config_record import ConfigDrift, ConfigRecord


def Snapshot(values, *, version=ConfigRecord.VERSION, compared=None):
    """A recorded snapshot, as it would come back out of a manifest."""
    document = {
        "version": version,
        "git_sha": None,
        "sources": {name: "DefaultValue" for name in values},
        "values": dict(values),
    }
    document["compared"] = (
        [name for name in values if ConfigRecord.IsCompared(name)]
        if compared == None else compared)
    return document


def Registered(values):
    """Patch the live variable table to hold exactly `values`."""
    table = {}
    for name, value in values.items():
        variable = ConfigVariable.Base(name)
        variable.value = value
        variable.set_from = "DefaultValue"
        table[name] = variable
    return mock.patch.object(ConfigVariables, "variable_dict", table)


class TestWhatIsCompared(unittest.TestCase):

    def test_a_plain_flag_is_compared(self):
        self.assertTrue(ConfigRecord.IsCompared("enable_multiple_threads"))
        self.assertTrue(ConfigRecord.IsCompared("max_workers"))

    def test_a_flag_nobody_classified_is_compared(self):
        # The denylist direction. A gameplay flag added next year is compared
        # until someone decides otherwise, so the failure mode is noise rather
        # than a silently unchecked corpus.
        self.assertTrue(ConfigRecord.IsCompared("some_flag_added_next_year"))

    def test_paths_are_not(self):
        # Where this machine keeps things. Comparing them would fail every
        # corpus on any machine but the one that generated it.
        for name in ("replay_folders", "crash_file", "config_files",
                     "data_folder", "image_folders", "exclude_profile_files"):
            self.assertFalse(ConfigRecord.IsCompared(name), name)

    def test_the_generator_and_the_verifier_own_their_own_flags(self):
        # A verifier never runs a bot and a generator never runs a verifier, so
        # every one of these would read as drift. What they were is still in
        # `values`, and the ones that decide the game are first-class manifest
        # fields besides.
        for name in ("bot_policy", "bot_seed", "bot_max_steps",
                     "verify_allow_incomplete", "verify_replays"):
            self.assertFalse(ConfigRecord.IsCompared(name), name)

    def test_invocation_is_not(self):
        for name in ("device", "editor", "hidden_log_categories", "font",
                     "enable_profile_category"):
            self.assertFalse(ConfigRecord.IsCompared(name), name)

    def test_the_statistics_flags_are_compared(self):
        # They read as bookkeeping and are not. Audit finding F5 measured
        # `-no_pause_test_statistics` moving `forced_effect` id allocation from
        # 158 to 183, because it decides whether the statistics and achievement
        # abilities are registered at all. Excluding them would leave out one of
        # the two findings this record was built for.
        self.assertTrue(ConfigRecord.IsCompared("statistics"))
        self.assertTrue(ConfigRecord.IsCompared("pause_test_statistics"))

    def test_check_invariants_is_not(self):
        # Measured, not assumed: `Engine.Initialize` forces it on for
        # `-device bot` and leaves it off elsewhere, so a generator and a
        # verifier can never agree. The first end-to-end run of this gate failed
        # on exactly that, against a corpus that was in fact fine.
        self.assertFalse(ConfigRecord.IsCompared("check_invariants"))


class TestSnapshot(unittest.TestCase):

    def test_it_records_every_registered_variable(self):
        with Registered({"b": 2, "a": 1}):
            snapshot = ConfigRecord.Snapshot()

        self.assertEqual(snapshot["values"], {"a": 1, "b": 2})

    def test_names_are_sorted_so_the_manifest_is_stable(self):
        with Registered({"z": 1, "a": 1, "m": 1}):
            snapshot = ConfigRecord.Snapshot()

        self.assertEqual(list(snapshot["values"]), ["a", "m", "z"])

    def test_it_records_which_source_won(self):
        with Registered({"a": 1}):
            snapshot = ConfigRecord.Snapshot()

        self.assertEqual(snapshot["sources"]["a"], "DefaultValue")

    def test_the_comparison_policy_travels_with_the_values(self):
        # So a verifier years later reports against the rules the recording was
        # written under rather than silently applying its own.
        with Registered({"max_workers": 5, "replay_folders": ["./replays/"]}):
            snapshot = ConfigRecord.Snapshot()

        self.assertEqual(snapshot["compared"], ["max_workers"])

    def test_a_list_value_is_copied_not_aliased(self):
        live = ["./replays/"]
        with Registered({"some_list": live}):
            snapshot = ConfigRecord.Snapshot()
        live.append("./more/")

        self.assertEqual(snapshot["values"]["some_list"], ["./replays/"])


class TestCompare(unittest.TestCase):

    def Drifts(self, recorded, current):
        with Registered(current):
            return ConfigRecord.Compare(recorded)

    def test_an_identical_config_has_no_drift(self):
        self.assertEqual(self.Drifts(Snapshot({"a": 1}), {"a": 1}), [])

    def test_a_changed_value_is_drift(self):
        drifts = self.Drifts(Snapshot({"a": 1}), {"a": 2})

        self.assertEqual([(d.name, d.kind) for d in drifts], [("a", "changed")])
        self.assertTrue(drifts[0].is_failing)

    def test_a_variable_only_the_recording_has_is_not_a_failure(self):
        # Nothing in this process read it, so it cannot have influenced this
        # process. Where it changed the recorded game, the per-step digest is
        # what catches that.
        drifts = self.Drifts(Snapshot({"a": 1}), {})

        self.assertEqual([(d.name, d.kind) for d in drifts], [("a", "missing")])
        self.assertFalse(drifts[0].is_failing)

    def test_a_variable_only_this_process_has_is_not_a_failure(self):
        drifts = self.Drifts(Snapshot({}), {"a": 1})

        self.assertEqual([(d.name, d.kind) for d in drifts], [("a", "added")])
        self.assertFalse(drifts[0].is_failing)

    def test_uncompared_variables_are_ignored_on_both_sides(self):
        recorded = Snapshot({"max_workers": 5, "bot_policy": "first"})
        drifts = self.Drifts(recorded, {"max_workers": 5, "bot_policy": "random"})

        self.assertEqual(drifts, [])

    def test_results_are_sorted(self):
        drifts = self.Drifts(Snapshot({"z": 1, "a": 1}), {"z": 2, "a": 2})

        self.assertEqual([d.name for d in drifts], ["a", "z"])

    def test_a_missing_snapshot_is_drift_rather_than_a_pass(self):
        # An old manifest with no config block must not read as "agrees".
        for recorded in (None, {}, {"version": 1}):
            drifts = self.Drifts(recorded, {"a": 1})
            self.assertEqual(len(drifts), 1, recorded)
            self.assertTrue(drifts[0].is_failing, recorded)

    def test_a_snapshot_from_a_newer_engine_is_drift(self):
        # Refusing to guess at a format this code does not know.
        drifts = self.Drifts(
            Snapshot({"a": 1}, version=ConfigRecord.VERSION + 1), {"a": 1})

        self.assertEqual(len(drifts), 1)
        self.assertTrue(drifts[0].is_failing)

    def test_a_snapshot_written_before_compared_existed_still_works(self):
        recorded = Snapshot({"max_workers": 5}, compared=None)
        del recorded["compared"]

        self.assertEqual(self.Drifts(recorded, {"max_workers": 5}), [])
        self.assertEqual(len(self.Drifts(recorded, {"max_workers": 3})), 1)


class TestGitSha(unittest.TestCase):

    def test_it_reads_the_checked_out_commit(self):
        sha = ConfigRecord.GitSha()

        # None is a legitimate answer outside a checkout, so this pins the
        # shape rather than the presence.
        if sha != None:
            self.assertTrue(ConfigRecord.IsSha(sha), sha)

    def test_a_sha_is_forty_hex_characters(self):
        self.assertTrue(ConfigRecord.IsSha("0" * 40))
        self.assertFalse(ConfigRecord.IsSha("0" * 39))
        self.assertFalse(ConfigRecord.IsSha("ref: refs/heads/master"))
        self.assertFalse(ConfigRecord.IsSha("g" * 40))

    def test_a_packed_ref_is_found(self):
        text = ("# pack-refs with: peeled fully-peeled sorted\n"
                f"{'a' * 40} refs/heads/master\n"
                f"^{'b' * 40}\n"
                f"{'c' * 40} refs/tags/v1\n")
        with mock.patch.object(ConfigRecord, "ReadTextFile", return_value=text):
            self.assertEqual(
                ConfigRecord.ReadPackedRef("packed-refs", "refs/heads/master"),
                "a" * 40)
            self.assertEqual(
                ConfigRecord.ReadPackedRef("packed-refs", "refs/heads/nope"),
                None)

    def test_no_packed_refs_file_is_not_an_error(self):
        with mock.patch.object(ConfigRecord, "ReadTextFile", return_value=None):
            self.assertEqual(
                ConfigRecord.ReadPackedRef("packed-refs", "refs/heads/master"),
                None)


class TestDescriptions(unittest.TestCase):
    """The drift lines a person reads when a corpus fails to verify."""

    def test_a_changed_value_names_both_sides(self):
        text = ConfigDrift("max_workers", "changed", 3, 5).Describe()

        self.assertIn("3", text)
        self.assertIn("5", text)

    def test_the_unmatched_kinds_say_why_they_do_not_matter(self):
        self.assertIn("nothing in this process read it",
                      ConfigDrift("a", "missing", recorded=1).Describe())
        self.assertIn("nothing in the generating process read it",
                      ConfigDrift("a", "added", current=1).Describe())


if __name__ == "__main__":
    unittest.main()
