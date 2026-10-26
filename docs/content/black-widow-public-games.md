# Black Widow representative public games

MARVEL-790. This packet adds whole-game regression evidence to the reviewed
[public admission boundary](black-widow-public-admission.md). It uses source base
`a02316b8` plus reviewed admission `e6f3bba7` and independent review `4e4e5424`.
No gameplay, ability-book, setup-data or admission behavior changes are included.

`BlackWidowPublicGameTests` deals ordinary games through `DatasetGameFactory` and
opens an independent same-seed game through `EngineHost` with its production
session ledger. Every accepted decision compares the offered prompt, exact event
sequence, state fingerprint, RNG word count and terminal meaning against the
durable journal. Initial setup compares the complete authoritative setup events,
canonical digest and RNG count; public response events are visibility filtered.
The terminal save passes strict JSON serialization and `SessionReplay.Verify`,
and its reconstructed canonical digest, prompt, phase, round and outcome must
equal uninterrupted play. Policy queries also preserve these observed facts.

Seven starter cases cover solo Rhino, Klaw, Ultron, Crossbones and Absorbing Man,
plus two-seat Rhino and Absorbing Man with Captain Marvel. Each starter is the
actual published 40-card deck, including cards moved out of ordinary deck/hand
areas by scenario setup. One legal 40-card custom case combines Black Widow's
15 signature cards with the She-Hulk starter's non-signature Aggression/Basic
cards, replacing three non-signature cards with three Counterattacks. The
custom case requires an actual Counterattack play.

The policy reuses `CoreGamePolicy`, offered payment allocation and offered target
selection. Its small Black Widow addition prioritizes Preparation plays (with
Counterattack first when offered) and
offered Black Widow product timing abilities. It does not inspect encounter-deck
order or change state to pursue a result. Every case requires all seats to take
turns, play cards and pay resources; it requires accepted Preparation and
Widowmaker activations and a terminal game. The activation counters record
accepted offers, whose dependent choices can continue in later prompts; they do
not independently prove each ability's final consequence. Mission Prep checks the
observed one-card hand increase when reached. Losses are valid complete games;
these hero-pack checks impose no invented victory or all-mode matrix.

Detailed Windows evidence is external under `C:/Users/mggar/Source/`:

- `widow-public-games-initial.log`: five passed, three fixture failures from
  comparing complete setup events with visibility-filtered public events.
- `widow-public-games-final.log`: intermediate run after correcting setup-event
  comparison, eight passed, before the strengthened custom-play and characteristic
  assertions.
- `widow-public-games-overlapping-build.log`: an attempted rebuild failed because
  the preceding safe test process still held its Windows apphost. The final run
  starts after that process exits.
- `widow-public-games-verified.log`: final targeted run with decision-by-decision
  answers, source faces and before-state SHA-256 values, per-game reached-card
  sets, RNG counts, final digest hashes and accepted-trace hashes.

The strengthened matrix run passes all seven starter cases. Its custom seed 1
game reaches a terminal loss but fails the required Counterattack-play assertion:
the policy spends Counterattack on another Preparation. The custom retest in
`widow-public-games-custom.log` gives the offered Counterattack priority among
Preparation plays. The starter decks contain no Counterattack, so this preference
does not change their recorded decisions. The lead owns the combined final suite.

Command: `dotnet test tests/Marvel.Acceptance.Tests/Marvel.Acceptance.Tests.csproj
-c Release --filter FullyQualifiedName~BlackWidowPublicGameTests --logger
'console;verbosity=detailed'`.

This packet supplements focused authority tests. It does not independently prove
every card face, every obligation/nemesis branch, all four extra-pool cards,
Expert play, three/four seats, campaign progression or pending-choice restart.
The admission packet separately verifies strict restart at Mission Prep with
accepted/declined responses and invalid-answer preservation. A terminal replay
here does not establish restart at every Black Widow timing continuation.
Natural encounter and ability reach is recorded in each game's evidence rather
than inferred from deck membership. Native UI, broader managed suites and Linux
remain integration work owned by the lead.
