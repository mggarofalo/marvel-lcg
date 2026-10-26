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
