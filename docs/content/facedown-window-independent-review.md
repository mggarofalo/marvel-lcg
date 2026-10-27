# Facedown window admission independent review

MARVEL-859. Reviewed production commit `a0d941221646a6a68690edd4941d66158150a8d5`
in an isolated worktree. No production repair was needed during this review.

The guard is consistent with `rr:in-play-and-out-of-play.1`, `.2`, `.6`, `.10`
and `.13`: ordinary printed window abilities require a faceup card in an in-play
zone. It rejects Great Responsibility captured by Highway Robbery before any
printed candidate is published. This prevents disclosure through the false
offer as well as the eliminated-seat host rejection. Effective-profile minions
already suppress their underlying printed abilities through the existing profile
guard; their supported behavior does not depend on admitting facedown text.

The explicit event-origin exception remains necessary. `CardPlayOrigin.CanPlayEvent`
requires an event owned by the relevant seat, either in that seat's hand or in
bounded storage with an active printed permission. A hand card need not become
faceup before being offered. Being in a discard pile, deck, removed area or
set-aside area grants no event permission. The independent tests also exercise
faceup and facedown storage under a synthetic explicit permission; these prove
the origin contract and do not claim a printed Core Arrow interrupt exists.

The new guard only filters discovery/initiation candidates. It does not gate a
previously accepted ability's execution continuation. The source regression
initiates real Great Responsibility legally, eliminates its owner during its
damage, and asserts replacement, completion and an empty agenda. That is the
distinction required by `rr:player-elimination.5`.

Independent evidence is stored outside the repository under
`C:/Users/mggar/Source/evidence/`:

- `facedown-independent-final-focused.log`: 37 passing tests across the source
  Highway Robbery regressions, seven independent cases, Core threat abilities,
  facedown-minion admission, Hawkeye arrows and Quiver search.
- `facedown-independent-origin-mutant.log`: removing the explicit playable-event
  exception fails six independent cases; the faceup Jessica Walters control
  passes. Production source was restored before the final run.
- `facedown-independent-stale-v28.log`: strict replay rejects the unchanged
  preserved development archive at `unit 55 decision 0 events` (15 expected,
  18 actual). The preceding 55 units contain 95 decisions, so this is accepted
  decision 96, matching the source diagnosis.

The preserved diagnostic archive itself declares `engine-replay-v28`; it is not
a released v27 save. Its SHA-256 remains
`D49FB0C457A2DA66DB4C3E91E26E7D5387BDC3874D8D7288545EAC51133AB7F1`.
The repair belongs to this unpublished aggregate v28 candidate. Released-v27
metadata rejection and frozen v19/v20/v21 route verification are separate
compatibility checks; changing this unmerged candidate does not itself require
a v29 identity. Exact replay must still reject stale v28 candidate traces whose
recorded events differ, as the preserved archive demonstrates.

This review accepts the timing-admission repair's narrow engine scope. Ownership-
wide cleanup of eliminated players' cards is a separate finding, and complete
Ronan delivery, native review and combined platform certification remain outside
this packet.
