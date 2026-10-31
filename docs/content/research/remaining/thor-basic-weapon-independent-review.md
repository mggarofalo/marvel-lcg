# MARVEL-747 independent prerequisite review

Reviewed production candidate `7a2f7388`, independently checked out as
`review/thor-basic-weapon`. Verdict: accept the bounded E5 hero-basic-attack
prerequisite. No production fix is included in this review. This does not admit
Mean Swing, Teamwork or Thor, prove their actual ability rows, or establish native
interaction/product acceptance.

The generated 06015 text supplies the hero basic attack trigger, attached Weapon
exhaustion arrow cost and +3 ATK for this attack. RR1.8 distinguishes basic and
labeled attacks (`rr:attack-player-ability-type.1`, `.2`); costs belong to controlled
elements (`rr:cost.7`) and exhausted cards cannot exhaust again (`rr:exhausted.2`).
The implementation uses the existing chosen-cost preparation and incarnation-bound
payment. The physical host, controller, effective Upgrade kind, current trait and
readiness all constrain the payment. Compound attached-exhaustion sequences refuse
during lowering; unsupported positive adjusted event prices and supplied resource
payments refuse before generators/payment, including gratuitous payment at zero.
Action, window and prepared-window invocation paths all pass through that guard.

`BasicUseWeaponReviewTests` adds seven independent observations:

- A complete paid event-play path resolves a labeled attack, discards event and
  resource, returns to the root prompt, and leaves Mean Swing and Weapon untouched.
- A labeled attack nested inside the basic interrupt deals its own one damage;
  the outer basic attack retains exactly +3 and the following basic attack has no
  bonus. There is no unintended extra interrupt or unfinished agenda.
- Exhausting a selected Weapon after its offer rejects that stale payment without
  changing digest, RNG, pending work or occurrence bonus; another current copy
  pays on retry.
- Direct action, direct window and prepared-window paths reject gratuitous resource
  payment before source, resource or Weapon changes.
- Repeated eligibility/description queries retain a baseline captured before the
  first window query, including digest, RNG words/index, pending work and occurrence
  results. Their subsequent commitment matches an unprobed control's digest,
  bonus and serialized events.

The original `BasicUseWeaponTests` retain the wrong-host/controller, exhausted,
missing/non-Weapon, both-copy selection, invalid/foreign/duplicate target and
basic-thwart/ally/card-payload negatives. The full event-play and nested attack
tests above strengthen the original scheduled-payload negative.

## Performer departure boundary

`BasicUse` records actor incarnation, but neither admission nor execution nor
`BasicPowerResolution` reads it. Resolution validates target incarnation and reads
the current actor's ATK; it has no actor departure/reentry check. No supported legal
hero leave/reenter route was established during this review, so this is not asserted
as a reachable E5 defect. Do not generalize this acceptance to an incarnation-bound
selected ally or an arbitrary departed performer. E5b must enforce that boundary
and add a distinguishing legal/explicitly synthetic departure proof. An invented
identity leave/reentry state would not establish Thor gameplay failure.

## Replay 26 assessment

The new marker joins the ordinary attack occurrence; it leaves the first ordinary
condition unchanged. It adds no authored producer or public card offer, alters no
RNG/digest format and does not itself add a new scheduled frame. Existing accepted
Core optional attack-initiation observers concern enemy/villain attacks; they do
not become eligible during a hero basic attack. Core Webbed Up and RoRS
Photographic Reflexes retain their forced-interrupt behavior. Thus no additional
Core decision is expected from this packet alone. The new basic-attack interrupt
copy applies when a qualifying optional observer actually produces that window.

Replay 26 is unreleased. Keep the agreed Thor replay boundary and check the final
integrated revision, without rewriting frozen fixtures. Concrete expectations are:
saves under earlier replay identities are refused (MARVEL-896); `BasicAttackCostReplayTests`
reconstructs a suspended basic cost and the same paid/stunned outcome;
`NestedInterruptReplayTests` reconstructs the inner authority and future. Final
book integration additionally needs an actual 06015 pending interrupt saved and
reloaded, the same selected Weapon exhausted exactly once, +3 on the same basic
attack, and identical subsequent prompts/events/RNG/digest. This prerequisite has
no actual 06015 row, so that final journey remains with the book owner.

## Executed evidence

All logs are outside the repository under
`C:/Users/mggar/AppData/Local/Temp/` with prefix `thor-basic-review-`.

- `toolrestore.log`, `husky.log`: `dotnet tool restore`, `dotnet husky install`.
- `baseline.log`: 52 original basic/Weapon, additional attack, named-exhaust,
  minion-entry, variable-event and generic-resource cost cases passed.
- `combined.log`, `restored-tests.log`: 85 cases passed, adding the seven review
  cases plus cost-lowering and rule-citation checks. Filter:
  `BasicUseWeapon|AdditionalAttackCostTests|NamedExhaustCostTests|MinionEntryArrowCost|VariableEventPaymentTests|GenericResourceCostTests|AbilityCostLoweringTests|RuleCitationTests`.
- `mutant.log`: removing only the occurrence bonus from damage calculation compiled
  and failed the nested attack observation (expected 6, actual 3).
  `restored-build.log` is a successful non-incremental Release rebuild of the
  restored sources before the passing 85-case run.
- `replay.log`: 23 server cases passed using
  `BasicAttackCostReplayTests|NestedInterruptReplayTests`.

The candidate's separate compiled host-filter mutation remains its own evidence;
this review does not count it as a newly executed experiment. Full managed,
platform and final integrated replay/native gates remain the delivery lead's work.
