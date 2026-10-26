# Ordered deck-bottom movement prerequisite

MARVEL-724. Base revision: `de1e4e42b90330c45fa5c96f8f84d63a40aa4b5c`.

Printed authority: joined `datasets/cards/cards.json` 05006, Aamir Khan:
place one card from your discard pile on the bottom of your deck, then draw one.
Vendored MarvelCDB FAQ has 05006 in its acquisition inventory; no Aamir-specific
entry was found in the vendored rulings. `rr:then` requires the predecessor to
resolve fully.

The generic typed `placeOnBottomOfDeck` instruction takes a selected card and uses
the resolver's deck. `CardPlacement.MoveToBottom` reuses `World.MoveToTop`
movement validation and changes only the landing order. The placement collaborator
keeps `World` at its existing size without adding a layout exception.
Semantic movement supplies an observed completion
receipt. No card ID appears in production dispatch. No ability-book, setup or
public-admission data changes are included. The test-local composition exercises
selection, exhaustion, placement and dependent draw, not complete Aamir admission
or its alter-ego restriction.

The focused Content run passes 117 cases, covering the six new cases plus existing
typed movement and shuffle resolution controls. New cases distinguish bottom from
top placement, preserve the other cards' order and physical ownership, check no RNG
advance, draw after placement with zero/one/three original deck cards, exercise
normal deck reset, reject a foreign discard atomically and retain a valid retry,
and suppress an action with no discard target.

A temporary mutant replacing bottom placement with top placement compiled and
failed both nonempty-deck characteristic cases. The mutant was repeated against
the extracted placement collaborator. Source bytes were restored and the focused
run rebuilt the restored implementation and passed again. Logs:
`%TEMP%/aamir-bottom-extracted-mutant.log` and
`%TEMP%/aamir-bottom-extracted-final.log`. Filter:
`FullyQualifiedName~DeckBottomMovementTests|FullyQualifiedName~TypedMovement|FullyQualifiedName~Shuffle`.
Normal hooks remain enabled. Independent review,
actual Aamir authoring and integrated broad gates remain with the delivery lead.
