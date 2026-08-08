"""Increment the build number in `build.py` and commit the change.

`Build.BUILD` identifies a package. Bumping it is a release step, so this is a
command you run on purpose -- see `tools/package/__init__.py` for why it is no
longer a test.

Run from `py_src/`:

    python -m tools.package.bump              # bump and commit
    python -m tools.package.bump --no-commit  # bump only, leave it unstaged
"""

import argparse
import re
import subprocess
from pathlib import Path

DEFAULT_BUILD_FILE = Path("build.py")

# Anchored to the whole line so a bare `BUILD = <n>` assignment is the only
# thing that can match -- MAJOR, MINOR and PATCH sit beside it in the file.
BUILD_LINE = re.compile(r"^(?P<prefix>\s*BUILD\s*=\s*)(?P<value>\d+)\s*$", re.MULTILINE)


def ReadField(text: str, name: str) -> int:
    """The integer assigned to `name` at the top level of a build file."""
    match = re.search(rf"^\s*{name}\s*=\s*(\d+)\s*$", text, re.MULTILINE)
    if match is None:
        raise ValueError(f"no `{name} = <int>` assignment found")
    return int(match.group(1))


def Bump(build_file: Path = DEFAULT_BUILD_FILE, *, commit: bool = True) -> str:
    """Increment `BUILD` in `build_file`. Returns the new full version string.

    With `commit`, stages the file and commits it. Git failures propagate:
    a rewritten `build.py` that was never committed is not a successful bump.
    """
    text = build_file.read_text(encoding="utf-8")

    build = ReadField(text, "BUILD") + 1
    version = f"{ReadField(text, 'MAJOR')}.{ReadField(text, 'MINOR')}.{ReadField(text, 'PATCH')}.{build}"

    rewritten, count = BUILD_LINE.subn(rf"\g<prefix>{build}", text, count=1)
    if count != 1:
        raise ValueError(f"no `BUILD = <int>` line to rewrite in {build_file}")
    build_file.write_text(rewritten, encoding="utf-8")

    if commit:
        cwd = build_file.resolve().parent
        Git(cwd, "add", "--", build_file.name)
        Git(cwd, "commit", "-m", f"Package version {version}")

    return version


def Git(cwd: Path, *args: str) -> None:
    """Run git, quietly. On failure the captured output goes into the exception.

    Captured rather than inherited so the command reports the bump itself instead
    of echoing git's chatter -- and so a caller running under a test suite does
    not have git's output interleaved into theirs.
    """
    result = subprocess.run(["git", *args], cwd=cwd, capture_output=True, text=True)
    if result.returncode != 0:
        raise subprocess.CalledProcessError(
            result.returncode, result.args, output=result.stdout, stderr=result.stderr)


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--no-commit", action="store_true",
                        help="rewrite the version without staging or committing")
    parser.add_argument("--build-file", type=Path, default=DEFAULT_BUILD_FILE,
                        help=f"build file to rewrite (default {DEFAULT_BUILD_FILE})")
    args = parser.parse_args(argv)

    version = Bump(args.build_file, commit=not args.no_commit)

    print(f"package version {version}")
    if args.no_commit:
        print(f"  {args.build_file} rewritten, not committed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
