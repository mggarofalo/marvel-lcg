# Lightning recipient selector independent review

MARVEL-754. Reviewed the staged selector candidate based on `fd62beea` on
`feat/thor-lightning-selection`, including its projected-recipient extraction.
This is a synthetic engine primitive review; Lightning Strike is not admitted.

## Semantic review

Authority: generated card `06006` says "the villain and each minion engaged with
you"; `rr:engage.1` keeps a minion engaged until defeat, removal or another
engagement; the active-villain interpretation uses the existing encounter routing.
The selector spelling and villain-first order are engine choices.

Reviewed lowering, live selection, projected areas, repeated candidate tracing,
resolving-seat binding, effective kind, stage promotion, simultaneous cohort
delivery and ordering. Existing controls cover one, two and four players including
non-first seats, effective minions, another player's minions, departure, changed
engagement, ordinary villain promotion and multi-villain active-counter changes.
Projected engagement chronology remains explicitly unsupported in repeated traces
unless the selection supplies object order; no chronology is invented.

Found and fixed an ordering inconsistency: creating the engaged area before the
villain area made repeated traces enumerate minions first, unlike live and area
projection queries. The new adversarial test also reverses minion object order
within the physical area. Candidate tracing now places villains first for this
selector while preserving minion area order and explicit object-order wrappers.

Added a real action comparison against an unprobed control. Before querying, the
test captures the card digest, complete RNG state, words consumed and pending
Agenda work. Repeated live, projected and trace queries preserve those baselines;
subsequent damage and draw execution agrees with the control in state, RNG,
pending work and next offered actions.

Verdict: selector scope accepted after the ordering correction. This does not
accept Lightning Strike's payment or conditional tough composition, the separate
MARVEL-755 findings, public Thor admission, or native presentation.

## Cohesive refactoring

Separately resolved three complexity-hook findings without baseline changes:
projected relation dispatch is separate from wrapper selection, repeated relation
lookup is separate from card membership, and query area dependencies are separate
from recursive wrapper traversal. These preserve the established selector cases.

## Verification

Logs are outside the worktree under `C:/Users/mggar/Source/`:

- `lightning-selector-order-before.log`: new ordering test compiled and failed at
  the repeated-trace assertion before the fix.
- `lightning-selector-order-mutant.log`: compiled mutation changed only the new
  villain-priority sort from descending to ascending; the ordering test failed.
- `lightning-selector-restored-build.log`: restored sources rebuilt with
  `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental`;
  zero warnings and errors.
- `lightning-selector-restored-tests.log`: restored selector and simultaneous
  damage selection, 17 passed before adding the execution-purity comparison.
- `lightning-selector-review-final-focused.log`: final
  `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~LightningRecipientSelectionTests|FullyQualifiedName~SimultaneousDamage'`;
  18 passed, zero failed or skipped.

The lead owns final integrated broader lanes and Plane reconciliation.

## Encounter-topology ordering closeout

The previous review left non-null projected `EncounterOrder` unverified. Against
selector revision `b142d156`, that concern reproduced as a concrete defect: actual
encounter topology with an engaged area created before the villain area returned
minions before the current villain. The same mismatch occurs after projected
stage promotion. The ordinary-world ordering correction above did not cover this
final chronology pass.

`LightningTopologyOrderTests` constructs validated two-group encounter topology,
reverses the minions' object order within their physical area, and uses the real
power-state factory and encounter-stage advancement. Both repeated matching and
power selection now apply this query's villain-first choice after encounter
chronology. Wrapper filters and the selected `FirstAvailable` branch retain that
choice; explicit object ordering still wins. `Last` resolves against the same
ordered cohort. Controls retain ordinary `Enemies` chronology, including when it
is the first nonempty fallback before the new query.

Evidence outside the worktree under `C:/Users/mggar/Source/`:

- `lightning-topology-before.log`: compiled baseline, two ordering failures at
  the repeated-matching assertion, with and without projected promotion.
- `lightning-topology-mutant.log`: compiled mutant disables only the new query's
  villain-first policy; the same two cases fail with minions first.
- `lightning-topology-restored-build.log`: restored sources rebuilt with
  `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental`;
  zero warnings and errors.
- `lightning-topology-restored-tests.log`: restored selector, simultaneous-damage,
  topology-order, Wrecking power/repeated/singular/ranked/other-scheme projections;
  55 passed, zero failed or skipped.

The topology finding is corrected within this selector primitive's scope. This
adds no Lightning Strike ability row or public Thor admission. Final integrated
broader lanes and Plane reconciliation remain with the lead.
