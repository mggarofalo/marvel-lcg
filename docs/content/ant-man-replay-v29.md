# Ant-Man replay v29 boundary

Published predecessor `0dbd58241a2ae538b13ebb93f0968d9796b6b881` and candidate `c526e9d7d552b584090adc99cc8ce3e13ac35987` were compared with original production-host saves. The retained comparison archive SHA-256 is `945e23150acbbef5583c0e03855555e4730fe8139209962e384b32dba7169be8`. Its README, manifest, harness, logs and original request, response and durable records remain in the external evidence packet. The candidate predates integration `54940952`; the comparison packet reports that integration adds review tests and documentation, without independently proving identical gameplay source.

Rhino/Spider-Man seed 0's three decisions and a fresh predecessor process's four-decision continuation both verified diagnostically under candidate datasets. This used an explicitly constructed in-memory content upgrade, not production admission; it did not demonstrate candidate continuation or production migration. Rhino/Spider-Man seed 73 instead diverged at unit 1 decision 0 persisted prompt: Power of Justice 01062 changed from `GG` to `G` with a conditional bonus. This does not alone prove different payment resolution. She-Hulk/Taskmaster seed 0 legally played Split Personality; at unit 1 decision 0 event 6, the old runtime recorded a player draw while the candidate recorded Taskmaster's encounter discard. A fresh old-process Sync matched the separately captured original Sync's revision, prompt and world. Neither capture has an inactive redo suffix; no compatible predecessor campaign is established.

These differences justify v29. No earlier replay identity has a route to v29: under MARVEL-896 a save loads only under the current replay and dataset identities, and every other save is refused with `save_incompatible`. Ordinary current-identity saves load normally.

`tests/Marvel.Server.Tests/Fixtures/published-v28-rejection-870.zip` contains the untouched durable manifests and session/authority generations from the seed 73 and independently captured She-Hulk Sync originals. Its SHA-256 is `1a27eb1c902eacc982f8e6da5cae4602061269d7143b6712271b7efcbd64003d`. It excludes harness binaries and wire records; test capabilities are synthetic local credentials. `PublishedV28SaveRefusalTests` restores both captures across two restarts, answers each saved owner capability with `save_incompatible`, and compares every original stored byte, including retained generations. The archived diagnostic evidence is separate from that refusal test. Fixture archive hashes and capture limits are also recorded in the fixture README.

On `54940952` with this replay packet, the focused server suite passed 115 tests
covering named routes, historical guards, strict original facts, redo,
publication, campaign rejection and genuine v28 diagnostics. After replacing an
invalid quarantine capability with each capture's real synthetic owner
capability, the two rejection cases and three frozen-source positive controls
passed again (5 tests). The release policy checks passed 8 tests and the desktop
identity checks passed 7. A compiled mutant adding a frozen v28-to-v29 admission
was rejected by all three positive-route controls; the source was restored and
the affected controls passed. These checks do not establish a compatible v28
production migration, an inactive-redo v28 capture, or a compatible played
predecessor campaign.
