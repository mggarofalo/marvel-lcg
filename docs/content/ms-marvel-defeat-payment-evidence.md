# Defeat payment acceptance boundary (MARVEL-731)

Candidate is the commit containing this record, based on `32ad2490`. This fixes
engine initiation timing only; the ability book and public admission are unchanged.
The original independent finding remains in
[the ally/condition review](ms-marvel-ally-condition-independent-review.md).

`IWindowAbilities.PrepareWindow` binds the selected Cards ability, recipient and
complete event/arrow payment before any occurrence trigger or invocation registry
entry. Ordinary answers, defeat choices and automatic mandatory windows share
this boundary. Both authored decorators forward preparation. The default port
implementation is explicitly limited to resolvers without answer-dependent
validation or costs; it is not a transaction or an atomicity promise for arbitrary
custom resolver bodies. The prepared body is consumed immediately by the engine.

After preparation, trigger commitment precedes cost execution so a suspending
resource generator cannot offer the accepted parent ability again. The same
prepared payment plan and resolution state survive nested payment. No exception
is swallowed and accepted costs/effects are not rolled back.

## Authority and distinguishing observations

`rr:initiating-abilities.step.5` requires paying costs; `rr:triggering-condition.1`
limits each interrupt/response to one trigger per occurrence. Rejecting the
submitted payment is not initiation. Validation-before-trigger is the engine's
commitment policy implementing those boundaries.

The retained actual Red Dagger test uses controller seat 1, physical owner seat 0,
two printed wild cards, invalid energy/energy followed by legal energy/mental.
It now completes the retry, deals 2 damage, returns the physical ally to its
owner, and discards the controller's payment. New ordinary response tests use
synthetic exhaust plus distinct-resource costs to distinguish validation from
partial payment. Invalid equal wild declarations and an opponent's hand card
preserve digest, RNG, pending work, resolution entries, readiness and explicit
trigger eligibility. The original prompt supports either a legal retry that
executes once or a decline with no payment.

## Evidence

Release focused profile: **48 passed, 0 failed**. Includes actual Red Dagger and
other condition checks, ordinary rejection/retry/decline, nested resource-cost
suspension, event replay payment, window intent, self-defeat, would-be-defeated,
trigger bindings, payment-dependent preflight, forced completion, When Defeated
and step-seven guards. Logs outside the repository:

- `C:/Users/mggar/Source/marvel731-restored-build.log`: forced non-incremental
  restored Release build, 0 warnings/errors.
- `C:/Users/mggar/Source/marvel731-restored-focused.log`: restored 48-case profile.
- `C:/Users/mggar/Source/marvel731-mutant.log`: compiled mutant moving acceptance
  before preparation; all 3 selected regressions fail. Actual Red Dagger fails
  with the original consumed-opportunity error; both ordinary cases fail their
  explicit trigger-eligibility preservation assertion. Restored sources pass.

No broad suite, native UI, complete-pack admission or public save/restart claim.
Independent re-review and card acceptance remain with the lead integration step.
