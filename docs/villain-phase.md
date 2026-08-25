# The villain phase

`src/Marvel.Rules/Fold/VillainPhase.cs`. MARVEL-173.

The first thing in the C# engine that moves the board. Steps 0 to 4 of
`rhino / spider_man / 12345` are now produced as the output of folding four
declines — **five of the seven recorded digests**, up from three.

## Almost all of it is rules, not cards

The steps are numbered as `rr:villain-phase` numbers them, so a divergence can
be argued against the published text:

| | | |
|---|---|---|
| 1 | Place Threat | the main scheme's acceleration field, per player |
| 2 | Enemies Activate | in player order; the villain, then engaged minions |
| 3 | Deal Encounter Cards | one each, plus one per hazard icon |
| 4 | Reveal Encounter Cards | in player order, in the order dealt |
| 5 | Pass First Player Token | clockwise |
| 6 | End of Villain Phase and Round | lasting effects end |

**Only step 4 needs to know what a card says.** Everything else — the threat, the
scheme-versus-attack choice, the boost card, the discard — is the Rules
Reference, and `ICardAbilities` is the one seam a card's own behaviour comes
through. That is what makes the interpreter a drop-in later rather than a
rewrite: `docs/card-dsl.md` designs it and opens with "nothing here is
implemented", so until it exists `Marvel.Content.Cards.CoreSetAbilities` holds
**one card**, and the rule is to add one only when a recorded step reaches it.

Whether the villain schemes or attacks is `rr:activation.1`: hero form and it
attacks, alter-ego form and it schemes. Which face is showing *is* which form,
so no separate flag is needed.

## What the recording forced, that the rules text does not say

### A token pool is acquired on entering play and never given back

The recorded `01105` has **no `k_threat` key** in the encounter deck, and
`k_threat: 0` once it reaches the discard — still there two steps later. Absent
and zero are different in a digest, so this is the difference between a card
that never had a threat pool and one whose pool is empty.

The obvious model — "registers its pools while in play" — produces the wrong
digest, because the card is in a *discard pile* when the key is recorded. The
flag is on the card (`Card.HasRegisteredTokens`), not on the zone.

### And the pool is granted by the area a card passes through, not by being revealed

Both treacheries in round one end up with `k_threat`, and **neither ever reaches
an in-play zone.** The boost card goes `EncounterDeck → BoostingArea →
EncounterDiscardPile`; the encounter card goes `EncounterDeck →
DealtEncounterCardsDeck → RevealingArea → EncounterDiscardPile`.

That pair also rules out the other candidate — that *being revealed* is what
registers the pool. The engine's log never says the boost card was revealed, and
it gets a pool anyway. What the two have in common is the place they passed
through, which is why `DeckTypes.GrantsTokenPool` is a different predicate from
`DeckTypes.IsInPlay` and why the villain phase routes cards through areas no
recorded step ever catches them in.

### The order is observable in one place

The recorded discard pile holds the boost card at **index 0** and the revealed
encounter card at **index 1**. That single fact pins the whole phase order: the
villain activates before cards are dealt. Draw them the other way round and
every card left in the encounter deck shifts, and every board after this one is
wrong.

### Threat comes from two rules that both give 1

`k_threat` goes 0 → 2, and it is tempting to read that as one placement. It is
the main scheme's own escalation (`1*`, so 1 at one player) plus Rhino scheming
(`rr:scheme-enemy-activation.3`, SCH 1 plus a boost card worth nothing). Either
rule alone gives 1 and looks half-right.

## What one player and one lucky card cannot test

The milestone game has **one player**, and its round-one boost card has **no
boost icons**. So two pieces of the phase are exercised by nothing in it, and
both survived a mutation that deleted them outright:

- passing the first player token — at one player, the modulo and a no-op agree;
- adding boost icons to the scheme value — at zero icons, adding and not adding
  agree.

A third gap was the same shape: `01105` takes its "already Tough" branch on no
recorded step, so the branch was unexecuted code that read as though it worked.

`tests/Marvel.Rules.Tests/Fold/VillainPhaseTests.cs` holds all three on
hand-built boards — three players, boost values of 0, 1 and 3 — because a
recorded game is the strongest check available and is not the same thing as a
complete one.

## What step 5 needs

Folding past step 4 throws, naming `01099` **Charge**. It is an attachment, and
the transition it drives is a different subsystem rather than another card:

```
48 01097b  k_threat 2 -> 5           escalation 1 + SCH 1 + boost 1
49 01094   attack   2 -> 5           <- Charge modifies its host's printed stat
53 01099   EncounterDeck -> UpgradesArea; host -1 -> 49; boost_const 0 -> 2
56 01101   EncounterDeck -> EncounterDiscardPile; attack 0 -> 1; guard 0 -> 1
```

Two things are new, and neither is a card ability:

- **A card in play modifies another card's printed value.** Rhino's `attack`
  goes from 2 to 5 because Charge is attached to him. That is a modifier layer
  over printed stats, and nothing in the engine has one.
- **Printed values are filled in on entering play, for every kind.** `01101`
  Hydra Mercenary sits in the encounter deck with `attack: 0, guard: 0` and
  reaches the discard pile with `attack: 1, guard: 1`. `StateFields.FillInPlay`
  has branches for three kinds today; this needs them for the rest, and keyed on
  *having entered* play rather than being in it — the same correction the token
  pools already needed.

## Reproducing

```bash
dotnet test tests/Marvel.Content.Tests   # against the recording
dotnet test tests/Marvel.Rules.Tests     # the boards the recording cannot reach
```
