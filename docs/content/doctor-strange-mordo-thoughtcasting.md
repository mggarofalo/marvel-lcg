# Baron Mordo and Thoughtcasting actual rows

Base: `24285d394b0988ea8a4bff1059366a90c5957d85`; branch
`feat/strange-mordo-thought`. This packet appends only 09028 and 09031.
The ability book contains 684 rows and exactly 39/40 Doctor Strange faces;
Counterspell (09030) remains unauthored. No engine, setup, generated-card or
public-admission change is included. This is research content, not a complete pack.

Printed authority is generated `datasets/cards/cards.json`. The pinned
`datasets/marvelsdb/pack/drs_encounter.json` records one physical copy of each
face. The exact inventory test reconciles all 39 authored Strange faces against
all generated 09 faces except 09030, and verifies both quantities and no new
starting counters. A raw JSON decoder comparison against the base verifies all
682 predecessor objects remain byte-identical and only these two objects append.

Current RR 1.8 `attack-enemy-activation.5` equates “when [enemy name] attacks”
with attack initiation. `status-cards.2` gives status-card abilities priority;
`player-deck.1/.3/.4` governs immediate reset and the empty-deck boundary.
`printed` defines physically printed characteristics; `choose-game-element.1`
assigns the resolving player's choice; `non-numerical-variable` makes undefined
X zero. The vendored MarvelCDB FAQ entries and rulings search found no entries
naming 09028 or 09031; queried identifiers alone are not rulings.

09028 discards the attacked player's exact top card and reads only that discard's
resource receipt. Physical stuns, energy deals two extra damage, mental confuses,
and wild independently applies all three; the ordinary two-damage attack still
resolves. 09031 chooses a highest-cost card in the resolving player's own hand,
retains that exact physical binding after discard, and reads `sumPrintedCost:
"chosen"` for damage in hero form or main-scheme threat in alter-ego form.

Canonical fixture migration removes these two programs from
`doctor-strange-final-nemesis.json`, leaving only the explicit Counterspell
candidate. `DoctorStrangeNemesisFixture.Board` loads the canonical runner;
only its Counterspell helper installs the candidate. The actual tests are now
`DoctorStrangeMordoCardTests` (seven cases) and
`DoctorStrangeThoughtcastingCardTests` (ten cases). The retained
`doctor-strange-thoughtcasting-receipt-probe.json` remains an explicit diagnostic:
direct discard does not supply `printedCostDiscarded`, so that alternative is
not the production interpretation.

Mordo cases distinguish all resource branches, wild, older-discard and other-seat
isolation, stun priority, last-card reset with its encounter penalty, and an
empty deck with no discard (ordinary attack, no status, no reset or penalty).
Thoughtcasting cases exercise both forms, both physical tie choices, resolving
seat 1 while first player is 0, another seat's higher-cost card, a three-resource
payable discount without altering printed ranking/penalty, empty hand, zero cost
and undefined X. Existing assertions are retained; actual inventory migrations
keep Counterspell as the unauthored negative control.

Evidence logs are external under `C:/Users/mggar/Source/` with prefix
`marvel-strange-mordo-thought-`:

- `mutant-mordo.log`: actual row's energy damage 2→1 rejected, 3 failed / 4 passed.
- `mutant-thought.log`: actual row's `maxBy`→`minBy` rejected, 4 failed / 6 passed.
- `restored-build.log`: nonincremental Release Content build after both restores;
  zero warnings and errors.
- `restored-affected.log`: whole Doctor Strange classes except the deliberately
  red Counterspell candidate class; whole four inventory-affected Black Widow
  classes, WreckingDiscardedFactsTests, EventPlayCancellationTests, `~AbilityData`
  (the actual generated class names), and RuleCitationTests. Final result: 269 passed, zero failed or skipped. The first broad run caught one stale reprint exact-set
  assertion; that explicit expected set now includes only the two new faces.
- `offline-cards.log`: offline generated-card `-- check` gate.
- `commit.log`: normal pre-commit hooks, with no bypass.

Known prerequisite scope remains separate: MARVEL-835 covers the four existing
Counterspell candidate failures: attached-seat event cancellation in hero and
attached-then-flipped alter-ego, stunned attack-event priority, and Master of the
Mystic Arts cancellation. They are retained unchanged. Their cancellation before
Then lacks completed-result semantics at this base; no weakening, skip or repair
is part of this packet. Full Content and public setup/save/restart/whole-pack
verification are deferred until that separately owned prerequisite is integrated.
Independent review and subsequent integration remain separate acceptance steps.

## Independent actual-row review

Reviewed source `70d66010aed1c6fd6b7883fdc2fee869431513cc` on
`review/strange-mordo-thought`. Verdict: accept the actual 09028/09031 research
rows, their fixture migration and bounded inventory changes. This verdict excludes
the retained Counterspell prerequisite failures (MARVEL-835), public admission,
UI/product acceptance and whole-pack setup/save/restart verification.

Authority review independently compared the generated printed text, RR 1.8 attack
initiation, status priority, player-deck reset, printed values, choosing player
and undefined X clauses, plus the local FAQ/rulings. Both target codes were queried
in the vendored FAQ and have no entries. A raw JSON decoder comparison against
`70d66010^` confirms 682 byte-identical predecessor objects, only 09028/09031
appended, 684 total rows and 39 Strange faces with 09030 still absent.

Seven added cases retain every source assertion. Four Mordo resource cases use
the generated RY, RB, YB and BG encodings: distinct printed icons combine their
effects independently, and a mental-plus-wild card still applies all effects.
These cards supply printed discard facts only; their unrelated abilities are not
claimed as implemented. The fifth Mordo case empties seat 1's deck with a physical
card above an older wild discard, proves both exact physical cards enter the
reset deck, the exact encounter top is dealt only to seat 1, no wild effects leak,
and the ordinary attack completes without ending the game.

Two Thoughtcasting cases give seat 1 one unique highest-cost physical card and
a cheaper remaining card, in both forms, while first player is seat 0. They prove
that discarding the four-cost card leaves its four-point printed penalty bound,
despite its payable discount to zero, another player's card and an older discard.
A compiled actual-row mutant replaced the bound `chosen` amount with the highest
cost remaining in the resolving player's hand after discard. All ten original
Thoughtcasting cases passed that mutant; both added cases failed for the intended
four-versus-two penalty. The actual book was then restored byte-identically and
rebuilt nonincrementally before the restored verification.

Independent logs under `C:/Users/mggar/Source/`, prefix
`marvel-mordo-thought-review-`:

- `raw.log`: predecessor raw-object, exact appended-face and local FAQ checks.
- `baseline.log`: 24 Mordo/Thoughtcasting/independent cases passed.
- `mutant.log`: compiled binding mutant, 10 passed / 2 failed as described.
- `restored-build.log`: nonincremental Content build, zero warnings/errors.
- `restored-affected.log`: 276 passed, zero failed/skipped; whole Doctor Strange
  classes except Counterspell candidates, the four affected Black Widow classes,
  WreckingDiscardedFactsTests, EventPlayCancellationTests, `~AbilityData` and
  RuleCitationTests. The known Counterspell failures are explicitly excluded,
  not treated as a passing prerequisite.
- `offline-cards.log`: generated cards/search match the offline generator.
- `commit.log`: normal pre-commit hooks for this review commit.

No engine, authored ability-book, generated-card, admission or Counterspell
candidate changes are included in the review commit. No full Content or native
run is claimed.
