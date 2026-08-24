"""The setup dataset, and the order the engine deals a board from it.

`datasets/setup/setup.json` is the fourth cross-language dataset (MARVEL-176).
The other three describe how the engine *computes*; this one describes what it
is asked to compute with -- which scenario holds which encounters, which hero
opens with which forty cards -- and until now that data lived only in
`py_src/data/` and `py_src/deck/`, where a C# engine cannot reach it without
depending on the oracle it is supposed to replace.

Two claims are tested here and they are different in kind.

**The dataset is a projection.** Every record comes out of the same dataclass
the engine loads it through, so a key the engine ignores cannot appear.
`deck/starter/spider_man.json` carries `set_aside` and `metadata`;
`HeroDescriptor` declares neither; the dataset has neither. A port that read the
raw file would implement fields the oracle does not have.

**The deal order is the id contract.** A card's `object_id` is its position in
the sequence `tools/setup/deal.py` describes, and `object_id` is on the wire in
every state digest. So the strongest available test is to hold the sequence
against a recorded digest: `datasets/digest/vectors.json` names the card at
every id for `rhino / spider_man / 12345`, and all eighty-one agree.

    python -m unittest unit_test.test_setup_dataset
"""

from __future__ import annotations

import json
import os
import tempfile
import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from tools.setup import deal, emit_setup

SETUP_DATASET = os.path.join("..", "datasets", "setup", "setup.json")
DIGEST_VECTORS = os.path.join("..", "datasets", "digest", "vectors.json")


def _Load(path: str, what: str):
    if not os.path.exists(path):
        raise unittest.SkipTest(f"run from py_src/ -- {what} missing")
    with open(path, encoding="utf-8") as handle:
        return json.load(handle)


class SetupDataset(unittest.TestCase):
    """What the emitted file holds, and what it deliberately does not."""

    def setUp(self) -> None:
        self.setup = _Load(SETUP_DATASET, "datasets/setup")

    def test_every_group_is_populated(self):
        for group, _ in emit_setup.RESOLUTION:
            self.assertGreater(len(self.setup[group]), 0, group)
            self.assertEqual(self.setup["counts"][group], len(self.setup[group]))

    def test_the_hero_record_is_the_descriptor_and_not_the_file(self):
        """`set_aside` and `metadata` are in the file and not in the dataclass."""
        spider_man = self.setup["heroes"]["spider_man"]
        self.assertNotIn("set_aside", spider_man)
        self.assertNotIn("metadata", spider_man)
        self.assertNotIn("version", spider_man)
        self.assertEqual(
            sorted(spider_man),
            ["hero", "hero_deck", "name", "nemesis_set", "obligations", "player_deck"])

    def test_a_campaign_keeps_its_declared_lists_separate(self):
        """`modular_sets` is not folded into `encounter_sets` at emit time.

        The scene loader appends one to the other only when the caller names no
        sets of its own. Joining them here would make the other case -- a
        scenario played with chosen modulars -- inexpressible from the dataset.
        """
        rhino = self.setup["campaigns"]["rhino"]
        self.assertEqual(rhino["encounter_sets"], ["standard"])
        self.assertEqual(rhino["modular_sets"], ["bomb_scare"])
        self.assertEqual(deal.EncounterSetNames(rhino), ["standard", "bomb_scare"])

    def test_nothing_is_shadowed_today_and_shadowing_is_recorded_when_it_is(self):
        """A name resolvable in two folders is reported, never silently taken."""
        self.assertEqual(self.setup["shadowed"], {})
        self.assertIn("campaigns", self.setup["resolution"])


class MirrorsTheEngineSearchOrder(unittest.TestCase):
    """`RESOLUTION` is `FindJsonPath`'s own order, minus a declared exclusion.

    Nothing else ties the two together. Add a folder to `SCENARIOS_FOLDERS` and
    the dataset would quietly stop covering a scenario the engine can still
    load, with every byte-comparison gate green, because the gate only asks
    whether the file matches what the emitter produces -- not whether the
    emitter still looks where the engine looks.
    """

    def _EngineOrder(self, load_type: str):
        """The folders `FindJsonPath` walks for `load_type`, in order.

        Read out of the function rather than re-derived from the module
        constants: `get_type_path_list` is a closure, it composes a different
        pair of lists per type, and it prepends `./` to all of them. Spying on
        `Exists` is the only way to see the composition it actually performs.
        """
        from engine.file import FileManager
        from engine.log import Log

        seen = []

        def spy(path: str) -> bool:
            seen.append(os.path.normpath(os.path.dirname(path)).replace(os.sep, "/"))
            return False

        # Nothing is ever found, so the search runs to the end of the list --
        # which is the point -- and then warns. `nullable=False` would
        # `Log.Assert` instead, so the warning is the quiet branch already.
        with mock.patch.object(FileManager, "Exists", staticmethod(spy)), \
                mock.patch.object(Log, "Warn", staticmethod(lambda *a, **k: None)):
            FileManager.FindJsonPath(load_type, "no_such_name_exists", nullable=True)
        return seen

    def test_every_group_walks_what_the_engine_walks_minus_the_exclusions(self):
        for group, folders in emit_setup.RESOLUTION:
            engine_order = self._EngineOrder(emit_setup.LOAD_TYPE[group])
            kept = [f for f in engine_order if f not in emit_setup.EXCLUDED[group]]
            self.assertEqual(kept, list(folders), group)

    def test_the_excluded_folders_are_folders_the_engine_really_searches(self):
        """An exclusion for a folder nobody searches is a stale comment."""
        for group, _ in emit_setup.RESOLUTION:
            engine_order = self._EngineOrder(emit_setup.LOAD_TYPE[group])
            for folder in emit_setup.EXCLUDED[group]:
                self.assertIn(folder, engine_order, f"{group}: {folder}")

    def test_the_working_directory_is_searched_first_and_holds_no_setup_file(self):
        """`.` is the exclusion that cannot show up in `shadowed`.

        `FindJsonPath` prepends `./` to every list, so a name found there beats
        every folder the dataset does read. `shadowed` only records the *later*
        hit, so this collision would be invisible in the file rather than
        reported in it. The whole defence is that `py_src/` holds one `.json`
        and it is not a campaign, a hero or an encounter set.
        """
        for group, _ in emit_setup.RESOLUTION:
            self.assertEqual(self._EngineOrder(emit_setup.LOAD_TYPE[group])[0], ".")
            self.assertIn(".", emit_setup.EXCLUDED[group])

        setup = _Load(SETUP_DATASET, "datasets/setup")
        for name in emit_setup._Names("."):
            for group, _ in emit_setup.RESOLUTION:
                self.assertNotIn(name, setup[group],
                                 f"./{name}.json shadows the {group} named {name}")


class Resolution(unittest.TestCase):
    """Names resolve in the engine's folder order, and collisions are visible."""

    def _Write(self, folder: str, name: str, villain: str) -> None:
        os.makedirs(folder, exist_ok=True)
        with open(os.path.join(folder, f"{name}.json"), "w", encoding="utf-8") as handle:
            json.dump({"version": "0.0.0", "name": name, "villain": [villain]}, handle)

    def test_the_first_folder_wins_and_the_second_is_reported(self):
        with tempfile.TemporaryDirectory() as root:
            first, second = os.path.join(root, "a"), os.path.join(root, "b")
            self._Write(first, "duplicated", "AAA")
            self._Write(second, "duplicated", "BBB")
            self._Write(second, "only_in_b", "CCC")

            records, shadowed = emit_setup.BuildGroup("campaigns", [first, second])

            self.assertEqual(records["duplicated"]["villain"], ["AAA"])
            self.assertEqual(records["only_in_b"]["villain"], ["CCC"])
            self.assertEqual(shadowed, [emit_setup._Posix(second, "duplicated")])

    def test_a_shadowed_path_is_forward_slashed_on_every_host(self):
        """It goes into a byte-compared fixture, so it cannot carry `os.sep`.

        Nothing is shadowed today, which is precisely the problem: the two CI
        legs would have agreed until the first collision landed and then
        disagreed about a file neither of them touched.
        """
        self.assertEqual(emit_setup._Posix(os.path.join("data", "scenarios"), "rhino"),
                         "data/scenarios/rhino.json")
        self.assertNotIn("\\", emit_setup._Posix(os.path.join("a", "b"), "c"))

    def test_names_are_sorted_and_non_json_is_ignored(self):
        with tempfile.TemporaryDirectory() as root:
            self._Write(root, "zulu", "Z")
            self._Write(root, "alpha", "A")
            with open(os.path.join(root, "notes.txt"), "w", encoding="utf-8") as handle:
                handle.write("not a campaign")

            self.assertEqual(emit_setup._Names(root), ["alpha", "zulu"])

    def test_a_missing_folder_contributes_nothing(self):
        self.assertEqual(emit_setup._Names(os.path.join("no", "such", "folder")), [])


class DealOrder(unittest.TestCase):
    """The sequence that decides every `object_id`."""

    def setUp(self) -> None:
        self.setup = _Load(SETUP_DATASET, "datasets/setup")

    def test_it_reproduces_every_recorded_id(self):
        """Held against the digest that a real game of it produced.

        `datasets/digest/vectors.json` records the card at each `object_id` for
        `rhino / spider_man / 12345`. The deal order has to name the same card
        at the same position, eighty-one times, or the whole id contract is
        wrong -- and every zone, index and field claim built on it with it.
        """
        vectors = _Load(DIGEST_VECTORS, "datasets/digest")
        case = vectors["cases"][0]
        self.assertEqual((case["campaign"], case["heroes"], case["seed"]),
                         ("rhino", ["spider_man"], 12345))

        recorded = json.loads(case["step_digests"][0])["cards"]
        order = deal.DealOrder(self.setup, "rhino", ["spider_man"])

        self.assertEqual(len(order), len(recorded))
        for record, creation in zip(recorded, order):
            self.assertIn(record["card"], creation.faces,
                          f"id {record['id']}: recorded {record['card']}, "
                          f"dealt {creation.spec} ({creation.source})")

    def test_the_card_showing_is_the_first_face_except_the_main_scheme(self):
        """One flip happens between creation and the first recorded step.

        An identity does not need one -- `MoveBToFront` reorders the spec so the
        alter-ego side is already first. A main scheme is created `1A,1B` and
        turned to its `1B` side by `PutIntoPlay`. Pinned because it is the only
        exception, and a port that flipped both or neither would still pass a
        test that only checked membership.
        """
        vectors = _Load(DIGEST_VECTORS, "datasets/digest")
        recorded = json.loads(vectors["cases"][0]["step_digests"][0])["cards"]
        order = deal.DealOrder(self.setup, "rhino", ["spider_man"])

        flipped = [(record["id"], record["card"], creation.spec)
                   for record, creation in zip(recorded, order)
                   if record["card"] != creation.faces[0]]

        self.assertEqual(flipped, [(48, "01097b", "01097a,01097b")])

    def test_sources_are_dealt_in_the_documented_order(self):
        order = deal.DealOrder(self.setup, "rhino", ["spider_man"])
        seen = []
        for creation in order:
            if not seen or seen[-1] != creation.source:
                seen.append(creation.source)
        self.assertEqual(seen, ["rules", "identity", "obligation", "nemesis",
                                "hero_deck", "player_deck", "main_scheme",
                                "villain", "encounter", "encounter_set"])
        for source in seen:
            self.assertIn(source, deal.SOURCES)

    def test_every_player_is_dealt_before_the_scenario(self):
        """Seat order, then the scenario. Two players, so the boundary is real."""
        order = deal.DealOrder(self.setup, "klaw", ["captain_marvel", "she_hulk"])
        seats = [c.player for c in order]
        self.assertEqual(seats[0], deal.SCENARIO)          # the rules card
        self.assertEqual(seats.index(0), 1)
        self.assertLess(max(i for i, s in enumerate(seats) if s == 0),
                        seats.index(1))
        self.assertTrue(all(s == deal.SCENARIO
                            for s in seats[max(i for i, s in enumerate(seats) if s == 1) + 1:]))

    def test_sp_dr_is_dealt_the_card_the_engine_hard_codes(self):
        """The one hero whose identity is not the identity it declares.

        `SelectIdentity` tests the first spec against `HACK_HERO_ID` ('3100')
        and, on a hit, throws the descriptor's list away for a literal
        `31002a,31002b` -- Peni Parker, whom the descriptor carries under the
        dropped `set_aside` key -- with no `move_b_to_front`. So the card at
        SP//dr's first `object_id` is `31002a`, not `31001b`, and a port that
        applied the normal rule would be wrong from the second card of the game
        onwards.

        This is not card-script behaviour and it is not a rule flag: the branch
        reads the hero spec and nothing else, so the dataset already contains
        everything needed to reproduce it.
        """
        order = deal.DealOrder(self.setup, "rhino", ["sp_dr"])
        identity = [c for c in order if c.source == "identity"]
        self.assertEqual([c.spec for c in identity], ["31002a,31002b"])
        self.assertEqual(identity[0].faces[0], "31002a")

        # And the hero it displaces is still what the dataset declares.
        self.assertEqual(self.setup["heroes"]["sp_dr"]["hero"], ["31001a,31001b"])

    def test_every_other_hero_takes_the_ordinary_identity_path(self):
        """One exception, and the test says which one rather than how many."""
        exceptional = [name for name, hero in self.setup["heroes"].items()
                       if deal.IdentitySpecs(hero)
                       != [deal.MoveBToFront(s) for s in hero["hero"]]]
        self.assertEqual(exceptional, ["sp_dr"])

    def test_move_b_to_front_puts_the_alter_ego_first(self):
        self.assertEqual(deal.MoveBToFront("01001a,01001b"), "01001b,01001a")
        self.assertEqual(deal.MoveBToFront("01094"), "01094")
        # A three-face identity keeps the non-`b` faces in their printed order.
        self.assertEqual(deal.MoveBToFront("29001a,29001b,29001c"),
                         "29001b,29001a,29001c")

    def test_an_unknown_name_is_a_failure_not_a_short_board(self):
        for campaign, heroes in (("no_such_scenario", ["spider_man"]),
                                 ("rhino", ["no_such_hero"])):
            with self.assertRaises(KeyError):
                deal.DealOrder(self.setup, campaign, heroes)


if __name__ == "__main__":
    unittest.main()
