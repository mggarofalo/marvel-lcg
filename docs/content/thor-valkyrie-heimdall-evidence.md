# Valkyrie and Heimdall actual research rows

Candidate base: `46827c67`; branch `feat/thor-lightning-valkyrie-heimdall`.
This packet adds exactly 06012 and 06020: 24 Thor research faces, 600 total
before other independently authored packets integrate. It changes no public
admission, engine, setup, version or native interaction behavior.

Printed authority is the joined generated `datasets/cards/cards.json`:

- Valkyrie: optional entry response deals 2 damage to a minion, or 3 when an
  energy resource paid for that physical card. The existing original-entry
  receipt owns the bonus, including declared wilds and the entered incarnation.
- Heimdall: optional entry response looks at the top 3 encounter cards,
  discards one, and returns the others in any order. Vendored
  `ruling:2001c6705b63bf4f` confirms only the acting player looks. The existing
  bounded inspection transaction retains the exact physical group.

Actual full-book tests cover energy/physical prices, wild declared energy or
physical, no-price reentry, another player's minion, rejected villain selection,
optional decline and no minion. Heimdall covers 0/1/2/3/4-card decks, paid entry,
second-seat response, explicit look, rejected out-of-group answer, chosen discard
and return order, short-deck reset, decline and prompt-scoped restricted privacy.
These are nonpublic supported-board tests; they do not establish pack admission.
Existing `AllyEntryPaymentTests` separately covers opposite response resource
costs, excess icons, retained generators and ally-limit suspension.

Evidence is outside the repository at
`C:/Users/mggar/Source/thor-final-three-evidence/`:

- `baseline.trx`: 48 content/card-data/citation checks pass.
- `privacy.trx`: 3 actual privacy/citation checks pass.
- `mutants.trx`: changing Valkyrie's paid-resource predicate Y to R and reducing
  Heimdall's looked-at group from 3 to 2 yields four intended assertion failures
  (two physical-price cases and two three-card-group cases), nine passing cases.
- `restored-build.log`: forced non-incremental Release rebuild succeeds with
  zero warnings/errors; `restored.trx`: all 48 checks pass after restoration.

Final additional checks: `final-content.trx` passes 725 tests (the broad Thor
filter also matches Authored test names); `cards-check.log` confirms generated
card files match. That broad run overlapped the normal commit architecture hook,
so final combined integration should provide the serialized regression record.
The normal hook fails on the inherited base finding
`src/Marvel.Rules/Timing/Occurrence.cs:30: SL003: Occurrence=303`;
`commit.log` records 65 passing architecture checks and that one failure.
No hook bypass or source-layout baseline change is included. The staged packet
requires the parent-integrated engine decomposition before a normal commit.

Initial hand-authored public answers omitting resource allocations were invalid
fixtures. Explicit legal allocations pass; those failures are not engine gaps.
The initial one-card Heimdall expectation was also corrected to ordinary
encounter-deck reset, and optional response context may contain its source card.

Lightning Strike remains withheld. The proposed exact row is preserved as
`lightning-proposed-unlowerable.json`, with executable source
`ThorLightningCompositionRedProof.cs` and `lightning-selector-red.trx`.
The intended lowering test fails because `villainAndMinionsEngagedWithYou` is not
a supported card query. Existing selectors cannot form that combined group in one
simultaneous damage instruction. Sequential damage would violate the required
damage-step barriers. An independent engine packet must supply the exact group
before actual Lightning Strike authoring and composition proof. The proposed row
uses printed 1 plus X energy, no attack label (faq:06006), and conditional
Aerial ignore-Tough damage; it is not in the authored book.

Independent content review, integrated save/replay and native evidence remain
separate. No broader readiness or product acceptance is claimed here.
