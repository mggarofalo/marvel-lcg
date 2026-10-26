# Ms. Marvel public Melee restart proof

Candidate base: `5034a368`; isolated branch `test/ms-melee-public-restart`.

The public `DatasetGameFactory` and `EngineHost` admit a legal 40-card Ms. Marvel Aggression deck: all 15 mandatory signature cards, three Melee (05030), and Core Aggression/Basic cards. `AssessDeck` validates the exact deck. Normal Klaw Standard setup supplies the engaged minion; there is no world, deck, admission or ability mutation.

A bounded opening-hand probe considered seeds 1..20 in order and stopped at seed 2, the first with Melee after keep-hand. The committed regression pins 2 and contains no seed search. Weapons Runner naturally has 2 HP, so Melee's first printed 3-damage effect produces health 2→0 and defeat/discard. The public event clamps remaining health; discarded state resets damage. This is not evidence of a surviving-minion exclusion branch.

The test saves with Melee's required second-enemy choice pending, confirms the first physical enemy is excluded and the villain is offered, round-trips the protected stored session into an independent store, loads a fresh production factory, and synchronizes a new host. It compares the exact pending record and complete save, submits the invalid old target, checks no publication and an identical retry snapshot, then selects the villain independently on both hosts. Complete protocol responses (including ordered events), full saves, RNG word counts and pending records must agree. It asserts three damage to the villain, explicitly declines the offered Morphogenetics response, and checks return to player actions with Melee discarded and the unit complete.

Authority: generated `datasets/cards/cards.json`, printed 05030; vendored `datasets/marvelcdb-faq/faq.json`, entry 05030 (different enemies and sequential damage). The action's public `Description` is null; this packet does not assert an unavailable textual preview.

Validation command: `dotnet test tests/Marvel.Server.Tests/Marvel.Server.Tests.csproj -c Release --filter 'FullyQualifiedName~MsMarvelMeleePersistenceTests|FullyQualifiedName~PublicMsMarvelSetupTests' --logger 'console;verbosity=normal'`. Latest result and normal commit-hook output are retained in `%TEMP%/ms-melee-restart.log` and `%TEMP%/ms-melee-commit.log`. The final handoff reports the observed outcome. No native UI evidence or broad platform certification is claimed by this packet.
