# Hulk Thunderclap — MARVEL-846

This nonpublic one-face packet appends actual 10005 Thunderclap after accepted
starter review `3588d6ac72d02d3e4a2c2bc46dec8ab62c0c7b1e`: 709 authored rows,
24 of 33 Hulk faces. All 708 predecessor raw objects remain byte-identical.
Their concatenated UTF-8 SHA-256 is `2945f4c3bcbd3d9bd486862086ab9988c9f7c504b04f14f9f6df4d7f7e17ce13`.
Toe to Toe 10015 stays absent; its preserved draft is untouched. No setup,
public admission, starting-counter or engine-source change belongs to this packet.

Printed authority is corrected `datasets/cards/cards.json` 10005: a cost-three
Hero Action choosing up to three different enemies and dealing three damage to
each. The vendored MarvelCDB FAQ queried 10005 and contains no entry. Current
RR 1.8 choose-game-element.3.1 prohibits duplicate targets; target.3.6 allows
prevented damage to affect a target. The printed optional maximum permits zero,
one or three choices after valid initiation. The row uses the independently
reviewed `dealDamage.maximum` prerequisite to choose one group, then resolve
simultaneous ordinary damage. DSL event, query and maximum spellings are engine
choices. This is neither an Attack-labelled ability nor an attack trait event.

The prerequisite commits are `7a98feea86e1ac0d45192cddb72b045e505983a0`,
`5fea29debe760c9e66e08ce5468938b7b198c3e1`,
`65190efed5f074fe65c95f89715e3ea7075cb387` and
`b3c9d8c72493860e6f867aa4f0dc4e9e63c572ec`. Their local cherry-picks end at
`c354b6b9`; the author commit contains only the row, test inventories, card proof
and documentation.

`HulkThunderclapCardTests` loads the actual full canonical book. The paid hero
action spends a double physical and single physical card, discards the event,
selects zero/one/three distinct enemies, includes another player's minion, and
keeps a Tough enemy eligible. Hero Stun stays attached, Guard does not bar
villain damage, Retaliate causes no damage, and no Attack event is emitted.
Alter-ego initiation is absent with a valid same-payment hero control.
Maximum/duplicate/stale group rejections preserve cards, RNG state and word
count, current and outstanding agenda work; a retry completes. Repeated offer,
preview and choice queries match an unprobed control, including causal events
when selection order is reversed.

Suspension tests retain canonical 10005 unchanged and substitute only a named
synthetic minion interrupt. A prevention choice opens before any group damage
is placed. A would-defeat interrupt opens after every group placement, survives
both players' optional opportunities, and finishes defeat and event cleanup.
These fixtures prove the actual card's use of the reviewed shared mechanism;
they do not claim the synthetic minion program is printed content.

Windows evidence is serialized under
`C:/Users/mggar/Source/thunderclap-evidence/`. Four valid data mutants are killed:
amount 4, maximum 2, only own engaged minions, and alter-ego instead of hero.
`mutations.json` records exits and summaries; each named log contains the
behavioral failure. The discarded invalid form-spelling probe is separately
labelled `alterEgo.log`; only `alterEgo-valid.log` counts as the form mutant.
The original bytes are restored before `final-build.log` (nonincremental Release
build) and `final.log`/`final.trx` (all affected inventory classes, Hulk,
BoundedDamage, AbilityData and RuleCitation). `filter.txt` preserves the exact
whole-class filter; `predecessors.json` records raw-object preservation.
Normal commit hooks remain enabled; `commit.log` retains their output.

This card evidence does not establish complete Hulk admission, public games,
native UI acceptance, save/replay or Linux CI. Those remain integration scope.

Final restored whole-class regression: **296 passed, zero failed**.
