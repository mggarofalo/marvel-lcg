# Forms

`src/Marvel.Rules/State/Forms.cs`, `src/Marvel.Rules/State/Seat.cs`,
`src/Marvel.Content/CardCatalog.cs`.

**Form is read off the board and never stored.** `rr:identity`: *"A player's
identity card is a double-sided card that represents their hero on one side and
their alter-ego on the other. The side that is face up indicates the form
*(hero or alter-ego)* that player is currently in."*

That sentence is the whole design. `Forms.Of(world, seat, facts)` computes an
answer every time it is asked; nothing anywhere holds a `bool IsHero`. A stored
copy would be a second source of truth, and `rr:form-change-form.3` guarantees
the two ways of changing form do not both go through one place — so the copy
would drift.

## It is a set, not a choice

`rr:form-change-form.6`: *"Cards with the '[type] form' keyword grant an
identity unique forms. These forms are in addition to the identity's alter-ego
and hero forms, and they come with their own conditions for changing into
them."*

Measured over the 4,344-card pool, **three such types exist on nine faces the
engine has**, and every one of them is a permanent on a card of its own — not
one is a face of an identity:

| form | cards | hero |
|---|---|---|
| `energy` | `21002` Gamma, `21003` Photon, `21004` Pulsar | Spectrum |
| `mass` | `26002a` Intangible / `26002b` Dense | Vision |
| `mass` | `32031a` Solid / `32031b` Phased | Shadowcat |
| `suit` | `50035a` Assault / `50035b` Stealth | Nick Fury |

These coexist with hero form rather than replacing it. `21002` Gamma reads
"Spectrum gets +2 ATK" and "**Hero** Response: After you change to this form…",
both of which only parse if Spectrum is in *hero form* while an energy form is
faceup. So `Forms.Of` returns a `SortedSet<string>` and a player in Gamma is in
`{energy, hero}`.

Sorted, and ordinally, because non-negotiable 1 in [AGENTS.md](../AGENTS.md)
forbids iteration over an unordered set where order can affect game state.

Three more faces print the keyword and are deliberately **not** among the nine:
`26002` and `57046a/b` are MarvelSDB-only, and the keyword is read from the
engine's text for the same reason traits are read from the engine's trait list —
see [card-dataset.md](card-dataset.md). A card the engine does not have gets
nothing.

### Reading the keyword

The keyword is a **sentence of its own** on the keyword line. That is what
separates it from prose naming a form, and the pool contains all three kinds of
prose:

```
"Energy form. Permanent."                                   → energy
"Permanent. Mass form."                                     → mass      (not always first)
"If you are in Archangel form, place 2 threat."             → nothing   (42024, an obligation)
"After you attack or defend in Solid mass form, flip this." → nothing   (32031a, its own text)
"Hero form only."                                           → nothing   (rr:form-change-form.7)
```

`CardCatalog.FormOf` therefore requires the whole sentence to be
`<Capitalised> form`. A scan for the words would mark four extra cards.

## Changing form

`rr:form-change-form.1`: *"Once each round, during their turn, each player is
permitted to change form by flipping their identity card."* All three
qualifications are enforced, and they live in two places on purpose:

- **The flip** is `Forms.Change`, which turns the identity card to its other
  face and touches nothing else.
- **The permission** is `Seat.FormChangedInRound`, spent by `Game`'s
  `Change_Form` affordance.

They are separate because `rr:form-change-form.3` says so: *"If a card ability
causes a player to change forms, it does not count against the one voluntary
form change the player is permitted."* An ability calls the flip; only the
turn option spends the budget. A player who has spent it is **not offered the
option again** that round, rather than being offered one that throws — the
same defect MARVEL-130 fixed on the action menu.

Changing form does not end the turn. It is one thing a player may do in a turn,
so the same prompt is put again with the option gone.

### What survives a flip

`rr:form-change-form.2`: *"When a player changes form, **only the form
changes.** The character retains their sustained damage, status cards, lasting
effects, attached cards, tucked cards, tokens, and current state (ready or
exhausted)."*

This falls out of both faces being one `Card`: the flip moves `FaceIndex` and
nothing else. It is worth stating because **the specific rule is beating a
general one here**. `rr:flip.2.2` says a card whose new face has a *different
card type* discards its attached cards, tucked cards, status cards and tokens —
and hero and alter-ego are different card types. Applied to a form change it
would throw away exactly what `rr:form-change-form.2` keeps.

### Three-faced identities are refused by name

`rr:flip.1`: *"A foldable, 'three-sided' card is considered to have flipped any
time the faceup side of the card changes."* Three identities in the pool are
built that way, each with a **second hero face carrying its own stat line**:

| set | alter-ego | hero | second hero |
|---|---|---|---|
| `ant` | `12001b` Scott Lang | `12001a` Ant-Man | `12001c` Ant-Man |
| `wsp` | `13001b` Nadia Van Dyne | `13001a` Wasp | `13001c` Wasp |
| `angel` | `42001b` Warren Worthington III | `42001a` Angel | `42001c` **Archangel** |

Angel is the one where the titles differ, and `rr:identity.2` makes that
matter: *"If a card refers to a hero or alter-ego by title, it refers only to
the identity with that title."* A card naming Angel does not reach Archangel.

Which hero face a flip from alter-ego arrives at is **not settled where the
flip is described**, and the faces do not print the same numbers — Archangel
prints THW 0 where Angel prints 2. So `Forms.Change` throws naming the card
rather than guessing a stat line onto the board. This wants a ruling; see
[rules-provenance.md](rules-provenance.md) for the patch loop.

Ironheart is *not* this. `29001`/`29002`/`29003` are three separate
double-sided identity cards, a deckbuilding choice rather than a form.

## What the digest does not yet carry

Two gaps, both named rather than guessed at.

**The `Hero` key set is reasoned, not measured, and is the only row in
`StateFields.Registered` that is.** No recorded digest reaches hero form:
`rr:identity.1` starts every player in alter-ego form, `rhino / spider_man /
12345` is the only case carrying full `step_digests` rather than hashes, and
`01001a` appears in none of its seven steps. The row is derived from the
measured `AlterEgo` row by the two differences between the faces — an alter-ego
prints `REC` and a hero does not; a hero prints `ATK`, `THW` and `DEF` and an
alter-ego does not.

`defense` is consequently the one key in that whole table that no recorded card
has, which is consistent rather than alarming: across all 58 faces and 65
distinct field keys of the recording there is no `defense`, because DEF is
printed on hero faces alone and no hero face is ever faceup. Writing the row
down is what makes a future disagreement loud — an emitted key set is compared
whole — where a hero registering nothing was silent.

**No `f_<name>` key is emitted.** [state-digest-v2.md](state-digest-v2.md)
reserves that namespace for form keys and says they *"come from game data, so
the key set is open-ended and a port cannot enumerate it from a fixed schema"* —
which is this document's claim in the digest's words. But no recording shows
one, so which card carries the key (the identity, or the card granting the form)
and what its value counts are both unknown. The digest is a wire format where a
guessed key changes every game outcome, so naming a form is answered here and
putting one on the wire waits for a recording.

`Seat.FormChangedInRound` is not on the wire either, so a game restored from a
digest alone would forget whether the voluntary change had been spent. That is a
gap in the digest rather than a reason to invent a key for it.

## What is not implemented

- **Changing into a keyword form.** The nine cards are read and the forms are
  named, but nothing puts a player into one: that needs `flip` as a card-DSL
  operation and Spectrum's Forced Response as a row in
  `datasets/abilities/abilities.json`. See [card-dsl.md](card-dsl.md).
- **Flipping a three-faced identity**, above.
