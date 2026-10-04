# External legal Core window fixtures

The fixtures run outside the repository; this directory retains public provenance. Unsuffixed identities and seventh verification are historical. Fresh eighth runtime copies and verification use the eighth-prefixed records, engine replay v3 / protocol19 / save4. They do not establish native comprehension or product acceptance.

Both start from an ordinary DatasetGameFactory seeded Core deal: Rhino standard, Spider-Man starter deck, recommended Bomb Scare modular set. The search uses only engine-offered decisions and the shared DecisionComposer, recording every exact accepted EngineDecision and pre/post fingerprint. No ECS, card, zone, counter, form, agenda or prompt injection is used.

| Fixture | Seed | Accepted prefix decisions | Current boundary | Listener |
|---|---:|---:|---|---|
| threat | 2 | 4 | Round1 optional imminent threat-placement interrupt | 127.0.0.1:57345 |
| reveal | 3 | 7 | Round1 optional revealed-encounter interrupt, before its effects | 127.0.0.1:57346 |

Threat final fingerprint: `9cd71048c78ab00f419c87f9bcecbfe2c3782d8336ce24835bddd2e006c7e194`.
Reveal final fingerprint: `1cafd8fc28e9ac23420101e0979548726f5ff814257042ba7a6eeb99de91ffe1`.

For the independent reviewer, provide only the scenario, setup/build provenance and these goals:

- Threat: assess the pending scheme progress and available responses; explain timing, answering authority, costs and consequences before choosing; carry out one choice and explain its result and next decision.
- Reveal: handle the current encounter and any resulting attack; explain what has resolved, what remains pending, the available commitments and known costs/consequences; make and explain a real accepted choice.

Use the actual hosted Join flow with the separate private connection file for the desired fixture. Do not hand the reviewer policy source, prefixes, state audits, exact expected copy, decision sequences, follow-up transcripts or click instructions. No UI was operated by this helper.

## Hosted provenance and authority

A disposable ordinary EngineHost accepts every prefix decision through its usual Resolve API. Its resulting StoredSession therefore includes the actual save, complete prior units and the current open unit. Ordinary SessionReplay verifies that journal against fresh seeded deals and matches the retained final fingerprint.

For an isolated fresh review host only, the bootstrap seat0 non-owner operational grant is converted back into an unused one-time invitation with exactly the same verifier and seat scope. The former active bootstrap grant is removed by that conversion; the owner remains spectator-only. The save/game/journal bytes are compared before and after and remain unchanged. This operational authority conversion is separate from game fixture provenance and is recorded in each operational-authority-provenance.json. It is not a production migration or widened authority policy.

Fresh verification hosts have exercised ordinary Join, confirmed exact seat0 scope, and accepted Great Responsibility / Enhanced Spider-Sense at the current revisions. A separate legal decline branch verifies Assault continues to an incoming Rhino attack and a legal defense choice, with intervening optional opportunities kept as distinct decisions. These verification branches do not consume the fresh serving invitations.

## Evidence

- Program.cs and WindowFixtures.csproj: external helper and HintPath-only Release references; no ProjectReferences.
- build.log: external build result.
- search.log: fresh legal seed search, prefix replay and hosted acceptance.
- release-copy-verification.json: all eight copied Marvel DLL hashes equal final repository Release DLLs.
- Each prefix.json: exact accepted decisions and pre/post fingerprints.
- Each final-canonical.json: full-information determinism evidence, not reviewer material.
- Each verification.json: ordinary Join/acceptance and legal follow-up evidence.
- Each operational-authority-provenance.json: save unchanged, journal hash, unit state and isolated authority conversion.
- Each assembly-identities.json: actual loaded fixture DLL hashes.
- Each connection.json: PRIVATE unused invitation and connection information; never publish or copy into repository evidence.

From this directory, without building the repository:

```sh
dotnet bin/Release/net8.0/WindowFixtures.dll serve threat 57345
dotnet bin/Release/net8.0/WindowFixtures.dll serve reveal 57346
```

Each serve reconstructs the legal journal, verifies it, starts a fresh isolated host and writes a new private connection file. Stop only the corresponding temporary host before resetting. Reproduce the independent checks with `dotnet bin/Release/net8.0/WindowFixtures.dll verify`; reproduce search with the `search` mode. The two serving processes are owned by bug_hunter; their tool session IDs are recorded in hosts.json.

## Eighth replay verification

The same retained legal prefixes were replayed using eight freshly copied eighth Release DLLs; each copy was byte-compared with the server outputs. Seed2/prefix4 ends at fingerprint `9cd71048c78ab00f419c87f9bcecbfe2c3782d8336ce24835bddd2e006c7e194`; seed3/prefix7 at `1cafd8fc28e9ac23420101e0979548726f5ff814257042ba7a6eeb99de91ffe1`. Eighth verification, assembly identities and operational provenance are retained separately. This replay evidence does not predict or approve the independent player journey.
