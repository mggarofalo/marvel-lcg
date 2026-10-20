# Content upgrade predecessor

`content-91098022.zip` contains byte-for-byte runtime datasets and search metadata
from commit `91098022d37ce739dd2880e8964b40ee3675eeb3` (also present unchanged in
PR 433's squash commit). It is an offline test fixture, not a supported runtime
content source.

Archive SHA-256:
`7184323687CE9DFB5CAB49AA00447A9D5233D2DF3EC24F7EF58B5C5187C9F880`.

The ZIP entries are the four paths under `datasets/abilities`, `datasets/cards`
and `datasets/setup`. They use fixed 1980 timestamps and preserve the original
file bytes. The three runtime hashes and canonical admitted-entry fingerprints
are pinned in the executable's `ContentCompatibility/core-rors-91098022.json`.
Future content additions must not regenerate this fixture from their current
checkout. Its fixed identity is necessary to test an actual predecessor.

The current focused tests generate legal pending Core and RoRS sessions from
these bytes. They do not yet constitute an old-executable, multi-scenario
campaign victory fixture; MARVEL-630 requires that additional evidence.
