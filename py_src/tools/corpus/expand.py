"""Turn published shards back into a corpus, and prove it is the right one.

    python -m tools.corpus.expand ./frozen/ --out ./corpus/
    python -m tools.corpus.expand ./frozen/ --out ./corpus/ --manifest ../datasets/corpus/manifest.json
    python -m tools.corpus.expand ./frozen/ --only rhino --out ./corpus/

Run from `py_src/`.

## Why this exists

`tools.corpus.freeze --shards` writes the corpus as one gzipped file per
scenario, and until this module there was nothing that read them back. That is
a worse gap than it sounds: the shards are the **only** published artefact --
`mggarofalo/marvel-lcg-corpus` holds them and nothing else -- so a corpus that
could not be expanded was a corpus nobody could use.

It also hid a correctness defect. The manifest hashes each scene's **bytes on
disk**, and those bytes are not canonical JSON. The first version of `Shard`
stored the *parsed* document, so expanding it re-serialised each scene and lost
about 0.3% of its length to separator and key-order differences. Every
expanded scene would have failed the manifest check, and the failure would have
looked like corruption rather than like a lossy round trip. Shards now carry
the exact text.

## What `--manifest` proves

Without it, expansion is a decompression. With it, every scene is hashed as it
lands and compared to the manifest entry for its path, and the root hash of the
whole expanded set is recomputed. That is the difference between "these bytes
came out of a gzip file" and "these are the scenes the oracle was frozen
from" -- which is the only claim worth making about an oracle.
"""

from __future__ import annotations

import argparse
import gzip
import hashlib
import json
import os
import sys
from typing import Dict, List, Sequence

from tools.corpus.freeze import RootHash, Entry


def read_shard(path: str) -> Dict[str, str]:
    """One shard: scene path -> the scene's exact text."""
    with gzip.open(path, "rb") as handle:
        bundle = json.loads(handle.read().decode("utf-8"))
    if not isinstance(bundle, dict):
        raise SystemExit(f"{path}: not a shard")
    for scene_path, contents in bundle.items():
        if not isinstance(contents, str):
            raise SystemExit(
                f"{path}: {scene_path} holds a parsed document rather than the "
                "scene's text. This shard was written before the format was "
                "fixed and cannot round-trip; re-run `tools.corpus.freeze "
                "--shards`.")
    return bundle


def shard_paths(root: str, only: Sequence[str]=()) -> List[str]:
    if not os.path.isdir(root):
        raise SystemExit(f"no shard directory at {root}")
    names = sorted(n for n in os.listdir(root) if n.endswith(".json.gz"))
    if only:
        wanted = {o.lower() for o in only}
        names = [n for n in names if n[:-len(".json.gz")].lower() in wanted]
        if not names:
            raise SystemExit(f"no shard matched {', '.join(only)}")
    return [os.path.join(root, n) for n in names]


def expand(shards: str, out: str, *, only: Sequence[str]=(),
           manifest_path: str="") -> int:
    manifest: Dict = {}
    if manifest_path:
        with open(manifest_path, encoding="utf-8") as handle:
            manifest = json.load(handle)
    expected = {entry["path"]: entry["sha256"]
                for entry in (manifest.get("entries") or [])}

    written: List[Entry] = []
    mismatched: List[str] = []
    unknown: List[str] = []

    for shard in shard_paths(shards, only):
        for scene_path, contents in sorted(read_shard(shard).items()):
            data = contents.encode("utf-8")
            target = os.path.join(out, scene_path.replace("/", os.sep))
            os.makedirs(os.path.dirname(target), exist_ok=True)
            with open(target, "wb") as handle:
                handle.write(data)

            digest = hashlib.sha256(data).hexdigest()
            written.append(Entry(path=scene_path, sha256=digest,
                                 bytes=len(data),
                                 scenario=os.path.basename(shard)[:-len(".json.gz")]))
            if expected:
                if scene_path not in expected:
                    unknown.append(scene_path)
                elif expected[scene_path] != digest:
                    mismatched.append(scene_path)

    print(f"expanded {len(written)} scene(s) into {out}")

    if not expected:
        print("no --manifest given: the bytes were decompressed, not verified")
        return 0

    for path in mismatched[:10]:
        print(f"  MISMATCH {path}")
    for path in unknown[:10]:
        print(f"  NOT IN MANIFEST {path}")
    if mismatched or unknown:
        print(f"\n{len(mismatched)} scene(s) do not match the manifest, "
              f"{len(unknown)} are not in it")
        return 1

    if not only:
        # A per-scene match says each file is right. The root hash says the
        # *set* is right -- that nothing was quietly left out, which per-file
        # hashing cannot notice.
        #
        # Sorted by path, because `RootHash` hashes `path\tsha256\n` lines in
        # the order given and `Collect` sorts before hashing. Expanding walks
        # shards instead, which is a different order for the same set -- so
        # without this the root hash differs while every single scene matches,
        # which reads like corruption and is not.
        root = RootHash(sorted(written, key=lambda entry: entry.path))
        if root != manifest.get("root_sha256"):
            print(f"\nroot hash {root}\n     want {manifest.get('root_sha256')}")
            print(f"every scene matched individually, but {len(written)} of "
                  f"{manifest.get('scenes')} were expanded")
            return 1
        print(f"root hash matches: {root}")
    else:
        print(f"all {len(written)} expanded scene(s) match the manifest "
              "(subset: root hash not checked)")
    return 0


def main(argv: Sequence[str] | None=None) -> int:
    parser = argparse.ArgumentParser(
        description="Expand gzipped corpus shards back into scenes.")
    parser.add_argument("shards", help="directory of .json.gz shards")
    parser.add_argument("--out", required=True, help="where to write scenes")
    parser.add_argument("--manifest", default="",
                        help="verify every scene against this manifest")
    parser.add_argument("--only", nargs="*", default=[],
                        help="expand only these shards, by name")
    args = parser.parse_args(argv)
    return expand(args.shards, args.out, only=args.only,
                  manifest_path=args.manifest)


if __name__ == "__main__":
    raise SystemExit(main())
