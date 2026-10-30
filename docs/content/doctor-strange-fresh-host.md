# Doctor Strange fresh-host continuation evidence

MARVEL-838. Bounded packet based on `d887173f0eed622cbabbcd6a466d7744bdccc75d`.
The tests use the admitted production `DatasetGameFactory`, printed starter,
normal mulligan and engine-offered decisions. The observing factory captures the
created Game for assertions; it changes no setup, cards, programs or World state.

One twelve-seed harvest, shared by the test class, inspects naturally dealt
starter hands and the first two Invocation positions. It performs no iterative
game search. The chosen journeys are:

- Seed 2: keep the starter, Natural Talent discards Seven Rings, change form,
  save/reconstruct a separate factory and host, then Spell Mastery resolves Winds.
  Three cards are drawn, the exact physical Winds is discarded and Strange is
  exhausted, matching the independent uninterrupted continuation.
- Seed 1: keep the starter and use Natural Talent. The saved host reconstructs
  the new top, physical discard and consumed phase use; a subsequent ordinary
  form change matches the uninterrupted trace.
- Seed 2: keep the starter, change form and pay for Seven Rings through Spell
  Mastery. Reconstruct while its recipient choice is pending, then give tough
  and discard the exact Invocation without charging or exhausting again.
- Seed 12: keep the starter, Natural Talent discards Vapors, change form and
  play the naturally dealt Master of the Mystic Arts on Seven Rings. Reconstruct
  while the Special's recipient choice is pending, then finish the Special and
  return the same physical Seven Rings to the public top. Strange remains ready.

Every restart round-trips the complete stored session through strict JSON into
a separate store. It creates a new production factory and EngineHost, asserts
distinct Game and World instances, and verifies the stored bytes are unchanged
by restore and rejected stale commitments. The resumed path is compared with the
uninterrupted host for complete saves (setup, ordered decisions, prompts, events,
RNG receipts and state fingerprints), canonical digest, full RNG state/count,
visibility-safe synced world, current prompt and authorized history. Continuation
events are compared in order. Sync is compared with sync: a resolve response also
carries transient EventFaces for its just-emitted events.

The paid journeys reject an unpaid commitment and retain the exact durable
prefix before accepting payment. Their pending Special already has committed
costs; completing its choice preserves the paid hand and exhaustion state. This
proves the precommit rejection and committed Special boundaries. An unsubmitted
client payment draft is not a server checkpoint and is not claimed to persist.
An actual resource-generator interruption during cost execution remains unproved.

This packet does not cover a suspended Vapors status replacement, a pause after
Master's Special discard but before its return, final-card recycling, or Open the
Dark Dimension's hosted Invocation return. Master's pending recipient choice is
before the Special completes, not a separately exposed return decision. Those
remaining obligations require another bounded packet. It does not claim complete
MARVEL-838 closure, all-public coverage of every deep checkpoint, full-game
victory, native interaction or MARVEL-839 owner acceptance.

No schema, replay identity, production factory, content book, setup record,
historical save or compatibility route is edited. Existing frozen compatibility
tests remain the evidence for accepted predecessor archives and genuine
incompatibility rejection; the current Strange trace alone cannot establish
historical compatibility.

Verification logs and TRX are outside the repository under
`C:/Users/mggar/Source/strange-fresh-host-evidence/` and
`C:/Users/mggar/Source/strange-fresh-host-*.log`. Initial fixture runs failed on
seed selection, transient response metadata and a retained prior candidate World
reference; they are not passing evidence. The fourth focused run passed all four
cases before the unpaid-payment and tough-result assertions were added.

A compiled test-harness mutant deliberately substitutes the original live host
and Game for reconstruction. The Natural Talent test fails specifically at
`Assert.NotSame`, distinguishing a fresh-host proof from same-instance reuse.
Sources are restored in a finally block, followed by a nonincremental rebuild
and the relevant replay/compatibility class run.

Temporary diagnostic instrumentation failed compilation and was restored; it
does not establish the earlier cause. The final source build is checked again.

After adding an independent discard/reveal/return ordering assertion, the focused
run passed five tests and failed one (four public cases plus citation gates).
The failing case is Master with public seed 12. The raw durable journal lifecycle
is checked before the projected response lifecycle in the retained regression.
`InvocationDeck.RefreshTop` emits the intermediate faceup flip;
`VisibilityEventFilter.KeepFlipped` retains only final addressable IDs, so the
temporary next top is absent from the public response after Master conceals it.
Same-candidate replay equality alone did not detect this disclosure loss. The
missing event must remain a failing regression until a separate View/history
implementation and independent review preserve that legitimately observed
information. No assertion is skipped or relaxed here. This evidence packet is
blocked by that disclosure finding and the earlier compatibility diagnostic.

The transient public reveal is implemented on `fix/invocation-transient-reveal`,
based on proof commit `546a5517243b53b2160cae7b98842e5c051249e4`. The retained
public seed-12 lifecycle assertion is unchanged. `InvocationDeck.RefreshTop`
captures an engine-only `CardsFlipped.PublicFaces` grant for that flip's exact
physical card and printed face. View retains that faceup occurrence even after
Master conceals it, strips the authority metadata, and supplies the existing
`subject_faces` and `event_faces` contracts. It does not make the card currently
addressable or expose later movement, private hands, inspection or future order.
Ordinary ungranted flips, unrelated grant keys and facedown grants retain their
privacy restrictions. Both physical owner seats, owner/other-seat restricted
scopes, permissive scope and spectator scope are covered. The hosted Invocation
privacy test now distinguishes a legitimately observed public historical reveal
from the currently concealed pile; it still rejects concealed movement,
attachments, detachment and shuffle order.

The completed seed-12 session is strictly serialized and reconstructed again
after Master's return. `SessionReplayVerification` resolves the saved decisions
and passes regenerated events to `ReplayHistory.ObserveEvents`; it does not use
deserialized journal events as disclosure authority. The test independently
projects the reconstructed `HistoryUnitInspection` and checks the intermediate
physical Invocation and captured face. It also checks that the restored host's
actual `HistoryDescriptor` names that revealed face. The server projects each
history unit's authorized occurrence faces; the existing formatter interleaves
explicitly granted public reveals with its existing movement-result segments.
Its existing actor/scope restriction remains in force. Current pile snapshots
remain concealed, and historical titles come from the captured face reference.

`PublicFaces` is `[JsonIgnore]`; a direct contract test verifies byte-identical
event JSON with and without that metadata. No event wire field, protocol
version, raw journal spelling, save schema, replay identity, state digest, RNG,
ability book or setup recipe changes. This is a disclosure/history-display fix,
not a change to the Special's engine timing or effects. The existing earlier
compatibility diagnostic remains outside this packet.

Verification artifacts are outside the worktree under
`C:/Users/mggar/Source/invocation-transient-evidence/` and sibling
`invocation-transient-*.log` files. The final affected runs include the complete
420-test View suite, 40 selected Server history/restart/citation tests, 21
presentation ownership/layout checks, and 17 Invocation deck rule tests. Two
individually compiled mutants remove event-time card retention and disable
public history reveals. Each fails both physical-owner cases; sources are
restored in `finally` blocks, then rebuilt nonincrementally and the complete
View and selected Server classes rerun. Query-purity checks capture digest,
complete RNG state/count, agenda and prompt before projection, then compare
the next accepted decision with an unprobed control. The public host tests
retain strict save, digest, RNG, pending choice and uninterrupted-control checks.
This packet does not claim native UI acceptance, all remaining MARVEL-838
checkpoints, a complete game, or MARVEL-839 owner acceptance.
