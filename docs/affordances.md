# Affordances

MARVEL-161. The prompt stops being a list of option strings and becomes a list of
things the player can do, each anchored to the board object they click.

Designed alongside [event-stream.md](event-stream.md) on purpose. The two halves
of the engine's return value share a `Trigger`, and inventing that twice is how
they would come to disagree.

## Why strings were enough and are not any more

A web client repaints a document, so an option can be a label and an index. A
game has to know that "Play Enhanced Spider-Sense" belongs to *that card, there*
— to highlight it, to animate from it, to grey it out.

MARVEL-41 already requires the prompt to carry the option set and "enough context
to tell a mid-resolution prompt from a turn-level one". This is the shape that
satisfies it.

## The proposal was a third of the answer

[presentation-layer.md](presentation-layer.md) sketched five fields:

```
Affordance { Id, Kind, AnchorId, Label, Legality }
```

That sketch was written without looking at what the engine already renders. It
renders fourteen fields per option, and the corpus cannot settle which of them
matter because it records the input that was *chosen* and never the set it was
chosen from. So the census played games instead and counted the options
rendered at every prompt.

**30 games. 1,997 prompts. 6,351 options.**

| field | informative on | in the sketch? |
|---|---|---|
| `id` | 100% | yes |
| `name` — the verb | 100% | yes, as `Kind` |
| `bind_id` — the anchor | 100% | yes |
| `bind_player_id` | 100% | no |
| `all_legal_targets` | **86.5%** | **no** |
| `target_num_range` | **86.4%** | **no** |
| `target_payment` | **53.5%** | **no** |
| `is_search` | 2.8% | no |
| `pay_size_is_effect` | 0.6% | no |
| `select_rule` | 0.3% | no |
| `target_groups` | 0.3% | no |
| `failure_reason` | 0% observed | yes, as `Legality` |
| `target_must_include_traits` | 0% observed | no |

The two the sketch dropped are the two a player is mostly deciding about. An
affordance without targets and costs can be clicked; it cannot be *chosen*.

## What each measurement decided

### The anchor is never absent

`bind_id` is informative on every one of 6,351 options, which is the whole
justification for the type. There is no fallback case to design for.

### Targeting is usually trivial and sometimes not

A target request is present on 86.5% of options. Two thirds of those offer
exactly **one** legal target — so the common case is a choice with one answer, and
a client can resolve it silently. But 20% offer two or more, so the list has to
travel rather than being collapsed by the engine.

`target_groups` appears on only 0.3%, and it is not optional for correctness.
`VillainAndMinionsEngagedWithYou` pools every player's minions but accepts
exactly one villain plus one player's whole group. The flat candidate list and a
min/max cannot express that, so a client obeying them would build an illegal
selection. When groups are present they are authoritative.

### Payment is plural, and it is not the cost

This is the measurement that most changes the design.

| ways to pay | share of priced affordances |
|---|---|
| 5 | 22.1% |
| 6 | 21.6% |
| 4 | 17.2% |
| 3 | 13.3% |
| 2 | 10.5% |
| 1 | 7.3% |

Only 7.3% of priced affordances have a single way to pay. **An interface that
picked for the player would be wrong more than nine times out of ten.**

That is the generation-and-payment distinction from MARVEL-169 made concrete.
Resources are generated incrementally — by discarding cards, by using abilities —
and then consumed once to pay. `CostOption.Sources` is the menu of generators;
which subset gets used is the player's decision, and the engine cannot collapse
it in advance.

When one payment covers simultaneous resource costs, `CostOption.Components`
keeps those costs separate and `Decision.Allocations` assigns individual icons
from the chosen generators to them. The same field carries a wild resource's
declared type and identifies which icons were actually paid when a generator
produced excess. A simulation policy may use `ResourcePayment.Allocate` to make
one deterministic choice; the engine never applies that helper to a client
command, because the choice belongs to the player.

`OrCost` is additive rather than a replacement, and that is load-bearing:
flattening "a mental resource *or* two of any type" to a bare `2` is what
corrupted a corpus during MARVEL-158, because the payer met the number with
resources of the wrong type and the ability failed mid-resolution.

### Prompt kind is an enum the engine already has

MARVEL-41 asks for enough context to tell a mid-resolution prompt from a
turn-level one. `ability_type` already is that, and it does not have to be
inferred from a trigger name:

| kind | share |
|---|---|
| `Normal` | 90.4% |
| `Response` | 6.4% |
| `Interrupt` | 3.2% |
| `ForcedInterrupt` | 0.1% |

All four were observed. None is speculative.

### Cancellable matters more than it looks

**34.8% of prompts offer exactly one affordance**, and **81% are cancellable**.
Without a cancellable flag a client cannot tell "your only move" from "your only
move, or pass" — and those are different screens.

### `Legality` survives, with a caveat about the evidence

`failure_reason` was **not observed once** in 6,351 options, which looks like
grounds for dropping it. It is not.

The mechanism is real and predates this work: `Effect.failures` is set for
"pay cost, need 3, but only have 2", failed target checks, out-of-play sources
and more, and the Python client already greys options out on exactly this —
`BotOption.is_selectable` is defined as `failure_reason == ""`. What the census
shows is that a bot which plays what it can afford does not surface many. **Treat
the zero as a gap in the sample, not in the engine.**

`target_must_include_traits` is in the same position: rare by construction — the
engine comment names one card — and absent from a 30-game sample rather than
absent from the game.

## The shape

`src/Marvel.Rules/Prompts/`.

```
Prompt        Player, Kind, Trigger, Label, Cancellable, Affordance[]
Affordance    Id, Verb, AnchorId, AnchorPlayer, Label,
              TargetRequest?, CostOption[], Illegal?
TargetRequest Legal[], Min, Max, Groups[][], MustIncludeTraits[], Rule, IsSearch
CostOption    Target, Cost, Rule[], OrCost, OrRule[], ResourceSource[],
              VariableRequest[], ResourceCost[]
ResourceSource Effect, Generates
VariableRequest Name, Min, Max
ResourceCost  Cost, Rule[]
Decision      Affordance, Targets[], Resources[], Values{}, ResourceAllocation[]
ResourceAllocation Source, Cost, PaidAs
```

Two constraints carried over from the event stream, for the same reasons:

**Wire types.** Integers, strings and lists of them. A prompt crosses a socket
when the server is hosted, and a live card reference would let the view layer
read hidden state through a field that was only meant to say what is clickable.

**The label is unchanged.** MARVEL-41 pins the domain-level label, and the spec
suite depends on it. `AnchorId`, `Targets` and `Costs` are additions beside it,
never a replacement for it.

## The engine

```
(state, input) -> (state, Prompt?, GameEvent[])
```

The two sides are deliberately asymmetric. A prompt is **absent** when the game
is over, and never empty — a decision with no options is not put to a player. The
event list is very often empty: 35.3% of recorded steps change no state at all.

A cost of X adds a `VariableRequest` beside its resource sources, and the
answer carries the chosen value in `Decision.Values`. This is an engine wire
choice: the rulebook requires X to be defined before payment and modifiers,
but does not define a command format. Keeping the value separate from the
selected generators means overpayment cannot silently redefine X.

Likewise, resource allocation is separate from generator selection. One
double-resource generator can pay one icon into each of two simultaneous costs,
while an excess icon or a wild declaration can change a card effect even though
the generator ids stay identical. `Decision.Allocations` preserves that answer.

## Reproducing the numbers

The census tool is gone and these numbers are not re-runnable.
`tests/Marvel.Rules.Tests/Prompts/` states the shape rules that follow from
them, on data small enough to read, and those are what holds the design.

## Verified against the corpus

MARVEL-164. Keeping eight of fourteen fields is a bet: that the six dropped
carry nothing a player needs, and that the eight carry everything. The corpus
settles it, because it recorded every input a bot actually chose.

> For every recorded step: the input the corpus holds must be expressible using
> only the `Prompt` the affordance model would have carried.

The verification replayed recorded scenes and, at every decision, projected the
rendered options down to the eight fields and nothing else — never falling back
to the effect behind them, which would have measured resolution rather than the
model.

**58 scenes, one from each shard. 6,554 steps: 5,809 choices, 745 declines.**

| | | |
|---|---|---|
| 1 | the chosen effect is in the offered list | **100%** (5,809/5,809) |
| 2 | targets are in `TargetRequest.Legal` | **100%** (4,147/4,147) |
| | the count is inside min/max | **100%** (4,137/4,137) |
| | the selection is inside a `Group` | **100%** (10/10) |
| 3 | resources are in `CostOption.Sources` | **100%** (922/922) |
| | declining was offered | **100%** (745/745) |

Level 3 is the one that mattered. Only 7.3% of priced affordances have a single
way to pay, so a generator set narrower than reality passes levels 1 and 2 and
still leaves a client unable to express a legal payment. The engine resolves a
recorded resource against the effect's own
`checker.cost_for_different_target.GetAllPayEffects()`; the model carries
`target_payment`. Nobody had checked that those agree. They do.

### `Id` is a handle, not a name

Nine of 5,809 recorded inputs named an effect id the offered list did not have.
That is not a missing affordance — every one of the nine was **exactly 25 too
high**, all under `WhenResolveSpecialAbility`, in four scenes across four
different campaigns.

Effect object ids are allocated per session and a recording is a different
session, which is why the engine re-resolves a recorded input through
`CommandDescriptor.FindNewEffectId` rather than trusting the number it wrote
down. In that corpus, `(AnchorId, Verb)` resolved all nine **uniquely**. The
engine's multiplayer prompts need two more existing fields: `AnchorPlayer`
distinguishes who accepts an implied request to use a shared encounter action,
and `Label` distinguishes multiple actions on one card.

The durable selector is therefore `(AnchorId, AnchorPlayer, Verb, Label,
Occurrence)`, where `Occurrence` is the zero-based position among exact
four-field matches. Repeated choice nodes can be identical on every public
field, so their authored order is the remaining discriminator. This is an
engine wire-format choice; the rules do not define persistent command
identifiers. A consumer persists that selector, never the id, and rejects an
occurrence that is absent from the rebuilt prompt.

### A grouped selection is not a count

`TargetRequest` said in prose that `Groups` is authoritative when present. It
did not say that `Min` and `Max` then become unfollowable, and they do:

> **Explosive Arrow** — *Hero Action: Exhaust Hawkeye's Bow and choose a player
> → deal 3 damage to the villain and each minion engaged with that player.*

Played against a player with one minion. Two groups were offered,
`[villain, minion A]` and `[villain, minion B]`; the flat range said `[3, 3]`,
because three cards were in the pool; the legal selection had two. **Two of the
ten grouped selections in the sample had a size the flat range forbids.** A
client that enforced both would reject legal play.

The rule is now executable rather than described — `TargetRequest.Allows` — for
the plain reason that the prose was already there and this happened anyway.

### `Legality` is still unobserved, at three times the sample

`failure_reason` did not appear once in **19,103 rendered options**, and neither
did `target_must_include_traits`. That is not new evidence in a new direction —
it is the [same zero](#legality-survives-with-a-caveat-about-the-evidence) from
the census, at 3x the sample and drawn differently: real corpus games rather
than bot games.

The reading is unchanged, and so is the reason. The mechanism is real and
predates this work; a bot that plays what it can afford does not surface it.
What has changed is how much sampling now stands behind "gap in the sample, not
in the engine" — enough that anyone proposing to delete `Illegal` should be
required to produce a case rather than a count.

### Reproducing

That verification is not re-runnable; the scenes and the harness are both gone.

## What is not settled here

- **Whether the offered list is *complete*.** This proves every input the bot
  took was expressible. It cannot prove that some legal action the bot never
  took is in the list, because the corpus records choices and not the sets they
  came from. That is a spec question, not a corpus one.
- **Ordering.** The census does not check whether affordance order is stable or
  meaningful. It has to be stable for replay; whether it is meaningful for
  display is a view question.
- **Grouping for display.** Which affordances belong to the same card, and how a
  client stacks them, belongs to `Marvel.View`.
