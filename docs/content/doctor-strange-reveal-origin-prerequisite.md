# Doctor Strange reveal-origin prerequisite

MARVEL-828, prerequisite of MARVEL-823. Candidate based on `fb6f6a1d`, branch
`feat/ward-reveal-origin`. No production ability row or public admission changes.

## Authority and interpretation

Generated `datasets/cards/cards.json` 09007 requires a treachery revealed from the
encounter deck, allows another player's revelation, and cancels every effect.
RR 1.8 `rr:reveal.step.4` applies the same lifecycle to reveals from other areas;
`rr:cancel.4` retains revelation and discard despite cancellation.

The FFG clarification by Alex, September 14, 2026, distinguishes directly revealed
cards from separate game zones from cards first dealt to a player. First-dealt
cards qualify even when their earlier source is a player deck (Mysterio). This
avoids treating the dealt queue's immediate zone as a disqualification and avoids
inventing persistent card history. Source:
<https://hallofheroeslcg.com/latest-ffg-rulings-post-rrg-1-7/>.
The integration owner acquired an offline candidate with new ruling
`ruling:7075a85e02f2203e`, hash
`sha256:2f33c800298828b5fded7f90645fd92c5f07af0ee16bd93535208cb2084e91cc`.
Its vendored source and rules-graph integration are owned by the parent packet.
This branch does not update vendored datasets. Existing local ruling
`2d5b4da522e21bb6` also covers Mysterio and the distinct Return the Favor case.
The primitive does not declare every discarded/search reveal equivalent: a future
Return the Favor implementation needs its own correct operation semantics.

## Owning contracts

`EncounterRevealOrigin` retains exact physical card id and identified immediate
source area. A dealt marker records the scheduled queue procedure. Explicit
ability reveals capture the area before entering RevealingArea and carry it
through AbilityRevealRequest and RevealStageOperation into the occurrence.
`revealedFromEncounterDeck: "trigger"` is a narrow checked flag; it additionally
requires the matching subject and CardRevealed condition. No renderer or event-log
inference participates. Nested operations each own their immutable receipt.

The receipt is scheduled procedure data, not card state. No digest/RNG spelling,
card field, public protocol, or save schema changes. Occurrence's derived receipt
is JsonIgnore, like existing replay-reconstructed facts; operation roundtrip and
occurrence reconstruction are tested. Hosted saves remain decision traces which
reexecute these deterministic operations. This is not a full hosted-save test.
Old diagnostic occurrence JSON contains no new field. Unannotated synthetic
RevealStageOperation fixtures do not gain invented origins.

## Evidence

`DoctorStrangeWardOriginTests` uses explicit synthetic 09007 and 01092 rows, plus
an independent reveal interrupt so an ineligible Ward cannot disappear merely
because the occurrence was skipped. The same printed Shadow of the Past is
revealed from encounter deck and discard; seat 1 pays Ward during seat 0's reveal.
Deck origin cancels all effects, suppresses Surge, discards event/payment/treachery,
and drains the agenda. Direct discard origin has no Ward offer with payment
untouched. Both deck-dealt and discard-dealt controls qualify and complete the
same paid cancellation lifecycle. Strict argument rejection and serialized
operation reconstruction are also covered.

Local logs are under `C:/Users/mggar/AppData/Local/Temp/` with prefix
`ward-origin-`. Initial affected run: 50/50 passed across the whole new class,
CoreAbilityRevealDisclosureTests, CoreInterruptContextTests, and AbilityData.
Mutation logs, restored nonincremental build, and restored run are separate files.
Final observed results are recorded in the implementation handoff.

Remaining review scope: independent authority and implementation review, nested
reveal interruption/replay beyond operation roundtrip, hosted save/restart,
actual production Ward admission and its complete form/payment matrix, and
matching Core 01004/01075 row restrictions. These are not claimed complete by
this prerequisite packet. No broad Content or whole-game lane was run here.

The first normal-hook attempt overlapped a replacement mutation run and is
excluded as acceptance evidence; it also reported TestFact complexity. The
receipt predicate is cohesively extracted, and the closeout reruns mutation,
restoration, focused checks, and normal hooks serially. The original always-true
mutant did not compile (CA1822) and is excluded. Only the separately rerun
zone-inversion and missing-dealt-marker mutants are claimed when their clean
builds reach the intended assertions. No process from the overlap remains live.

Serialized closeout: zone-inversion mutant compiled with zero warnings/errors
and failed 3 of 8 cases; missing-dealt-marker mutant compiled and failed 2 of 8.
The intended source was restored exactly, rebuilt nonincrementally, and the
50-case affected selection passed. Logs use `ward-origin-clean-` prefix.

## Independent review of engine and pinned authority

Reviewed engine `0712bd87dc9ab17310e440a651a897eeafd6a418` together with
authority `eae9f8a6` (review-worktree cherry-pick `e25c5a13`). The bounded
verdict is approval of the reveal-origin prerequisite and scoped source refresh;
it does not close Doctor Strange content or product acceptance.

The reviewer compared generated text for 01004, 01075 and 09007 with raw
September 14 HTML and ruling `7075a85e02f2203e`, including its complete answer
and attribution. The newer answer supports first-dealt cards from player decks
and excludes direct separate-zone reveals. The graph's three card text hashes,
ruling hash and superseded hash are checked offline. It deliberately leaves
Return the Favor's distinct instruction unresolved. All 1,100 predecessor
ruling IDs retain their hashes; only the post-1.7 page changes, adding 13 records.
Offline harvest verifies four page hashes and exact 1,113-record index parity.

`EncounterRevealOriginReviewTests` adds exact physical-card and non-reveal
occurrence controls, plus missing-origin controls. Separate compiling mutants
remove each guard; each fails its intended case (1 failure among 4 cases).
The source is restored byte-for-byte, rebuilt nonincrementally with zero
warnings/errors, and the whole affected Content selection passes 54 cases.

`EncounterRevealOriginReplayTests` uses the existing durable factory injection
seam with explicitly synthetic rows. A scenario-owned treachery is first moved
to a player deck, dealt, and offered Ward. A separate interrupt directly reveals
a discarded treachery: its independent observer produces a real prompt while
Ward remains absent and its payment remains in hand. JSON decision replay and
an actual EngineHost restart recreate that inner receipt, every compared prompt,
pending reveal origins, digest and RNG. Declining the inner observer resolves
the inner treachery, restores the exact dealt parent receipt, and offers Ward.
Paid cancellation then drains the queue and agenda, discards both treacheries,
Ward and payment, and reaches a live root prompt. Future causal event JSON is
equal across uninterrupted, replayed and restarted runs.

Whole-class checks: Content affected selection 54 passed; hosted origin,
NestedInterruptReplayTests and SurgeReplayReviewTests 6 passed; authority harvest,
relationship, citation and graph/index selection 25 passed. Evidence lives at
`C:/Users/mggar/Source/evidence/reveal-origin-review*`. Earlier synthetic-fixture
failures used an incorrect card id/selector, incomplete payment input, or
player-owned encounter card; they are not production findings. A transient
apphost file-copy retry is excluded from clean-build evidence; final clean
server rebuild and rerun are recorded separately. Normal commit hooks remain
enabled. No production source or historical replay fixture changes in review.

Actual Ward and the admitted Core 01004/01075 restrictions still require their
own authored-row evidence. MARVEL-825 must verify the newly vendored Shadow of
the Past unique-entry answer before pack acceptance. Full content, combined
pack/native journeys and cross-platform gates remain integration obligations.
