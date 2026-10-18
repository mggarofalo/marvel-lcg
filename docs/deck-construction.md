# Deck construction assessment

`DatasetGameFactory.AssessDeck` assesses an editable deck for one supported hero.
It returns immutable diagnostics with stable codes, affected printed face IDs,
messages, and quantities when applicable. An empty face identifies a whole-deck
constraint. Code spelling and ordering are repository choices. This method is a
host API; it is not yet exposed through the client protocol or Godot interface.

`CoreDeckAssessment` checks the submitted faces against the printed catalog and
the admitted ability pool. Malformed, unknown and unsupported faces remain
diagnostics. When these prevent rules assessment, the counted size is unknown;
the assessor never silently evaluates a reduced substitute deck.

`PlayerDeckAssessment` owns ordinary Core construction rules: exact signature
face quantities, counted size, player-card kinds, aspect membership, title copy
limits and matching unique cards including the selected identity. Its inputs
must already be admitted printed facts and the authored hero recipe. It does
not support expansion deckbuilding exceptions. Table-wide identity conflicts
remain setup constraints in `DeckConstruction`.

`PlayerDeckAdmission` uses that assessment and preserves a valid recipe's exact
ordering. The host assesses explicit decks before allocating a world. Existing
session recipes capture their own immutable card sequence; assessing or editing
a draft does not modify a game or consume its randomness. No protocol, save,
RNG, allocation-order or digest format changes are part of this increment.

## Authority and evidence

The glossary entry `rr:deck-customization` redirects to Appendix I of Rules
Reference v1.8, page 50. Its player-deck clauses were checked against the rendered
source PDF (SHA-256
`7254a7e9e891ce358e87a1faad5cc3a603c5826684287a1feefb8bb309c76595`).
The matching Core rules are readable offline in
[Customization Rules](../datasets/rules-packs/mvc01/customization-rules.md).
Uniqueness is directly citable as `rr:unique-icon.2`, and printed limits come
from the generated card catalog. Appendix I does not yet have clause IDs in the
vendored Rules Reference index; the redirect citation is not a claim that its
destination has been ingested. That narrow authority intake remains tracked in
MARVEL-391.

`PlayerDeckAssessmentTests` checks all five starters, exact Black Panther
signature variants, aggregate corrections, size boundaries and content errors.
`DeckAssessmentTests` checks host rejection without persistence and repeatable
seeded creation after repeated preview. `ExplicitDeckSessionTests` retains the
ordered-deck wire and durable-replay checks.

The next delivery connects this foundation to JSON and MarvelCDB URL import,
local editable drafts and the deck browser/builder. Passing these engine tests
does not establish that those player-facing workflows exist or are accepted.
