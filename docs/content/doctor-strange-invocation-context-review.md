# Invocation continuation independent review

MARVEL834, source `7ccdcd738daba01b17ff9446af36529e2a5276c3`, compared with
`3df6bdf0`. Review branch: `review/invocation-context`.

Verdict: the reviewed continuation semantics pass the added controls. Reject the
current copy for product review until the two clarity findings below are repaired.
This review does not admit Doctor Strange to public setup or establish owner
acceptance.

## Required clarity repairs

1. In both recipient screens, “Its instructions independently discard this
   Invocation and refresh the public top” has an ambiguous subject. In the Master
   path, the preceding sentence names Master's paid event cost; in Spell Mastery,
   it names Doctor Strange's exhaustion. Name the Invocation or its Special
   explicitly. Use ordinary game language, for example “This Invocation's Special
   discards it and reveals the next Invocation, even if its other effects do not
   apply.” Retain the separate exact physical return for Master and the absence
   of a discard promise for the synthetic Special without discard. Derive these
   facts from typed instructions, as the current implementation does.
2. The complete recipient-choice paragraph appears once at the end of the
   continuation description and again below “Choose status recipients.” Preserve
   the continuing action, accepted costs and consequences in the situation text,
   and show the current choice instructions once. Use contract boundaries or
   typed equality; do not remove sentences by parsing prose. Other nested choices,
   including Vapors' two distinct decisions, must retain their own meaning.

The findings are visible in `closeout-rendered/master/recipients.png`,
`closeout-rendered/spell/selected-spider-man.png` and
`final-rendered-built/master/recipients.png` under
`C:/Users/mggar/Source/marvel-evidence/invocation-native/`. These captures were
opened and inspected directly. The selected Spell screen retains its paid action
context, selection, exhaustion and commitment together. Master's result capture
shows Spider-Man Tough and Strange ready. The closed Invocation piles do not
prove their contents visually; authoritative assertions own that evidence.

## Independent behavioral evidence

The added View controls invoke the live nested `AbilityRunner.Choosing` query,
rather than rereading the already cached pending prompt. Both paid paths preserve
the card digest, RNG state, pending frames and ancestor list over repeated queries.
The next accepted recipient decision produces the same board, RNG state and next
prompt as an unprobed control. A synthetic departure of the captured physical
Invocation removes the paid context; the new public top cannot inherit it. A
failure-only return instruction does not acquire Master's successful-return
promise inside the Special.

The new Rules control constructs unrelated and nested suspended work. It requires
only the nearest parent and outer ancestor, in that order, excluding unrelated
work and the current child. Foreign and consumed frames are rejected. The
returned collection remains available after execution advances. Replacing exact
execution identity matching with any non-null execution kills this test at the
ancestor collection assertion.

After restoring the mutant, a nonincremental Release View-project build succeeded
with zero warnings/errors; all 416 View tests passed. The affected Rules filter
(`EnclosingAbilityContinuationsTests`, `AbilityExecutionOwnershipTests`,
`ContinuationExtensionTests`, `AgendaFrameIdentityTests`) passed 12 tests.
TRX files are under `independent-review/{view-full,rules-restored,mutant}` in the
external evidence root. Build and test logs are `review-*.log` beside it. The
first new-test run failed compilation on a `Decision.Take` overload; the final
successful build includes the corrected `Decision` construction. That initial
diagnostic is in the tool transcript; the named View log contains the rerun.

Source review also checked exact captured card/incarnation/owner/cost matching,
accepted exhaustion/event-cost descriptions, ordinary discard versus Master's
exact return, synthetic no-discard behavior, private-seat filtering and concealed
order noninterference. Existing tests cover the latter visibility and synthetic
boundaries; this review does not claim a new mutant for each existing branch.

No production source changed in this review. No native rerun was needed for these
test-only additions; the source journeys and the inspected captures are evidence
for their recorded source revision. Copy/dedup repairs need a fresh affected
managed run and both native paths with rebuilt dependencies, followed by direct
inspection. Other profiles, native Vapors, explicit-zero native selection,
ordinary setup and complete games remain outside this review's native scope.

## Copy repair closure review

Reviewed production source `6382318cbf73977af83602070ea3594e4020985e`, with
the independent tests above replayed as `68ca52b6` on
`review/invocation-copy`. The named-Special copy and exact duplicate suppression
are accepted as repairs of the two findings above. The bounded native matrix
is **not accepted as fully green**: Spell Mastery at 80% cannot expose the whole
Energy payment control. Owner review should not be used to discover or waive
this finding.

Source review confirms that the title comes from the captured physical
Invocation. The discard sentence still requires the typed final discard
instruction; Master's return still requires its successful dependent return.
The original nonempty prompt description remains in the paid continuation
context. Only an exactly equal description on a sole affordance is cleared;
distinct alternatives and both Vapors decisions retain their own meaning.
Lineage matching, accepted costs, no-refund meaning, source identities,
private-seat filtering and concealed-order noninterference retain the independent
controls described above. No protocol, renderer, ability book or public setup
change is part of this closure.

The test-only launcher now enumerates both paths at 80%, 100% and 150%, with
separate `<scale>/<path>` artifact directories. It retains actual viewport
input, reversible recipient staging, exact physical identities and all native
state assertions. Five cases pass with exactly two resolve requests each;
Spell Mastery at 80% fails before any resolve. The unchanged full-visibility
assertion requires the entire 48-pixel-high Resource41 button inside its
clipping scroll ancestor, whose available height is only 36 pixels. Its top
12 pixels are clipped even after Tab focuses it. This is a payment-layout
finding, not a failure of the recipient copy or a license to weaken the driver.

The same failure reproduces against exact production base `7ccdcd73` in detached
worktree `C:/Users/mggar/Source/marvel-invocation-scale-base`, with only the
test-only scale launcher copied. Fresh Debug and Release builds precede the
single filtered Spell/80 rendered run. `base-rendered.trx` records one failure
with zero resolves; `base-rendered/80/spell/49024-8852594/failure.{png,json}`
records the same 48-pixel control inside a 36-pixel clipping ancestor. This
establishes that the layout failure predates the reviewed copy repair.

Fresh nonincremental Debug and Release builds of the candidate native test host
succeed with zero warnings/errors. The full combined View assembly passes 416
tests; the affected Rules filter passes 12. Evidence is under
`C:/Users/mggar/Source/marvel-evidence/invocation-native/copy-review/`:
`debug-build.log`, `release-build.log`, `view.trx`, `rules.trx`,
`rendered.trx`, and per-profile `rendered/<scale>/<path>/run.log` and PNGs.
The failing driver's pixels and geometry are in
`rendered/80/spell/27532-5612515/failure.{png,json}`.

Both original `copy-clarity/rendered-final` recipient captures and all five
newly produced recipient/result pairs were opened and inspected directly.
The current action, accepted costs, single current-choice instruction and
independent discard/return meaning are readable together. The 150% recipient
sheet reaches the viewport bottom, but its complete commitment control remains
visible. Results show Spider-Man Tough and Strange exhausted only for Spell
Mastery; closed Invocation piles still do not prove exact pile contents in
pixels. The authoritative assertions supply that evidence. No 80% Spell
recipient or result image exists because payment is blocked.

Commands build `tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj` in Debug
and Release before the Release `dotnet test --no-build` run filtered to
`InvocationNativeJourneyTests`, with `MARVEL_NATIVE_INVOCATION=true`,
`MARVEL_NATIVE_RENDERED=true`, discovered `GODOT_BIN`, and a fresh external
`MARVEL_UI_EVIDENCE_DIRECTORY`. The profile is Windows Godot 4.7.1 Mono,
1920x1080, two seats, reduced motion. This is prepared nonpublic evidence;
native Vapors, explicit-zero submission, other scales/viewports/platforms,
ordinary setup and complete games remain outside this closure. Automated
evidence and this source review do not establish owner product acceptance.
