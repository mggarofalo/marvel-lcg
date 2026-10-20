# Additive content save upgrades

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
It admits only this named predecessor. It is not a promise to accept arbitrary
older dataset triples, new replay contracts, or changed card interpretations.

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
Ordinary session publication also checks the current content identity under that
lease. An older host cannot overwrite a migrated archive with its stale content
provenance; it must restore the current generation with a compatible runtime.

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
