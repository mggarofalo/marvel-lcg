# Ant-Man public journey evidence

The candidate was tested on production source at `08ff9b12`, equivalent to the reviewed admission integrated as `54940952`. The separate [public admission review](ant-man-public-admission.md) establishes the starter recipe and both offered identity destinations.

The restart test opens the published starter against Rhino with seed 851, keeps the dealt hand, changes Scott to Giant and suspends at the optional response. It copies the strict save and authority metadata into a fresh host, rejects stale and illegal choices, resolves the printed one damage to Rhino on both paths, and compares prompt, events, digest, RNG state and pending work. Authorized undo returns Scott to alter-ego; redo restores the completed response, its journal events and state. A subsequent basic attack also matches the uninterrupted path. This is an unarranged public setup, not a prepared board.

The complete-game matrix follows the existing Ms. Marvel public starter policy and scenario/seed choices. It checks legal starter membership, each player's actions, payment and card play, a terminal outcome and the exact decisions, events, state and RNG of a same-seed second deal. The results are policy defeats and do not establish victory coverage:

| Scenario | Players | Seed | Outcome | Round | Decisions |
|---|---:|---:|---|---:|---:|
| Rhino | 1 | 1 | VillainWins | 4 | 25 |
| Klaw | 1 | 2 | PlayersLose | 5 | 46 |
| Ultron | 1 | 3 | PlayersLose | 4 | 43 |
| Crossbones | 1 | 4 | PlayersLose | 7 | 45 |
| Absorbing Man | 1 | 5 | PlayersLose | 4 | 46 |
| Rhino | 2 | 6 | PlayersLose | 6 | 109 |
| Absorbing Man | 2 | 7 | PlayersLose | 7 | 101 |

The Release focused run passed all eight tests, using `dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release -m:1 --no-restore --filter 'FullyQualifiedName~AntManPublic'`. Detailed output is `C:/Users/mggar/Source/evidence/ant-man-public-final.log`. The initial run passed all seven games and failed the restart assertion because it retained a card reference from the host's earlier reconstructed game; the corrected test reads the current game. No runtime defect was found in this packet. These results do not establish native interaction, final integrated gates or whole-pack acceptance.

## Independent journey review

Reviewed `767691e36cb58501d49aea6c31dc3e69e42ec789` against the printed Giant response, `rr:ability.11` and the vendored Ant-Man rulesheet. The production factory deals the starter without arranging hidden state; the rulesheet permits Scott's direct change to Giant. The restart checks the reconstructed prompt and save, full RNG state, digest, pending work and events against the uninterrupted path, then exercises authorized undo/redo and a subsequent attack. The seven games establish terminal outcomes and same-seed replay for the stated policy, not victory coverage. No production defect was found in this bounded review.

The review strengthened the rejection control to require `stale_decision` and `invalid_decision` respectively, rather than accepting any error. The Release focused restart case passed (one test) with `dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release -m:1 --filter 'FullyQualifiedName~AntManPublicRestartTests'`; output is `C:/Users/mggar/Source/evidence/ant-man-public-independent-restart.log`. The original seven game results above were not rerun for this assertion-only change. Native interaction, replay-version compatibility and final integrated gates remain separate.
