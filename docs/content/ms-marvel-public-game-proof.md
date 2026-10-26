# Ms. Marvel public game and pending-choice proof

Tested engine/content revision: `5034a368`. This document's introducing commit adds only proof tests and this evidence; it changes no runtime, admission, or ability data. Runs measured on Windows on 2026-10-26.

`MsMarvelPublicGameTests` deals the published, unarranged starter through `DatasetGameFactory.Create`. Fixed seeds 1 through 7 were chosen before playing; no victory seed search, deck reordering, prepared world, or engine mutation is involved. The existing `CoreGamePolicy` chooses only offered actions and engine-accepted payments. Every decision compares canonical state, RNG words, outcome, phase, round, full durable prompt, events and information observations against an independently dealt replay. Completion requires a genuine terminal game; reaching the decision bound or throwing fails the test. Each seat must take a turn, pay and play a card.

| Scenario | Heroes | Seed | Result | Round | Decisions | Cards | Payments | Basic attacks | Villain faces | RNG words |
|---|---|---:|---|---:|---:|---:|---:|---:|---|---:|
| Rhino | Ms. Marvel | 1 | PlayersLose | 4 | 33 | 5 | 5 | 1 | 01094 | 92 |
| Klaw | Ms. Marvel | 2 | PlayersLose | 5 | 40 | 8 | 8 | 1 | 01113 | 137 |
| Ultron | Ms. Marvel | 3 | PlayersLose | 3 | 30 | 5 | 6 | 1 | 01134 | 133 |
| Crossbones | Ms. Marvel | 4 | PlayersLose | 4 | 29 | 5 | 8 | 2 | 04058,04059 | 141 |
| Absorbing Man | Ms. Marvel | 5 | PlayersLose | 3 | 24 | 5 | 5 | 1 | 04076 | 165 |
| Rhino | Ms. Marvel, Captain Marvel | 6 | PlayersLose | 6 | 82 | 16 | 17 | 6 | 01094 | 139 |
| Absorbing Man | Ms. Marvel, Captain Marvel | 7 | PlayersLose | 7 | 119 | 21 | 27 | 8 | 04076 | 246 |

These are operational complete games, including multiplayer and a Crossbones villain promotion. They establish neither victory nor a competitive Ms. Marvel strategy.

`MsMarvelChoicePersistenceTests` uses `EngineHost` and separate durable stores. A bounded opening-only probe of seeds 1..20 found Bruno first at Rhino seed 6; the committed test pins that seed. The normal starter keeps its hand, pays for Endurance and Bruno, stores one hand card facedown, ends its first round in alter-ego form, and discards Endurance rather than Bruno to Caught Off Guard. It then starts Bruno's nonempty retrieval. No card is created, moved or rearranged outside accepted public decisions.

At the suspended retrieval, Bruno's exhaustion is committed, exactly the stored card is offered, and zero or one selection is legal. Restart reconstructs the identical public response and save. An invalid selection rejects without changing either. Retrieving the card gives the same full response and exact serialized save as the uninterrupted control, returns the selected physical card to hand, removes its host and retains Bruno's exhaustion. The facedown stored card's public face remains concealed before retrieval.

Final focused checks:

- `dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj -c Release --filter FullyQualifiedName~MsMarvelPublicGameTests --logger 'console;verbosity=detailed'`: **7 passed**, 24.7 seconds.
- `dotnet test tests/Marvel.Server.Tests/Marvel.Server.Tests.csproj -c Release --filter 'FullyQualifiedName~MsMarvelChoicePersistenceTests|FullyQualifiedName~PublicMsMarvelSetupTests' --logger 'console;verbosity=detailed'`: **3 passed**, 8.1 seconds. This includes the existing public Teen Spirit / Big Hands / Morphogenetics restart and one-through-four-seat starter setup proofs.

Raw local logs are outside the repository: `%LOCALAPPDATA%/Temp/ms-public-games-final.log` and `%LOCALAPPDATA%/Temp/ms-bruno-public-final.log`. The fixtures are executable public decision journeys in the named tests, rather than opaque prepared boards.

This packet does not prove public Melee sequential-target persistence, expert modes, every Rise of Red Skull scenario, campaign continuation, victory, all card interactions, native UI comprehension, or Linux certification. It does not change the accepted product boundary or replace independent authority/admission review and the final combined gates.
