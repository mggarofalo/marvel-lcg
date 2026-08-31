# Canonical Core scenes

Tracked as `MARVEL-302`. `Marvel.Content.Behavior.CanonicalCoreScene` constructs
the legal state used by executable behavioral transcripts.

## The boundary

A scene begins with `Dealer.DealOrder`, `Blueprints.From` and `WorldSetup.Deal`.
Those existing components remain the authority for starter-deck membership,
signature sets, copy limits, encounter composition, setup order and seeded
shuffles. A complete card-ability interpreter is required, so mandatory Core
scenario setup cannot silently become a no-op. The scene constructor does not
have another card factory or a smaller deck format.

An arrangement selects a physical card by printed face id plus zero-based copy
number. Selection is stable by `object_id`, independent of the card's current
zone. The typed operations can:

- stack a player or encounter deck, optionally moving the rest of a player's
  current draw pile to their discard pile;
- move an existing card to a legal player, encounter or in-play area;
- attach player upgrades and encounter attachments to an in-play host;
- set legal damage, scheme threat, printed counter types, form and readiness;
- create a tough, stunned or confused status through the rules' status-card
  operation.

Every operation is followed immediately by a whole-world invariant check.
Every dealt card, and every status card explicitly created by an operation, must
occur in exactly one area. Object ids remain contiguous, ownership remains what
the legal deal assigned, and matching unique cards cannot both be in play.
Zone-specific operations additionally check printed kind, owner, seat and host.
Cards entering play use the engine's ordinary entry lifecycle, including
starting threat, Uses counters and Toughness. Upgrade and attachment hosts are
checked against printed targeting and maximums before any area is created.

The constructor chooses this small operation vocabulary. The Rules Reference
does not define a test-fixture API. What the rules do decide is whether the
state produced by an operation is possible.

## Failures

`CoreSceneConstructionException` names both the behavior obligation and the
operation rejected by the first invariant. An impossible state never becomes a
plausible fixture:

```text
behavior:setup:hero:iron_man:hero-deck; move-card:
no copy 0 of printed face '01006' exists in this deal
```

That is the Aunt May distinction in structural form. A Spider-Man/Iron Man
game contains Aunt May, but moving her to Iron Man's hand fails ownership; an
Iron Man-only game contains no Aunt May to select.

## The empty-deck boundary

`StackPlayerDeck(..., DiscardOthers: true)` moves every unselected card still
in the draw pile to that player's discard pile before stacking the named cards.
Cards already in hand or play stay there. A one-card draw pile therefore remains
a complete legal forty-card deck distributed among legal zones. It does not
model deck exhaustion by replacing the player's deck list with one card.

The later transcript runner owns decisions. A `Given` uses these arrangements;
each `When` advances the ordinary engine by one recorded decision. This keeps
fixture construction from bypassing the very timing or resolution branch a
scenario claims to test.
