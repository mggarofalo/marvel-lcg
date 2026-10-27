# Invocation continuation transcript review

MARVEL-834 follow-up against `54b9055a`.

The chosen-player activation test invoked `AbilityRunner.Chose` directly and
then resumed `Sequence.Work`. `Chose` consumes the interpreter permit; the
scheduler's `Sequence.AnswerAgendaStep` additionally marks the answered choice
applied. Bypassing that scheduler operation left the consumed choice current.
The Invocation context query correctly rejected that frame. No production
ownership or stale-frame check needs relaxing to accept this transcript.

The repaired test obtains its bound prompt from `Sequence.Work`, answers through
`Sequence.Answer`, and observes that the original choice is no longer current.
It still proves the second player's selected binding survives the activation
and draws that player exactly one card, and now also requires no choice remains
outstanding afterward.

`InvocationActivationContinuationTests` uses an explicitly synthetic Special on
a prepared nonpublic board. For both Spell Mastery and Master of the Mystic
Arts, a choice schedules an enemy activation and then a second choice. Stun
cancels that activation through the ordinary engine procedure. The second
choice retains the exact accepted parent's paid context. Repeated queries
preserve the card digest, full RNG state and outstanding work; the next accepted
outcome and prompt equal an unprobed control. Both paths reach the root prompt
and reject a retained stale choice. This is engine boundary evidence, not
printed-card admission or product acceptance.

Windows PTY evidence is stored outside the repository under
`C:/Users/mggar/Source/evidence/`:

- `invocation-context-reproduce.log`: original Content class, 6 passed and 1 failed
  with the consumed-continuation diagnostic.
- `invocation-context-content-restored.log`: repaired whole Content class, 7 passed.
- `invocation-context-activation.log`: both synthetic activation cases passed.
- `invocation-context-mutant.log`: a compiled removal of Invocation context killed
  both new cases on the missing paid-context assertion; no build failure counted.
- `invocation-context-restored-build.log`: source restored, nonincremental Release
  build passed with zero warnings or errors.
- `invocation-context-full-view.log`: full View regression run, including the
  existing independent Invocation query-purity and physical-identity controls.
- `invocation-context-rules.log`: whole enclosing-continuation class, 1 passed,
  retaining nearest-parent, unrelated-work, foreign-agenda and consumed-frame controls.

No full Content run was duplicated; the lead owns that combined gate. Independent
review and integration remain separate from this implementation evidence.

## Independent transcript review

Reviewed `dcea2dc5` in `review/invocation-context-transcript` on Windows,
with test-only strengthening recorded in the review commit. Verdict: accept the
scheduler entry correction; no production fix or stale-frame relaxation is
justified by this failure.

`Sequence.Answer` requires the current bound prompt, enters the procedure scope,
dispatches `Chose`, and marks that exact choice applied. Its answered agenda item
may remain behind newly scheduled activation work until normal cleanup. It must
not become the current question again. The Content transcript preserves this
distinction and requires no choice outstanding after completion. Review adds
the physical deck-top card and the other player's unchanged hand as independent
observations of the selected player's draw.

The synthetic activation test now observes the final physical draw and exactly
one added card, with the other player's hand unchanged. This distinguishes a
skipped final effect even if both probed and control paths reach the root.
Repeated-query checks also retain the exact current frame and its enclosing
lineage from before the first explicit query. Existing View closure retains
restricted-seat and concealed-order checks, physical Invocation identity,
parent cost/consequence meaning, and detached snapshots. Rules closure retains
nearest-parent ordering, unrelated-work exclusion, foreign-agenda rejection and
consumed-frame refusal. Neither guard was changed.

Independent Release results (logs in `C:/Users/mggar/Source/evidence/`):

- `invocation-transcript-review-view-focused.log`: the three complete affected
  Invocation classes, 13 passed.
- `invocation-transcript-review-content.log`: complete nested dependent-choice
  class, 7 passed.
- `invocation-transcript-review-rules.log`: enclosing-continuation and execution
  ownership classes, 7 passed.
- `invocation-transcript-review-view.log`: full View project, 418 passed.

Reviewed the author's compiled context-removal mutation and restored-build logs.
Both cases fail on missing paid context at the first choice; this establishes
that removal is detected, not a separate measured mutation after activation.
The explicit second-choice assertions exercise that later boundary. No redundant
full Content sweep was run. This review covers scheduled activation resumed
through stun cancellation, not the full defense/damage workflow, public Strange
admission, native interaction, Linux execution or owner product acceptance.
