# The setup dataset — authored

What a supported Core Set game is dealt from: the scenario, villain and main
scheme decks, encounter sets, and the five starter decks.

| | |
|---|---|
| Kind | authored |
| Product boundary | Marvel Champions Core Set |
| Records | 6 scenario modes, 5 heroes, 7 encounter sets |
| Gated by | `SetupDatasetTests` |

## Why it is authored

Most setup facts are printed in the Learn to Play guide, scenario instructions,
and deck lists rather than on cards. There is no complete upstream dataset to
vendor, and the values cannot be regenerated from `datasets/cards/`.

The complete generated card catalog stays in the repository. This file is
smaller by design: its keys are the products the runtime claims can deal and
play. Later products return to this dataset only when their complete runtime
support is ready.

## What the gate holds

`SetupDatasetTests` verifies that:

- every referenced card exists in `datasets/cards/`;
- every named encounter or modular set is defined;
- every scenario has valid main schemes and villain stages;
- every hero has two identity faces, an obligation, and a nemesis set;
- the exact runtime keys remain the 6 Core Set modes, 5 heroes, and 7 sets;
- the header counts match the file.

Every failure is one a game would otherwise meet while dealing a board.
