# Deck browser, builder and import

Open **Deck library · browse, build, import** from desktop setup. The labelled
identity selector comes first. Changing identity replaces the supplied signature
recipe and retains all other draft cards. A new deck starts with that
identity's signature cards. Select **Preview identity** to inspect either form using the Hero / Alter ego tabs.
Card lists show names and quantities; their full procedural previews retain
printed rules, symbols, traits and stats.

Card search supports MarvelCDB field keys, quoted phrases, pipe alternatives,
AND clauses and numeric comparisons. **Filters** contains aspect/type/sort controls and a field/operator/value query builder. Search defaults to playable Core cards. The scope selector also exposes all
research cards, player cards or encounter cards from the complete offline snapshot.
Research presence never admits a card to gameplay. Search-only metadata is generated offline
in `datasets/cards/search.json`; gameplay card bytes and compatibility hashes
remain unchanged. The extractor's existing `check` verifies both outputs.

**Find published decks** searches MarvelCDB online by hero, aspect, tag, author,
name, included cards and allowed packs, with popularity/date/activity/likes/
author-reputation ordering and pagination. The site supplies current heroes and
pack choices. Selecting a result imports a local snapshot through the same
bounded importer as URL import. Account-specific favorites and private decks
are not part of public search. The search adapter parses HTML without executing
scripts; unexpected pages produce an error rather than replacing a draft.

Save, reopen, duplicate and delete local drafts, including unfinished or
invalid decks. Files live in Godot's `user://decks` directory; an explicit
`MARVEL_DECK_LIBRARY_DIRECTORY` overrides it for isolated tests. Saves replace
one file atomically and reject stale revisions. A failed save retains the open
draft. Switching decks or returning to setup saves edits automatically; no
discard confirmation is required. An untouched initial starter is not an edit.
Library files are independent of server saves and replay.

**Import JSON** accepts MarvelCDB's `hero_code` and `slots` format; **Export JSON**
writes those exact face quantities. **Import URL** accepts public HTTPS
`marvelcdb.com/decklist/view/{id}` links and public decklist API links. Private
`/deck/view` links require a JSON export. Requests are cancellable, limited to
20 seconds and 1 MiB, and reject redirects. Acquisition never runs in gameplay.
Malformed JSON or quantities fail without replacing the draft. Unknown and
unsupported card codes remain visible for correction. Nonempty special
deck-limit slots are rejected explicitly; expansion exceptions are unsupported.

The seat selector and **Use deck** capture a valid immutable recipe for setup. Subsequent edits
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
matching clients and hosts. This unmerged protocol 34 increment also adds
public catalog previews, bounded 200-card research pages and search metadata.
It requires matching hosts and clients; earlier PR builds are not compatible.
Save, RNG, allocation-order and digest formats are
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

## Search verification and sources

The field vocabulary and query operators follow the public
[MarvelCDB card search](https://marvelcdb.com/search) and its
[search implementation](https://github.com/zzorba/marvelsdb/blob/master/src/AppBundle/Services/CardsData.php).
Published search uses the public [deck search form](https://marvelcdb.com/decklists/find).
The included-card picker uses this repository's offline snapshot; current online
hero and pack choices can include newer content absent from that snapshot.

`CardSearchTests`, `PublishedDeckSearchTests`, `DeckResearchTests` and
`DeckPreviewResponseTests` cover query syntax, response bounds, preview admission
and the research/runtime boundary. Transport tests carry research pages over
both embedded and socket connections. Manual mutations cover numeric boundaries,
exclusion, signature replacement, preview authority and public response admission.
Rendered search-to-import probes additionally exercise the live public site; these
are manual acceptance evidence, never network-dependent CI tests.

### Fast local editing

The editor keeps deck name, identity, count and validation in a fixed header.
The pool, deck and full card preview occupy independent full-height panes;
editing never scrolls the identity out of view. Small quantity segments sit left
aligned in a fixed-width column at the right of every row. Bulk actions appear
with selection; a drag shows its proposal in the deck pane.

**Decks** opens saved drafts. **Deck** contains new/duplicate/save/import/export
and clear commands; **Filters** holds advanced search controls. Command menus
open at their trigger; task dialogs support Done/Escape and restore focus.
Import progress and errors stay inside the import dialog. The validity summary
opens complete diagnostics and signature repair. One seat selector and **Use
deck** capture the recipe for setup.

Results and deck rows support Ctrl/Shift multiselection. Ctrl+Shift adds the
anchor-to-click range to the existing selection: click 0, Ctrl-click 2,
Ctrl+Shift-click 4 selects 0, 2, 3 and 4. Search pages contain up to 50 rows;
Select page selects that page. Each optional row offers its accepted total
quantities, including zero: 0/1, 0/1/2 or 0/1/2/3. Required identity cards are
absent from the pool and fixed in the deck. Their full previews remain available.
Persistent row controls preserve focus and avoid rebuilding the table on clicks.
Rapid quantity edits retain independent row intentions and the latest pending
choice for each face.

A read-only host assessment supplies `CanExtend` and, when `IncludeQuantities`
is requested, accepted per-face quantities. The engine accepts a partial deck
below 40 cards but requires every other construction constraint, including the
50-card maximum. These are additive fields in this unreleased protocol 34
increment; matching hosts and clients remain required. Unavailable quantities
are hidden. Zero remains available to correct optional imported cards.
Restore required cards repairs a malformed signature recipe while retaining
optional imports; other construction errors must be corrected before positive
quantity edits become available.

Add selected and drag from results into the deck add one copy per selected face
using the same checked proposal. Drag previews name cards and show resulting
counts; the target explains pending validation, and completion reports added
quantities. Dropping during validation waits for the checked proposal. Edits and
identity changes invalidate pending proposals and drags. Clear selected retains
required signature copies; Clear deck restores the identity's complete signature
recipe. Both are immediate and undoable.
Undo/Redo restore whole local edits, including bulk edits and identity changes,
with up to 100 undo steps. Opening another deck starts a separate history. Saving
does not clear history; captured setup seats and running games remain independent.
