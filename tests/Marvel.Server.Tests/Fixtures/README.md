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

`played-campaign-91098022.zip` is the genuine predecessor campaign fixture,
SHA-256 `1199392F3439FE89B6A21AA03D195DF18BEF1CC70F20DD65A998A2E32B29FFE8`.
Its embedded README, provenance, harness sources and logs document generation
using the untouched 910 runtime. Spider-Man's published starter plays a standard
campaign at seed504: ten Crossbones attempts and two Absorbing Man attempts,
including one victory in each scenario, then five accepted Taskmaster decisions.
The 781 terminal decisions were independently matched including prompts, choices,
events, RNG word counts and digests; the active five decisions were independently
executed and their resulting digest/RNG verified. Fresh old-runtime processes
restored the full aggregate and resumed it. A separate copy supplies the next
legal decision and exact old-runtime continuation response.

The ZIP preserves original campaign generations/manifests, all authority and
receipts, host export, actual owner/game wire responses, and the independent
miner's full traces. These capabilities belong only to the generated test
campaign. The new-runtime tests work on temporary copies and verify all proofs,
all 13 attempts, the carried experimental weapon, consumed tech, awarded
condition/HP bonus, restart, continuation and independent-copy import. They
normalize only explicitly migrated compatibility and import identity fields.
This is standard-mode evidence with zero delay counters; it does not establish
expert-mode or nonzero-delay campaign coverage.

`played-campaign-91098022-continued.archive.gz` preserves the exact persisted
archive from that original predecessor continuation copy, after the recorded
End turn request advanced Taskmaster from revision 5 to revision 6. SHA-256:
`970501B99649ED1B2FBC5EB18DD337C9F7CA3A355D7FD01D137EEF02F5970DC6`.
It was compressed from the original retained store, without rebuilding the
engine or modifying its JSON. The packaged native upgrade probe compares its
entire resulting archive, including the new decision's events, RNG and digest,
against this control, normalizing only explicitly migrated compatibility.
The original ZIP's `control/continuation-request.json`, response, producer
source and provenance identify the actual request and predecessor runtime.

`expert-campaign-91098022.zip` preserves an actual Expert campaign from the
same untouched predecessor, SHA-256
`1A7B953709EE3E390863BFD28A6C920EE00B53F7F24853B197013AA72903538B`.
Spider-Man's published starter plays seed 528 with Expert campaign rules and
Standard scenario difficulty. Six Crossbones attempts include five defeats and
one victory; all 395 terminal decisions were independently matched for prompt,
choice, events, RNG word count and digest. Recorded health is 7, the carried
experimental weapon is 04072, and the awarded tech is 04155. The seventh attempt
is Absorbing Man at its initial recovery prompt, revision zero. A fresh
predecessor process restored the exact authority, synchronized owner and game,
and exported the campaign.

MARVEL-655 prevents that old host from committing either recovery choice.
The archive includes both original rejected requests/responses and verification
that neither changes the durable campaign. It therefore does not claim an old
Absorbing Man victory, acquired recovery obligations, or successful predecessor
continuation. The current host's accepted recovery is checked against a separate
engine execution, while migration comparisons preserve the entire original
aggregate, proofs, authority and backup bytes. Import creates new authority and
preserves all played records. Harness sources, the independent trace, and logs
are included; capabilities belong only to this generated test campaign.

`published-v28-rejection-870.zip` (SHA-256 `1a27eb1c902eacc982f8e6da5cae4602061269d7143b6712271b7efcbd64003d`) contains the unchanged durable files, excluding empty writer locks, from the genuine published `0dbd58241a2ae538b13ebb93f0968d9796b6b881` seed 73 Rhino/Spider-Man and seed 0 She-Hulk/Taskmaster captures. Both include retained prior generations. Synthetic local authority bytes are preserved verbatim. Their full comparison archive is externally retained with SHA-256 `945e23150acbbef5583c0e03855555e4730fe8139209962e384b32dba7169be8`; see `docs/content/ant-man-replay-v29.md` for scope and diagnostic limitations. This archive is not a migration or a relabelled save.
