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
The current v29 delivery retains explicit frozen v19/v20/v21 routes to v29 and
frozen-content v29 additive upgrades. Published v28 and other v22-v28 sources
have no named v29 admission. Complete original facts must verify before any
metadata changes; unsupported and divergent saves remain unchanged. See
[the v29 evidence](content/ant-man-replay-v29.md).

The historical v28 delivery retains named routes from the exact frozen v19/v20/v21
content triple to `engine-replay-v28`, plus frozen-content v28 additive upgrades.
Every original recorded fact must verify; none is regenerated. V22, v23, v24, v25,
v26 and v27 have no named source admission to v28 and are quarantined unchanged.
V28 completes an ordinary enemy attack before offering its defeat responses, so
a real Rapid Response trace can change prompt and event order with identical
content. Historical v27 routes remain exact v27 destination routes. V27 evaluates
dynamic lasting amounts against current state and preserves captured actor incarnation
across suspended responses. These corrections can change accepted Core/RoRS outcomes.
Protocol 42 and save schema 7 applied to that delivery. Historical v26 routes remain exact v26
destination routes. V26 completes
minion Quickstrike before optional engagement and entry responses;
real Core Hawkeye/Vulture traces can therefore change independently of content
hashes. Historical v25 routes remain exact v25 destination routes. V25 emits one
ally-entry response occurrence after the mandatory ally-limit choice, including
ordinary play and put-into-play. Core Nick Fury, Maria Hill and Mockingbird can
therefore change a recorded trace independently of content hashes. V24 discards unattached
revealed attachments after reveal/surge completion: Core 01163/01185 and RoRS
04117â€"04119/04135 can change recorded digests and later shuffles even with
identical datasets. Historical v22/v23 adapters remain explicit destination-bound
routes; they do not authorize a v22/v23 source upgrade to v24.
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
are rejected unchanged on restore and import. The authentic five-decision Taskmaster attempt is also rejected by the deliberate
frozen-content v27 compatibility probe. Its first difference is unit 1 decision
0: changing form invokes Taskmaster (04093), which discards 04153 and deals one
direct damage. The original five events include identity health 12 to 11; the
new runtime emits the other four events and pauses before placing that damage.
The frozen ability book's Backflip (card 10) lacks the printed attack-only
condition and is offered at this new optional damage window. This is a probe of
new runtime behavior with frozen content, not current authored Backflip behavior.
The actual current book does not offer Backflip for Taskmaster's direct damage;
the same setup and recorded initial decisions produce the original five events.

The frozen Expert campaign remains quarantined unchanged. Its journal entry 13
(Crossbones attempt 4) first differs at unit 28 decision 1 under the same
frozen-content v27 probe. Great Responsibility (01061, card 27) replaces threat
with one direct damage. The original four events are Play, identity health 6 to
5, Discard and AttackBoundary. The new runtime emits Play and pauses at the new
optional damage window, offering the frozen book's Backflip cards 9 and 10.
Declining that window places the original damage. This earlier difference
prevents replay from reaching the separately recorded stacked-prevention
sequence at unit 31; that original history is retained unchanged.

RR `rr:damage.step.3` requires abilities that trigger "when [character] would
take any amount of damage..." before damage placement at step 5.
`DirectDamagePrevention`, introduced in `3684923b`, supplies that window. Current
Backflip's printed 01003 condition additionally requires damage from an attack;
the frozen book does not encode that restriction. The compatibility probe does
not retrofit the frozen book or substitute an expected event. Strict prefix
verification and exact first-divergence checks live in
`ExpertCampaignReplayUpgradeTests` and `PlayedCampaignReplayUpgradeTests`.
The Taskmaster session is also checked in an explicitly authored standalone
storage envelope: two restores reject publication while every stored byte and
the original authentic session remain unchanged. Whole-campaign restore/import
quarantine remains covered for both archives.

The unaffected initial Absorbing Man attempt still upgrades exactly and permits
both recovery and decline. The nonempty authentic Core session in
`core-session-91098022.zip` still upgrades exactly through named v19/v20/v21
routes (`ReplayContractUpgradeTests.CurrentNamedAdapterVerifiesTheOriginalHistory`).
These controls do not establish complete-campaign upgrade acceptance. A newly
recorded authentic compatible predecessor campaign aggregate is still required
for positive full-campaign upgrade acceptance. No frozen expected gameplay value
or historical archive is rewritten.

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
