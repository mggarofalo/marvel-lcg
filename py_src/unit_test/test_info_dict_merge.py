"""`GetInfoDict` has one merge direction, and the key sets must stay disjoint.

There are nine definitions of `GetInfoDict` -- the base plus eight overrides --
and they used to disagree about which side won a collision. Six returned
`local | super()`, so the *base* won; `Identity` and `Minion` returned
`super() | local`, so the *subclass* did. Nothing chose that: it was nine
independent expressions that happened to differ, which is no rule for the C#
port to reproduce.

These dictionaries feed the state digest, so a collision would not merely
resolve arbitrarily -- it would silently drop a named field from the wire, which
is a state change that looks like no change at all.

`CardFace.MergeInfo` is now the single place the merge happens: the more derived
class wins, and a collision raises rather than resolving by MRO accident.

See MARVEL-49, and `docs/state-digest-v2.md` finding D7.
"""

import unittest

# `game.*` modules import each other in a cycle that only resolves once `engine`
# has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from core.errors import EngineIntegrityError
from game.card.face.card_face import CardFace
from tools.spec.case import SpecCase, ThenStep
from tools.spec.harness import EnsureEngine, NewGameForCase
from tools.spec.policy import TranscriptPolicy


class TestMergeInfo(unittest.TestCase):

    def test_disjoint_keys_are_combined(self):
        self.assertEqual(
            CardFace.MergeInfo({"health": 10}, {"attack": 5}),
            {"health": 10, "attack": 5},
        )

    def test_the_more_derived_class_wins_is_the_stated_direction(self):
        # Unobservable while the key sets are disjoint -- which is the point of
        # the guard below -- but the direction still has to be written down for
        # the port, so pin the argument order that expresses it.
        merged = CardFace.MergeInfo({"a": 1}, {"b": 2})
        self.assertEqual(list(merged), ["a", "b"])

    def test_a_collision_raises(self):
        with self.assertRaises(EngineIntegrityError):
            CardFace.MergeInfo({"health": 10}, {"health": 12})

    def test_the_error_names_the_colliding_key(self):
        # The whole value of failing loudly is that the next person knows which
        # key to rename.
        with self.assertRaises(EngineIntegrityError) as caught:
            CardFace.MergeInfo({"surge": 1, "health": 10}, {"health": 12})
        self.assertIn("health", str(caught.exception))
        self.assertNotIn("surge", str(caught.exception))

    def test_a_collision_raises_even_when_the_values_agree(self):
        # Equal values would merge harmlessly today, but two classes owning one
        # key is the defect -- they will not stay equal.
        with self.assertRaises(EngineIntegrityError):
            CardFace.MergeInfo({"health": 10}, {"health": 10})

    def test_empty_sides_are_fine(self):
        self.assertEqual(CardFace.MergeInfo({}, {"a": 1}), {"a": 1})
        self.assertEqual(CardFace.MergeInfo({"a": 1}, {}), {"a": 1})
        self.assertEqual(CardFace.MergeInfo({}, {}), {})

    def test_it_raises_integrity_rather_than_asserting(self):
        # A dropped digest field is a wrong artefact, not a wrong frame, so it
        # has to survive the engine's catch-alls. `Log.OnCrash` re-raises
        # `EngineIntegrityError` regardless of the build; an `AssertionError`
        # would be swallowed on a release build and reach a corpus file.
        with self.assertRaises(EngineIntegrityError):
            CardFace.MergeInfo({"x": 1}, {"x": 1})


def DefinitionsOf(method_name: str, face) -> set:
    """Which classes in this face's MRO define `method_name` themselves."""
    return {
        klass.__name__
        for klass in type(face).__mro__
        if method_name in vars(klass)
    }


class TestNoCollisionInRealPlay(unittest.TestCase):
    """Every card the digest reads, on real boards, across every override.

    The unit tests above prove the guard fires. This proves it does not fire
    today -- that the nine key sets really are disjoint, which is what made the
    direction flip a no-op rather than a silent change to every digest.

    Boots the engine, so it belongs in the slower tier.
    """

    BOARDS = (
        ("rhino", ("spider_man",)),
        ("klaw", ("captain_marvel", "she_hulk")),
    )

    @classmethod
    def setUpClass(cls):
        EnsureEngine()
        cls.worlds = []
        for scenario, heroes in cls.BOARDS:
            case = SpecCase(
                name=f"info dict sweep {scenario}",
                scenario=scenario,
                heroes=heroes,
                beats=(ThenStep("__none__", "__none__", 0),),
            )
            game = NewGameForCase(case, TranscriptPolicy())
            assert game.GameSetup()
            cls.worlds.append(game.world)

    def DigestReadableCards(self, world):
        # Every card, in every zone, since MARVEL-59 removed the in-play guard.
        return world.object_manager.card_dict.values()

    def test_no_card_the_digest_reads_hits_a_collision(self):
        seen = 0
        for world in self.worlds:
            for card in self.DigestReadableCards(world):
                # Raises EngineIntegrityError on a collision; that is the assertion.
                card.face.GetStateFields()
                seen += 1
        self.assertGreater(seen, 0, "no cards were swept, so this proves nothing")

    def test_the_sweep_actually_reaches_the_overrides(self):
        # Without this the test above could pass by never exercising a subclass
        # that defines GetInfoDict at all.
        reached = set()
        for world in self.worlds:
            for card in self.DigestReadableCards(world):
                reached |= DefinitionsOf("GetInfoDict", card.face)

        # The base plus the attribute mixins every card carries.
        self.assertIn("CardFace", reached)
        self.assertIn("HasAttribute", reached)
        self.assertIn("CanPlaceToken", reached)
        self.assertIn("CanPlaceCounter", reached)
        self.assertIn("Identity", reached)

    def test_every_override_merges_through_the_guard(self):
        # A new override that writes `local | super()` by hand would reintroduce
        # exactly the disagreement MARVEL-49 removed, and no runtime test would
        # catch it while the keys stayed disjoint. Read the source instead.
        import inspect
        from pathlib import Path

        root = Path(inspect.getfile(CardFace)).parent
        offenders = []
        for path in root.rglob("*.py"):
            source = path.read_text(encoding="utf-8")
            if "def GetInfoDict" not in source:
                continue
            for line in source.splitlines():
                stripped = line.strip()
                if not stripped.startswith("return"):
                    continue
                if "super().GetInfoDict()" in stripped and "MergeInfo" not in stripped:
                    offenders.append(f"{path.name}: {stripped}")
        self.assertEqual(offenders, [], "GetInfoDict overrides must use MergeInfo")


if __name__ == "__main__":
    unittest.main()
