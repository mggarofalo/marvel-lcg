# MarvelSDB card data — vendored snapshot

Printed card text for every released Marvel Champions card, maintained by the
MarvelSDB community. This is the **authoritative text** the spec-authoring
dataset is built from: the words the game's designers wrote, transcribed and
proofread by people who own the cards.

| | |
|---|---|
| Upstream | https://github.com/zzorba/marvelsdb-json-data |
| Commit | `dc6201686331c34c061ea57e0fdb3956585149cb` |
| Committed | 2026-07-23 |
| Vendored | 2026-08-06 |

## Why vendored rather than fetched

`datasets/cards/` must be regenerable offline and byte-identically, years from
now, with nothing but this repository. A pinned SHA that still has to be fetched
gives reproducibility only while the network and the upstream repo cooperate.
This mirrors the decision recorded in [../../docs/migration.md](../../docs/migration.md)
the same way a pinned dependency is: record the version, check the integrity
data in *this* repo.

## What is here

Copied verbatim from upstream, no reformatting:

```
pack/*.json      one file per pack; 4,298 cards
packs.json       pack code -> name, release date, size
sets.json        set code -> name (a set is a hero's cards or an encounter set)
types.json       type code -> name
factions.json    faction code -> name (aggression, justice, ...)
subtypes.json    subtype code -> name
settypes.json    set type code -> name
packtypes.json   pack type code -> name
```

Upstream's tooling (`validate.py`, `add_octgnid.py`, the schema directory and the
translations) is not copied — nothing here writes back upstream.

## Refreshing

Refreshing changes printed card text, which changes every spec authored from it.
Treat it as a deliberate, reviewable act, not a routine sync.

```bash
git clone --depth 1 https://github.com/zzorba/marvelsdb-json-data.git /tmp/msdb
cp /tmp/msdb/pack/*.json  datasets/marvelsdb/pack/
cp /tmp/msdb/{packs,sets,types,factions,subtypes,settypes,packtypes}.json datasets/marvelsdb/
git -C /tmp/msdb rev-parse HEAD    # record above, with the commit date
```

**`datasets/cards/` cannot be rebuilt from this** — the join that produced it
has no implementation here. MARVEL-252. Until it does, refreshing this snapshot
moves the input without moving the output, which is worse than not refreshing
it: read that issue before running the above.

When it can be rebuilt, read the diff on `datasets/cards/cards.json`. A changed
`text` on a card that already has a spec means the spec needs re-reading, not
just regenerating.

## Provenance and licence

Upstream ships no licence file. The card text is Fantasy Flight Games' and
Marvel's. It is here to be read by the engine, not
redistributed as a product.
