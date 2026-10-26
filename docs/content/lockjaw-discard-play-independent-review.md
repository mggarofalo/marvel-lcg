# Lockjaw discard-play prerequisite independent review

Reviewed implementation: `c9c2e87b` (`feat(cs): support passive ally play from own discard`).
Review date: 2026-10-26. Verdict: **request changes** for published-offer rejection purity.

This is a nonpublic engine prerequisite review. It does not admit 05018 or the Ms. Marvel pack, and does not establish native interaction or public product readiness. The reviewed commit contains 16 changed files, including the synthetic `DiscardPlayPermissionTests`; it contains no implementation evidence document. This record describes the actual source diff and independently executed checks.

## Authority and implementation assessment

- Generated `datasets/cards/cards.json`, 05018: cost 4, unique Basic ally; “You may play Lockjaw from your discard pile during your turn (paying his resource cost).”
- Vendored `datasets/rulings/rulings.json`, `ruling:f53a301e8096657d`: Lockjaw in discard is not treated as in hand; a hand-only Team-Building Exercise target cannot select him there. Permission must preserve the physical discard origin and must not grant hand-resource access.
- `rr:ally-limit`: the mandatory discard “occurs before abilities that resolve upon entering play.”
- Repository `docs/affordances.md` requires rejected answers to preserve the current decision, including information signals. This API-purity requirement is an engine contract, not a tabletop rule.

The typed, direct unconditional Constant marker and runtime owner/area/ally checks match the bounded prerequisite. `CardPlayOrigin.Candidates` owns stable ordinary-play candidate enumeration; `GamePrompts` consumes it. The new placement port is abstract, with no permissive default body. The implementation retains the existing stored-event origin path. Pricing uses ordinary costs and resource generators, rather than relocating the ally into hand. No production ability-book row or public-admission change is included.

## Findings

1. **Published offer rejection loses information signals.** Publish a valid, explicitly allocated discard-play answer, then move Lockjaw to the owner's deck or change its owner before submission. `Game.Resolve` rejects at live card-play legality, but `GameResolution.ResolveInput` has already cleared information signals. The regression records a Reveal signal before submission and observes that it disappears. Card digest, full RNG state and consumed words, agenda, registered effects, and prompt identity remain intact. The new tests intentionally fail on this final information assertion. Revalidate the live origin before clearing signals, then rerun the complete regression with an accepted retry/control.

2. **Shared entry response executes twice after an ally-limit choice.** A synthetic 05018 body combines the permission with a ForcedResponse on `WhenCardEntersPlay` drawing one card. Seat 1 starts with three allies and pays four resources. Before choosing the mandatory discard, no response has drawn a card: ordering is correct. After choosing the discard, two cards are drawn. The same paired test fails when playing from hand, establishing that this shared lifecycle defect is not introduced by discard permission. Printed Lockjaw has no such response. Triage this as a separate engine finding; do not describe the passing agenda-order test as proof of completed response execution.

There is no revision counter on `Game`/`World`; revision is a session concern, outside this packet. No session-revision assurance is claimed.

## Independent evidence

All commands used Release configuration in isolated worktree `marvel-lockjaw-review`, with PTY execution and normal hooks enabled. Logs live outside the repository under `C:/Users/mggar/Source/`.

- `lockjaw-review-entry-control.log`: the eight new `DiscardPlayDecisionTests` cases compile; four pass and four fail as specified above. Accepted ordinary play at seat 1 passes at cost 4 and discounted cost 3. Foreign identity targets and self-payment are rejected before consuming the recorded information signal.
- `lockjaw-review-focused.log`: earlier combined run of discard permission, storage permission, Quiver search and stored-event play, plus the then-seven review cases: 33 pass, three fail. The extra hand-origin control was added afterward and independently reproduces the entry-response failure.
- Existing storage/Quiver/stored-event tests in that combined run pass. This is focused regression evidence, not a full Content-suite result.

No new production logic was authored by the reviewer. No additional mutant was run in this review packet; previously reported implementation mutants were not treated as independently reproduced evidence. The failing information and duplicate-entry regressions are preserved for implementation and fresh review. Required integrated managed/platform gates remain the lead's responsibility.

## Final bounded re-review at d174876e

Reviewed the origin fix `f66bf09d` and entry-window fix `d174876e` in isolated
worktree `marvel-lockjaw-final-review` on 2026-10-26. The original published-origin
finding is resolved: both moved-to-deck and changed-owner submissions retain
information signals and accept a corrected retry. Ordinary second-seat discard
play costs four resources, respects a cost reduction, and is unavailable off turn.
Uniqueness, foreign target and self-payment controls retain ordinary restrictions.
The paired hand/discard ally-limit response tests and put-into-play controls pass:
mandatory discard precedes entry response, entry resolves once, and only ordinary
play opens the separate optional played response with its causal context.

Verdict: **request changes for live payment rejection purity**. The bounded
origin fix and shared duplicate-entry correction (MARVEL-740) are accepted.
This is not public 05018 admission or a completed pack.
Two integration concerns remain separate from that bounded acceptance:

- A newly checked adjacent invalid-payment path still violates the published
  answer contract. Publish an allocated discard-play answer, move one selected
  resource card from hand to deck, record a Reveal signal, then submit. Rejection
  occurs after information signals are cleared. Card digest, complete RNG state
  and consumed words, agenda, registered effects and prompt identity are unchanged.
  `DiscardPlayFinalReviewTests` preserves this failing counterexample. Short and
  duplicated payment answers and newly blocked uniqueness each reject without
  losing information and accept a corrected retry. The stale-payment failure is
  inherited from general live resource validation, separate from live origin.
- The shared entry correction changes outcomes for a previously duplicated entry
  response after an ally-limit choice. Engine replay remains v24. Release policy
  requires a compatibility decision when the same setup/decisions resolve
  differently. Existing replay verification checks prompts, events, RNG and digest,
  which guards against silently publishing divergent history. The focused passing
  replay tests below do not establish a genuine predecessor save exercising this
  exact path. Integration must preserve such a source fixture (for example actual
  Core Maria Hill entering as the fourth ally), prove its rejection or supported
  transition, and record the version/admission choice. No fixture was regenerated
  and no compatibility identity was changed during this review.

Independent Release evidence, logs outside the repository at
`C:/Users/mggar/Source/`:

- `lockjaw-final-content.log`: discard permission/decision tests, 24 passed.
- `lockjaw-final-core.log`: `FullyQualifiedName~CoreCardFaceTranscript`, all 20
  pinned Core transcript tests passed. The earlier exact `...TranscriptTests`
  filter did not match their refactored class names and is not Core evidence.
- `lockjaw-final-replay.log`: SessionPersistenceTests, ReplayContractUpgradeTests
  and QuinjetReplayTests, 33 passed.
- `lockjaw-final-negative.log`: independent controls, 3 passed and the one
  stale-payment information assertion failed as described above.

No production logic was changed. These results are focused evidence; integrated
managed/platform gates remain the lead's responsibility.
