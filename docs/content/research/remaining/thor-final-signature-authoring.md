# Thor final signature rows — MARVEL-563

Authorship base: `8bcf3050`. Required reviewed living-target changes are cherry-picked as `764db50c` (original `9ed19527`) and `ad956ce1` (original `a7051ba8`). The authorship revision is the commit containing this record. This packet appends exactly `06006` Lightning Strike and `06014` Get Over Here!: 608 to 610 authored faces, including 32 to 34 Thor faces. It changes no engine, setup, public catalog or admission code. Loki and complete pack admission remain separate work.

## Authority and interpretation

The printed source is the joined `datasets/cards/cards.json`, faces `06006` and `06014`. The vendored FAQ in `datasets/marvelcdb-faq/faq.json` supplies the non-Attack Lightning Strike erratum and the Get Over Here! restriction against re-engaging a minion already engaged with the same player. No external source candidate supplies these rows.

Lightning Strike pays its printed 1 plus separately allocated positive X energy. It deals direct simultaneous damage to the current villain and the resolving seat's minions. Aerial bypasses Tough prevention without discarding Tough. It has no attack label or attack instruction. Get Over Here! is a labelled attack against a minion; after its damage, Aerial engages only the retained, still-in-play incarnation. A lethal attack cannot resurrect the minion. The selector, payment, retained binding and in-play spellings are engine choices implementing those printed instructions.

## Actual-book behavioral evidence

`ThorLightningStrikeCardTests` and `ThorGetOverHereCardTests` load the complete authored book through `AuthoredCards.Runner()` on nonpublic boards. They contain no replacement ability book.

- Lightning Strike: printed `1+X` and two payment components; energy eligibility with same-price mental-resource negative control; hero versus alter-ego with otherwise payable control; Aerial/non-Aerial Tough behavior; stunned hero remains stunned; no Jarnbjorn attack response; own versus foreign minions; second-seat execution and two lethal minions; Ultron III remains protected during the damage that defeats his last drone, followed by an otherwise equivalent Lightning Strike that damages the exposed villain.
- Get Over Here!: lethal/nonlethal damage, Tough and ordinary prevention, no-Aerial control, already-engaged control without an extra Thor draw, exact chosen minion versus decoy, retained incarnation on transfer, stun cancellation of damage and follow-up, and Quickstrike resolving before Thor's draw without an entry/reveal occurrence.
- The explicit authored-face inventory admits exactly the two added faces. Full card-data/lowering and rule-citation checks are included. Existing living-target suspension/incarnation tests and simultaneous-defeat continuation/review tests supplement actual-row evidence for those general engine contracts.

## Validation and mutation

All logs and TRX files are outside the repository under `C:/Users/mggar/Source/thor-final-*`.

1. Baseline actual-card plus Cards namespace and citation tests: 184 passed.
2. Mutating only Lightning Strike's recipients to `enemies`: 3 failures, including foreign-minion damage from the second-seat case (`thor-final-mutant-selector.log`).
3. Removing only Get Over Here!'s `isInPlay` guard: its lethal case fails because the defeated minion returns (`thor-final-mutant-guard.log`).
4. Removing only Lightning Strike's `ignoresTough`: Aerial damage fails, expected 2 versus actual 0 (`thor-final-mutant-tough.log`). Each mutant compiled and exited 1 for a distinguishing assertion.
5. Restored exact ability bytes, then `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental`: zero warnings/errors. Restored focused, card-data, citations and related prerequisite tests: 205 passed (`thor-final-restored.log`).
6. Final additional printed-price and typed-energy controls: 207 passed (`thor-final-final.log`, `thor-final-test-results/final.trx`). Filter: `ThorGetOverHereCardTests|ThorLightningStrikeCardTests|Marvel.Content.Tests.Cards|RuleCitationTests|GetOverHereLivingTargetTests|SimultaneousDefeat|LightningPaymentCompositionTests`, each as a `FullyQualifiedName~` disjunction.
7. `dotnet run --project tools/Marvel.Cards.Extract -c Release -- check`: cards and search match the generator (`thor-final-cards-check.log`).

These are focused engine/content results. This packet does not claim a public Thor journey, native interaction acceptance, a final integrated platform sweep or complete pack acceptance. The existing simultaneous-defeat tests supply the distinguishing shared-stage and suspended causal-context engine evidence; the actual-row Ultron/minion cases supplement that evidence rather than replacing it. Integration and independent review remain with the lead.

## Independent actual-row review

Reviewed source: `9dee18e3b3d7750df0d2a53fef7e3d94ca61b312`, in isolated branch `review/thor-final-signature`. Review additions are the commit containing this section. This review uses the pack-delivery implementation/proof guidance and adds only tests and evidence; it does not change the ability book, engine, setup or public admission.

Authority inspected before expectations: generated faces `06006` and `06014`; Rules Reference v1.8 (vendored 2026-08-24), especially `rr:cost.5.1` and `rr:initiating-abilities.step.5`; MarvelCDB FAQ snapshot harvested 2026-08-22 (06006 erratum updated 2020-08-28, 06014 engagement ruling updated 2020-08-29); Hall of Heroes snapshot harvested 2026-08-31, including the matching non-Attack and same-player engagement clarifications. Printed 06006 requires separate generic 1 and X energy; its damage ignores Tough when Aerial, without an attack label. Printed 06014 deals 1 attack damage and conditionally engages that enemy. The living-target guard matches in-play/incarnation rules. The review does not assert or modify overkill rules.

The source tests exercise the actual authored rows and meaningful controls: Ultron III protected during simultaneous last-drone defeat; own versus foreign minions; lethal targets; Tough bypass and retention; stunned non-Attack resolution; no Jarnbjorn response; retained target identity; no same-player reengagement; Quickstrike before Thor's draw. Fixture inspection confirms Energy `01088` generates `YY`, Genius `01089` generates `BB`, Hydra Mercenary `01101` has 3 HP, and Vulture `01167` has Quickstrike and 3 ATK. The source record's non-energy negative control is mental, not physical; the eligibility assertion remains valid.

`ThorFinalSignatureIndependentReviewTests` adds three actual-book cases on nonpublic boards:

- From seat 1, an explicit allocation pays the generic component with energy and incorrectly assigns mental to the energy-only component. Rejection preserves the digest, RNG words/index/consumption, pending work and registered effects. A corrected allocation with the same action and cards succeeds and discards all payment cards to seat 1. The baseline precedes the first Actions/Describe query.
- Get Over Here! from seat 1 checks Aerial on that acting identity. Aerial only on seat 0 does not transfer; Aerial only on seat 1 transfers the chosen surviving minion to seat 1, preserves its incarnation and leaves the decoy untouched. Thor at seat 0 receives no draw from the other player's engagement.

Evidence outside the repository:

1. Initial new checks: 3 passed (`C:/Users/mggar/Source/thor-final-independent-initial.log`, `thor-final-independent-results/independent-initial.trx`).
2. Valid actual-row mutant: changed only Get Over Here!'s Aerial selector from `you` to `this`. The book successfully lowered and executed; one case passed, and the acting-seat Aerial case failed specifically at target engagement (expected p1, actual p0), exit 1 (`thor-final-independent-mutant-aerial.log`).
3. Nonqualifying mutation attempt: replacing `spendEnergyX: Y` with `spendEnergyX: B` is rejected by the DSL's fixed-word validation, before behavior executes (`thor-final-independent-mutant-energy.log`). This is not counted as a distinguishing behavioral mutant.
4. Restored exact ability bytes, then rebuilt Content.Tests Release with `--no-incremental`: zero errors (`thor-final-independent-restored-build.log`). Restored new tests plus actual-card, Cards namespace, citations, living-target, simultaneous-defeat and payment-composition tests: **210 passed**, zero failed/skipped (`thor-final-independent-restored.log`, `thor-final-independent-results/independent-restored.trx`). Filter is the source final filter plus `FullyQualifiedName~ThorFinalSignatureIndependentReviewTests`.
5. Generated card check passed (`thor-final-independent-card-data.log`). The ability book has no review diff.

Verdict: no behavioral finding in the two authored rows; accepted for lead integration within this nonpublic content packet. This verdict does not establish complete Thor admission, a public Thor session, native UI acceptance or final cross-platform certification. Those broader integration gates remain outside this review.
