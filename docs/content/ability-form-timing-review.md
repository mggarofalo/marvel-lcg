# Independent ability form timing review — MARVEL-862

Reviewed source `7a9b38e3b082f6db51c2dd43d52f2a2413a69776` in a fresh
`review/ability-form-timing` worktree. Review owns tests and this report only.
The accepted three-face selection source remains the base; the voluntary
flip-before-interrupt defect in the source evidence packet remains separate.

## Verdict

The printed Split Personality / Taskmaster path passes the six source cases.
The deferred operation has unresolved live-state revalidation defects, so this
review does not accept its general suspended execution behavior.

`IdentityFormChangeOperation.Apply` reads the seat's current identity without
retaining the scheduled incarnation, and calls the voluntary-style throwing
permission/destination guards. The new interrupt interval can invalidate either.

1. A synthetic Forced Interrupt with an exhaustion cost changes Jennifer to hero
   while an outer ability's change-to-hero is pending. The nested change succeeds;
   the outer change then throws `ArgumentException` at `Forms.Change`, instead of
   settling an ineffective outer flip and resuming its independent suffix. This
   is an interpreter-supported nested timing case; no public product is admitted.
2. A synthetic Forced Interrupt deals lethal damage to the owner while another
   player survives. Elimination removes that identity. The pending outer change
   nevertheless changes the removed card from Jennifer to She-Hulk and emits a
   successful flip. RR `ability.1` limits card abilities to in-play elements;
   `player-elimination.step.5` removes the eliminated player's game elements.
   `player-elimination.5` preserves the original ability's independent suffix.
3. A synthetic scheduler-boundary control adds an absolute form prohibition
   after scheduling. `FormChangePermissions.Require` throws, leaving the ability
   unfinished. The effect should respect the live prohibition and settle zero;
   RR `cannot.1` gives the ability with “cannot” precedence. This control makes no
   printed-card reachability claim.
4. A synthetic scheduler-boundary control moves the scheduled identity through
   discard and back to its original area. Its incarnation changes, but the
   pending operation flips that new copy and gives success credit. This control
   makes no claim that supported identities can legally return from discard.
   The operation needs an explicit exact-copy policy across its suspension.

For ineffective flips, dependent drawing must remain suppressed while the
independent villain-damage sentence still resolves. RR `then.2` says post-then
text does not attempt resolution when its prerequisite fails. The tests require
the existing receipt to communicate zero and the agenda to finish cleanly.

## Evidence and limits

`AbilityFormTimingReviewTests` adds five independent cases. The focused command
`dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter
'AbilityFormTimingReviewTests|AbilityFormTimingTests|AbilityFormCompletionTests'`
compiles and runs eleven cases: seven pass, four fail as described above. The
complete output is `C:/Users/mggar/Source/ability-form-independent-review.log`.

The passing independent case cancels the pending form occurrence, suppresses
its dependent draw, retains the paid exhaustion and executes the independent
villain-damage suffix. All six source cases pass, including old-face observation,
actual three-damage Taskmaster resolution, optional response before the draw,
voluntary-limit preservation, cancellation receipt and response-side elimination.

No production changes, ability-book rows, public admission or UI changes are in
this review. No broad integration result is claimed. The source's stopped broad
run is not evidence; replay/version and unrelated fixture reconciliation remain
root-owned. These failing tests are regression witnesses for the reported gaps,
not an accepted green delivery.

## Closure review of `3b186f64`

Verdict: accept the ability-driven form timing and revalidation packet within its
stated boundary. The four prior findings are addressed: the pending destination,
owner/identity eligibility, live prohibition, and original incarnation are checked
at application; a replacement physical identity is also rejected. Failed changes
settle zero and do not offer an after-change response. The synthetic tests verify
that the dependent draw is skipped and the independent damage suffix still resolves.
Cancellation, nested changes, elimination, prohibition, re-entry and replacement
remain synthetic boundary controls; they do not claim all are reachable in admitted
content. The response suppression predicate is restricted to the failed
`IdentityFormChangeOperation`, while ordinary lifecycle windows retain their
existing path and the Rules lifecycle regression selection passes.

`AbilityFormGameReviewTests` adds a distinct normal `Game` offer/`Resolve` proof.
It uses a manually prepared dealt She-Hulk/Rhino world with the villain replaced by
actual Taskmaster, actual Split Personality, printed She-Hulk and a known boost;
it is not public Taskmaster scenario setup or Ant-Man admission. Both accepting
and declining the optional response pass. Three payment discards precede the
single flip; Taskmaster discards the boost and deals three with its source and
occurrence association before She-Hulk's response; acceptance deals two before
both actual draws. The ordered event assertions include payment, receipts, flip,
forced response, draw and event cleanup, and check the resumed draw does not
borrow Taskmaster's attribution. The game returns to the root turn prompt with
RNG state and consumption unchanged. Mulligan fills the prepared hand, so two
cards are drawn to the printed hand size of four.

Independent run on this tree, Release and `-m:1`, with
`MSBUILDDISABLENODEREUSE=1`: Content filter
`AbilityFormGameReviewTests|AbilityFormTimingReviewTests|AbilityFormTimingTests|AbilityFormCompletionTests|FormChange`
passed 86/86; Rules filter `Forms|Lifecycle|RuleCitation` passed 65/65;
Architecture passed 129/129. Logs are `C:/Users/mggar/Source/ability-form-closure-focused.log`,
`ability-form-closure-rules.log`, and `ability-form-closure-architecture.log`.
The initial fixture expected four cards drawn but mulligan had already filled its
hand; that incorrect fixture expectation was corrected after inspecting the state.
The event-inspection diagnostic intentionally failed and is not test evidence.
The predecessor packet records its compiled mutants and broader 216-case Content
and Gherkin runs. No production changes were made in this review. Voluntary
flip-before-interrupt remains MARVEL-863; Tiny/Giant DSL, projections, admission,
and replay compatibility remain outside this acceptance.
