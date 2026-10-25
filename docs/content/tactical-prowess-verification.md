# Tactical Prowess verification

Canonical 07031 follows its printed text in `datasets/cards/cards.json`: move all threat from the least-threat side scheme to the most-threat side scheme, then gain surge unless the destination's Forced Response actually triggered. The vendored MarvelCDB FAQ records 07031 as queried with no entry; the rulings corpus has no Tactical Prowess record.

`TacticalProwessTests` and `TacticalProwessChoiceTests` exercise the canonical book in arranged worlds, without public scenario admission. Sixteen controls cover both revealing seats; both first-player seats with tied minimum/maximum and all-equal distinct endpoints; unique endpoints; all-zero and a zero minimum; a single remaining scheme; nonpaired source and destination schemes; destination response resetting threat to three; and a nested highest-cost discard choice. The nonpaired source is a protected signature scheme in an explicitly arranged world. An ordinary destination above ten with no Forced Response still surges, distinguishing actual initiation from a threshold approximation. The nested choice asserts that movement already happened once, surge remains undecided, and completion does not repeat movement.

Focused Release execution passes 16/16 cases. Three canonical-row mutants are killed: minimum changed to maximum (11 failures), all changed to one (9 failures), and the surge result changed from destination response initiation to threat moved (8 failures). Logs are `%TEMP%/tactical-card-focused.log` and `%TEMP%/tactical-card-mutant-{source-rank,amount-one,surge-result}.log`.

Runtime foundation: `369a0dc8`. This card increment does not admit Wrecking Crew into public setup and does not establish full-pack or native acceptance.
