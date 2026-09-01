# The card dataset

`datasets/cards/cards.json` is what a printed card says, as the engine reads it.
It is **generated** — `AGENTS.md` non-negotiable 8's first kind — from two
inputs and nothing else:

| | |
|---|---|
| `datasets/marvelsdb/` | the vendored MarvelSDB snapshot: printed text, typed stats, traits |
| `datasets/cards/supplement.json` | what that snapshot does not record, authored here |

```
$ dotnet run --project tools/Marvel.Cards.Extract -- write    # rebuild it
$ dotnet run --project tools/Marvel.Cards.Extract -- check    # is the committed file what the generator produces?
$ dotnet run --project tools/Marvel.Cards.Extract -- diff     # what would change
```

`check` is a CI gate on both legs. It is the whole of what "generated" means
here: the same inputs give the same bytes, offline, and a hand edit is a red
build.

## Why printed text is the authority

Behavioural specs and card abilities are authored from **printed card text**.
Implementation tables are not authorities. The generated catalog supplies one
normalized answer for each printed fact and keeps corrections in the auditable
supplement rather than merging competing runtime interpretations.

## What a record holds

```json
{
  "card_id": "01094",
  "name": "Rhino",
  "subname": "",
  "type": "Villain",
  "traits": ["BRUTE", "CRIMINAL"],
  "attributes": { "ATK": "2", "HP": "14*", "SCH": "1", "Stage": "1" },
  "text": "",
  "text_plain": "",
  "pack": "core",
  "set": "rhino"
}
```

`card_id` is MarvelSDB's `code`, which the engine calls a **face** id: `01001a`
is Spider-Man and `01001b` is Peter Parker.

`text` keeps upstream's markup and `text_plain` does not. Both, because they
answer different questions — `CardCatalog` reads the plain text for "does this
card print a Boost ability", and somebody authoring a card needs the bold
markers that say which ability is which.

`attributes` is everything printed the engine reads. **What the keys are called
is our choice**: the Rules Reference names the values and not the spelling of a
JSON key, and `StateFields.PrintedFrom` is written against these. The only
property they have to keep is holding still.

## Where the attributes come from

Two places on the card, so two readers.

**The stat box** is structured data upstream. `Printed` maps it, and three
things in that mapping are worth knowing:

- **A character's box, an attachment's modifiers.** `rr:attachment.1` makes an
  attachment's printed numbers modifiers on the card it is attached to, so they
  are `ATK+`, `SCH+` and `THW+` rather than `ATK`, `SCH` and `THW`.
- **The `*` suffix is a per-player icon** on hit points and threat, and a
  **consequential damage** count on an ally's ATK and THW —
  `rr:consequential-damage.1`. `CardCatalog.PrintedValue` multiplies the first
  and `ConsequentialDamage` counts the second, and telling them apart is what
  the card kind is for.
- **Threat is per player unless the card fixes it**, which is the opposite way
  round from the stat box: upstream flags the *fixed* case, so the star is the
  default and the flag removes it.

**The text box** is where the keywords are. `rr:keywords.1` puts them at the top
of the box, each its own sentence, and `Keywords.Line` reads exactly that run —
stopping at the first sentence that is not one. That boundary is the whole of
the reader's correctness: `04067` Full Auto's "**When Revealed (Alter-Ego)**:
Surge." is an ability whose *effect* is a surge, and the card does not have the
keyword. A bare substring search gives it one.

A colon, a lower-case first letter, or a name longer than three words ends the
keyword run. Reminder text neither counts nor ends it — `rr:reminder-text`,
"reminder text has no effect on gameplay".

## The supplement

`datasets/cards/supplement.json` is the second input, and it exists because
MarvelSDB stops in two places.

**Cards it does not have.** The status cards are the clearest: `rr:status-card`
has the *game* make a tough, a stunned and a confused card, so they are not
printed cards at all. The generic minions and allies are the same shape —
`Reveal.EnterPlay` needs a face for "put a minion into play" when no printed
card is named — and the 26 Challenge cards and two rule inserts are the campaign
expansions' own components.

The core set adds no engine-only face. Its three Android Efficiency cards are
`01144a`, `01144b` and `01144c`; there is no base `01144` card. A player card
dealt facedown as an Ultron drone also remains that card. The Ultron Drones
environment supplies its minion values, so there is no separate Drone Minion
face to add.

**Printed facts it does not record.** The small `ATK +1` in an attachment's
stat box, which it carries for most of the 170 cards that have one and not for
these. A keyword missing from a transcription. Four villain stages whose hit
point box prints an infinity glyph, which upstream records as zero — a
character already defeated.

It is **grouped by reason**, and every group says why. That is the discipline: a
supplement nobody can audit is a place for a made-up number to live, and the
reason is what tells a reader whether an entry is a transcription or a guess.

The core-set supplement has 2 corrections. Concussion Blasters prints `ATK +1`,
and Whiplash prints the `CRIMINAL` trait. Each entry names its English Core Set
card as its authority. Expansion entries remain unchecked until expansion work
begins.

## What is not here

- **`datasets/setup/`** is authored rather than generated. Scenario composition
  and starter decks come from product instructions, so `SetupDatasetTests`
  provide its gate. See [setup-dataset.md](setup-dataset.md).
- **Rulings.** `datasets/marvelcdb-faq/` carries official rulings and nothing
  here is built from them: a ruling is an input an author reads, not a field the
  dataset derives. See [its UPSTREAM.md](../datasets/marvelcdb-faq/UPSTREAM.md).
- **Arbitrary deck building.** The runtime validates the 5 published Core Set
  starter decks. It does not accept a user-built deck, so identity-specific
  deck-building exceptions are outside the current product boundary.
