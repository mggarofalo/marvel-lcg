# Actual Bring the Hammer Down independent review

Reviewed source revision `bb4e38fd07fc7b0eb392cabea5983cba231cd611` in isolated Windows worktree `C:/Users/mggar/Source/marvel-lcg-review-ronan-hammer`. Verdict: accept the bounded actual 90004 interpretation. No production correction was identified. This review adds tests and a fixture runner-injection seam only; Kree Fanatic setup and public admission remain separate.

## Authority and interpretation

Joined `datasets/cards/cards.json` 90004 supplies both printed instructions: Ronan activates against his engaged player, with surge if absent; the boost deals the first player one facedown encounter card if this activation defeats a character. The vendored FAQ lists 90004 as queried and has no card-specific entry. Current RR 1.8 `rr:activation.2`, `.7`, `.8`, `rr:boost-boost-icon.2`, `.3` and `rr:surge.1` govern form, completion, queued activation separation, boost-only text and damage, and surge.

The actual reveal locates Ronan in each player's own play area, then selects attack or scheme using that player's form. The absence branch gains surge for the original revealing player. The boost retains its exact activation receipt, requires at least one actual defeat and deals one card. `AbilityContinuations` checks the completed attack's activation ID; `AttackCompletion` copies its owned defeat receipts. These engine-chosen representations agree with the printed condition. The [accepted receipt review](activation-defeat-outcome-review.md) provides separate prerequisite evidence.

## Distinguishing evidence

The seven source cases in `RonanHammerActualCardTests` use the production book directly and cover engaged-seat activation, full absent-Ronan reveal/surge/cleanup, equal damage with zero or one defeat, scheme boost-only defeat, two overkill defeats dealing once, and a following activation not borrowing a prior defeat.

Six new cases in `RonanHammerActualReviewTests` add:

- Ronan engaged with non-first player 1, revealed by player 0, with opposite forms in both directions. Only the engaged seat's form selects attack or scheme.
- First-player token moved to player 1 while player 0 suffers the lethal attack. The facedown encounter card follows the token.
- A separate synthetic supporting boost queues a lethal minion attack. The actual 90004 boost on the original nonlethal activation does not borrow the queued activation's defeat.
- A synthetic supporting would-defeat interrupt recovers the attack recipient, both alone and while defeating a different ally with separate direct damage. The actual boost deals no card despite lethal attack damage placement and the collateral defeat.

Supporting fixtures replace only 01105 or 01083, retaining all other production rows. They assert the 90004 raw JSON object is identical to the actual book's row. They do not replace 90004 with a synthetic interpretation. Every case drains the agenda, checks activation retirement and rejects unintended game completion through the shared fixture.

## Verification and mutation

The source book has 723 actual rows. A raw JSON span comparison against `bb4e38fd^` verified all 722 earlier objects byte-exact. Encoding is UTF-8 without BOM and LF-only. Original and restored SHA-256: `4b1032c2d6b512708ab58549fe49da010337dad7b045b803af950e179596a3c9`.

Logs are outside the repository under `C:/Users/mggar/Source/evidence/ronan-hammer-review-`:

- `baseline.log`: 199 passed, selecting `RonanHammerActual`, `ActivationDefeatOutcomeTests`, `RuleCitationTests`, and the complete `Marvel.Content.Tests.Cards` namespace.
- `mutant.log`: the single actual-row mutation `dealEncounterCards.player: firstPlayer -> you` parsed, lowered and executed normally. Three of 13 actual-card cases failed for the intended recipient behavior, including the moved-token review case. The remaining ten passed. No threshold or deal quantity changed.
- `restored-build.log`: exact original bytes restored, then a forced non-incremental Release build succeeded with zero warnings/errors.
- `restored.log`: the same 199-check selection passed after restoration.

The focused selection uses `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~RonanHammerActual|FullyQualifiedName~ActivationDefeatOutcomeTests|FullyQualifiedName~RuleCitationTests|FullyQualifiedName~Marvel.Content.Tests.Cards'`. The restored run uses `--no-build` after the forced rebuild. Normal commit hooks remain enabled.

This review does not claim combined-candidate gates, a full managed lane, Linux, public setup/gameplay/save, native UI, or whole-pack acceptance. Final integration must verify the combined candidate independently of these isolated source-revision results.
