# Target Acquired boost-ability cancellation

Nonpublic B6 mechanism packet for MARVEL-771, based on `2adfa430`. Production ability
book, setup, public admission and contract versions are unchanged.

Authority: generated `datasets/cards/cards.json` face `08024` prints Hero Response,
discard Target Acquired, cancel that card's boost ability. Vendored official ruling
`a66ca21bca34a71f` explicitly preserves that response before the boost ability resolves.
RR 1.8 `rr:boost-boost-icon.1` distinguishes stars from numeric icons; `rr:response.4`
closes declined windows. The ruling is cited in comments, not a Rule attribute,
because this packet does not add a rules-graph relationship.

`TargetAcquiredBoostAbilityTests` uses the complete printed nonpublic 08024 row and
isolated synthetic boost effects. It proves Response timing, actual cancellation of
only the first of two stars, unchanged numeric icons, attack/scheme and different
controller/activation seats, discarded-source Preparation completion and incarnation,
invalid/stale rejection, hero-form/no-star/decline controls and query/RNG/pending-work
purity compared with an unprobed control. A stale activation and a new in-play card
incarnation each invalidate the exact cancellation receipt.

Evidence logs live outside the repository in `%LOCALAPPDATA%/Temp/b6-*.log`:

- `b6-first.log`: printed-card probe fails for unsupported cancelBoostAbility.
- `b6-mutant.log`: initial mutant survived the idempotent Stunned observation; the
  probe was strengthened with counted damage to distinguish both star executions.
- `b6-mutant-counted.log`: compiled canceled-star-executes mutant killed by both
  attack/scheme cases (damage expected 5/1, actual 6/2); source restored in finally.
- `b6-restored-build.log`: nonincremental restored Release build passed.
- `b6-restored.log`: 249 focused content boost/Preparation tests passed.
- `b6-rules.log`: 58 Rules boost/active-attack tests passed.

The final architecture and normal-hook results are reported with the commit handoff.
Independent review, integrated managed lanes, replay/session and native acceptance
remain lead-owned integration evidence. This packet does not establish pack readiness.

## Independent review of `4167443afbe6b27e51fe1882fff3521485a9d94a`

No production correctness finding from raw 08024, current RR 1.8, the vendored
Target Acquired ruling, affected admission/completion/execution paths, or the
separate exact-card star/icon receipts. The discarded Preparation source retains
its captured controller and incarnation. Source tests cover attack/scheme,
multiple boosts, decline, invalid/stale rejection and query/control equivalence.

`BoostAbilityReplayReviewTests` adds four independent synthetic cases through the
existing `IDurableGameFactory` seam. They use actual `FileSessionStore` JSON and
new factories/hosts at the pending faceup Response and, when accepted, the
discarded-source Preparation completion Response. Both accept/decline branches
run with numeric cancellation accepted/declined first. They compare full card
digests, pending prompt records and RNG states with uninterrupted controls,
preserve every existing store-file hash on restart, and assert the counted boost
damage, numeric contribution, attack completion and Suit response. This is
persistence mechanism evidence, without public admission of Black Widow.

Independent Release evidence, with logs outside the repository in `%TEMP%`:

- `b6-review-content.log`: 23 Target Acquired and numeric boost cancellation tests.
- `b6-review-durable.log`: final four durable receipt/independence cases passed.
- `b6-review-historical.log`: 45 replay/upgrade/publication/Preparation tests passed.
- `b6-review-played-historical.log`: 10 genuine standard/expert played-campaign
  predecessor tests passed, retaining their expected first-divergence checks and
  original-file preservation. These incompatible campaigns are rejection evidence,
  not successful migration evidence for their remaining histories.

The global deferral changes the relative order of faceup Responses and the star;
that change is required by the ruling. The production book currently has the
05014 faceup Interrupt, preserving its pre-star order, and no faceup Response.
The additional scheduler frame does not allocate another dynamic occurrence id.
Boost execution still creates its own occurrence id zero and retains the authored
ability ordinal. Frame identities are reconstructed internal execution state,
not saved event ordinals. No supported-history event/ordinal divergence was
observed in these historical checks. This finite evidence does not prove every
possible supported history byte-identical. The integrating lead owns the final
replay identity decision; this review changes no compatibility version.

Public pack setup/admission, integrated broader lanes, native interaction and
visible cancellation explanation remain outside this mechanism review.
