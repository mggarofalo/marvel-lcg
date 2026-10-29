# Independent review: player ally entry interrupt

Reviewed source `39927976` against base `1b4eed16`. This is a bounded engine prerequisite, not Hank Pym admission or zero-health settlement.

The printed 12011 text, `rr:interrupt.3`, `rr:initiating-abilities.1` and `.step.7`, `rr:enters-play`, `rr:ally-limit`, and `rr:leaves-play.1` were checked directly. Interrupt before completed entry and limit before entry responses is an inference reconciling the clauses, not a direct Hank/ally-limit ruling. The independent authority adjudication at `C:/Users/mggar/Source/evidence/hank-entry-timing-adjudication.md` records its basis and the unresolved simultaneous zero-HP/limit issue MARVEL-885.

The review traced exact pending-card admission, play and put occurrence construction, staged counters and movement reset, paid Make the Call suspension, unique-card no-op, and binding of a continuation to the entered incarnation. Existing payment tests retain their receipt and rejection assertions; their direct-runner setup now drives the actual entry continuation. Rapid Response and Emergency Teleporter are relevant existing continuations. The independent synthetic timing test exercises both played and put allies with an optional own-entry interrupt, a mandatory fourth-ally discard, then the own-entry response; it checks both counters and completed work.

The focused review filter passed 29 tests, including the new two-case probe and the complete Rapid Response and Emergency Teleporter classes (`hank-entry-review-focused.log`). An initial fixture expected a played card to remain in hand; actual play correctly puts it in the revealing area before this interrupt, and the corrected test asserts that area. A compiled mutant omitting the ordering of pending children before entry responses failed both new cases at the limit prompt (`hank-entry-review-mutant.log`). After restoring the exact source, a non-incremental build succeeded and both cases passed (`hank-entry-review-restored-build.log`, `hank-entry-review-restored-test.log`). Logs are under `C:/Users/mggar/Source/evidence/`.

Verdict: no blocking defect found in the bounded entry prerequisite. This review does not cover full Content or replay publication, zero-HP settlement, excess-resource expression, or actual Hank Pym admission. Those remain integration or separate prerequisite work.

