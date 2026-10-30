# Ant-Man baseline fixture repair review

Candidate reviewed: `420a2cae4a6f874833b7f47f08663b3c8464ca14`, against
`f6357d293a32fc6b29fe245a7ddd60fe77eff3cc`. Independent review covers the five
changed test fixtures and their agenda/continuation dispatch, not pack readiness.

The baseline log `../evidence/resize-review-base-failures.log` records five
failures and 49 passes across the affected classes. The candidate log
`../evidence/ant-base-regressions-focused.log` records 54 passes. These logs are
local evidence outside the repository, and the latter predates the review edits.
The inventory correction includes already-authored Power Gloves (`12017`); it
does not admit the pack.

The candidate preserves the prior outcomes but answers two scheduler-produced
removal prompts directly through the runner. That bypasses the exact owner
advancement performed by `Sequence.Answer`. Review edits answer the prompts
through the sequence and finish successful removals. The invalid removal remains
at its prompt, and Eviction Notice intentionally stops at its next choice.

The pre-existing thwart surge test allowed either its pre-thwart or post-thwart
grant to satisfy the single-card assertion and did not observe the resumed
suffix. Review edits retain the repeated-grant case, add a case with only the
pre-thwart grant, and observe a suffix counter. The neighboring attack test has
the same pre-existing sensitivity limitation and was outside this repair.

The five affected classes pass 55/55 after the review edits. A compiled mutation
that removed restoration of `thwart.SurgeGained` failed the repeated-grant case
at the suffix counter but the single-grant case passed. Thus this mutation
distinguishes the suffix observation; the single-grant case alone does not prove
restoration of the carried state. The mutation was restored, followed by a
non-incremental Release build and a 55/55 rerun. Logs are
`../evidence/ant-base-fixture-review-mutant.log`,
`../evidence/ant-base-fixture-review-rebuild.log`, and
`../evidence/ant-base-fixture-review-restored.log`. An attempted `dotnet test
--no-incremental` was rejected as an unknown switch; the successful rebuild and
`--no-build` test replaced it.

Verdict: the inventory and fixture repairs are suitable for integration with
the review edits. No engine behavior regression is inferred from the old fixture
failures. Broader pack and platform verification remain outside this review.
