"""What a flag resolves to, when two sources both name it.

`-bot` and `-test` are arg *groups*: one flag that expands to several. Expansion
happens inside `ConfigVariables.ParseArguments`' scan loop, before the rest of
the command line has been read, and it used to write straight into
`instance_command` and stamp `set_from = "CommandLine"` on every key it named.
`SetValue` then opened with `if self.set_from == set_from: return`, which reads
as an idempotence guard and was a precedence rule in disguise: between two
writes from the same nominal source, the *first* one won.

So a flag inside a group could not be overridden by the flag the user actually
typed. `python main.py -bot -no_check_invariants` reported
`check_invariants: true` in the run manifest. That is MARVEL-64, and it is not
specific to bools -- a string in a group was equally stuck.

The fix is precedence rather than arrival order. A group's expansion goes into
its own dictionary, `InitVariable` resolves command line -> group -> launch.json,
and `SetValue` replaces on equal-or-higher standing instead of returning early.
The answer no longer depends on where in the command line the flags appear, or
on how many times `InitVariable` happens to run.
"""

import unittest

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported. This module also needs `engine.engine`
# imported for its arg groups to be registered.
import engine  # noqa: F401  pylint: disable=unused-import
from engine.engine import Engine  # noqa: F401  pylint: disable=unused-import

from engine.config import ConfigVariable, ConfigVariables


class ConfigTestCase(unittest.TestCase):
    """Runs against an empty config namespace.

    `ConfigVariables` is module-level state the whole engine shares, and these
    tests declare variables and parse command lines. Swapping every dictionary
    for an empty one keeps that out of whatever else runs in this process.
    """

    def setUp(self):
        self.real_group = ConfigVariables.group
        saved = (ConfigVariables.variable_dict, ConfigVariables.instance_command,
                 ConfigVariables.instance_group, ConfigVariables.instance_launch,
                 ConfigVariables.group, ConfigVariables.is_initialized)
        self.addCleanup(self.Restore, saved)

        ConfigVariables.variable_dict = {}
        ConfigVariables.instance_command = {}
        ConfigVariables.instance_group = {}
        ConfigVariables.instance_launch = {}
        ConfigVariables.group = {}
        ConfigVariables.is_initialized = False

    @staticmethod
    def Restore(saved):
        (ConfigVariables.variable_dict, ConfigVariables.instance_command,
         ConfigVariables.instance_group, ConfigVariables.instance_launch,
         ConfigVariables.group, ConfigVariables.is_initialized) = saved

    @staticmethod
    def Resolve(variable, argv):
        """Parse `argv` and return what `variable` settled on."""
        ConfigVariables.ParseArguments(argv)
        ConfigVariables.SetupVariables([variable.name])
        return variable.value


class TestAGroupValueLosesToAnExplicitFlag(ConfigTestCase):
    """The bug, from both sides and for every type.

    Order-independence is the part worth having. "The last flag wins" would fix
    the reported case and leave `-no_check_invariants -bot` broken, which is the
    same command line with the words swapped.
    """

    def Group(self, expansion):
        ConfigVariables.SetGroupArgs("probe_group", expansion)

    def test_a_bool_can_be_turned_off_after_the_group(self):
        flag = ConfigVariables.Bool("probe_flag", False)
        self.Group("-probe_flag")

        self.assertFalse(self.Resolve(flag, ["-probe_group", "-no_probe_flag"]))

    def test_a_bool_can_be_turned_off_before_the_group(self):
        flag = ConfigVariables.Bool("probe_flag", False)
        self.Group("-probe_flag")

        self.assertFalse(self.Resolve(flag, ["-no_probe_flag", "-probe_group"]))

    def test_a_bool_can_be_turned_on_against_a_group_that_turns_it_off(self):
        flag = ConfigVariables.Bool("probe_flag", False)
        self.Group("-no_probe_flag")

        self.assertTrue(self.Resolve(flag, ["-probe_group", "-probe_flag"]))

    def test_a_string_can_be_overridden(self):
        # The issue is titled "a bool flag", and the primitive never cared.
        text = ConfigVariables.Str("probe_str", "default")
        self.Group("-probe_str fromgroup")

        self.assertEqual(self.Resolve(text, ["-probe_group", "-probe_str", "explicit"]),
                         "explicit")

    def test_a_string_can_be_overridden_from_the_left(self):
        text = ConfigVariables.Str("probe_str", "default")
        self.Group("-probe_str fromgroup")

        self.assertEqual(self.Resolve(text, ["-probe_str", "explicit", "-probe_group"]),
                         "explicit")

    def test_an_int_can_be_overridden(self):
        number = ConfigVariables.Int("probe_int", 1)
        self.Group("-probe_int 2")

        self.assertEqual(self.Resolve(number, ["-probe_group", "-probe_int", "3"]), 3)

    def test_a_list_can_be_overridden(self):
        items = ConfigVariables.ListStr("probe_list", ["default"])
        self.Group("-probe_list a b")

        self.assertEqual(self.Resolve(items, ["-probe_group", "-probe_list", "c", "d"]),
                         ["c", "d"])


class TestAGroupStillSetsWhatNothingElseDoes(ConfigTestCase):
    """Losing to an explicit flag is the only thing that changed."""

    def test_a_group_sets_its_flag(self):
        flag = ConfigVariables.Bool("probe_flag", False)
        ConfigVariables.SetGroupArgs("probe_group", "-probe_flag")

        self.assertTrue(self.Resolve(flag, ["-probe_group"]))

    def test_a_group_value_still_counts_as_the_command_line(self):
        # `-bot` implying `-device bot` is a command-line decision, and
        # `Engine.Initialize` reads `is_from_command_line` to tell a value
        # somebody asked for from one that defaulted.
        flag = ConfigVariables.Bool("probe_flag", False)
        ConfigVariables.SetGroupArgs("probe_group", "-probe_flag")
        self.Resolve(flag, ["-probe_group"])

        self.assertTrue(flag.is_from_command_line)

    def test_a_group_can_name_another_group(self):
        flag = ConfigVariables.Bool("probe_flag", False)
        ConfigVariables.SetGroupArgs("probe_inner", "-probe_flag")
        ConfigVariables.SetGroupArgs("probe_group", "-probe_inner")

        self.assertTrue(self.Resolve(flag, ["-probe_group"]))

    def test_two_groups_disagreeing_is_settled_by_the_later_one(self):
        # Nothing outranks anything here -- both values come from the same
        # source -- so the one written further right wins, which is the only
        # answer that reads off the command line. Neither shipped group
        # overlaps with the other, but the rule should not be a surprise.
        flag = ConfigVariables.Bool("probe_flag", False)
        ConfigVariables.SetGroupArgs("probe_on", "-probe_flag")
        ConfigVariables.SetGroupArgs("probe_off", "-no_probe_flag")

        self.assertFalse(self.Resolve(flag, ["-probe_on", "-probe_off"]))

    def test_and_the_other_way_round(self):
        flag = ConfigVariables.Bool("probe_flag", False)
        ConfigVariables.SetGroupArgs("probe_on", "-probe_flag")
        ConfigVariables.SetGroupArgs("probe_off", "-no_probe_flag")

        self.assertTrue(self.Resolve(flag, ["-probe_off", "-probe_on"]))

    def test_an_explicit_flag_still_beats_both_of_them(self):
        flag = ConfigVariables.Bool("probe_flag", False)
        ConfigVariables.SetGroupArgs("probe_on", "-probe_flag")
        ConfigVariables.SetGroupArgs("probe_off", "-no_probe_flag")

        self.assertTrue(self.Resolve(flag, ["-probe_on", "-probe_off", "-probe_flag"]))

    def test_a_nested_group_still_loses_to_an_explicit_flag(self):
        # However deep the expansion went, it is still a group value.
        flag = ConfigVariables.Bool("probe_flag", False)
        ConfigVariables.SetGroupArgs("probe_inner", "-probe_flag")
        ConfigVariables.SetGroupArgs("probe_group", "-probe_inner")

        self.assertFalse(self.Resolve(flag, ["-probe_group", "-no_probe_flag"]))


class TestPrecedenceBetweenSources(ConfigTestCase):

    def test_the_default_is_the_weakest(self):
        flag = ConfigVariables.Bool("probe_flag", False)
        ConfigVariables.instance_launch["probe_flag"] = True

        self.assertTrue(self.Resolve(flag, []))

    def test_a_group_value_beats_launch_json(self):
        flag = ConfigVariables.Bool("probe_flag", False)
        ConfigVariables.instance_launch["probe_flag"] = False
        ConfigVariables.SetGroupArgs("probe_group", "-probe_flag")

        self.assertTrue(self.Resolve(flag, ["-probe_group"]))

    def test_the_command_line_beats_launch_json(self):
        flag = ConfigVariables.Bool("probe_flag", False)
        ConfigVariables.instance_launch["probe_flag"] = False

        self.assertTrue(self.Resolve(flag, ["-probe_flag"]))

    def test_launch_json_cannot_take_back_a_command_line_value(self):
        # `LoadConfig` runs after the command line is parsed, and a config file
        # arriving late must not undo what was typed.
        flag = ConfigVariables.Bool("probe_flag", False)
        self.Resolve(flag, ["-probe_flag"])

        ConfigVariables.instance_launch["probe_flag"] = False
        ConfigVariables.SetupVariables(["probe_flag"])

        self.assertTrue(flag.value)

    def test_resolving_twice_lands_on_the_same_answer(self):
        # `InitVariable` runs eagerly during parsing so `Initialize` can read
        # `config_files`, and again from `SetupVariables`. Both re-derive the
        # winner rather than remembering who went first.
        flag = ConfigVariables.Bool("probe_flag", False)
        ConfigVariables.SetGroupArgs("probe_group", "-probe_flag")
        first = self.Resolve(flag, ["-probe_group", "-no_probe_flag"])

        ConfigVariables.SetupVariables(["probe_flag"])

        self.assertFalse(first)
        self.assertFalse(flag.value)


class TestVariablesDeclaredAfterParsing(ConfigTestCase):
    """Most config variables are module-level in files imported long after
    `ConfigVariables.Initialize` has run."""

    def test_a_late_declaration_still_sees_the_command_line(self):
        ConfigVariables.ParseArguments(["-probe_flag"])
        ConfigVariables.is_initialized = True

        flag = ConfigVariables.Bool("probe_flag", False)

        self.assertTrue(flag.value)

    def test_a_late_declaration_still_sees_a_group(self):
        ConfigVariables.SetGroupArgs("probe_group", "-probe_flag")
        ConfigVariables.ParseArguments(["-probe_group"])
        ConfigVariables.is_initialized = True

        flag = ConfigVariables.Bool("probe_flag", False)

        self.assertTrue(flag.value)

    def test_a_late_declaration_prefers_the_explicit_flag(self):
        ConfigVariables.SetGroupArgs("probe_group", "-probe_flag")
        ConfigVariables.ParseArguments(["-probe_group", "-no_probe_flag"])
        ConfigVariables.is_initialized = True

        flag = ConfigVariables.Bool("probe_flag", False)

        self.assertFalse(flag.value)


class TestMalformedInput(ConfigTestCase):

    def test_a_value_after_a_group_name_is_ignored_rather_than_fatal(self):
        # Groups take no arguments. This used to raise `KeyError` out of
        # argument parsing, before the engine had logged anything at all.
        flag = ConfigVariables.Bool("probe_flag", False)
        ConfigVariables.SetGroupArgs("probe_group", "-probe_flag")

        self.assertTrue(self.Resolve(flag, ["-probe_group", "3"]))

    def test_a_leading_bare_value_is_ignored(self):
        flag = ConfigVariables.Bool("probe_flag", False)

        self.assertTrue(self.Resolve(flag, ["stray", "-probe_flag"]))

    def test_an_unknown_flag_is_recorded_and_ignored(self):
        flag = ConfigVariables.Bool("probe_flag", False)

        self.assertTrue(self.Resolve(flag, ["-probe_unknown", "-probe_flag"]))


class TestTheShippedGroupsAreUnchanged(ConfigTestCase):
    """MARVEL-64's acceptance criterion: `-test` and `-bot` must resolve to the
    same effective config as before the primitive was touched.

    Asserted on what the expansion writes rather than on resolved variables, so
    the check cannot disturb the engine's real config for the rest of the run.
    """

    def Expand(self, flag):
        ConfigVariables.group = self.real_group
        ConfigVariables.ParseArguments([flag])
        return dict(ConfigVariables.instance_group)

    def test_bot_expands_to_what_it_always_did(self):
        self.assertEqual(self.Expand("-bot"), {
            "device": "bot",
            "editor": False,
            "hidden_log_categories": ["CONTROLLER", "WEB", "VERSION", "STATISTICS"],
        })

    def test_test_expands_to_what_it_always_did(self):
        # `-test` was repointed at `-verify_replays` by MARVEL-28; before that
        # it expanded to a bare `-device`, which is the bug that issue fixed.
        self.assertEqual(self.Expand("-test"), {
            "verify_replays": True,
            "editor": False,
            "statistics": False,
            "hidden_log_categories": ["CONTROLLER", "WEB", "VERSION", "STATISTICS"],
        })

    def test_a_group_leaves_the_real_command_line_alone(self):
        self.Expand("-bot")

        self.assertEqual(ConfigVariables.instance_command, {})


class TestSetValuePrecedence(unittest.TestCase):
    """The rule underneath, on a variable with no parser attached."""

    @staticmethod
    def Variable():
        variable = ConfigVariable.Bool("probe_flag")
        variable.SetDefault(False)
        return variable

    def test_a_stronger_source_replaces(self):
        variable = self.Variable()

        variable.SetValue(True, "CommandLine")

        self.assertTrue(variable.value)
        self.assertEqual(variable.set_from, "CommandLine")

    def test_a_weaker_source_does_not(self):
        variable = self.Variable()
        variable.SetValue(True, "CommandLine")

        variable.SetValue(False, "LaunchJson")

        self.assertTrue(variable.value)

    def test_equal_standing_replaces(self):
        # This is the line the bug lived on. Returning early here is what made
        # a group value permanent.
        variable = self.Variable()
        variable.SetValue(True, "CommandLine")

        variable.SetValue(False, "CommandLine")

        self.assertFalse(variable.value)

    def test_every_source_has_a_rank(self):
        # A missing key would be a `KeyError` in the middle of startup.
        from typing import get_args
        for set_from in get_args(ConfigVariable.SET_FROM):
            self.assertIn(set_from, ConfigVariable.PRECEDENCE)


if __name__ == "__main__":
    unittest.main()
