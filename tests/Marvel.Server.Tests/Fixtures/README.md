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

`core-session-91098022.zip` is an actual predecessor-process save, SHA-256
`63BD6E38C7668F6789BB48300165026D34CA55432B59ADF08FF2B19793E87ABC`.
An untouched detached checkout of that commit built `Marvel.Server.Host` in
Release. The process used restricted visibility, seat zero, and a private
temporary save directory. TCP requests opened Rhino with Spider-Man and Captain
Marvel, seed 73; attached the second seat with its invitation; and kept the first
seat's opening hand. The process was killed after the acknowledged commit.
The archive preserves the original current manifest, session and authority bytes.
Its capabilities belong only to this synthetic local test session.

An independent copy restarted on the predecessor executable, synchronized the
second seat's pending mulligan, and kept that hand. Its complete authorized
responses are the continuation control. A separate copy upgraded on the candidate
executable with an unrelated added setup entry and produced equivalent responses.
Evidence manifests and process logs are included. ZIP entries have fixed 1980
timestamps; payloads are unmodified. JSON object key order is not semantic;
all array order, values, visibility and gameplay fields are compared.

This Core fixture is not a multi-scenario campaign victory fixture.
MARVEL-634 remains required for the latter evidence.
