"""Tests for the packaging tooling (`tools/package/`, MARVEL-55).

Everything here runs against a synthetic tree in a temp directory. That is the
point: these commands rewrite files and commit, so a test that exercised the
real `build.py` would reintroduce exactly the bug this tooling was moved to fix.

The last test is the guard. `bump` and `zip_cards` used to live in
`unit_test/test_task.py`, where running the suite bumped the version and left a
commit on whatever branch was checked out. `TestSuiteDoesNotPackage` fails if
anything under `unit_test/` reaches for the packaging tooling again.

No engine bootstrap: `tools/package/` is stdlib-only, so this runs anywhere.

    python -m unittest unit_test.test_package_tools
"""

import ast
import subprocess
import tempfile
import unittest
import os
import struct
import zipfile
from pathlib import Path

from tools.package.bump import Bump, ReadField
from tools.package.zip_cards import CardFolders, ZipCards

UNIT_TEST_DIR = Path("unit_test")

# Mirrors `py_src/build.py` byte for byte, trailing blank line included. That
# blank line matters: the first version of `BUILD_LINE` ended in `\s*$`, which
# swallowed everything after the digits, and a fixture ending in a single
# newline is not enough to notice.
BUILD_FILE = (
    "import os\n"
    "\n"
    "class Build:\n"
    '    release = "RELEASE" in os.environ\n'
    "    release = True\n"
    "\n"
    "    # Version\n"
    "    MAJOR = 0\n"
    "    MINOR = 5\n"
    "    PATCH = 9\n"
    "    BUILD = 204\n"
    "\n"
)


def WriteBuildFile(directory: Path, text: str = BUILD_FILE, *, newline: str = "\n") -> Path:
    """Write the fixture as exact bytes.

    Deliberately not `write_text`: its default translates line endings, which is
    the same translation that hid a defect here. Tests assert on `read_bytes`
    for the same reason.
    """
    path = directory / "build.py"
    path.write_bytes(text.replace("\n", newline).encode("utf-8"))
    return path


def InitRepo(directory: Path) -> None:
    """A throwaway git repo with one commit, so HEAD exists to compare against."""
    def Run(*args: str) -> None:
        subprocess.run(args, cwd=directory, check=True,
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    Run("git", "init")
    Run("git", "config", "user.email", "test@example.com")
    Run("git", "config", "user.name", "Test")
    Run("git", "config", "commit.gpgsign", "false")
    Run("git", "add", "-A")
    Run("git", "commit", "-m", "initial")


def Head(directory: Path) -> str:
    result = subprocess.run(["git", "rev-parse", "HEAD"], cwd=directory,
                            check=True, capture_output=True, text=True)
    return result.stdout.strip()


class TestBump(unittest.TestCase):

    def test_increments_the_build_number(self):
        with tempfile.TemporaryDirectory() as tmp:
            build_file = WriteBuildFile(Path(tmp))

            version = Bump(build_file, commit=False)

            self.assertEqual(version, "0.5.9.205")
            self.assertEqual(ReadField(build_file.read_text(encoding="utf-8"), "BUILD"), 205)

    def test_leaves_the_other_version_fields_alone(self):
        with tempfile.TemporaryDirectory() as tmp:
            build_file = WriteBuildFile(Path(tmp))

            Bump(build_file, commit=False)

            text = build_file.read_text(encoding="utf-8")
            self.assertEqual(ReadField(text, "MAJOR"), 0)
            self.assertEqual(ReadField(text, "MINOR"), 5)
            self.assertEqual(ReadField(text, "PATCH"), 9)

    def test_changes_nothing_in_the_file_but_the_number(self):
        """Byte-exact, because that is the only way to see this class of defect.

        Comparing `splitlines()` cannot distinguish a file ending in one newline
        from one ending in none, and reading through `read_text` normalises the
        line endings it would need to be checking.
        """
        with tempfile.TemporaryDirectory() as tmp:
            build_file = WriteBuildFile(Path(tmp))

            Bump(build_file, commit=False)

            self.assertEqual(build_file.read_bytes(),
                             BUILD_FILE.replace("BUILD = 204", "BUILD = 205").encode("utf-8"))

    def test_keeps_the_trailing_blank_line(self):
        with tempfile.TemporaryDirectory() as tmp:
            build_file = WriteBuildFile(Path(tmp))

            Bump(build_file, commit=False)

            # Stated separately from the byte-exact test above so a failure says
            # which property broke.
            self.assertTrue(build_file.read_bytes().endswith(b"BUILD = 205\n\n"))

    def test_preserves_crlf_line_endings(self):
        with tempfile.TemporaryDirectory() as tmp:
            build_file = WriteBuildFile(Path(tmp), newline="\r\n")

            version = Bump(build_file, commit=False)

            self.assertEqual(version, "0.5.9.205")
            self.assertEqual(build_file.read_bytes(),
                             BUILD_FILE.replace("BUILD = 204", "BUILD = 205")
                                       .replace("\n", "\r\n").encode("utf-8"))

    def test_preserves_lf_line_endings(self):
        with tempfile.TemporaryDirectory() as tmp:
            build_file = WriteBuildFile(Path(tmp))

            Bump(build_file, commit=False)

            # The default `write_text` would rewrite these to CRLF on Windows.
            self.assertNotIn(b"\r\n", build_file.read_bytes())

    def test_refuses_a_file_with_no_build_line(self):
        with tempfile.TemporaryDirectory() as tmp:
            build_file = WriteBuildFile(Path(tmp), "class Build:\n    MAJOR = 0\n")

            with self.assertRaises(ValueError):
                Bump(build_file, commit=False)

    def test_no_commit_leaves_head_untouched(self):
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            build_file = WriteBuildFile(directory)
            InitRepo(directory)
            before = Head(directory)

            Bump(build_file, commit=False)

            self.assertEqual(Head(directory), before)

    def test_commit_records_the_new_version(self):
        with tempfile.TemporaryDirectory() as tmp:
            directory = Path(tmp)
            build_file = WriteBuildFile(directory)
            InitRepo(directory)
            before = Head(directory)

            version = Bump(build_file, commit=True)

            self.assertNotEqual(Head(directory), before)
            message = subprocess.run(["git", "log", "-1", "--pretty=%s"], cwd=directory,
                                     check=True, capture_output=True, text=True).stdout.strip()
            self.assertEqual(message, f"Package version {version}")

            # The bump is in the commit, not left dirty in the working tree.
            status = subprocess.run(["git", "status", "--porcelain"], cwd=directory,
                                    check=True, capture_output=True, text=True).stdout
            self.assertEqual(status.strip(), "")


class TestZipCards(unittest.TestCase):

    def MakePack(self, root: Path, folder: str, names: list[str]) -> None:
        path = root / folder
        path.mkdir(parents=True, exist_ok=True)
        for name in names:
            (path / name).write_text("# card\n", encoding="utf-8")

    def test_excludes_scaffolding_and_campaign_files(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.MakePack(root, "pack/core", ["01001.py", "01002.py", "__init__.py", "campaign.py"])
            output = root / "cards.zip"

            written = ZipCards(output, [f"{root.as_posix()}/pack/core/"])

            self.assertEqual(written, 2)
            with zipfile.ZipFile(output) as zf:
                self.assertEqual(sorted(zf.namelist()), ["01001.py", "01002.py"])

    def test_ignores_subdirectories_of_a_listed_folder(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.MakePack(root, "pack/core", ["01001.py"])
            self.MakePack(root, "pack/core/nested", ["01002.py"])
            output = root / "cards.zip"

            # A folder contributes its own files only; nesting is reached by
            # `CardFolders` listing the subfolder separately, not by recursing
            # here. That split is what let the hand-maintained list drift
            # (MARVEL-56) and is why the list is now derived.
            written = ZipCards(output, [f"{root.as_posix()}/pack/core/"])

            self.assertEqual(written, 1)
            with zipfile.ZipFile(output) as zf:
                self.assertEqual(zf.namelist(), ["01001.py"])

    def test_entries_are_ordered_and_named_by_file(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.MakePack(root, "pack/core", ["01003.py", "01001.py", "01002.py"])
            self.MakePack(root, "pack/wsp", ["02001.py"])
            output = root / "cards.zip"

            ZipCards(output, [f"{root.as_posix()}/pack/core/", f"{root.as_posix()}/pack/wsp/"])

            with zipfile.ZipFile(output) as zf:
                self.assertEqual(zf.namelist(), ["01001.py", "01002.py", "01003.py", "02001.py"])

    def test_reproduces_the_same_archive_from_the_same_tree(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            self.MakePack(root, "pack/core", ["01001.py", "01002.py"])
            folders = [f"{root.as_posix()}/pack/core/"]

            ZipCards(root / "a.zip", folders)
            ZipCards(root / "b.zip", folders)

            self.assertEqual((root / "a.zip").read_bytes(),
                             (root / "b.zip").read_bytes())


class TestSuiteDoesNotPackage(unittest.TestCase):
    """The regression guard for MARVEL-55.

    Running the unit suite must not rewrite `build.py`, commit, or drop a zip in
    the working tree. The way that broke before was a packaging chore sitting in
    `unit_test/` under a `test_*` name, so the rule is that no test module may
    reach the packaging tooling at all.
    """

    def test_no_unit_test_module_imports_the_packaging_tooling(self):
        offenders = []
        scanned = 0

        for path in sorted(UNIT_TEST_DIR.glob("*.py")):
            scanned += 1
            if path.name == Path(__file__).name:
                continue

            tree = ast.parse(path.read_text(encoding="utf-8"), filename=str(path))
            for node in ast.walk(tree):
                if isinstance(node, ast.Import):
                    names = [alias.name for alias in node.names]
                elif isinstance(node, ast.ImportFrom):
                    names = [node.module or ""]
                else:
                    continue

                if any(name == "tools.package" or name.startswith("tools.package.")
                       for name in names):
                    offenders.append(f"{path}:{node.lineno}")

        # Without this the test passes vacuously from the wrong working
        # directory -- an empty glob would look like a clean suite. Run from
        # `py_src/`, as AGENTS.md requires.
        self.assertGreater(scanned, 5,
                           f"found {scanned} modules in {UNIT_TEST_DIR.resolve()}; "
                           "run the suite from py_src/")

        self.assertEqual(offenders, [],
                         "packaging chores must stay out of the unit suite -- run "
                         "`python -m tools.package.bump` / `.zip_cards` deliberately")


if __name__ == "__main__":
    unittest.main()


class TestZipCardsIsReproducible(unittest.TestCase):
    """MARVEL-57. The uniform timestamp has to reach the *local file headers*.

    `ZipFile.write()` emits the local header before it returns, using the file's
    mtime. The `ZipInfo` that `getinfo()` hands back afterwards is the one in
    `filelist`, which is only re-serialised into the central directory at close.
    So assigning `date_time` after `write()` left every entry carrying two
    different timestamps -- and because most readers show the central-directory
    value, the archive *looked* uniform while not being reproducible.

    These tests read the local headers by hand rather than through `zipfile`,
    because going through `zipfile` is exactly how the defect stayed invisible.
    """

    @staticmethod
    def LocalHeaderTimes(raw: bytes) -> list[tuple]:
        """Decode the MS-DOS date/time out of every local file header."""
        times, offset = [], 0
        while raw[offset:offset + 4] == b"PK\x03\x04":
            name_len, extra_len = struct.unpack("<HH", raw[offset + 26:offset + 30])
            time_field, date_field = struct.unpack("<HH", raw[offset + 10:offset + 14])
            compressed = struct.unpack("<I", raw[offset + 18:offset + 22])[0]
            times.append((
                ((date_field >> 9) & 0x7F) + 1980, (date_field >> 5) & 0xF,
                date_field & 0x1F, (time_field >> 11) & 0x1F,
                (time_field >> 5) & 0x3F, (time_field & 0x1F) * 2))
            offset += 30 + name_len + extra_len + compressed
        return times

    def Pack(self, root: Path, names: list[str], mtime: float | None = None) -> list[str]:
        path = root / "pack" / "core"
        path.mkdir(parents=True, exist_ok=True)
        for name in names:
            target = path / name
            target.write_text("# card\n", encoding="utf-8")
            if mtime is not None:
                os.utime(target, (mtime, mtime))
        return [f"{root.as_posix()}/pack/core/"]

    def test_the_local_headers_carry_the_uniform_timestamp(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            folders = self.Pack(root, ["01001.py", "01002.py"])
            output = root / "cards.zip"

            ZipCards(output, folders)

            self.assertEqual(set(self.LocalHeaderTimes(output.read_bytes())),
                             {(2022, 1, 1, 0, 0, 0)})

    def test_two_trees_with_different_mtimes_produce_the_same_bytes(self):
        """The claim that matters, and the one the old order could not make.

        Two checkouts on different days are the realistic case: the files are
        identical and their mtimes are not.
        """
        with tempfile.TemporaryDirectory() as tmp:
            first, second = Path(tmp) / "first", Path(tmp) / "second"
            first.mkdir(), second.mkdir()
            a = self.Pack(first, ["01001.py", "01002.py"], mtime=1_600_000_000)
            b = self.Pack(second, ["01001.py", "01002.py"], mtime=1_700_000_000)

            ZipCards(first / "a.zip", a)
            ZipCards(second / "b.zip", b)

            self.assertEqual((first / "a.zip").read_bytes(),
                             (second / "b.zip").read_bytes())

    def test_the_file_mode_does_not_reach_the_archive(self):
        # `write()` copies each file's mode into `external_attr`, so the archive
        # otherwise varied with the umask of whoever built it.
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            folders = self.Pack(root, ["01001.py", "01002.py"])
            os.chmod(root / "pack" / "core" / "01001.py", 0o600)
            os.chmod(root / "pack" / "core" / "01002.py", 0o755)
            output = root / "cards.zip"

            ZipCards(output, folders)

            with zipfile.ZipFile(output) as zf:
                self.assertEqual({i.external_attr for i in zf.infolist()},
                                 {0o644 << 16})


class TestCardFolders(unittest.TestCase):
    """MARVEL-56. The folder set is derived from the tree, not maintained by hand."""

    def test_every_folder_holding_a_card_script_is_found(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            for folder in ("core", "aoa/apocalypse", "gam/gamora_nemesis"):
                path = root / folder
                path.mkdir(parents=True)
                (path / "01001.py").write_text("# card\n", encoding="utf-8")

            self.assertEqual(
                CardFolders(root),
                [f"./{root.as_posix()}/aoa/apocalypse/",
                 f"./{root.as_posix()}/core/",
                 f"./{root.as_posix()}/gam/gamora_nemesis/"])

    def test_a_folder_holding_only_scaffolding_is_not_a_card_folder(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            (root / "empty").mkdir(parents=True)
            (root / "empty" / "__init__.py").write_text("", encoding="utf-8")
            (root / "empty" / "campaign.py").write_text("", encoding="utf-8")

            self.assertEqual(CardFolders(root), [])

    def test_the_real_tree_yields_far_more_than_the_list_it_replaced(self):
        """The regression this exists to prevent.

        The hand-maintained list named 69 folders; walking the tree finds 396.
        Pinning a floor rather than the exact number keeps this from failing
        every time a pack is added, while still failing if the derivation
        breaks and silently returns a subset again.
        """
        if not Path("cards/pack").is_dir():
            self.skipTest("run from py_src/")
        self.assertGreater(len(CardFolders()), 300)

    def test_a_duplicate_arcname_is_refused(self):
        """Arcnames are flat, so a repeated basename would silently overwrite.

        Not reachable today -- 3,455 files, zero collisions -- but it became a
        real risk rather than a theoretical one when the folder set went from 69
        folders to 396.
        """
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            for folder in ("pack/core", "pack/wsp"):
                path = root / folder
                path.mkdir(parents=True)
                (path / "01001.py").write_text("# card\n", encoding="utf-8")

            with self.assertRaises(ValueError) as caught:
                ZipCards(root / "cards.zip",
                         [f"{root.as_posix()}/pack/core/", f"{root.as_posix()}/pack/wsp/"])
            self.assertIn("01001.py", str(caught.exception))
