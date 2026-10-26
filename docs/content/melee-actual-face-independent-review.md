# Melee actual-row independent review

Verdict: **accept the bounded actual Melee row and full-book test migration**
at `afbd25c23137699badc94c46a86f9f973a7105ed`, reviewed on candidate
`1bd776a1`. This accepts one authored face, not public Ms. Marvel admission,
native interaction, hosted persistence, or pack readiness. The staged-attack
engine prerequisite has its own independent acceptance and was not reopened.

The appended 05030 row matches generated `datasets/cards/cards.json`: cost 3,
Hero Action (attack), three damage to an enemy, then three to another enemy.
Reviewed the raw Melee FAQ and rulings for distinct enemies, stun cancelling the
whole attack, Guard removal, villain-stage identity, final responses and
Embiggen. The ability provenance cites Guard ruling `e0333bc6316bd7f0` correctly.

The migration replaces synthetic book appends with `AuthoredCards.Runner()`
without weakening the existing assertions for fresh second selection, Guard,
villain-stage exclusion, both Embiggen hits and the next paid event. Synthetic
alternative bodies and observers remain isolated. Inventory adds only 05030;
571 authored faces and 29 Ms. Marvel faces do not imply complete pack admission.

Added independent actual-book cases establish the exact cost, hero-form-only
availability with an affordable same-cost Helicarrier control, completion with
only one enemy, and payment plus cancellation of the entire attack by stun.
The initial stun fixture incorrectly skipped the initial target choice; matching
the existing engine control by selecting that enemy resolves cancellation and
passes. No production defect was established. These cases complement the
implementer's separately reported first-damage data mutant; this review did not
run new mutants or repeat the accepted engine mechanism review.

Independent Release command:
`dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release
--filter 'FullyQualifiedName~MeleeActualRowReviewTests|FullyQualifiedName~StagedAttack|FullyQualifiedName~AbilityData'`.
Result: **64 passed**, including four new actual-book cases. Log:
`C:/Users/mggar/Source/.agent-evidence/melee-actual-independent.log`.
Normal pre-commit output is preserved beside it as
`melee-actual-independent-commit.log`. No broad suite or platform certification
was performed by this focused review.
