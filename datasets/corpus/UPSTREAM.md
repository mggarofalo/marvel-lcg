# Frozen replay corpus — manifest and pin

The **manifest** for the frozen corpus. The scenes themselves are 579 MB and
live in their own repository (MARVEL-4); only their hashes are here, so
integrity is checkable without fetching them.

| | |
|---|---|
| Corpus repository | https://github.com/mggarofalo/marvel-lcg-corpus |
| Pinned commit | `9642b291b2e337126e933cfa7f1d1ccae8fb75a8` |
| Root hash | `b4e3946150a02dc6f77b2c9513d3ed5eaff209827734163b40c905d2c6fa212d` |
| Engine | `3446f61` |
| Frozen | 2026-08-24 |

**1,773 scenes, 58 shards, 631 MB gzipped** (9.7 GiB raw, 15.4×).

## How it was made

Coverage-directed generation over **all 108 scenarios and all 71 heroes** at one
to four players, then replay-verified scene by scene:

> every scene reproduces every recorded step with every digest matching.

Zero divergences. That is the property the corpus exists to have.

## Why three rounds and not six

Generation ran six coverage-directed rounds. The return collapses almost
immediately:

| Round | Cards newly resolved | Scenes | Shards |
|---|---|---|---|
| 1 | **+3,145** | 910 | 229 MB |
| 2 | +92 | 879 | +222 MB |
| 3 | +51 | 879 | +221 MB |
| 4 | +41 | 875 | +221 MB |
| 5 | +10 | 865 | +215 MB |
| 6 | +11 | 872 | +227 MB |

Round 1 resolved 94% of everything six rounds reach. Rounds 4–6 cost 11 MB per
additional card against round 3's 4.3 MB, so the frozen set is **rounds 1–3**:
3,288 cards resolved, 98.2% of what the full run achieved, at half the bytes.

Rounds 4–6 were generated and discarded rather than never run — which is how
the curve above is known rather than assumed.

## The 27 quarantined scenes are an acceptance list

Each reproduced every recorded step with every digest matching and *then* made
the engine log an error. They are not divergences and they are not corpus, and
they are named in the manifest under `excluded`.

They are the most useful thing here for the port: cases the C# engine should
handle correctly **because** the Python oracle could not. A long tail across 12
different scenarios rather than one clustered bug.

## Fetching and verifying

```bash
git clone https://github.com/mggarofalo/marvel-lcg-corpus ../../marvel-lcg-corpus
cd py_src
python -m tools.corpus.expand ../../marvel-lcg-corpus/ \
    --out ./corpus/ --manifest ../datasets/corpus/manifest.json
```

`--manifest` makes it a verification rather than a decompression: every scene
is hashed as it lands and compared to its manifest entry, then the root hash
over the whole set is recomputed so an omission cannot pass as success.
`--only <scenario>` expands one shard, which is why they are split by scenario.

A corpus already on disk is checked in place:

```bash
python -m tools.corpus.freeze ./corpus/ --check ../datasets/corpus/manifest.json
```

### Shards carry text, not documents

Worth knowing before touching `Shard`. The manifest hashes each scene's **bytes
on disk**, and scenes are not written as canonical JSON. The first version of
the shard format stored the *parsed* document, so expansion re-serialised each
scene and lost ~0.3% of its length to separator and key-order differences —
every expanded scene failed its hash, and the failure looked like corruption
rather than like a lossy format. Costs 8% in size; buys an artefact that can be
checked against the manifest that describes it.

Freezing is a **phase boundary**, not a one-way door — see `docs/corpus.md`. If
a rules error surfaces, `py_src` is fixed and a *new* corpus is cut at a new
SHA. What must never happen is this one changing underneath a validation run.
