# Simultaneous direct-damage defeat final review

Reviewed candidate: `dd63ad0627ceab787f7ac068d270f9822f5ffe15`, on an isolated `review/simultaneous-defeat-final` checkout. This review changes only its new synthetic test class and this evidence record.

Verdict: the shared damage-step 6 → 7 → 8 correction passes the exercised engine contracts. **Integration remains blocked by missing per-offer recipient meaning** and requires the compatibility decisions below. This is not acceptance of Thor, Lightning Strike, or simultaneous attack damage.

## Finding: indistinguishable recipient occurrences

`SameSourceStepSixOffersIdentifyTheirDifferentImminentRecipients` fails for different-title and same-title physical recipients. A single global source receives two valid step-six handles, but serializing every affordance field after normalizing only its opaque `Id` produces one distinct value. No offer identifies which imminent defeat it answers. In the same-title case, the global observer is outside the damaged pair, so the two recipients really are separate physical copies of Sandman.

`DefeatWindowEligibility.Instances` retains the exact origin internally. That fact never reaches `PendingAbility` or the offer's semantic context. `DefeatProcedure.ChooseWouldBeDefeated` describes the whole choice using the first recipient, and `AbilityAffordanceDescription.Caption` cannot describe a would-defeat occurrence. The player therefore cannot intentionally select one recipient's occurrence first. Opaque handles, declaration order, or a title-only suffix cannot supply physical recipient identity.

The engine should carry each offer's exact causal recipient through the visibility-safe contract, permitting the client to explain the choice without deriving rules or guessing from handle order. Do not misuse effect-target selection or manufacture a complete intention to carry that context: the interrupt's causal recipient and its effect's chosen targets can differ. This is required by root and `src/AGENTS.md`'s meaning-at-the-owning-layer rules.

## Authority and source review

Vendored `rr:damage.step.6`, `.step.7`, and `.step.8` separate imminent-defeat interrupts, defeat interrupts, and departure. `ruling:304a4798e0b453b4` explicitly says each damage step resolves simultaneously for overkill recipients. Applying the shared barrier to simultaneous direct damage is supported by `rr:and` and the Ground Stomp clarification `ruling:3eb442853617fe14`. `rr:forced.5` assigns simultaneous mandatory ordering to the first player; `rr:interrupt.2` and `rr:triggering-condition.1` preserve separate triggering occurrences. The generic `rr:triggered-ability` entry defines the timing-trigger form; it does not itself establish a shared damage barrier.

The candidate's red-review expectation edits add the newly required ordering prompts and retain the original semantic assertions. Squirrel Girl's actual two-soldier test asserts first-player authority and both recipients remaining in play, deliberately selects the second soldier, then retains the original dealt-card, departure, villain-damage and Squirrel Girl-damage checks. The two direct-entry When Defeated fixtures now capture the incarnation their synthetic defeat facts require.

The coordinator completes the shared step-six window before recording the step-seven cohort; the combined step-seven window completes before any departure. Eligibility is re-read after ordered interrupts, while exact recipient incarnation guards prevent old opportunities or departures applying to re-entered copies. Offer handles distinguish repeated instances without selecting by object id or collapsing targets. The marker suppressing authored per-target scheduling belongs to the exact occurrence, and the narrow Cards integration preserves the per-target entry hook. No AbilityRunner or Agenda internals changed in this candidate.

## Independent measured evidence

Windows Release runs at the reviewed engine revision; logs are outside the repository under `%TEMP%`:

- `marvel-defeat-final-focused.log`: **40 cases, 39 pass, 1 fails**. The only failure is the first recipient-meaning probe. Existing barrier, continuation, Tough, actual Squirrel Girl and When Defeated controls pass.
- `marvel-defeat-final-full-content.log`: **5,560 cases, 5,559 pass, 1 fails**, in 3.73 minutes. All 5,556 candidate tests pass. The four initial independent probes contribute three passing continuation cases and the recipient-meaning failure. This run predates only the refinement of that new red probe into two theory cases; engine/candidate sources are identical.
- `marvel-defeat-final-new-focused.log`: **5 cases, 3 pass, 2 fail** after strengthening the red probe to compare all fields except `Id` and adding the same-title physical-recipient case. Both failures are the missing meaning contract.
- `marvel-defeat-final-wholegame.log`: **38/38 pass**: all 30 Core hero/scenario/mode matrix games and all eight Captain America seeded starter journeys. The latter compare every decision against a separately dealt replay, including state, prompt, events and RNG consumption. These are whole-game regressions, not genuine predecessor archive compatibility proofs.

The new passing probes establish that a nested unrelated would-defeat retains its own optional healing opportunity while both outer recipients remain in play; declining the shared optional opportunity invokes neither occurrence; and repeated query work plus a rejected handle leaves state, RNG state/consumption, pending frames, current occurrence and events unchanged. Selecting the second repeated occurrence, then retrying the remaining occurrence, produces exactly the same final observation as an unprobed control.

The author's full Rules, hooks and compiled-mutant observations remain separately attributed in `thor-e3-simultaneous-damage-review.md`; this independent packet does not claim to rerun them or create new mutation evidence.

## Compatibility and remaining scope

The actual supported Squirrel Girl case gains a player ordering decision, so the same recorded decisions may resolve differently. The current unchanged identity is `engine-replay-v26`. If v26 is unpublished, this correction can be consciously included in that pending boundary, but its documented scope and destination-specific frozen v19/v20/v21 routes must be revalidated against the integrated candidate. If v26 has shipped, a new replay boundary is required. Do not rewrite authentic historical facts to accommodate changed outcomes. The 38 replayed games here compare the candidate to itself and do not settle predecessor compatibility.

The offer-context repair needs its own engine/View/wire review and version decision, including restricted visibility and exact same-title physical recipients. This review supplies no native interaction or socket-contract certification. It does not repair the separate `SimultaneousAttackDamage` path, supply Lightning Strike's actual authored rows, or establish full Thor admission. The full Acceptance lane, full integration lane, genuine frozen upgrade routes, and final combined all-platform gates remain integration responsibilities.
