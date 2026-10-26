# Conditional status attack payment admission (MARVEL-729)

Engine packet based on `fdaf4a9e`; no production ability rows or public product admission changes.

Authority: joined `datasets/cards/cards.json` faces `05015` (Tackle) and `05031`
(Concussive Blow), `rr:target.2`, `rr:target.3.4`, and vendored ruling
`ruling:218bb67093e9682e` in `datasets/rulings/rulings.json`. The ruling permits
Tackle against a Stalwart/unstunnable enemy. The printed physical-resource damage
sentence can affect an enemy even when the preceding status sentence cannot.

The action offer checks one selected event body against feasible declared resource
receipts instead of assuming its empty prepayment receipt selects the final branch.
A known receipt fixes resource-only predicates while other mutation and binding
preflight guards remain active. The exact declared payment is checked again before
any cost commits. This packet covers events without arrow costs; existing admission
for other costs is retained. Window execution uses the same final-payment check;
payment-dependent window offer expansion is outside this action packet.

`ConditionalStatusPaymentAdmissionTests` uses test-local rows matching both printed
cards, in nested conditional-damage and root payment-branch forms. It checks physical
payment against an already-statused enemy; nonphysical fresh-status controls;
nonphysical no-effect refusal; unpayable cost refusal; and mutation-free repeated
queries plus subsequent execution against an unprobed control. Digest, RNG words
and index, pending work, final damage/status, event cleanup and resource cleanup
are observed. Public content remains unchanged.

Evidence outside the repository under `C:/Users/mggar/Source/`:

- `status-admission-guards.log`: 101 focused and retained payment/player guards passed.
- `status-admission-restored.log`: 103 passed after adding shortfall controls and
  nonincremental restored build; `status-admission-restored-build.log`: zero warnings/errors.
- `status-admission-final-focused.log`: final 16 new cases passed, including control comparison.
- `status-admission-mutant-offer.log`: compiled mutant restoring the old offer entry
  failed all four already-statused physical-payment cases. Restored source was rebuilt
  nonincrementally before the restored run.

Separate engine review and integration gates remain with the lead. No broad or native
suite was run for this bounded packet.

Independent review accepted `430a85f3`: 37 focused cases cover rejected/retried
payments, unselected and foreign sources, Stalwart and query purity. Removing
the final receipt guard killed four intended controls; a forced restored build
passed. The nine additional controls are retained in
`ConditionalStatusPaymentReviewTests`. External evidence is
`C:/Users/mggar/Source/status-review-conditional-{baseline,mutant-finalguard,restored,restored-build}.log`.
Integration with prepared window payment preserves receipt validation before
the payment plan is returned; 32 combined payment/window controls passed at
`4e9a46c2` (`fifth-window-payment-integrated.log`).
