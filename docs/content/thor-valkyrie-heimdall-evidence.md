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

## Independent actual-row review

Reviewed candidate `0bde0100` in the isolated `review/valkyrie-heimdall-actual`
tree. The two rows agree with generated printed 06012/06020. Valkyrie's entry
response has no response resource cost; its bonus depends on the original card
price, and its minion selector includes other players' minions. Heimdall's
optional response retains three physical top cards and permits a chosen discard
and return order. `rr:response`, `rr:wild-resource.1`, `rr:look-looked-at.1`,
`rr:discard.5` and vendored `ruling:2001c6705b63bf4f` support those boundaries.
The vendored MarvelCDB FAQ lists both faces as queried without card entries.
No authority or production behavior finding was identified in this scope.

The reviewer inspected the actual payment, no-price reentry, no-minion,
foreign-minion/invalid-target retry, short-deck/reset and second-seat privacy
tests. Synthetic opposite response costs remain separately identified as
mechanism evidence: they are not a feature of printed Valkyrie.

`ThorEntryReplayReviewFactory` uses the existing `IDurableGameFactory` seam,
the full authored book and a deterministic prepared Core board. It does not
admit Thor or establish public setup. New actual-row tests use composer-created
payment and verify both energy- and physical-declared doubled wilds across
restart at entry response and minion selection. Heimdall restarts at its discard
and order prompts, rejects a duplicate physical card without changing the save,
then returns the chosen physical cards. Stored-session JSON round trips into a
fresh store, events are compared through the durable journal, and resumed
digests, complete RNG states, consumption and pending prompts match an
uninterrupted control. Final completed traces restart again.

Review evidence lives at `C:/Users/mggar/Source/thor-vh-review-evidence/`.
`review-server.trx` records 12 passing new actual-row, existing synthetic
payment/inspection and citation checks. The review data mutants change only
Valkyrie's energy predicate to physical and Heimdall's count to two;
`actual-mutants.trx` records two intended failures on physical-price damage and
looked-at group size. The energy-positive case still passes because its price
also includes a physical icon; the physical-only negative control distinguishes
the wrong predicate.
Restored-source runs are recorded separately in `restored-server.trx`,
`review-content.trx` and `review-privacy.trx`.

Two initial review-fixture mistakes are retained in logs: the helper initially
reused an older store after restart, and an assertion compared a private
`AreaReordered` event with filtered client events instead of the durable journal.
Neither is an engine finding. Native interaction, complete pack admission,
historical compatibility and final combined-branch certification remain outside
this bounded review.
