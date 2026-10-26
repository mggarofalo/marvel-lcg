# Product and repository scope

The accepted executable content comprises the Marvel Champions Core Set, Rise of
Red Skull, Captain America Hero Pack and Wrecking Crew Scenario Pack. The latter
two merged in PR #439 with final cross-platform verification. Ordinary content
and engine acceptance is delegated to agents; owner review is reserved for
materially new UI interactions or genuine concerns requiring human judgment.
Wrecking Crew's multiple-villain UI remains a separate owner-review surface.
The runtime supports:

- Spider-Man, Captain Marvel, She-Hulk, Iron Man, Black Panther, Hawkeye, Spider-Woman and Captain America;
- Rhino, Klaw, Ultron, Crossbones, Absorbing Man, Taskmaster, Zola and Red Skull in Standard and Expert modes;
- Bomb Scare, Masters of Evil, Under Attack, Legions of Hydra and The Doomsday
  Chair, Hydra Assault, Weapon Master and Hydra Patrol;
- all 485 unique printed faces across the four products;
- Wrecking Crew Standard, Expert, Extreme and all sixteen custom A/B combinations;
- one through four seats using authored starters or validated, ordered decks; and
- the five-scenario Rise of Red Skull campaign, with persistent progress, rewards
  and saved games. Standard or Expert campaign rules are selected independently
  of scenario difficulty.

`RuntimeProductAdmission` defines the closed product policy. The Captain America
pack requires its exact 35-face inventory and published starter; Wrecking Crew
requires its exact 60-face inventory and all nineteen setup recipes. Removing any
required authored face or recipe closes that entire pack.

The integrated Green Goblin candidate also has public admission for its exact
57-face inventory, Risky Business and Mutagen Formula in Standard and Expert,
and all four modular sets. Admission pins stage and scheme order, fixed sets,
set-aside cards and physical encounter quantities. A missing or changed required
face or recipe closes the whole product. The Ms. Marvel candidate also has public
admission for all 34 printed faces and its exact published 40-card starter,
obligation and nemesis set. Its public setup and characteristic action/restart
slice are verified separately from whole-game and native acceptance; those
results belong to the integrated pack delivery evidence. Neither candidate is
claimed accepted by this admission increment.
`datasets/setup/setup.json` supplies the recipes the runtime can start.
`datasets/abilities/abilities.json` defines which printed faces have executable
card text. Both datasets fail closed outside that boundary.

## Broader source material

The repository keeps broader source material for research and future work:

- `datasets/cards/` contains the complete generated printed-card catalog;
- `datasets/rules-reference/` contains Rules Reference v1.8;
- `datasets/rules-packs/` contains vendored expansion rules;
- `datasets/marvelcdb-faq/` and `datasets/rulings/` contain published rulings.

These datasets are authorities and research inputs. Their presence does not make
a later product executable.

The executable ability book contains the 209 Core Set, 181 Rise of Red Skull,
35 Captain America and 60 Wrecking Crew faces, plus the candidates' 57 Green
Goblin and 34 Ms. Marvel faces (576 total). Product admission requires complete
playable content and the verified published starter; physical packaging copy
quantities are outside that requirement. Language support for a later card pattern
does not admit that product.

Some engine tests use synthetic cards or later-product patterns to prove a
general rule or data shape. Those tests validate an engine primitive. They do
not open a product boundary.

## Opening a product boundary

A later product becomes supported only when one coherent change supplies:

1. authored setup data for every supported way to start it;
2. executable ability rows for every reachable printed face;
3. any product rules that the base Rules Reference does not supply;
4. legal behavioral transcripts and narrow rule citations; and
5. fail-closed tests for adjacent products that remain unsupported.

Do not add isolated expansion cards to the runtime. A few working cards can make
an unsupported product look playable while unresolved cards silently produce a
plausible, wrong board.
