"""Freeze a corpus: content-address it, and say where it came from.

    python -m tools.corpus.freeze ./corpus/ --out ../datasets/corpus/manifest.json
    python -m tools.corpus.freeze ./corpus/ --check ../datasets/corpus/manifest.json
    python -m tools.corpus.freeze ./corpus/ --out manifest.json --shards ./frozen/

From here the corpus is immutable and every later phase validates against it.
A corpus that can drift is not an oracle. See MARVEL-18.

That is a claim about *this* corpus, not about the project. Freezing marks a
phase boundary -- work stops on `py_src`, starts on the C# engine -- and a
later rules fix means a *new* corpus at a new SHA, not a mutated one. What must
never happen is a frozen corpus changing underneath a validation run; cutting a
second one is ordinary.

## What is hashed, and what is only recorded

The root hash covers the **scene files and nothing else**. That is a decision
rather than an omission, and it has a measured reason: since MARVEL-34 a
per-run `bot-manifest-*.json` records the fully resolved config, which includes
`bot_save_folder` -- an absolute path. Two byte-identical corpora generated into
different directories therefore have different run manifests. Hashing them would
make the root hash depend on where the generator happened to be standing.

The scenes are the oracle. Everything else is *recorded* with its own hash under
`artefacts`, so drift in the coverage report or a run manifest is visible
without making the identity of the corpus depend on derived data.

Three things are fenced out of the hash entirely, in `provenance`: the Python
version, the platform, and the freeze date. They are what MARVEL-18 asks to
record and they are exactly what cannot be reproduced -- a manifest that changed
because it was rebuilt on a different machine would be useless as an integrity
check. The engine git SHA is provenance too, but it *is* reproducible, so it
sits there as the thing that says which engine to check the corpus out against.

## Quarantined scenes are excluded, by name and on the record

`-verify_quarantine_folder` (MARVEL-17) writes `quarantine.json` listing every
scene that failed to reproduce. `--quarantine` reads it and leaves those scenes
out of the frozen set, recording how many and which. A non-reproducing replay
must not enter the corpus; it must also not vanish without trace.

## Sharding

Entries are grouped by scenario, read from each scene's own `campaign.name`
rather than parsed out of its filename -- MARVEL-4 asks for shards so CI can
fetch a subset instead of the whole corpus, and a shard boundary derived from a
filename would move the first time the naming changes.

`--shards` writes the gzipped shard tree that decision calls for, one
`.json.gz` per scenario, ready to commit to the corpus repo. The manifest goes
in *this* repo, so integrity is checkable without fetching the corpus at all.
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import os
import platform
import sys
from typing import Any, Dict, List, NamedTuple, Sequence

MANIFEST_VERSION = 1

RUN_ARTEFACT_PREFIXES = ("bot-manifest-", "bot-coverage-")
NOT_A_SCENE = ("progress.jsonl", "corpus-manifest.json", "corpus-rounds.json",
               "quarantine.json", "verify.json")


class Entry(NamedTuple):
    path: str           # corpus-relative, forward slashes
    sha256: str
    bytes: int
    scenario: str

    def ToDict(self) -> Dict[str, Any]:
        return {"path": self.path, "sha256": self.sha256,
                "bytes": self.bytes, "scenario": self.scenario}


def Sha256(path: str) -> str:
    digest = hashlib.sha256()
    with open(path, "rb") as handle:
        for block in iter(lambda: handle.read(1 << 20), b""):
            digest.update(block)
    return digest.hexdigest()


def IsScene(path: str) -> bool:
    """A saved scene, rather than a run artefact sitting beside one.

    Recognised by the two fields a game cannot be built without, the same way
    `ReplayVerifier.IsSceneDocument` does it -- every field of `Scene` has a
    default, so loading a manifest as one *succeeds* and yields an empty game.
    The filename checks are a cheap pre-filter, not the test.
    """
    name = os.path.basename(path)
    if name in NOT_A_SCENE or name.startswith(RUN_ARTEFACT_PREFIXES):
        return False
    if not name.endswith(".json"):
        return False
    try:
        with open(path, encoding="utf-8") as handle:
            document = json.load(handle)
    except (OSError, ValueError):
        return False
    return (isinstance(document, dict)
            and bool(document.get("players")) and bool(document.get("campaign")))


def ScenarioOf(path: str) -> str:
    """The scenario a scene was played against, from the scene itself."""
    try:
        with open(path, encoding="utf-8") as handle:
            document = json.load(handle)
    except (OSError, ValueError):
        return "(unreadable)"
    campaign = document.get("campaign")
    if isinstance(campaign, dict):
        return str(campaign.get("name") or "(unnamed)")
    return "(unnamed)"


def Quarantined(folder: str) -> List[str]:
    """Scene filenames a verification run set aside. See MARVEL-17."""
    path = os.path.join(folder, "quarantine.json")
    if not os.path.isfile(path):
        return []
    try:
        with open(path, encoding="utf-8") as handle:
            index = json.load(handle)
    except (OSError, ValueError):
        return []
    return sorted({str(case.get("file")) for case in (index.get("cases") or [])
                   if case.get("file")})


def Collect(root: str, exclude: Sequence[str]=()) -> List[Entry]:
    """Every scene under `root`, sorted, with its hash."""
    skip = set(exclude)
    entries: List[Entry] = []
    for current, subfolders, names in os.walk(root):
        subfolders.sort()
        for name in sorted(names):
            if name in skip:
                continue
            path = os.path.join(current, name)
            if not IsScene(path):
                continue
            entries.append(Entry(
                path=os.path.relpath(path, root).replace(os.sep, "/"),
                sha256=Sha256(path),
                bytes=os.path.getsize(path),
                scenario=ScenarioOf(path),
            ))
    # Sorted by corpus-relative path, so the root hash does not depend on
    # filesystem walk order.
    return sorted(entries, key=lambda entry: entry.path)


def RootHash(entries: Sequence[Entry]) -> str:
    """One hash over the whole set.

    Over `path\\tsha256\\n` lines rather than over the concatenated file bytes:
    a corpus with two identical scenes at different paths is a different corpus
    from one with a single scene, and a hash over content alone could not tell
    them apart. Renaming a scene therefore changes the root hash, which is the
    intended behaviour -- the path is part of the corpus.
    """
    digest = hashlib.sha256()
    for entry in entries:
        digest.update(f"{entry.path}\t{entry.sha256}\n".encode("utf-8"))
    return digest.hexdigest()


def Artefacts(root: str) -> Dict[str, Any]:
    """Hashes of what sits beside the scenes, recorded but not part of identity."""
    found: Dict[str, Any] = {}
    for current, subfolders, names in os.walk(root):
        subfolders.sort()
        for name in sorted(names):
            if not (name.startswith(RUN_ARTEFACT_PREFIXES) or name in NOT_A_SCENE):
                continue
            path = os.path.join(current, name)
            if not os.path.isfile(path):
                continue
            found[os.path.relpath(path, root).replace(os.sep, "/")] = Sha256(path)
    return found


def Provenance(dated: str) -> Dict[str, Any]:
    """Where this corpus came from. Deliberately outside the root hash."""
    from engine.config_record import ConfigRecord

    return {
        "engine_git_sha": ConfigRecord.GitSha(),
        "python": platform.python_version(),
        "platform": f"{platform.system()} {platform.machine()}",
        "frozen": dated,
    }


def Build(root: str, *, quarantine: str="", dated: str="") -> Dict[str, Any]:
    excluded = Quarantined(quarantine) if quarantine else []
    entries = Collect(root, excluded)

    shards: Dict[str, List[str]] = {}
    for entry in entries:
        shards.setdefault(entry.scenario, []).append(entry.path)

    return {
        "tool": "corpus-freeze",
        "manifest_version": MANIFEST_VERSION,
        # The identity of the corpus. Everything below `provenance` is outside
        # it on purpose -- see the module docstring.
        "root_sha256": RootHash(entries),
        "scenes": len(entries),
        "bytes": sum(entry.bytes for entry in entries),
        "shards": {name: sorted(paths) for name, paths in sorted(shards.items())},
        "entries": [entry.ToDict() for entry in entries],
        "excluded": {
            "quarantined": excluded,
            "count": len(excluded),
            "source": quarantine or "",
        },
        "artefacts": Artefacts(root),
        "provenance": Provenance(dated),
    }


def Check(root: str, manifest_path: str, *, quarantine: str="") -> List[str]:
    """Differences between a recorded manifest and the corpus on disk.

    Empty means the corpus is exactly what was frozen. Every problem is
    reported rather than the first, because "one file changed" and "everything
    changed" want different responses.
    """
    try:
        with open(manifest_path, encoding="utf-8") as handle:
            manifest = json.load(handle)
    except (OSError, ValueError) as exc:
        return [f"cannot read {manifest_path}: {exc}"]

    version = manifest.get("manifest_version")
    if version != MANIFEST_VERSION:
        return [f"manifest version {version!r}, this tool writes "
                f"{MANIFEST_VERSION}"]

    excluded = Quarantined(quarantine) if quarantine else list(
        (manifest.get("excluded") or {}).get("quarantined") or [])
    entries = Collect(root, excluded)

    recorded = {str(item.get("path")): str(item.get("sha256"))
                for item in (manifest.get("entries") or [])}
    current = {entry.path: entry.sha256 for entry in entries}

    problems: List[str] = []
    for path in sorted(set(recorded) | set(current)):
        if path not in current:
            problems.append(f"missing: {path}")
        elif path not in recorded:
            problems.append(f"added:   {path}")
        elif recorded[path] != current[path]:
            problems.append(f"changed: {path}")

    root_hash = RootHash(entries)
    if root_hash != manifest.get("root_sha256"):
        # Last, and reported even when the per-file list explains it: the root
        # hash is the claim, and a reader should see it fail rather than infer
        # that it must have.
        problems.append(
            f"root hash {root_hash} does not match the recorded "
            f"{manifest.get('root_sha256')}")
    return problems


def Shard(root: str, manifest: Dict[str, Any], out: str) -> List[str]:
    """Write one gzipped file per scenario, as MARVEL-4 decided.

    Gzip because the measurement said so -- 8.2x on v1-era scenes and 69.6x on
    the far more repetitive v2 documents. One file per scenario because CI
    should be able to fetch the shard it needs instead of the whole corpus.
    """
    os.makedirs(out, exist_ok=True)
    written: List[str] = []
    for scenario, paths in sorted((manifest.get("shards") or {}).items()):
        bundle = {}
        for path in paths:
            with open(os.path.join(root, path.replace("/", os.sep)),
                      encoding="utf-8") as handle:
                bundle[path] = json.load(handle)
        name = "".join(char if char.isalnum() or char in "-_" else "_"
                       for char in scenario.lower()) or "unnamed"
        target = os.path.join(out, f"{name}.json.gz")
        # `mtime=0` and a fixed compresslevel: a gzip header carries a
        # timestamp, and a shard that changed only because it was rebuilt on
        # Tuesday is not an immutable artefact.
        with gzip.GzipFile(target, "wb", compresslevel=9, mtime=0) as handle:
            handle.write(json.dumps(bundle, sort_keys=True,
                                    separators=(",", ":")).encode("utf-8"))
        written.append(target)
    return written


def Summarize(manifest: Dict[str, Any]) -> List[str]:
    provenance = manifest.get("provenance") or {}
    excluded = manifest.get("excluded") or {}
    lines = [
        f"scenes     {manifest['scenes']} "
        f"({manifest['bytes'] / 1024:.1f} KiB) in "
        f"{len(manifest.get('shards') or {})} shard(s)",
        f"root       {manifest['root_sha256']}",
        f"engine     {provenance.get('engine_git_sha') or '(not a git checkout)'}",
    ]
    if excluded.get("count"):
        lines.append(f"excluded   {excluded['count']} quarantined scene(s)")
    return lines


def main(argv: List[str] | None=None) -> int:
    parser = argparse.ArgumentParser(
        description=__doc__,
        formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("corpus", help="the corpus root to freeze or check")
    parser.add_argument("--out", default="", help="write the manifest here")
    parser.add_argument("--check", default="",
                        help="verify the corpus against this manifest")
    parser.add_argument("--quarantine", default="",
                        help="folder holding quarantine.json; its scenes are excluded")
    parser.add_argument("--shards", default="",
                        help="write the gzipped per-scenario shard tree here")
    parser.add_argument("--dated", default="",
                        help="freeze date to record; provenance only, never hashed")
    args = parser.parse_args(argv)

    if not os.path.isdir(args.corpus):
        print(f"error: {args.corpus} is not a directory")
        return 2

    if args.check:
        problems = Check(args.corpus, args.check, quarantine=args.quarantine)
        if problems:
            print(f"corpus does not match {args.check}:")
            for problem in problems[:50]:
                print(f"  {problem}")
            if len(problems) > 50:
                print(f"  ... and {len(problems) - 50} more")
            return 1
        print(f"corpus matches {args.check}")
        return 0

    manifest = Build(args.corpus, quarantine=args.quarantine, dated=args.dated)
    if not manifest["scenes"]:
        # Freezing nothing must not look like freezing everything, the same
        # reasoning `-verify_replays` applies to an empty folder.
        print(f"error: no scenes found under {args.corpus}")
        return 1

    for line in Summarize(manifest):
        print(line)

    if args.shards:
        written = Shard(args.corpus, manifest, args.shards)
        print(f"shards     {len(written)} file(s) in {args.shards}")

    if args.out:
        os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
        with open(args.out, "w", encoding="utf-8") as handle:
            json.dump(manifest, handle, indent=2, sort_keys=True)
        print(f"wrote      {args.out}")

    return 0


if __name__ == "__main__":
    raise SystemExit(main())
