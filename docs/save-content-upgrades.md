# Named content and replay save upgrades

MARVEL-630 owns acceptance of this path. Its implementation and focused checks do
not establish packaged upgrade or played campaign acceptance.

The release-owned baseline is
`src/Marvel.Server/ContentCompatibility/core-rors-91098022.json`, pinned to
`91098022d37ce739dd2880e8964b40ee3675eeb3`. It names the exact three predecessor
dataset hashes and the replay, RNG and digest contracts. The fixture archive in
`tests/Marvel.Server.Tests/Fixtures/content-91098022.zip` retains those original
runtime bytes and the search catalog offline.

`ContentUpgradeAdmission` requires every baseline Core/RoRS and shared rule/status card entry, authored
ability and setup entry to retain its canonical JSON fingerprint. Object member
order and whitespace are irrelevant; array order and all entry values are not.
Adding entries or correcting research-only cards outside the admitted products
does not itself invalidate the predecessor. Changing an admitted entry does.
The baseline is embedded in the executable; a save cannot supply a new baseline
or authorize its own migration.

This is a conservative product policy, not a rule inferred from the tabletop.
The v20 delivery admits two named source identities on this exact frozen content
triple: historical `engine-replay-v19` to `engine-replay-v20`, and this delivery's
`engine-replay-v20` to itself when additive content changes the dataset identity.
The latter describes the corrected runtime writing the baseline content; it does
not relabel newly generated games as historical v19 evidence. Both paths require
the original RNG and state-digest identities, current save schema, unchanged
admitted content, and complete deterministic verification. Unknown v18/v21
sources, other dataset triples and application downgrades are rejected.

Replay v20 corrects card-power continuation and responsible-player attribution.
A historical save which recorded a different outcome under an affected rule is
not migratable merely because its dataset bytes match. The named adapter checks
its original expected values against the corrected runtime and rejects any
mismatch before publication. It does not rewrite expected events, decisions,
state digests or campaign records. Ordinary strict replay continues to reject a
v19 identity under a v20 runtime; only release-owned source admission grants the
named adapter.

After admission, `SessionContentUpgrade` replays the original save with the new
runtime while comparing every recorded prompt, decision, event, RNG count,
digest and information exposure. It verifies the complete retained history,
including inactive redo units, and the active pending prompt. No expected
gameplay value is regenerated or substituted. Only after verification does the
candidate receive the current compatibility metadata.

A campaign upgrade verifies its complete journal, every proof session and every
retained attempt. The entire aggregate then receives current compatibility
metadata together. Campaign state, completed records, decks, inventories,
receipts, authority verifiers and game revisions remain unchanged. Import uses
the same verification before the existing independent-copy identity remapping.

The file stores retain exact predecessor bytes under `.upgrade-` names outside
ordinary generation cleanup. Session backup pairs include the session and
authority files. Campaign backups include the checksummed archive and its
original manifest. A stale predecessor is rejected. A standalone session writer
lease excludes concurrent publication during predecessor checking and migration;
the campaign store retains its existing process-wide writer lease.
Ordinary session publication checks the current replay, RNG, digest and content
identities under that lease, including version-only migrations with identical
dataset hashes. Ordinary campaign publication applies the same check under its
read/guard/write lock; sharing one store cannot let a retained stale host replace
a migrated aggregate. Both reject application downgrades. An older host must
restore the current generation with a compatible runtime. Deletion tombstones
use their own typed operational payload and remain independently repeatable.

Publication uses the existing flushed generation and atomic manifest replacement.
Startup publishes no migrated game before verification and persistence succeed.
A restart reads only the current manifest, never a convenient older generation.
An already migrated archive follows ordinary strict replay and creates no further
migration generation. Rejected archives remain available for read-only diagnosis.

Rollback requires the matching predecessor artifact and its complete original
session/authority or campaign archive/manifest pair in a stopped host's restored
storage volume. Never alter a saved trace, dataset hash or campaign checksum to
make an incompatible runtime accept it. Ordinary pre-upgrade volume backups
remain the operator's recovery boundary; the retained migration originals provide
an additional exact copy.
