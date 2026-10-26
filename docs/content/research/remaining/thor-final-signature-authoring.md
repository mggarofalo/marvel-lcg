# Thor final signature rows — MARVEL-563

Authorship base: `8bcf3050`. Required reviewed living-target changes are cherry-picked as `764db50c` (original `9ed19527`) and `ad956ce1` (original `a7051ba8`). The authorship revision is the commit containing this record. This packet appends exactly `06006` Lightning Strike and `06014` Get Over Here!: 608 to 610 authored faces, including 32 to 34 Thor faces. It changes no engine, setup, public catalog or admission code. Loki and complete pack admission remain separate work.

## Authority and interpretation

The printed source is the joined `datasets/cards/cards.json`, faces `06006` and `06014`. The vendored FAQ in `datasets/marvelcdb-faq/faq.json` supplies the non-Attack Lightning Strike erratum and the Get Over Here! restriction against re-engaging a minion already engaged with the same player. No external source candidate supplies these rows.

Lightning Strike pays its printed 1 plus separately allocated positive X energy. It deals direct simultaneous damage to the current villain and the resolving seat's minions. Aerial bypasses Tough prevention without discarding Tough. It has no attack label or attack instruction. Get Over Here! is a labelled attack against a minion; after its damage, Aerial engages only the retained, still-in-play incarnation. A lethal attack cannot resurrect the minion. The selector, payment, retained binding and in-play spellings are engine choices implementing those printed instructions.

## Actual-book behavioral evidence

`ThorLightningStrikeCardTests` and `ThorGetOverHereCardTests` load the complete authored book through `AuthoredCards.Runner()` on nonpublic boards. They contain no replacement ability book.

- Lightning Strike: printed `1+X` and two payment components; energy eligibility with same-price physical-resource negative control; hero versus alter-ego with otherwise payable control; Aerial/non-Aerial Tough behavior; stunned hero remains stunned; no Jarnbjorn attack response; own versus foreign minions; second-seat execution and two lethal minions; Ultron III remains protected during the damage that defeats his last drone, followed by an otherwise equivalent Lightning Strike that damages the exposed villain.
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
