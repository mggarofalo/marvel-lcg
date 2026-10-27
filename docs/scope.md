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
The isolated Thor integration candidate adds all 35 printed faces, the exact
40-card starter, obligation and reconciled five-card nemesis set. Missing a
required face or changing the pinned recipe closes the whole pack. Integration
is on hold pending independent Loki and Hammer Throw preflight reviews.
Whole-game, native and final combined gates remain separate from focused setup
and admission evidence. Black Widow has candidate public admission.

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
Goblin, 34 Ms. Marvel, 35 Thor, 34 Black Widow, 40 Doctor Strange and 33 Hulk faces
and five Ronan Kree Fanatic faces (723 total). Product admission requires complete
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


Black Widow now has candidate public admission for the complete 34-face inventory
and exact printed 40-card starter, obligation and nemesis composition. All four
extra customization cards enter legal custom decks. Missing authored faces,
changed catalog membership or wrong required recipes close the whole product.
Public setup and Natasha Preparation-play response restart checks provide focused
integration evidence; they do not establish complete-game or native acceptance.


Doctor Strange has candidate public admission for all 40 printed faces
(09001a/b and 09002-09039). The pinned mc08 divider in
`datasets/rules-packs/mc08/UPSTREAM.md` supplies 15 signature, 17 Protection and
8 Basic cards, a separate obligation, five nemesis cards and five Invocations.
The typed Invocation recipe has one each of 09032-09036, outside the ordinary
40-card deck. Missing any required face or changing the pinned recipe closes
the entire product. Extra cards 09037-09039 remain outside the starter and enter
legal custom decks at printed copy limits. Discovery now has thirteen heroes,
39 scenario/mode recipes and fourteen encounter sets. Public setup and focused
action evidence are recorded in `docs/content/doctor-strange-public-admission.md`;
complete-game, fresh-host replay and final combined acceptance belong to the
separate MARVEL-838 packet.


Hulk has candidate public admission for all 33 printed faces (10001a/b and
10002–10032). The pinned FFG divider in `datasets/rules-packs/mc09/UPSTREAM.md`
supplies the exact 40-card starter: 15 signature, 17 Aggression and 8 Basic.
Identity, Inner Demons (10025 ×1) and the five-card nemesis set
(10026 ×1, 10027 ×1, 10028 ×3) are separate. Missing any required authored or
catalog face, changed pack membership or an incorrect required recipe closes
the complete pack. Extras 10029–10032 stay outside the starter and enter legal
custom decks at printed copy limits. Public discovery has thirteen heroes,
39 scenario/mode recipes and fourteen encounter sets. Focused setup/admission
evidence is recorded in `docs/content/hulk-public-admission.md`; this candidate
does not establish complete-game, native or final combined acceptance.


Kree Fanatic has candidate public admission as a modular encounter set only.
The pinned seven-page FFG PDF recorded in
`docs/content/research/remaining/ronan-source-pin.md` supplies one copy each of
90001–90003 and two copies each of 90004–90005. All five authored/catalog faces,
exact pack membership, modular role and the complete seven-copy recipe are required
together. A missing face or malformed recipe closes the entire set while preserving
predecessor products. Standard and Expert scenario slots accept its normal modular
selection with starter or legal custom player decks. Recommended defaults remain
scenario-owned. Discovery has thirteen heroes, 39 scenario/mode recipes and fifteen
encounter sets, thirteen of them modular. The separate Galaxy’s Most Wanted Ronan
scenario remains unsupported. Focused evidence is in
`docs/content/ronan-public-admission.md`; this gate does not establish complete-game,
fresh-host or native acceptance.
