# Yellowjacket's Plan actual card

The canonical `12029` row follows the printed card in
`datasets/cards/cards.json`: discard encounter cards until an Ant-Man nemesis-set
card is discarded, then reveal that physical card. MARVEL-883's reviewed set
matching mechanism owns the bounded search, exact physical result, active deck
routing and nested reveal continuation. Current `rr:encounter-deck.1` and `.2`
require a reset and acceleration at exhaustion without continuing this discard
instruction into the refreshed deck. No card-specific official ruling is present
in the vendored corpus.

All 753 previous ability objects and their raw prefix are preserved. The book
has 754 rows and 31 authored Ant-Man faces. The pack remains incomplete and
publicly excluded. Six actual-card tests schedule the full encounter-reveal
procedure: Yellowjacket engages either revealing player, each preferred/fallback
Size Increase attachment resolves with three counters, and both last-card and
no-match exhaustion finish with one reset and the correct physical result.
The same-trait Rhino minion is deliberately skipped. Tests distinguish the two
physical Size Increase copies and assert outer treachery cleanup.

These actual-row tests cover Yellowjacket and Size Increase. Actual Tech Theft is now authored and its full Plan-to-Tech reveal and blanking
are verified in the [Tech Theft packet](tech-theft-actual.md), independently
reviewed at `d0e7234a`. Recursive same-title searches
and all four printed card kinds have separate synthetic prerequisite evidence;
that does not establish a complete actual nemesis journey.

Windows Release verification passes all 98 selected Content/data, nemesis,
search and nested-reveal tests, including the six new actual-card cases. A valid
runtime JSON mutation changing the target set from `ant_nemesis` to `rhino`
fails all six actual-card cases on physical result and reset assertions. The
exact book bytes were restored; a forced non-incremental build and the same
98 tests pass. This is an authored-row mutation, not a compiled engine mutation.
Logs are `Source/evidence/yellowjacket-plan-{focused,set-mutant,restored-build,restored-tests}.log`.
The 753 prior objects and their raw prefix are unchanged, independently checked
by `yellowjacket-plan-book-prefix.json`. Commit uses normal repository hooks.
Independent review and final full-pack/public/native/replay verification remain.

