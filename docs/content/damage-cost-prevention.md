# Damage-cost prevention prerequisite (MARVEL-788)

Candidate base: `59973d61`; branch `fix/damage-cost-prevention`.

Current RR 1.8 `rr:initiating-abilities.1` permits interrupts during cost
payment. `rr:cost.11` considers dealing damage paid even after prevention;
`.12` requires all taking damage actually taken. `rr:cost.5` requires
simultaneous costs, and `rr:initiating-abilities.step.5` aborts without paying
other costs when payment fails. Vendored FAQ `01027` (updated 2020-08-28)
specifically confirms that preventing Focused Rage's damage with Energy Barrier
does not pay for its draw. That answer agrees with current `.12`.

Damage-cost placement retains the existing direct-damage prevention receipt,
including its exact recipient and nested interrupt work. A prevention shortfall
completes payment as unpaid, settles actual damage and suppresses the effect.
Both immediate action/window initiation and suspended resumption honor that
result. Physical/resource sibling costs follow successful taking damage;
nested cost sequences are flattened before that ordering. Defeat still waits
until successful sibling payments finish. No dataset admission, digest field,
RNG contract, protocol or save field changes.

Focused evidence on this candidate, Windows Release:

- 60 content tests pass: damage costs, actual Focused Rage/Energy Barrier,
  partial/full prevention, taking/dealing, nested costs, replacement choices,
  defeat continuations, minion-entry costs and ally-entry payments.
- Restoring the base damage-cost placement as a compiled mutant causes both
  actual Focused Rage tests to fail because Energy Barrier is absent. Restored
  sources receive a non-incremental build before the same 60 tests pass.
- Three damage-cost receipt ownership/settlement tests pass.

Logs are outside the repository at
`C:/Users/mggar/AppData/Local/Temp/damage-cost-{focused,mutant,restored-build,restored,receipts}.log`.
The mutant log includes a successful build and two intended assertion failures.
Broader managed/platform/native lanes and independent review remain lead-owned.

Review concern: multiple taking-damage components can place an earlier damage
instance before prevention makes a later component unpaid. Single partially
prevented damage likewise retains actual damage taken; the engine has no
rollback transaction spanning nested interrupt effects. This packet preserves
existing combined-damage happy paths and does not claim that broader atomicity
question resolved. Event cards with taking-damage costs retain their existing
explicit unsupported-payment refusal. No printed admitted card requiring
multiple taking-damage components was identified in this packet.

Independent review of `845668e3`:

- Current RR `.5`, `.10`, `.11`, `.12`, initiating-abilities step 5 and
  clause 1 agree with the Focused Rage FAQ. Taking damage remains unpaid
  after any prevention; dealing damage remains paid after prevention.
- Reviewed interpretation: an attempted damage placement and its resolved
  interrupts are real game operations. They are distinct from considering
  any component of an aborted combined cost paid. The cited clauses do not
  prescribe rewinding resolved interrupt effects or restoring actual damage
  already taken during that attempt. No rollback requirement is inferred.
- Six synthetic compound-cost cases pass for flat and nested sequences:
  decline both interrupts, prevent the first damage, or prevent the second.
  Both successful components pay the resource/exhaustion siblings and draw
  once; either failure preserves those siblings and suppresses the draw.
  Prevention of the second component retains the first actual damage.
- Three printed-card server cases pass through the existing nonpublic
  durable-factory injection seam: decline Energy Barrier, reflect to the
  villain, or reflect to a minion. This is composition evidence, not public
  Ms. Marvel admission. Strict stored-session JSON reconstructs into fresh
  hosts/stores at the cost interrupt, nested target choice, and completed
  unit. Prompts, events, card digests, every MT19937 word, index and consumed
  count match uninterrupted controls. Invalid, stale and repeated submissions
  preserve the current prompt, save and state, without duplicate payment,
  draw or reflected damage.

Independent Windows Release logs:
`C:/Users/mggar/AppData/Local/Temp/damage-cost-independent-restart.log` (3/3),
`C:/Users/mggar/AppData/Local/Temp/damage-cost-independent-compound.log` (6/6).
No blocking finding was identified in this reviewed scope. Broader integrated
revision and cross-platform evidence remain lead-owned.
