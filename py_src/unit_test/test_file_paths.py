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


class TestIsAbsolute(unittest.TestCase):
    """`FileManager.IsAbsolute`, which replaced `IsDrivePath` (MARVEL-74).

    The old test asked whether a path began with a Windows drive letter. Both
    of its callers were asking whether the path should be used as given rather
    than searched for, and on POSIX the answer was always False -- so
    `FindJsonPath` joined an absolute path onto every folder in its search list
    instead of returning it.
    """

    def test_a_native_absolute_path_is_absolute(self):
        self.assertTrue(FileManager.IsAbsolute(tempfile.gettempdir()))

    def test_a_relative_path_is_not(self):
        self.assertFalse(FileManager.IsAbsolute("data/x.json"))
        self.assertFalse(FileManager.IsAbsolute("./data/x.json"))

    def test_a_windows_drive_path_is_still_recognised_on_windows(self):
        """The replacement is a superset, not a swap.

        `IsDrivePath` answered True for these; `os.path.isabs` does too, on the
        platform where they mean anything. Asserting them on POSIX would be
        asserting the wrong platform's answer -- there, `C:/x` is a relative
        path naming a directory called `C:`.
        """
        if sys.platform != "win32":
            self.skipTest("drive letters are only absolute on Windows")
        self.assertTrue(FileManager.IsAbsolute("C:/x"))
        self.assertTrue(FileManager.IsAbsolute("C:\\x"))

    def test_a_short_path_does_not_raise(self):
        # `IsDrivePath` indexed [1:3] behind a length guard; the point here is
        # that the replacement has no length precondition at all.
        for path in ("", ".", "/", "a"):
            with self.subTest(path=path):
                FileManager.IsAbsolute(path)


class TestListFiles(unittest.TestCase):
    """`FileManager.ListFiles`, whose condition did not parse as it was laid out.

    `and` binds tighter than `or`, so

        IsFile(...) and ext == None or ext == GetExtension(f) and (...)

    meant `(IsFile and ext is None) or (ext matches and ...)`. With the default
    `ext=None` that dropped `check_file_name` entirely; with an `ext` given it
    dropped the `IsFile` test, so a directory named `foo.json` was returned as
    a file. See MARVEL-79.
    """

    def setUp(self):
        self.folder = tempfile.mkdtemp()
        for name in ("a.json", "b.json", "c.txt"):
            with open(os.path.join(self.folder, name), "w", encoding="utf-8") as handle:
                handle.write("{}")
        # A *directory* that ends in the extension being filtered for. This is
        # the one the old precedence returned as a file.
        os.mkdir(os.path.join(self.folder, "decoy.json"))

    def Names(self, **kwargs):
        return sorted(os.path.basename(p)
                      for p in FileManager.ListFiles(self.folder, **kwargs))

    def test_a_directory_ending_in_the_extension_is_not_a_file(self):
        self.assertEqual(self.Names(ext=".json"), ["a.json", "b.json"])

    def test_check_file_name_runs_when_no_extension_is_given(self):
        # Silently ignored before the fix: every file came back.
        self.assertEqual(self.Names(check_file_name=lambda n: n.startswith("a")),
                         ["a.json"])

    def test_check_file_name_and_extension_apply_together(self):
        self.assertEqual(
            self.Names(ext=".json", check_file_name=lambda n: n.startswith("b")),
            ["b.json"])

    def test_no_filters_returns_every_file_but_no_directory(self):
        self.assertEqual(self.Names(), ["a.json", "b.json", "c.txt"])

    def test_a_missing_folder_contributes_nothing(self):
        self.assertEqual(
            FileManager.ListFiles(os.path.join(self.folder, "nope"), ext=".json"), [])
