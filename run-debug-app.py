#!/usr/bin/env python3
"""Build and run the Marvel Champions Godot client on Windows or macOS."""

from __future__ import annotations

import argparse
import os
import platform
import shutil
import subprocess
import sys
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parent
PROJECT = REPO_ROOT / "src" / "Marvel.Godot"


def arguments() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Build and start the Windows or macOS debug client."
    )
    parser.add_argument(
        "--godot",
        type=Path,
        help="Godot 4.7 .NET executable (otherwise uses GODOT_BIN or discovers it).",
    )
    parser.add_argument(
        "--skip-build",
        action="store_true",
        help="Start the client without first building its C# project.",
    )
    parser.add_argument(
        "--dry-run",
        action="store_true",
        help="Print the commands without running them.",
    )
    parser.add_argument(
        "godot_args",
        nargs=argparse.REMAINDER,
        metavar="-- GODOT_ARGUMENT ...",
        help="Arguments after -- are passed to Godot.",
    )
    return parser.parse_args()


def candidates(system: str) -> list[Path]:
    found: list[Path] = []
    configured = os.environ.get("GODOT_BIN")
    if configured:
        found.append(Path(configured).expanduser())

    path_names = ("godot-mono", "godot")
    for name in path_names:
        resolved = shutil.which(name)
        if resolved:
            found.append(Path(resolved))

    if system == "Windows":
        system_drive = os.environ.get("SystemDrive", "C:")
        tools = Path(f"{system_drive}\\") / "Tools"
        patterns = (
            "Godot*_mono_win64_console.exe",
            "Godot*_mono_win64.exe",
            "Godot*/Godot*_mono_win64_console.exe",
            "Godot*/Godot*_mono_win64.exe",
        )
        for pattern in patterns:
            found.extend(sorted(tools.glob(pattern), reverse=True))
    elif system == "Darwin":
        found.extend(
            [
                Path("/Applications/Godot_mono.app/Contents/MacOS/Godot"),
                Path.home()
                / "Applications"
                / "Godot_mono.app"
                / "Contents"
                / "MacOS"
                / "Godot",
                Path("/opt/homebrew/bin/godot-mono"),
                Path("/usr/local/bin/godot-mono"),
            ]
        )
    return found


def find_godot(requested: Path | None, system: str) -> Path:
    if requested is not None:
        executable = requested.expanduser()
        if executable.is_file():
            return executable.resolve()
        raise SystemExit(f"Godot executable does not exist: {executable}")

    for executable in candidates(system):
        if executable.is_file():
            return executable.resolve()

    raise SystemExit(
        "Godot 4.7 .NET was not found. Set GODOT_BIN or pass --godot with "
        "the executable inside the extracted Windows archive or macOS app bundle."
    )


def display(command: list[str]) -> None:
    print(subprocess.list2cmdline(command))


def main() -> int:
    options = arguments()
    godot_args = options.godot_args
    if godot_args[:1] == ["--"]:
        godot_args = godot_args[1:]
    system = platform.system()
    if system not in ("Windows", "Darwin"):
        raise SystemExit("This launcher supports Windows and macOS.")

    godot = find_godot(options.godot, system)
    build = [
        "dotnet",
        "build",
        str(PROJECT / "Marvel.Godot.csproj"),
        "--configuration",
        "Debug",
        "--nologo",
    ]
    launch = [str(godot), "--path", str(PROJECT), *godot_args]

    if options.dry_run:
        if not options.skip_build:
            display(build)
        display(launch)
        return 0

    version = subprocess.run(
        [str(godot), "--version"],
        cwd=REPO_ROOT,
        check=True,
        capture_output=True,
        text=True,
    ).stdout.strip()
    if not version.startswith("4.7.") or "mono" not in version.lower():
        raise SystemExit(f"Godot 4.7 .NET is required; found: {version or 'unknown'}")

    if not options.skip_build:
        subprocess.run(build, cwd=REPO_ROOT, check=True)
    return subprocess.run(launch, cwd=REPO_ROOT, check=False).returncode


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except subprocess.CalledProcessError as error:
        raise SystemExit(error.returncode) from error
