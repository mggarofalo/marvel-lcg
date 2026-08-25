# The villain phase

`src/Marvel.Rules/Play/VillainPhase.cs`. MARVEL-173.

The first thing in the C# engine that moves the board — and, with the modifier
layer below, the last thing MARVEL-173 needed.

**All seven recorded digests of `rhino / spider_man / 12345` are produced by
resolving, and the game ends after exactly seven prompts,** which is what the
recording does. The fixture asks for twenty steps and holds seven because the
villain wins in round three.

## Almost all of it is rules, not cards

The steps are numbered as `rr:villain-phase` numbers them, so a divergence can
be argued against the published text:

| | | |
|---|---|---|
| 1 | Place Threat | the main scheme's acceleration field, per player |
| 2 | Enemies Activate | in player order; the villain, then engaged minions |
| | ↳ Attack *or* Scheme | `rr:activation.1`; an attack has [six steps of its own](enemy-attacks.md) |
| 3 | Deal Encounter Cards | one each, plus one per hazard icon |
| 4 | Reveal Encounter Cards | in player order, in the order dealt |
| 5 | Pass First Player Token | clockwise |
| 6 | End of Villain Phase and Round | lasting effects end |

**Two places need to know what a card says**, and both go through the same seam.
Step 4 asks a revealed card what it does; the window around an activation asks
the board what is waiting in it. Everything else — the threat, the
scheme-versus-attack choice, the boost card, the discard — is the Rules
Reference, and `ICardAbilities` is the one way a card's own behaviour enters. That
is what makes the interpreter a drop-in later rather than a rewrite:
`docs/card-dsl.md` designs it and opens with "nothing here is implemented", so
until it exists `Marvel.Content.Cards.CoreSetAbilities` holds **three cards**,
and the rule is to add one only when a step some test reaches actually needs it.

Whether the villain schemes or attacks is `rr:activation.1`: hero form and it
attacks, alter-ego form and it schemes. Which face is showing *is* which form,
so no separate flag is needed. They are two different steps with two different
triggering conditions, and the attack is much the larger of them — see
[enemy-attacks.md](enemy-attacks.md).

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

## What the other two recorded boards say

`vectors.json` carries two more games as per-step hashes. Both diverge at
**step 0** — the deal, not the engine:

| board | C# deals | the engine deals |
|---|---:|---:|
| `klaw / she_hulk / 2026` | 81 | 83 |
| `ultron / black_panther / 4242` | 83 | 84 |

Cards short at setup, which is the deal-order coverage gap MARVEL-176 already
measured: scenarios whose setup fires a card ability, or allocates a status card
mid-setup, deal more cards than the deal order describes. Nothing here is a resolve
gap, and the engine cannot be measured against either board until the deal is
right.

## What one player and one lucky card cannot test

The milestone game has **one player**, and its round-one boost card has **no
boost icons**. So two pieces of the phase are exercised by nothing in it, and
both survived a mutation that deleted them outright:

- passing the first player token — at one player, the modulo and a no-op agree;
- adding boost icons to the scheme value — at zero icons, adding and not adding
  agree.

And three more of exactly the same shape turned up with the modifier layer:

- `01105` takes its "already Tough" branch on no recorded step, so the branch was
  unexecuted code that read as though it worked;
- the modifier's in-play guard — the recorded board has no out-of-play modifier
  to exclude;
- the *equal to* half of "equal to or greater than" the target threat.

**Six blind spots, all found by deleting the code and watching the tests pass.**
That is the argument for mutation-testing a suite whose strongest check is a
single recorded game: the recording is the best evidence available and is not
the same thing as complete coverage.

`tests/Marvel.Rules.Tests/Play/VillainPhaseTests.cs` holds all of them on
hand-built boards — three players, boost values of 0, 1 and 3, thresholds above
and below and exactly at the target.

## Modifiers are printed data, not card text

Step 5 takes Rhino's `attack` from 2 to 5 because Charge is attached to him. The
tempting reading is that Charge's ability does it. It does not:

```
01099 Charge   engine attributes: {"Boost": "2", "ATK+": "3"}
```

`ATK+` is the engine's own attribute name for a modifier, and the convention is
closed and small: **116 cards carry `ATK+`, 50 carry `SCH+`, four carry `THW+`**,
and every one of the 170 is an attachment or an upgrade. So a card that prints a
modifier does not need an ability to apply it — `StateFields` reads it off
whatever is hosted on the card being described.

The *other* half of Charge is a Forced Interrupt that fires when Rhino attacks,
and it is implemented — on a hand-built board, because no recorded step reaches
it: the hero never leaves alter-ego form. [enemy-attacks.md](enemy-attacks.md).

**The modifier only counts while the attachment is in play.** The recorded game
cannot tell — its one modifier sits in `UpgradesArea`, and its one other hosted
card is a Tough with nothing printed on it — so a resolve that counted modifiers
from anywhere passes every recorded digest. A discarded attachment does not
modify the card it used to be on, and that needs a hand-built board to state.

## Printed values are filled at registration, not in play

`01101` Hydra Mercenary leaves the encounter deck with `attack: 0, guard: 0` and
reaches the **discard pile** with `attack: 1, guard: 1`. It was a boost card; it
never entered play. So the printed constants are filled when the card registers
— the same correction the token pools needed, for the same reason.

`health` is the exception and stays gated on being in play: the same minion
reaches the discard with `health: 0` against a printed `HP 3`. Whether that is
"printed only while in play" or "a pool filled on entry" the recording cannot
say, because nothing in it takes damage.

Now that something does, it is neither: `health` is printed hit points **less the
damage on the card** (`rr:damage.1`), and damage is a counter on `Card` rather
than a token pool, because the digest records no damage key at all. On every
recorded board the subtrahend is zero, which is why the recording still cannot
tell a subtraction from a printed constant.

## The villain wins

`rr:main-scheme-main-scheme-deck.2`: threat at or above the target completes the
scheme, and completing the **final** stage wins the game for the villain. The
Rhino deck holds one stage, so that is this case, and it is why the recording
stops at seven steps of a twenty-step request.

Checked after each threat placement rather than at the end of the phase, because
the engine's own log completes the scheme in the middle of the villain's
activation and never deals the encounter cards that would have followed.

**"Equal to or greater" is untested by the recording**, which reaches 8 against
a target of 7 — so *strictly greater* also fires there, and a resolve that required
it produces every recorded digest and ends the game a round late. The boundary
needs a hand-built board.

Advancing to a *next* stage is not implemented and throws rather than being
skipped: a scheme that completed and did not advance would sit there
accumulating threat for ever.

## Reproducing

```bash
dotnet test tests/Marvel.Content.Tests   # against the recording
dotnet test tests/Marvel.Rules.Tests     # the boards the recording cannot reach
```
