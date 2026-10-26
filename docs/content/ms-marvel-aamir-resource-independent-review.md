# Aamir/resource actual-row independent review

Reviewed candidate `ddc39027` against base `a3862b32`, 2026-10-26. Scope is
the seven actual authored rows 05006, 05016, 05019, 05020, 05021, 05022 and
05024. Verdict: accepted for this bounded nonpublic card-execution packet.
This does not establish complete Ms. Marvel content or public admission.

The joined printed rows in `datasets/cards/cards.json` supply the card text,
forms, costs, resources and Uses. The vendored MarvelCDB FAQ queried all seven
faces and has no entries for them. Ruling `ruling:966be0c679fc50e7` distinguishes
spending hand cards from resource abilities; it does not change this text.
Reviewed Rules Reference ownership/control, you/your, Uses and resource-ability
entries alongside the actual authoring. Bottom-placement primitive review is a
separate accepted prerequisite; this review checks its actual Aamir caller.

The diff adds exactly seven rows, increasing 551 to 558 total and Ms. Marvel
from nine to sixteen authored faces. Every existing row remains semantically
identical, and the original ability-book byte prefix through the old final row
is identical. The updated exact-face gate and counter inventory include these
rows while retaining genuinely unauthored negative controls. Mansion and Power
of Protection preserve their existing Core reprint semantics. Enhanced Reflexes
matches its energy icon and three energy counters, rather than copying the mental
output of Enhanced Awareness.

Author tests check exact Aamir discard identity and bottom order, draw after
placement with empty/one/multiple-card decks and normal exhaustion reset,
foreign-choice rejection, no empty-discard offer, form/exhaustion restrictions,
actual card-class doubling and payment, Mansion recipients, and Reflexes entry,
exhaustion, counter expenditure, final-use disposal and generator eligibility.

Retained independent tests add five cases: Aamir with different physical owner
and controller; both Mansion recipients from a non-first controller; Protection
payment context for a card versus an ability cost on a Protection card; and a
non-first Reflexes controller spending its last counter, completing payment and
discarding to the physical owner's pile. These are prepared execution scenes,
not full scenario journeys or product acceptance.

Three valid ability-row mutants execute under the independent tests: Aamir draws
two (exact controller hand count fails); Mansion draws for its controller
(selected other-player draw fails); Reflexes loses Uses (owner discard fails).
An initial Aamir mutant survived the first independent test because that test
only asserted the drawn physical card. The retained test adds the missing exact
hand-count observation and kills the same mutant. No lowering rejection earns
behavioral mutation credit. Mutations are restored byte-for-byte.

Focused command selects `MsMarvelResourceIndependentTests`, the three author
test classes, `AbilityData`, `AbilityProgramTests` and `RuleCitationTests` in
`Marvel.Content.Tests`, Release. The restored selection passes 68 cases.
Normal pre-commit hooks remain enabled. Final run and hook evidence are retained
outside the repository under `%LOCALAPPDATA%/Temp/ms-aamir-independent-*`:
`restored-build.log`, `restored.log`, `commit.log`, and three `mutant-*.log` files.
No server, native or full-suite acceptance is claimed.
