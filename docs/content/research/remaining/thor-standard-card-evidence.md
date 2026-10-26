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
content packet. Independent authority and adversarial review follows integration.
