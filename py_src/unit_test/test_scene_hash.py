"""What a corpus may hash, and what a deterministic save must not write.

`Scene.PrepareSave` stamps three values that vary with the machine and the
moment -- `sign` (a host fingerprint), `time` (wall clock) and `playtime`
(elapsed seconds). Two runs of the same seed therefore produced different
files, which makes a content-addressed corpus manifest impossible.

The answer is two-sided: `Scene.HashablePayload` defines the part of a scene a
hash may depend on, and a deterministic save leaves the ambient metadata out of
the file altogether so nothing host-identifying reaches the repository.

See MARVEL-27 and `docs/determinism-audit.md` (F6). End-to-end proof that two
real runs agree is `tools/determinism/check_scene_repro.py`.
"""

import unittest
from unittest import mock

# The `game.*` packages import each other circularly and only resolve once the
# `engine` package has been imported.
import engine  # noqa: F401  pylint: disable=unused-import

from game.scene.scene import AMBIENT_KEYS, PROVENANCE_KEYS, Scene


def SavedScene(**metadata):
    """A scene as `Json.Load` hands it back: plain dicts, no dataclass."""
    base = {
        "version": "0.5.9.201",
        "metadata": {"seed": 4242, **metadata},
        "rules": [],
        "campaign": {"name": "Rhino"},
        "players": [{"name": "spider_man"}],
        "puzzle": [],
        "inputs": [{"index": 0, "effect": {"id": 3}}],
    }
    return base


def StubFingerprint(value: str = "deadbeef"):
    """`UserInfo.fingerprint` is only an annotation until `Initialize` runs."""
    from engine.user.user_info import UserInfo

    return mock.patch.object(UserInfo, "fingerprint", value, create=True)


class FakeGame:
    """`PrepareSave` only reaches `game.controller_manager.replay`."""

    def __init__(self, inputs=None):
        replay = type("Replay", (), {"history_inputs": inputs or []})()
        controller_manager = type("ControllerManager", (), {"replay": replay})()
        self.controller_manager = controller_manager


class TestHashablePayload(unittest.TestCase):

    def test_provenance_does_not_reach_the_payload(self):
        bare = SavedScene()
        stamped = SavedScene(
            sign="6cc7e9dc535aa2e9a1fb49c2562d5cc9",
            time="2026-08-06 21-43",
            playtime="1.2",
            path="./replays/whatever.json",
            clients=["someone"],
            report="a report",
        )

        self.assertEqual(Scene.HashablePayload(bare), Scene.HashablePayload(stamped))

    def test_every_provenance_key_is_individually_ignored(self):
        # Guards against a key being dropped from the tuple by accident.
        bare = Scene.HashablePayload(SavedScene())
        for key in PROVENANCE_KEYS:
            with self.subTest(key=key):
                self.assertEqual(bare, Scene.HashablePayload(SavedScene(**{key: "x"})))

    def test_checksum_does_not_reach_the_payload(self):
        with_checksum = SavedScene()
        with_checksum["checksum"] = "0" * 64

        self.assertEqual(
            Scene.HashablePayload(SavedScene()),
            Scene.HashablePayload(with_checksum),
        )

    def test_metadata_insertion_order_does_not_reach_the_payload(self):
        # `PrepareSave` reorders metadata as it writes it, so the payload must
        # not depend on which order a given build happened to emit.
        one = SavedScene()
        one["metadata"] = {"seed": 4242, "comment": "a", "is_puzzle": False}
        other = SavedScene()
        other["metadata"] = {"is_puzzle": False, "seed": 4242, "comment": "a"}

        self.assertEqual(Scene.HashablePayload(one), Scene.HashablePayload(other))

    def test_a_different_seed_changes_the_payload(self):
        other = SavedScene()
        other["metadata"]["seed"] = 4243

        self.assertNotEqual(Scene.HashablePayload(SavedScene()), Scene.HashablePayload(other))

    def test_a_different_input_changes_the_payload(self):
        other = SavedScene()
        other["inputs"] = [{"index": 0, "effect": {"id": 4}}]

        self.assertNotEqual(Scene.HashablePayload(SavedScene()), Scene.HashablePayload(other))

    def test_a_scene_without_metadata_is_tolerated(self):
        no_metadata = SavedScene()
        del no_metadata["metadata"]

        self.assertIn("campaign", Scene.HashablePayload(no_metadata))


class TestDeterministicSave(unittest.TestCase):

    def test_a_deterministic_save_writes_no_ambient_metadata(self):
        scene = Scene()
        scene.SetSeed(4242)

        scene.PrepareSave(FakeGame(), playtime=12.5, deterministic=True)

        for key in AMBIENT_KEYS:
            self.assertNotIn(key, scene.metadata)
        self.assertEqual(scene.metadata["seed"], 4242)

    def test_a_deterministic_save_drops_metadata_carried_in_from_a_loaded_scene(self):
        # Loading a human save and re-saving it for the corpus must not
        # smuggle that machine's fingerprint through.
        scene = Scene()
        scene.SetMetadataStr("sign", "6cc7e9dc535aa2e9a1fb49c2562d5cc9")
        scene.SetMetadataStr("time", "2026-08-06 21-43")
        scene.SetMetadataStr("path", "./replays/from-someones-machine.json")

        scene.PrepareSave(FakeGame(), playtime=None, deterministic=True)

        for key in AMBIENT_KEYS:
            self.assertNotIn(key, scene.metadata)

    def test_a_deterministic_save_ignores_clients(self):
        scene = Scene()

        scene.PrepareSave(FakeGame(), playtime=None, clients=["browser-1"], deterministic=True)

        self.assertNotIn("clients", scene.metadata)

    def test_a_normal_save_still_stamps_sign_time_and_playtime(self):
        scene = Scene()

        with StubFingerprint():
            scene.PrepareSave(FakeGame(), playtime=12.5)

        self.assertEqual(scene.metadata["sign"], "deadbeef")
        self.assertNotEqual(scene.metadata["time"], "")
        self.assertEqual(scene.metadata["playtime"], "12.5")

    def test_a_normal_save_does_not_overwrite_an_existing_stamp(self):
        scene = Scene()
        scene.SetMetadataStr("sign", "original")
        scene.SetMetadataStr("time", "2020-01-01 00-00")

        scene.PrepareSave(FakeGame(), playtime=None)

        self.assertEqual(scene.metadata["sign"], "original")
        self.assertEqual(scene.metadata["time"], "2020-01-01 00-00")

    def test_both_save_modes_agree_on_the_payload(self):
        # The whole point: how a scene was saved must not change what it
        # hashes to.
        def Payload(deterministic):
            scene = Scene()
            scene.SetSeed(4242)
            with StubFingerprint():
                scene.PrepareSave(FakeGame(), playtime=12.5, deterministic=deterministic)
            return Scene.HashablePayload({
                "version": scene.version,
                "metadata": dict(scene.metadata),
                "rules": scene.rules,
                "inputs": scene.inputs,
            })

        self.assertEqual(Payload(True), Payload(False))


if __name__ == "__main__":
    unittest.main()
