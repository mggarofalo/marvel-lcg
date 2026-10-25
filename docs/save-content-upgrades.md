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
The v22 delivery admits four explicit routes on this exact frozen content
triple: `engine-replay-v19` to `engine-replay-v22`, `engine-replay-v20` to
`engine-replay-v22`, `engine-replay-v21` to `engine-replay-v22`, and frozen-content
v22 to v22 after additive content changes.
Users do not need an intermediate installation. Every route requires the original
RNG and state-digest identities, current save schema, unchanged admitted content,
and complete deterministic verification. Unknown versions, other dataset triples
and application downgrades are rejected. Exact current identities restore normally.

Replay v22 completes a nested card reveal, including minion Quickstrike, before
resuming its enclosing reveal effect (`rr:reveal.step.4`). The frozen v19 and v20
played-campaign aggregates first differ at unit 7 decision 2: Shadows of the Past
records scheme/shuffle events before the nested minion completes. Both aggregates
are rejected unchanged on restore and import. The separately verified, authentic
five-decision Taskmaster attempt remains compatible; this proves an unaffected
played attempt, not a compatible complete campaign aggregate. A newly recorded,
authentic compatible predecessor campaign aggregate is still required for positive
full-campaign upgrade acceptance. No frozen expected gameplay value is rewritten.

Replay v21 moves retaliation into the forced after-attack response window and
corrects zero-damage retaliation. The actual predecessor Black Panther/Rhino trace
in `tests/Marvel.Acceptance.Tests/Fixtures/retaliate-predecessor-91098022.jsonl.gz`
first differs at decision 9: the corrected runtime deals one retaliation damage.
Such a history must be rejected before publication. The named adapters never
rewrite expected events, decisions, RNG counts, state digests or campaign records.
The v19 route also verifies the continuation and attribution corrections introduced
by v20. Ordinary strict replay still rejects a predecessor identity under v22;
only release-owned admission permits verification through a named adapter.

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
lease excludes cooperating current-runtime writers during predecessor checking and migration;
the campaign store retains its existing process-wide writer lease.
Ordinary session publication checks the current replay, RNG, digest and content
identities under that lease, including version-only migrations with identical
dataset hashes. Ordinary campaign publication applies the same check under its
read/guard/write lock; sharing one store cannot let a retained stale host replace
a migrated aggregate. Both reject application downgrades. A stale host using this
publication implementation must restore the current generation with a compatible
runtime. Deletion tombstones
use their own typed operational payload and remain independently repeatable.

Before upgrading standalone session storage, stop every predecessor server and
keep it stopped until rollback or completion. The immutable 910 predecessor does
not implement the standalone `.writer` lease or compatibility publication guard;
a still-running old binary can overwrite its original storage layout. Current
stale-writer tests exercise the new publication implementation and do not prove
protection against that historical binary. The predecessor campaign store already
holds a lifetime process writer lease, so its concurrent-process exclusion is a
different boundary. Take the stopped storage-volume backup described below before
starting the new artifact.

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
