# Keywords, and which of them the engine has

`rr:keywords` lists 28, and the Rules Reference writes almost every one out as
the ability it is equivalent to. That sentence is what the engine implements —
not the reminder text on the card.

Icons are the same shape and are here too: they have their own entries and their
own "equivalent to the following constant ability" line.

## Implemented

| keyword | the ability it is equivalent to | where |
|---|---|---|
| **Alliance** | any player may help pay this card's costs | `CardPlay.Paying` |
| **Linked (title)** | one product set per bringing deck is set aside; control transfers ownership | `Blueprints.From`, `Reveal.EnterPlay` |
| **Assault** | a basic thwart against this scheme uses ATK instead of THW | `BasicPowers` |
| **Form** | grants an identity a unique form | [forms.md](forms.md) |
| **Guard** | the engaged player cannot attack any villain | `BasicPowers.Attackable` |
| **Hinder X** | enters play with X threat **on the card** | `Reveal.EnterPlay` |
| **Incite X** | *When Revealed: place X threat on the main scheme* | `Reveal.Keywords`, through `Threat.Place` |
| **Overkill** | excess damage from a defeated ally goes to its controller's identity; from a minion, to the villain | `Damage.Attack` |
| **Patrol** | the engaged player cannot thwart the **main** scheme | `BasicPowers.Thwartable` |
| **Peril** | while it resolves, only the resolving player may trigger anything | `Offering.Eligible` |
| **Permanent** | set aside before setup, and put into play by another card | `Blueprints.SetAside` (half — see below) |
| **Piercing** | discard **each** tough status card before dealing damage | `Damage.Attack` |
| **Quickstrike** | *Forced Response (Hero): after this minion engages a player, it attacks that player* | `Reveal.Quickstrike` |
| **Ranged** | this attack ignores retaliate | `Damage.Attack` |
| **Requirement (resources)** | those resource types must be among what pays the cost | `Resources.Required`, `CardPlay.Price` |
| **Restricted** | a third one forces a choice, after it is in play | `CardPlay` |
| **Retaliate X** | *Forced Response: after this character is attacked, deal X damage to the attacker* | `Damage.Retaliate` |
| **Stalwart** | cannot have confused or stunned status cards | `Statuses.Limit` |
| **Steady** | one additional card of each; not afflicted until two | `Statuses.Limit` |
| **Surge** | *When Revealed: deal yourself 1 facedown encounter card* | `Reveal.Keywords` |
| **Teamwork (trait)** | *Forced Response: after this minion enters play, if another minion shares the trait, it activates against the engaged player* | `Reveal.Teamwork` |
| **Team-Up (a and b)** | cannot be played unless both named friendly characters are in play | `CardPlay.TeamedUp` |
| **Temporary** | *Forced Interrupt: when the round ends, discard this card from play* | `PhaseEnd` |
| **Toughness** | *Forced Response: after this character enters play, give it a tough status card* | `Reveal.EnterPlay` |
| **Uses (X "type")** | enters play with X counters of that type | `Reveal.EnterPlay` |
| **Victory X** | *When Defeated: add this card to the victory display* | `Defeat` |
| **When Defeated** | the card's own ability, resolved before it leaves play | `Defeat`, through `ICardAbilities.WhenDefeated` |
| **Villainous** | *Forced Interrupt: when this character uses a basic power, give it a boost card* | `Keywords.IsBoosted` |
| **Vulnerable** | *Forced Interrupt: when this character becomes confused or stunned, discard it* | `Statuses.Vulnerable` |

| icon | | where |
|---|---|---|
| **Acceleration** | 1 additional threat during Place Threat | `MainScheme.Acceleration` |
| **Amplify** | each boost card gains one boost icon | `MainScheme.Amplify` |
| **Boost** | adds to the activating enemy's ATK or SCH | `Attack`, `VillainPhase` |
| **Consequential damage** | an ally takes it after attacking or thwarting | `BasicPowers.AllyPower` |
| **Crisis** | player cards cannot remove threat from the main scheme | `MainScheme.Crisis` |
| **Hazard** | one additional encounter card during Deal Encounter Cards | `Deal.HazardIcons` |
| **Per-player (`*`)** | multiply by the player count | `CardCatalog.PrintedValue` |

The three status cards — **tough**, **stunned**, **confused** — are in
`Statuses` and `Damage`. They are cards, not flags: the recorded board shows a
Tough on Rhino with its own object id, and Rhino's own `toughness` field at
zero.

## Not implemented

| keyword | cards | what it needs |
|---|---|---|
| **Permanent** | 86 | set aside — "except by card abilities **in the same set**" needs the effect's set, not just the card's |
| **Setup** | 39 | set aside — the "Put Setup Cards Into Play" step is not written |

### Teamwork's two statements disagree

The entry says *"at least one other minion **that shares the specified trait**
in play"*; `rr:teamwork.1`'s equivalent ability says only *"another minion in
play"*. The trait is followed — it is what the keyword prints and what the entry
says, and a reading that dropped it would activate an Acolyte beside an
unrelated Hydra trooper.

It also **activates** rather than attacking, which is the difference from
quickstrike. Quickstrike says outright *"a player whose identity is in hero
form"*; teamwork does not, so `rr:activation.1` reads the form and a teamwork
minion engaging an alter-ego schemes rather than doing nothing.

### Three keywords, one destination, and no id moves

`rr:permanent.2` ("set aside **before step 1 of setup**"),
`rr:setup-keyword.1` ("put into play during the *Put Setup Cards Into Play*
step") and `rr:linked-card-title.1` ("set this card aside during setup") all put
a card outside every deck at the deal. 139 cards carry one.

**What the engine did instead was shuffle them in**, and the failure was worse
than the cards never entering play: a permanent attachment in the encounter deck
is dealt, revealed and discarded like a treachery, so the board looks plausible
and the card is gone.

**No object id moves.** A creation's position in the deal *is* the card's id and
the id is on the wire, so this changes where a card goes and not when it is
made. The recorded corpus reads the same way — in one game the modular set runs
40147–40150 into the encounter deck at ids 202–205 and 40151–40158 into the
aside pile at ids 206–215, unbroken. And no card in the fixture board's scenario
or hero set carries any of the three, which is stated as its own test.

**The aside pile's own order is not settled.** The corpus shows the villain's
pile coming out differently between seeds of one board, so the engine that
recorded it shuffles that pile — and a shuffle draws from the game's single
random stream, so reproducing it needs the exact position in that stream rather
than merely the right permutation. This deals in creation order, which is
deterministic and which no recorded digest contradicts. MARVEL-210.

**Half the job.** Permanent is now correct: set aside, and put into play later
by another card's ability. Setup is not — `rr:setup-keyword.1` names a step this
engine does not have, and putting a setup card into play needs its own text run
(Flight says "attach to the villain"), which is the ability interpreter's
business rather than the dealer's.

### Alliance widens the payment and nothing else

`rr:alliance.2` is the limit of it: *"only the player playing the card with the
alliance keyword is considered to be resolving that card."* Helping to pay is
not playing, so everything downstream of the payment still reads the seat that
played the card.

Each spent card goes to **its own owner's** discard pile, which `Discard.Card`
already did by reading the card rather than the player who spent it — and which
`rr:player-deck.1` makes matter, because the owner is who reshuffles.

Ordinarily another player's hand is not a place a payment can come from at all:
`rr:cost.3` spends resources *"by discarding cards from **their** hand"*. That
is what the keyword suspends, and it is why alliance is checked when the play is
offered as well as when it is taken — a card three of whose cost sits across the
table is unplayable without it and playable with it.

### Peril is two rules with different reaches

`rr:peril.1` states one constant ability with two clauses, and they are not the
same restriction:

- *"While a player is resolving this card, that player cannot consult other
  players, and other players cannot trigger abilities."* — **any** ability, not
  only ones on this card. A peril card is resolved alone.
- *"While this card is in a player's play area, other players cannot trigger
  abilities on this card."* — narrower, and longer-lived: it lasts as long as
  the card sits there rather than only while it resolves.

Both land in `Offering.Eligible`, which is the one place the engine decides what
a seat may take out of a window. A player with nothing eligible is already
skipped in silence rather than asked, so nothing else had to change: the other
players pass, and the window closes.

**Table talk is not implemented and cannot be.** "Cannot consult other players"
is a rule about a room, and an engine that offers no opportunity has already
done everything it can about it.

### A team-up name is not always a card title

`rr:team-up.1` matches a friendly character "whose title or subtitle matches",
and `rr:friendly` makes "friendly" every player's rather than yours — one
sentence, "a blanket term that refers to cards **the players** control — so at a
table the other player's Wasp is the Wasp the card needs.

Thirty-four names appear across the 28 cards and thirty-two of them are a card's
title or subtitle. The other two are **`Black Panther/T'Challa`** and
**`Black Panther/Shuri`**, on "Heart of the Panther": two identities share the
hero title *Black Panther*, and the alter-ego is what tells them apart. No card
carries either name, so the slash is read rather than matched — as two halves,
against every face of the identity card.

`rr:unique-icon.1.2` is why that is not a liberty. The rules already use an
identity's **alter-ego title** as one of its identifying names: *"the identity
with the T'Challa alter-ego, the T'Challa ally, and the Black Panther ally with
the subtitle 'T'Challa' are all considered to match."* Reading only the faceup
side would make the notation name nothing at all, since neither face carries
both halves.

A **plain** name still reads the faceup side only, because `rr:identity.4` says
so: "the faceup side of an identity card is considered to be in play". A player
who has flipped down is not the hero a team-up card names.

The deck-building half of `rr:team-up.1` is not here. The decks reach this engine
already built, and a rule about what may go in one has nothing to check at play
time.

### The requirement is part of the cost, not additional to it

The same reading `rr:resource.4` gets: a cost of 1 requiring a physical is
**one** card that generates a physical, not one plus a physical. `Resources.Pays`
has taken the requirement since it was written; what was missing was anybody
passing one, so all thirteen cards that print a `Requirement` were payable with
any cards in hand.

It is checked when the play is **offered** as well as when it is taken, and the
check asks the whole pool rather than a subset. That is exactly the right
question: `rr:cost.4` permits generating beyond the cost, so if every generator
together cannot pay then no choice among them can, and if they can then spending
all of them is a payment.

`rr:requirement-resources.2` — "cannot be played 'ignoring its resource cost'" —
needs nothing yet, because nothing plays a card ignoring its cost.

### The keyword's argument is a word, not a number

`Teamwork ([[ACOLYTE]])` prints as the attribute `Teamwork = "ACOLYTE"`, so it
is read off the raw attribute table: `PrintedValue` answers zero for it, which
is what it answers for a card with no teamwork at all. **The digest field is
zero too** — the recorded corpus shows `"teamwork":0` on Senyaka, a Teamwork
(ACOLYTE) minion, with the trait riding on `t_ACOLYTE` instead. So the keyword's
presence is not on the wire, and the behaviour has to come from the printed
data.

Five other keywords print a word rather than a number: **Uses** (69 cards),
**Team-Up** (28), **Requirement** (13), **Form** (9) and **Linked** (9). Uses,
Form, Requirement and Team-Up are read; Linked is in the table below.

**Uses is half done and says so**: the counters are placed, and "when the last
all-purpose counter is removed, discard that card" waits for an ability that can
remove one — no node in [the card DSL](card-dsl.md) does yet.

## Two things the engine decides that a player should

Both are `rr:forced.5` — "if two or more forced abilities would initiate at the
same moment, **the first player determines the order**" — and both are
MARVEL-187:

- a card carrying **surge** and its own When Revealed text has two abilities
  initiating at once, and the engine runs the keyword first;
- **engaged minions** activate "in the order of that player's choice"
  (`rr:villain-phase.step.2.b`), and the engine takes them in play-area order.

A third is `rr:restricted`: **which** restricted card leaves is the player's
choice, and the engine keeps the one just played.

All three are deterministic and stated in comments, so a replay reproduces.
They become visible the moment two of the abilities interact.
