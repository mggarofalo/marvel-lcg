# Thor standard card packet

Base: `ecdc64fd`. Branch: `feat/thor-standard-cards`. This packet authors
`06017`, `06018`, `06019`, `06030`, and `06031` using the existing engine.
The authored inventory advances from 598 to 603 faces, including 27 Thor faces.
This does not admit the incomplete Thor product or change setup or replay formats.

Authority is the corrected printed text in `datasets/cards/cards.json`, the
vendored MarvelCDB FAQ entries for `06018` and `06019`, and Rules Reference
`rr:you-your.6`, `.12`, `.14`, `.15`, and `rr:player-deck.3`.

Actual-book behavioral evidence:

- Hall of Heroes distinguishes identity and controlled ally defeats, credits
  Jarnbjorn upgrade damage, and requires the controlling player's alter-ego form,
  ready support, and three glory. Spending the final counter keeps the support.
- Battle Fury plays under a second player's control while retaining its owner,
  enforces the destination player's maximum, and damages/readies that controller's
  hero. Tough prevents the response from being offered without discarding the
  upgrade or consuming tough. Ally kills and nonattack upgrade kills do not qualify.
- Jarnbjorn accepts basic and labelled hero attacks, excludes ally attacks,
  requires physical or wild payment, targets a different enemy, credits the upgrade
  defeat to the hero, and does not recursively attack or enable Battle Fury.
  Rejected empty payment preserves the digest, RNG state and outstanding agenda
  before a valid retry completes.
- Trickster counts distinct printed card types from this resolution's discard
  for a non-first player. Player-deck exhaustion stops the discard and resets the
  deck; preexisting discard types do not join the receipt.
- Under Surveillance adds four total target threat for one, two and four players,
  enforces one per main scheme and removes the modifier on departure.

Focused behavior, ability-data and citation run: 55 passed. Five data mutants
were each rejected by compiled tests: Hall glory count 1 to 2 (1 failure), Battle
Fury takeDamage to dealDamage (1), Jarnbjorn physical to energy cost (4), Trickster
card types to resource types (2), Surveillance +4 to +3 (3). Dataset text was
restored in a finally block after the sequential mutant runs.

Final verification uses a non-incremental Release rebuild of
`tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj`, then the focused tests
plus ability lowering, program, cost-lowering and effect-lowering gates. Raw logs
are outside the repository at `C:/Users/mggar/Source/thor-standard-*.log`;
the final restored results are in `thor-standard-restored-build.log` and
`thor-standard-restored-tests.log`. Normal Husky hooks remain enabled.

No full game, public setup, native UI, or whole-solution run is claimed by this
content packet. Independent review of this packet is recorded below.


## Independent card review

Reviewed candidate `a1c49c54bfc55a3db20405da25e44ff5120e67f3` against the
five corrected printed faces, the vendored `06018`/`06019` FAQ entries, and
`rr:you-your` / `rr:player-deck.3`. The six-file candidate diff and its existing
20 card cases were reviewed. No additional card-row correction was identified.
This verdict covers these five authored faces and their conventional engine
paths; it does not establish complete Thor admission or native acceptance.

`ThorStandardIndependentTests` adds four executed cases: Hall of Heroes credits
only the defeating identity's controller with each seat attacking in turn;
Battle Fury remains unavailable when the attacked minion survives; Trickster
counts three distinct discarded types without including an older discard type.
The Hall fixtures retain a minion engaged with player zero while player one
attacks, so engagement cannot substitute for defeat credit.

Two sequential compiled data mutants distinguish the added ownership and defeat
checks. Removing Hall's `defeatedByYou` condition exposed the foreign Hall response
and failed both seat cases. Removing Battle Fury's `wasDefeated` condition exposed
its response after a surviving minion attack and failed that case. Both mutations
were restored in `finally`; the ability book's restored SHA256 is
`15130b5a8723c992ef21c335e97b8120677fc7e5774b159a2d688c75418fd9e0`.

Restored verification uses `dotnet build
 tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental`
followed by `dotnet test` of that project with `-c Release --no-build` and this
`FullyQualifiedName` selection: `ThorGloryCardTests`, `ThorWeaponFuryCardTests`,
`ThorTricksterSurveillanceTests`, `ThorStandardIndependentTests`, `AbilityDataTests`,
`AbilityLoweringTests`, `AbilityProgramTests`, `AbilityCostLoweringTests`,
`AbilityEffectLoweringTests`, and `RuleCitationTests` (OR-combined). Build: zero
warnings/errors. Tests: 120 passed, zero failed/skipped, including the same three
cases that rejected the two mutants. The four independent cases also passed
before mutation. Normal `dotnet tool restore` succeeded; commit hooks remain
enabled.

Raw logs are outside the repository at
`C:/Users/mggar/Source/thor-standard-independent-{baseline,hall-mutant,fury-mutant,restored-build,restored-tests}.log`.
This records a Windows managed review run, without a whole-solution, public setup,
full-game or native UI claim.
