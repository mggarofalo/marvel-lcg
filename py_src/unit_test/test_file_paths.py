"""`FileManager.FormatPath` anchors relative paths and leaves absolute ones alone.

The engine addresses almost everything relative to `py_src/` -- `launch.json`
points at `./data/`, `./replays/`, `./assets/` -- so `FormatPath` exists to turn
`data/x` into `./data/x`. The part that matters is which paths it must *not*
touch.

"Absolute" used to mean `path[1] == ":"`: a Windows drive letter, and nothing
else. Every POSIX absolute path therefore took the relative branch and came
back anchored, so `/tmp/x` became `./tmp/x` and resolved against the working
directory. Nothing on Linux could hand the engine a `tempfile.mkdtemp()`
directory, which is what the replay and invariant probes do, and they failed
there for that reason alone. See MARVEL-72.

Absoluteness is a per-platform question and these tests treat it as one.
`os.path.isabs` is the definition on both, and it is not symmetric: since
Python 3.13 Windows no longer counts a single leading slash, so `/tmp/x` is
absolute on Linux and relative on Windows. Asserting a fixed string either way
would pin one platform's answer onto the other.
"""

import os
import sys
import tempfile
import unittest

import engine  # noqa: F401  pylint: disable=unused-import
from engine.file import FileManager


class TestAbsolutePathsSurviveUntouched(unittest.TestCase):
    """The realistic case: what `tempfile.mkdtemp()` hands the probe tools."""

    def setUp(self):
        self.absolute = tempfile.gettempdir()
        # Guard the premise rather than assume it. If this is ever false the
        # rest of the class is testing nothing.
        self.assertTrue(os.path.isabs(self.absolute))

    def test_a_native_absolute_path_is_returned_unchanged(self):
        self.assertEqual(FileManager.FormatPath(self.absolute),
                         os.path.normpath(self.absolute))

    def test_a_native_absolute_path_is_not_anchored(self):
        # The exact corruption: './' + '/tmp/x' -> './/tmp/x' -> './tmp/x',
        # which resolves under py_src/ and does not exist.
        self.assertFalse(FileManager.FormatPath(self.absolute).startswith("."))

    def test_a_deeper_absolute_path_is_returned_unchanged(self):
        deeper = os.path.join(self.absolute, "verify-probe", "short")

        self.assertEqual(FileManager.FormatPath(deeper), os.path.normpath(deeper))

    @unittest.skipIf(sys.platform == "win32", "POSIX absolute paths only")
    def test_a_posix_root_path_is_absolute_here(self):
        self.assertEqual(FileManager.FormatPath("/tmp/savetest/"), "/tmp/savetest")

    @unittest.skipUnless(sys.platform == "win32", "drive letters only")
    def test_a_windows_drive_path_is_absolute_here(self):
        self.assertFalse(FileManager.FormatPath("C:/Temp/x").startswith("."))
        self.assertFalse(FileManager.FormatPath("C:\\Temp\\x").startswith("."))


class TestRelativePathsAreAnchored(unittest.TestCase):

    def test_a_bare_folder_is_anchored(self):
        self.assertTrue(FileManager.FormatPath("data/x").startswith("."))

    def test_an_anchored_path_is_not_anchored_twice(self):
        # normpath drops the leading './', so the guard is that it comes back
        # with exactly one anchor rather than './/' or '././'.
        formatted = FileManager.FormatPath("./data/x")

        self.assertTrue(formatted.startswith("."))
        self.assertFalse(formatted.startswith("./."))
        self.assertFalse(formatted.startswith(".//"))

    def test_a_parent_relative_path_keeps_its_meaning(self):
        self.assertTrue(FileManager.FormatPath("../data/x").startswith(".."))


class TestShortPaths(unittest.TestCase):
    """`path[1]` raised IndexError on anything shorter than two characters.

    Reachable from an empty config value, which is the state MARVEL-72 left a
    valued flag in before that was made fatal.
    """

    def test_a_single_slash_does_not_raise(self):
        FileManager.FormatPath("/")

    def test_a_single_dot_does_not_raise(self):
        FileManager.FormatPath(".")

    def test_an_empty_string_does_not_raise(self):
        FileManager.FormatPath("")

    def test_a_single_letter_is_anchored(self):
        self.assertTrue(FileManager.FormatPath("x").startswith("."))


if __name__ == "__main__":
    unittest.main()
