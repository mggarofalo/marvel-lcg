# Power Drain authored-content evidence

This packet implements the five Power Drain research faces and its five-card modular recipe. It does not admit Green Goblin or Power Drain into public setup discovery. The setup authority remains `outside-core`; no executable Core transcript coverage is claimed.

Verified candidate: base `8124b333` plus this packet's authored data, tests and catalog changes, on Windows on 2026-10-25. Shared prerequisites are integrated in that base: resource-icon discards, physical encounter discard receipts/recruitment, and future-receipt indirect-damage admission (MARVEL-717). No engine implementation is changed by this packet.

| Face | Physical copies | Authored behavior and distinguishing evidence |
|---|---:|---|
| 02041 Power Drain | 1 | Two-card numeric boost receipt; both players choose sufficient hand resources while the defeated scheme remains in play; discard resumes afterward. |
| 02042 Electro | 1 | Full completed attack against player 1, with an ally defending or Tough preventing attack damage; the attacked player takes three indirect damage and player 0 takes none. Boost discards three. |
| 02043 Electromagnetic Pulse | 1 | Exactly seven cards milled; recruit the physical Electro from that receipt for player 1 even after reset; an Electro elsewhere does not suppress surge. |
| 02044 Lightning Bolt | 1 | Two-card numeric boost receipt through deck reset, including zero icons and star exclusion; player 1 receives damage. Boost discards three. |
| 02045 Shock Therapy | 1 | One encounter card per player; heal only numeric boost icons, including an undamaged villain and star-only cards. Boost discards three. |

Printed text and stats were inspected in `datasets/cards/cards.json`. The pinned `datasets/marvelsdb/pack/gob_encounter.json` records quantity one for each of the five faces. Electro's vendored MarvelCDB FAQ 02042 explicitly retains the response after ally defense. Hall of Heroes `ruling:a9007ff3a4727a80` agrees with `rr:when-defeated-abilities.2.1`: Power Drain resolves before leaving play. No card FAQ entry exists for the other four faces. Numeric boost icons exclude stars under `rr:boost-boost-icon.1`; the mill stops on encounter-deck exhaustion under `rr:encounter-deck.2`.

## Verification

Raw logs and TRX files are outside the repository at `C:/Users/mggar/Source/power-drain-evidence/`.

- `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~PowerDrain'`: 24 passed (`characteristic.log`, `power-drain-characteristic.trx`).
- Restored final content/ability/setup/boost/citation filter: 79 passed (`restored.log`, `power-drain-restored.trx`). Filter: `FullyQualifiedName~PowerDrain|FullyQualifiedName~AbilityData|FullyQualifiedName~SetupDataset|FullyQualifiedName~BoostAbilityTests|FullyQualifiedName~RuleCitationTests`.
- Rules authority/setup enumeration/citation filter: 27 passed (`authority.log`, `power-drain-authority.trx`). Filter: `FullyQualifiedName~BehaviorAuthorityTests|FullyQualifiedName~ExpansionSetupAuthorityTests|FullyQualifiedName~RuleCitationTests`.
- `dotnet run --project tools/Marvel.Behavior.Index -c Release --no-restore -- check`: catalog and generated Core card-face specification current (`catalog-check.log`).

Actual authored-row mutations were applied one at a time, exercised through the real authored book, and restored byte-for-byte in a `finally` block. Each failed for the intended behavior, and the restored 79-test filter passed:

| Mutation | Distinguishing failure | Raw log |
|---|---|---|
| Pulse mill seven to six | Two cards remain instead of one in the non-exhausted case; 1 of 2 cases failed. | `pulse-six-card-mill.log` |
| Electro numeric-boost receipt to physical-card count | Both completed-attack cases deal one damage instead of three; 2 of 2 failed. | `electro-card-count-receipt.log` |
| Electro boost mill three to two | Two deck cards remain instead of one; the Electro case failed among four boost faces. | `boost-two-card-mill.log` |

These are focused engine/content checks on synthetic boards using the actual rows. Public factory/session, save/restart, complete-game, native UI and second-platform evidence remain wave integration work. This packet does not establish those outcomes or whole-pack acceptance.
