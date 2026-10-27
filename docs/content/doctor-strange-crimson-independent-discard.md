# Crimson independent self-discard prerequisite (MARVEL-814)

This engine prerequisite is based on `86a858c3`. It authors no production
ability row, changes no setup or public admission, and makes no pack-acceptance
claim. `CrimsonIndependentDiscardFixture` holds the exact proposed printed
09032 effect; its Rhino damage prohibition is explicitly synthetic.

The engine recognizes only a direct enemy choice whose body gives the chosen
enemy a status and then deals positive constant ordinary damage to it. An empty
sentence may coexist with an independently effective sentence. The complete
ability still needs a valid target and partial resolution; dependent predecessors
keep their admission gates. Selection, execution and costs use existing owners.
No card identity or fallback villain participates in the recognition.

Authority: current RR 1.8 `target.2`, `target.3.4`, `target.3.7`, `ability.4`,
and `then.2`; current-compatible rulings `8883bc6b0af1c9b8` and
`8c9c9d30f4c01189`. Ruling `b07d53e12625ab23` supplies Master of the Mystic
Arts discard/disclosure/recycle/return order. The earlier pre-1.5 Vapors and
Master answers do not govern these outcomes.

Verification on the implementation working tree:

- 16 Crimson cases pass, including both callers, protected/unprotected same-cost
  controls, last/nonlast Invocation lifecycle, status-only legal choice, invalid
  answer preservation, ineffective whole ability, dependent draw gate, Guard and
  hero Stun, and query/control continuation comparison including RNG and agenda.
- Relevant whole classes plus citation checks: 99 pass. Filter includes
  `CrimsonIndependentDiscardTests`, `SpellMasteryTests`,
  `SpellMasteryPaymentTests`, `DoctorStrangeD1CardTests`,
  `ActionAbilityActionsAndDependentEffects`, and `RuleCitationTests`.
- Compiled mutant changes the empty-choice predicate from `Count == 0` to
  `Count > 0`: four protected lifecycle cases fail at missing physical discard;
  12 pass. Sources were restored and `dotnet build` of the content test project
  with `--no-incremental` succeeded before the 99-test restored run.

Raw logs are outside the repository at `C:/Users/mggar/Source/crimson-empty-`:
`mutant.log`, `restored-build.log`, and `restored-relevant.log`.

Integration: the sibling Invocation-row packet adds
`BoardActualWithRunner(runner, face, master)`. Preserve this packet's compatible
optional `last = false` extension when reconciling that fixture. Independent
review and integrated ability-book/card acceptance remain separate work.

## Independent review of b0eb9178

Verdict: approved for the bounded MARVEL-814 engine prerequisite; no correctness
blocker found in the diff from `86a858c3` to `b0eb9178`. This review adds tests and
evidence only. Production ability rows, setup, public admission, UI and integrated
Doctor Strange pack acceptance remain outside this verdict.

Printed `09032` has an enemy sentence followed by a separate self-discard
sentence. Current RR 1.8 `target.2`, `target.3.4`, `target.3.6`, `target.3.7`,
`ability.4`, `choose-option.2` and `then.2` support the existing complete-ability
target/partial-resolution checks and the independent empty-sentence allowance.
The three rulings listed above remain compatible with those current clauses;
the pre-1.5 Invocation answers do not supersede them. The recognizer directly
matches enemy choice, chosen status, then positive constant nonattack damage
without ignore-Tough. It contains no card-id or fallback-villain selection.
Dependent predecessors and other compositions retain their admission paths.

Independent `CrimsonIndependentDiscardReviewTests` adds eight passing cases:

- Both callers explicitly choose the legal minion while the stunned, protected
  villain is excluded. Ordinary damage removes the minion's Tough, leaves its
  damage at zero, and retains its newly given Stun.
- Both callers reject a chosen enemy that becomes entirely ineffective after
  the prompt, preserving the prompt, physical Invocation, card digest, RNG and
  outstanding agenda work after the committed payment.
- All four caller/last-card combinations assert exact physical discard areas,
  owned deck/discard membership, next-card disclosure, complete five-card
  recycle order where applicable, and exact Master return after disclosure.
  Doctor Strange is seat 1; seat 0 has no Invocation deck.

Verification in `review/crimson-discard`:

- Unmodified candidate: all 99 original relevant cases pass. The same complete
  affected classes, citation checks and eight independent cases pass after
  restoration: **107 passed, zero failed**.
- The compiled `Count == 0` to `Count > 0` mutant causes the four protected
  lifecycle failures among the original 16 cases, plus four failures among the
  independent eight. Every failure reaches the missing physical-discard
  assertion. The other 16 cases pass. The source was restored and a forced
  `--no-incremental` content-project build passed with zero warnings/errors
  before the restored 107-case run.
- Pre-commit group passes: 66 architecture-policy tests, complexity and
  file-length checks. No fixture, engine, dataset or admission changes are
  included in the independent review packet.

Raw logs are outside the repository under
`C:/Users/mggar/Source/crimson-review-`: `source.log`, `independent.log`,
`mutant-source.log`, `mutant-independent.log`, `restored-build.log`,
`restored-relevant.log` and `hooks.log`. The relevant test filter covers complete
`CrimsonIndependentDiscardTests`, `CrimsonIndependentDiscardReviewTests`,
`SpellMasteryTests`, `SpellMasteryPaymentTests`, `DoctorStrangeD1CardTests`,
`ActionAbilityActionsAndDependentEffects` and `RuleCitationTests` classes.

The optional `BoardActualWithRunner(..., last = false)` extension remains
untouched and must survive integration with the sibling actual-row fixture.
