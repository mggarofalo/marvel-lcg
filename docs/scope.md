# Product and repository scope

The executable products are the Marvel Champions Core Set and Rise of Red Skull. The runtime supports:

- Spider-Man, Captain Marvel, She-Hulk, Iron Man, Black Panther, Hawkeye and Spider-Woman;
- Rhino, Klaw, Ultron, Crossbones, Absorbing Man, Taskmaster, Zola and Red Skull in Standard and Expert modes;
- Bomb Scare, Masters of Evil, Under Attack, Legions of Hydra and The Doomsday
  Chair, Hydra Assault, Weapon Master and Hydra Patrol;
- all 390 printed faces across the two products;
- one through four seats using authored starters or validated, ordered decks; and
- the five-scenario Rise of Red Skull campaign, with persistent progress, rewards
  and saved games. Standard or Expert campaign rules are selected independently
  of scenario difficulty.

`datasets/setup/setup.json` defines which products the runtime can start.
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

The executable ability book contains the 209 Core Set and 181 Rise of Red Skull
faces. Language support for a later card pattern does not admit that product.

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
