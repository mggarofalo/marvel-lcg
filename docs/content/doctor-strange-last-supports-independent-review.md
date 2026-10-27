# Final Doctor Strange support rows: independent review

Reviewed source `99d4b16e78ffaf150b9c893e43d62c6788d35376` in isolated
`review/strange-last-supports`, 2026-10-26. Verdict: accept the bounded actual
09015, 09027 and 09029 authored-row packet. No functional defect was found.
This review changes tests and this record only. It does not admit Doctor Strange
to public setup or accept complete-pack, save/replay, native or platform behavior.

Authority review compared all three exact generated `datasets/cards/cards.json`
records with the appended programs and the current vendored RR 1.8. Desperate
Defense costs one, requires the player's defending hero and hero form, and binds
both its +2 DEF and undamaged ready to the exact attack. The independently
reviewed exact-attack mechanism supplies its delayed semantics. Physical Toll
offers the form change, associates the obligation with Stephen's player, and
requires ready Stephen in alter-ego form for exhaustion/removal. Its alternative
increases the next actually played event by three. RR obligation .1/.4, cost .1,
initiating-abilities steps 2 through 7 and event .2 agree with these programs.
Historical `ruling:277ab401b1269ec7` is pre-1.5; its distinction between resolving
an Invocation Special and playing an event agrees with current event rules and
printed Spell Mastery. No contrary card-specific FAQ/ruling was found.

Open the Dark Dimension stores the exact physical Invocation facedown. Its
When Defeated return precedes departure under RR when-defeated-abilities .2.1;
ordinary departure discards the stored card under attach-to .1, with the separate
Invocation recycle rule applying when necessary. The return does not shuffle the
separate discard or another host's card. DSL spelling and stored receipt shapes
are engine choices. The prior prerequisite reviews remain the engine mechanism
evidence; this review traces the actual programs into those mechanisms.

The source migrates canonical defense/Toll fixtures and the queued-attack defense
test to the complete book. Hosted choice:false uses actual 09029; choice:true
explicitly replaces it with a synthetic continuation. The actual-row four-policy
privacy test preserves its hidden-ID/face/location/metadata/event/order checks.
The reviewed diff removes only the duplicate local 09015 row from the queued
attack fixture, preserving its behavioral assertions. Inventory repairs retain
unknown-card rejection using the still-unauthored nemesis faces.

`DoctorStrangeLastSupportReviewTests` adds four Toll cases crossing current form
and readiness. They retain the optional form, verify the non-first named player,
reject an unavailable removal branch without changing card state, RNG or pending
work, distinguish exhaustion/removal from surcharge, and leave the revealer ready.
The fifth case probes actual Desperate Defense during a committed defense:
synthetic form mutation verifies admission in the same occurrence; foreign-seat
payment is rejected unchanged, then the valid same-cost payment completes the
actual event and attack. The form mutation is a synthetic admission control,
not a claimed played form-change transcript. Existing whole defense, cancellation,
Invocation and hosted classes supply the exact defender, event-vs-Special,
canceled-event, exact host and departure controls rather than duplicating them.

Independent preservation check compared source-parent Git bytes with the working
book: 679 predecessor rows, 682 resulting rows and 37/40 Doctor Strange faces.
All predecessor JSON objects compare equal, and the complete predecessor prefix
before its array closing delimiters is byte-identical, SHA-256
`b03718b04c8dde86d212820197abe739f56b9f0bc61d7364a4eb941c5b62bf37`.
The restored review book is byte-identical to source `99d4b16e`. No dataset change
is included in this review commit.

Windows Release evidence is outside the repository under `C:/Users/mggar/Source/`:

- `strange-last-review-initial.log`: five new actual-row controls pass, after
  correcting a nullable annotation in the new test before execution.
- `strange-last-review-mutant-build.log` and `strange-last-review-mutant.log`:
  valid actual 09027 `exhaust: yourAlterEgo` to `exhaust: you` mutation builds
  nonincrementally. One case fails because ready hero-form Stephen wrongly gains
  the removal option; the other four pass. This exercises the lowered actual
  program and kills a plausible wrong exhaustion target rather than a parser
  rejection. The exact original book bytes are restored in a finally block.
- `strange-last-review-restored-build.log`: nonincremental restored Content build
  passes with zero warnings/errors.
- `strange-last-review-restored-content.log`: 429/429 cases pass across whole
  classes selected by DoctorStrange, PhysicalToll, DesperateDefense,
  HostedInvocation, AbilityData, RuleCitationTests, EventPlayCancellation,
  EventPlayInitiation, NestedEventModifier, Obligation, HeroDefense,
  ExpertDefense, Invocation, BlackWidowBurnNotice, BlackWidowPreventionInventory,
  BlackWidowPreparationCard and BlackWidowFinalResponse substrings. `AbilityData`
  matches both actual ability-data class names, including all 33 data cases.
- `strange-last-review-view.log`: 38/38 cases pass across whole
  HostedInvocationPrivacy, InvocationPrivacyReview, VisibilityTests,
  CardFaceVisibility, PersistentFactsVisibility and RuleCitationTests selections.
- `strange-last-review-rules.log`: 52/52 cases pass across whole
  InvocationDeckTests, InvocationDeckBoundaryTests, ContinuousEffectTests,
  CardPlayAnOmittedCostTests and RuleCitationTests selections.

The test commands use `FullyQualifiedName~` for each substring, joined with `|`.
Builds, mutation, restoration and test lanes are serialized; PTYs are harvested by
this worker. No full Content/solution suite, second-platform run, offline card
regeneration or native/public session journey is claimed by this review. The
source packet's separate evidence remains identified in
[the authored-row record](doctor-strange-final-support-rows.md).
Normal enabled commit-hook results are recorded in the review handoff.
