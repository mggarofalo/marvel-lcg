# Timed printed text blanking prerequisite (MARVEL-734)

Private candidate based on 0d2a33ab implements a standalone self-blank Action
for a live nonpermanent minion until phase end. No actual 05028 row or public
admission is included. Printed text authority is datasets/cards/cards.json;
Rules Reference blank/traits/lasting effects determine suppression and expiry.
Traits, printed basic stats and icons outside the text box remain. The opcode
is an engine spelling; compound shapes and nonminion/permanent targets are
rejected before cost. The existing engine has no alternate-win minion hook or
DSL win/loss opcode. Any future alternate-win text needs explicit preservation
under rr:winning-the-game.1.1 before admitting it to this capability.

Active text filtering covers action/resource offers, derived and registered
constants, lifecycle discovery, damage protection, keywords and persistent
ability descriptions. Already-triggered continuations retain their compiled
body. Lasting effects already created remain. Markers bind the in-play
incarnation and expire through the phase scheduler.

Evidence: 14 focused cases pass after forced rebuild; final full Content 5,088 pass
in external blanking-final-content.log. Compiled registered-constant and
lifecycle-discovery mutants fail 1 and 2 intended assertions; forced restoration
passes 14. Earlier overlapping build failed with locked DLLs; that failed run
is not proof. Tests include phase expiry, offphase restoration, numerical
keywords, pending continuation, permanent cost rejection and replay of identical
synthetic worlds. This is not hosted persistence evidence.

Independent review remains required, especially projected-vs-live constant
queries, pending ordinal lookups and timing observers that may retain extra
scheduler boundaries despite suppressing actual waiting abilities. Current
native interaction uses existing encounter-action patterns; actual face tests
and any necessary native proof follow engine approval.

## Corrected prerequisite independent re-review

Reviewed engine revision `87b930d2` in an isolated worktree, including the
`745c6924` defeat-text correction and the pre-cost constant-ending correction.
Verdict: no blocking finding for the bounded standalone self-blank Action on a
live nonpermanent minion. This verdict does not admit an actual 05028 ability
row, prove hosted persistence, or replace the final combined integration lanes.

`PrintedTextBlankingReviewTests` independently checks refusal through repeated
offer queries and through a stale action execution. Both preserve the mental
payment, card digest, registered and active effects, pending agenda and RNG;
effective ally health remains 4 and the source remains unblanked after refusal.
The supported same-cost control uses `Game.Resolve` in the player-turn fixture:
the health constant ends, Black Cat is defeated, payment is discarded, the
source is blanked and the game returns to its turn prompt. Three extra queries
produce the same final digest, effects, pending work and RNG as a control world.
Blanked Hydra Soldier's printed defeat text permits the labelled continuation
both with and without an authored defeat definition.

Compiled mutations, serialized and rebuilt nonincrementally:

- Replacing `CanBeginBlank` with `CanBlank` fails two refusal cases: the stale
  execution spends its payment and the offer query fails to throw. Two other
  review cases pass.
- Removing only the explicit blank predicate fails the unauthored defeat-text
  case, with four review cases passing. The authored-only version initially
  survived because raw definition coverage independently prevents that case;
  the unauthored control distinguishes the explicit predicate.
- Restoring the complete pre-fix active-definition predicate fails both blanked
  defeat-text cases, with three review cases passing.

After source restoration, `dotnet build
tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-restore
-t:Rebuild` passes, followed by `dotnet test` on that project with `--no-build`
and filter `FullyQualifiedName~PrintedTextBlanking|FullyQualifiedName~RuleCitationTests`:
21 pass, zero fail. No full Content, Rules or other combined suite was rerun in
this re-review. Initial supported-control attempts using direct component
execution lacked a live agenda occurrence and failed; only the corrected
`Game.Resolve` fixture supplies the passing supported-control evidence.

External logs are under `C:/Users/mggar/Source/` with prefix
`blanking-final-review-`: `mutant-preflight-build.log`, `mutant-preflight.log`,
`mutant-defeat-guard-build.log`, `mutant-defeat-guard.log`,
`mutant-defeat-build.log`, `mutant-defeat.log`, `restored-build.log` and
`restored.log`. Source restoration and verification precede the review commit;
normal pre-commit architecture, complexity and file-length hooks remain enabled.
