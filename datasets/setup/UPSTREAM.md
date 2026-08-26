# The setup dataset — authored

What a scenario is dealt from: which encounter sets it holds, which villain and
main scheme, which modular sets, and what forty cards each hero opens with.

| | |
|---|---|
| Kind | **authored** — see below |
| Records | 135 scenarios, 63 heroes, 184 encounter sets |
| Card references | 7,026, every one resolving against `datasets/cards/` |
| Gated by | `SetupDatasetTests` |

## Why it is neither generated nor vendored

`AGENTS.md` non-negotiable 8 names two kinds of dataset, and this is a third.

It cannot be **generated**, because most of what it records is not printed on
any card. Which modular encounter sets a scenario uses, what is set aside
before the game, what a hero's pre-built deck holds — all of that is printed in
a scenario's rules insert and on the back of a product box. MarvelSDB carries
the cards; nothing carries the scenario.

It cannot be **vendored**, because there is no upstream to copy it from. The
data was transcribed by hand from the printed products, and the tooling that
transcribed it has been removed.

So it is authored here, and what the non-negotiable actually asks for — that a
dataset cannot drift unnoticed — is a gate rather than a regeneration.

## What the gate holds

Every failure in `SetupDatasetTests` is one a game would otherwise meet at the
table, and the reason it matters more now than it did is that
[`../cards/`](../cards/) is regenerated from an upstream that moves: the two
can come apart without either being edited.

| | |
|---|---|
| every card id resolves against `datasets/cards/` | otherwise a deal throws partway through a board |
| every named encounter and modular set is defined | otherwise a scenario silently deals fewer cards than its insert says |
| a main scheme is a main scheme, a villain stage is a villain stage | otherwise the board is plausible and wrong |
| every hero has an obligation and a nemesis set | `rr:obligation`, `rr:nemesis-encounter-set` — both are shuffled in when that hero plays |
| the header's counts are the file's real counts | a claim nobody checks is how a group stops covering a scenario |

Four of them carry a pinned list of exceptions rather than a clean rule,
because the game has four:

- **Sixteen scenarios name no villain.** The Wrecking Crew, the Sinister Six,
  the Four Horsemen and the rest put several enemies into play from their
  encounter sets rather than running one villain deck.
- **Eight villain faces are the `Leader` type**, which `CardKind` has no name
  for — so those four scenarios cannot currently be dealt at all. MARVEL-257.
- **Four main scheme faces are environments.** Venom Goblin's stages are
  double-sided cards whose backs are the locations the scenario moves between.
- **Eleven names are held by two groups.** A character who is both a hero and
  the villain of their own scenario. Harmless here, because a scenario, a hero
  and an encounter set are three separate tables — and not harmless to anything
  that flattens them.

## What would make it generated

The encounter set contents are the derivable part: 139 of the 150 sets
MarvelSDB names can be rebuilt from it exactly, by grouping cards on
`set_code`. That is about a third of the file's 7,026 card references, and the
other two thirds — the scenario structure and the pre-built decks — would have
to be authored anyway. Worth doing to remove the duplication, not worth doing
for its own sake. MARVEL-252 carries it.
