# Deck browser, builder and import

Open **Deck library · browse, build, import** from desktop setup. Search the
host's admitted Core player cards by title, trait or cost, filter aspect and
type, and inspect their printed text. Start from a starter or empty draft,
choose an identity, and add or remove exact card copies. Changing identity
retains the cards and asks the engine for fresh diagnostics.

Save, reopen, duplicate and delete local drafts, including unfinished or
invalid decks. Files live in Godot's `user://decks` directory; an explicit
`MARVEL_DECK_LIBRARY_DIRECTORY` overrides it for isolated tests. Saves replace
one file atomically and reject stale revisions. A failed save retains the open
draft. Library files are independent of server saves and replay.

**Import JSON** accepts MarvelCDB's `hero_code` and `slots` format; **Export JSON**
writes those exact face quantities. **Fetch URL** accepts public HTTPS
`marvelcdb.com/decklist/view/{id}` links and public decklist API links. Private
`/deck/view` links require a JSON export. Requests are cancellable, limited to
20 seconds and 1 MiB, and reject redirects. Acquisition never runs in gameplay.
Malformed JSON or quantities fail without replacing the draft. Unknown and
unsupported card codes remain visible for correction. Nonempty special
deck-limit slots are rejected explicitly; expansion exceptions are unsupported.

**Use for seat 1/2** captures a valid immutable recipe for setup. Subsequent edits
or deletion of the library file cannot change that selection or a started game.
The host validates the captured recipe again when opening the game.

## Shared assessment and protocol

`DatasetGameFactory.AssessDeck` assesses an editable deck for one supported hero.
It returns immutable diagnostics with stable codes, affected printed face IDs,
messages, and quantities when applicable. An empty face identifies a whole-deck
constraint. Code spelling and ordering are repository choices. Protocol 33
exposes read-only `decks` requests: no query returns a public catalog; a
`DeckQuery` returns the host's structured assessment. These requests require no
game or capability and do not create a session. Embedded and socket clients use
the same contract. Every edit invalidates assessment; stale completions cannot
enable a draft for use.

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
a draft does not modify a game or consume its randomness. Protocol 33 requires
matching clients and hosts. Save, RNG, allocation-order and digest formats are
unchanged. JSON slots are expanded in ordinal face-code order because objects
have no sequence contract; local library files preserve exact recipe order.

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

`DeckImportTests` and `DeckUrlImportTests` cover bounded interchange and acquisition;
`DeckLibraryTests` covers revision conflicts and restoration after source deletion;
`DeckWorkspaceTests` covers stale assessment; `DeckResponseTests` rejects malformed
host responses. `DeckCatalogTransportTests` exercises embedded and socket paths.
The native `deck-library` cases at 50%, 100% and 150% use keyboard activation to
edit, save an invalid draft, capture a valid deck and start a game. These are
regression checks, not independent player acceptance. Owner review of browsing,
import recovery, corrections and seat assignment remains required.
