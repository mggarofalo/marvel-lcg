# Hosted Invocation prerequisite — MARVEL-816

Base `36628a0b`; implementation branch `feat/hosted-invocation`. This packet
provides a nonpublic engine prerequisite for 09029, not a production ability row,
pack admission, UI acceptance or a general hosted-card capability certification.

Printed authority is the complete 09029 record in `datasets/cards/cards.json`:
“When Revealed: Place the top card of the Invocation deck facedown under this
scheme.” “When Defeated: Shuffle the Invocation card under here into the
Invocation deck.” The local FAQ and rulings have no 09029-specific answer.
`pack:mc08:the-invocation-deck` supplies the faceup top and immediate, penalty-free
empty-deck recycling. RR 1.8 `rr:in-play-and-out-of-play.10` keeps facedown hosted
cards out of play. `rr:when-defeated-abilities.2.1` resolves the printed return
before the scheme leaves play; `rr:attach-to.1` supplies ordinary hosted discard
when the scheme instead leaves without defeat.

The existing `storeBeneath` operation accepts the typed `invocationTop` query and
uses `BeneathCardsPile`, preserving the same physical card and its owner while
concealing it from every viewer. `BeneathCards.Place` refreshes that bound deck
after movement, including seeded recycling of the remaining four cards when the
stored card was the last draw-pile card. No Invocation is played or recreated.

The engine-chosen DSL extension is
`{"releaseBeneath":{"invocationDeck":"true"}}`. It calls
`InvocationDeck.ShuffleFromBeneath`, which validates the complete stored set
against one bound physical Invocation deck before mutation. It conceals the
previous public top, moves and detaches the stored physical card, shuffles once
on the world's existing RNG, and reveals the resulting top. The resolving
encounter player does not choose its owner or destination. Captive rescue and
host removal cannot be combined with this destination. The existing return-to-hand
release behavior remains the default. No card-ID dispatcher, digest vocabulary,
new prompt or presentation implementation is introduced.

`HostedInvocationFixture` authors 09029 only inside the test book. Its reveal
uses the normal encounter procedure, and its threat removal uses the normal
defeat scheduler. An additional first-player card choice is explicitly synthetic:
it proves that the host remains in play after the physical return, rejected input
preserves digest/RNG/pending work, and the original defeat resumes to completion.
The real printed card has no such choice. No new stale-revision contract is
claimed; public session revision validation is unchanged.

Evidence outside the repository is in `C:/Users/mggar/Source/evidence/`:

- `hosted-invocation-focused.log`: all seven hosted Content cases pass. Controls
  distinguish exact physical identity/owner/incarnation, a non-first Invocation
  owner with first-player reveal/defeat, last-card recycling against an independent
  seeded RNG oracle, unaffected ordinary player decks, no player-deck penalty,
  non-defeat discard, unbound lookalike/foreign-host refusal, and query purity
  against an unprobed continuation with identical events.
- `hosted-invocation-view.log`: the four-scope privacy case passes, covering no
  authorization, each restricted player and permissive cooperation. Stored and
  deeper deck cards disclose neither face nor physical ID through the snapshot,
  movement/flips/attachment events or subject-face metadata; public top remains
  readable. Shuffled deck order is filtered.
- `hosted-invocation-mutant-refresh.log`: a compiled removal of deck refresh
  fails both normal-top and last-card storage cases.
- `hosted-invocation-mutant-shuffle.log`: a compiled removal of the return
  shuffle fails the independent expected-order/RNG control.
- `hosted-invocation-restored-build.log`: forced `Rebuild` after restoring both
  mutants succeeds with zero errors. Restored full-lane execution follows this
  rebuild; those lane results and normal hook results are reported in the
  implementation handoff rather than inferred from the earlier focused pass.

Restored full lanes ran serially: Content passed 6,751 of 6,755, View passed all
370, and Rules passed all 1,173. Content's four failures are the exact-book-count
assertions in `BlackWidowBurnNoticeTests`, `BlackWidowPreventionInventoryTests`,
`BlackWidowPreparationCardTests` and `BlackWidowFinalResponseCardTests`.
The lead identified these as inherited base `36628a0b` inventory expectations
(659 versus the base's actual 661) and has the integration repair. This worker
does not independently claim a baseline rerun or full-suite green. No ability
rows or inventory assertions change in this packet. The restored full Content
run includes all seven new hosted cases and the existing beneath-card controls.
Full logs are `hosted-invocation-content.log`, `hosted-invocation-view-all.log`
and `hosted-invocation-rules-all.log` in the evidence directory above.

The normal hook exposed a complexity increase in `BeneathCards.Place`.
Invocation-specific post-movement lifecycle refresh now belongs to
`InvocationDeck.RefreshAfterMovement`; the storage operation calls that owner.
After this cohesive extraction, all 26 cases in the complete new hosted,
existing beneath-card and existing Invocation top-discard classes pass
(`hosted-invocation-final-focused.log`). The broader lanes and two mutants
above precede this extraction. Normal commit hooks then pass all 66 selected
architecture-policy cases, cyclomatic complexity and file length
(`hosted-invocation-commit.log`). Full-solution build, remaining managed lanes,
native UI and cross-platform CI were not run for this prerequisite packet.

Next: independent authority/diff review, then author the actual 09029 row as part
of the remaining nemesis packet. Complete-pack setup/admission, save/replay and
native proof remain separate delivery obligations.

Independent review on 2026-10-26 compared source
`76866793cd1ea7476c482ed384518d0ac061460b` with genuine pre-feature base
`36628a0b`. Verdict: accept the nonpublic hosted Invocation prerequisite; no
behavioral correctness finding in the reviewed diff. Printed 09029, the mc08
Invocation rules, RR 1.8 hosted/out-of-play and When Defeated timing, ownership
and the local FAQ/rulings were checked independently. In particular, the printed
shuffle resolves on defeat before departure; ordinary departure uses discard
cleanup instead. Existing delayed-choice rejection and continuation tests,
four-scope concealment and event metadata filtering remain intact.

`HostedInvocationReviewTests` adds three independent controls: returning while
two Invocation cards remain in their separate discard pile must shuffle only
the draw pile plus the exact stored card; returning one host must leave another
host's stored Invocation concealed; and returning a single card when all five
Invocations are hosted must expose that same owned physical card without RNG
consumption. Additional hosts in the latter two controls are explicitly
synthetic, not a legal multiple-copy 09029 scenario claim.

Serial Release verification at that source plus the independent tests:

- Content: 37/37 across complete `HostedInvocationTests`,
  `HostedInvocationReviewTests`, `BeneathCardsTests`, `InvocationTopDiscardTests`,
  `CampaignCaptiveReleaseTests` and `CaptiveRescueTests`.
- View: 35/35 across complete `HostedInvocationPrivacyTests`,
  `InvocationPrivacyReviewTests`, `VisibilityTests`, `CardFaceVisibilityTests`
  and `PersistentFactsVisibilityTests` (the substring filter also includes
  `WindowContractVisibilityTests` and `EffectiveValueVisibilityTests`).
- Rules: 17/17 across complete `InvocationDeckTests` and
  `InvocationDeckBoundaryTests`.
- A compiled mutant that also moves the Invocation discard into the return
  shuffle fails `PrintedReturnShufflesOnlyTheDrawPileAndStoredCardLeavingDiscardUntouched`
  at its expected physical deck assertion. Source restoration is byte-identical
  to the reviewed source and a forced Content project `Rebuild` passes with
  zero warnings/errors before all restored class runs above.

Logs are `hosted-invocation-review-{initial,mutant-discard,restored-build,content,view,rules}.log`
under `C:/Users/mggar/Source/evidence/`. This review adds no production ability
row, public admission, engine change or shared-fixture change. It does not
rerun the full Content lane, repair inherited book-count assertions, establish
save/replay or native UI acceptance, or claim cross-platform certification.
