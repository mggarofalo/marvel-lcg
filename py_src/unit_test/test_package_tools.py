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
import zipfile
from pathlib import Path

from tools.package.bump import Bump, ReadField
from tools.package.zip_cards import ZipCards

UNIT_TEST_DIR = Path("unit_test")

BUILD_FILE = """\
import os

class Build:
    release = "RELEASE" in os.environ
    release = True

    # Version
    MAJOR = 0
    MINOR = 5
    PATCH = 9
    BUILD = 204
"""


def WriteBuildFile(directory: Path, text: str = BUILD_FILE) -> Path:
    path = directory / "build.py"
    path.write_text(text, encoding="utf-8")
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

    def test_rewrites_only_the_build_line(self):
        with tempfile.TemporaryDirectory() as tmp:
            build_file = WriteBuildFile(Path(tmp))

            Bump(build_file, commit=False)

            before = BUILD_FILE.splitlines()
            after = build_file.read_text(encoding="utf-8").splitlines()
            differing = [(b, a) for b, a in zip(before, after) if b != a]
            self.assertEqual(differing, [("    BUILD = 204", "    BUILD = 205")])

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

            # Each folder is listed explicitly; a listed folder contributes its
            # own files only. This is what makes MARVEL-56 possible.
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

            # Same process, same mtimes: the entry lists must agree. Byte
            # equality is a stronger claim that MARVEL-57 blocks.
            with zipfile.ZipFile(root / "a.zip") as a, zipfile.ZipFile(root / "b.zip") as b:
                self.assertEqual(a.namelist(), b.namelist())
                self.assertEqual([i.date_time for i in a.infolist()],
                                 [i.date_time for i in b.infolist()])


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
