# Product and repository scope

The executable product is the Marvel Champions Core Set. The runtime supports:

- Spider-Man, Captain Marvel, She-Hulk, Iron Man and Black Panther;
- Rhino, Klaw and Ultron in Standard and Expert modes;
- Bomb Scare, Masters of Evil, Under Attack, Legions of Hydra and The Doomsday
  Chair; and
- all 209 Core Set card faces.

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

The card DSL was designed against the complete card pool before the runtime was
narrowed to the Core Set. That exercise validated the language against known
future card patterns so later products should not require a wholesale rewrite.
Only the 209 Core Set faces have executable ability rows today.

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
