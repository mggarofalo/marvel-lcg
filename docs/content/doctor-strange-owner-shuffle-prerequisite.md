# Owner-deck shuffle prerequisite

MARVEL-820 (`4ecb86d4-3e95-4593-813c-48abf19de403`), based on `dd689e71`.
This implements a reusable destination; it does not admit Clea or the Doctor Strange pack.

`datasets/cards/cards.json`, face `09013`, prints: “Interrupt: When Clea is defeated,
shuffle her into her owner's deck.” Rules Reference 1.8 `rr:ownership-and-control`
defines physical ownership by the starting deck; `rr:ownership-and-control.7.2`
returns departing controlled cards to their owner's equivalent out-of-play area.
`rr:interrupt.3` places the interrupt before defeat resolution. `rr:shuffle`
defines randomization; the existing seeded shuffle remains the implementation.

The local MarvelCDB FAQ contains no Clea-specific entry. Vendored rulings
`ruling:266a6f8eea7a213b` and `ruling:91af48d6b34ac91f` concern Operation Zero
Tolerance finding Clea after departure. Both have `rrg_scope: pre-1.5`; neither
supplies a reason to alter current ordinary defeat cleanup or ownership routing.
That scenario interaction remains outside this prerequisite.

The strict instruction is `{"shuffleInto":{"cards":"this","deck":"ownerDeck"}}`.
`yourDeck` still means the resolving player's deck and `encounterDeck` retains its
existing behavior. Destinations use a separate compiled enum rather than broadening
search-area syntax. Unknown destinations fail lowering with their source location.
`ownerDeck` is not a search area, nor an alias for controller or resolving player.

For a multi-card selector, each physical card selects its owner's player deck.
The engine chooses ascending owner order for the shared random stream and retains
the selector's order within each owner group. It moves the group's cards before one
shuffle of that destination. If an owner has been eliminated, their selected cards
move to the shared removed area instead, as `rr:ownership-and-control.7.2` requires.
That area is not a deck: removal neither shuffles its existing contents nor consumes
randomness. Active owner groups retain their ordinary deck shuffle, including in a
mixed selection. Empty selections identify no owner and shuffle nothing.
Scenario ownership and nonexistent player seats raise `RulesNotImplementedException`
before any selected card moves or any shuffle consumes RNG. This is an explicit
unsupported execution boundary, not a general rollback guarantee for committed costs.
Existing movement events, departure checks, physical-card bindings and shuffle
semantics are reused. New owner-deck area projection reports source and player-deck
mutation; the previous destination projections are unchanged.

The initial probe established that foreign-owned Clea with `yourDeck` goes into the
controller's deck while retaining its foreign owner. `ownerDeck` was rejected at
`effect/shuffleInto/deck`. Ordinary defeat cleanup was separately disproved as a
blocker: accepting the same-owner interrupt already leaves Clea in the deck with
one movement; declining puts her in the discard with one movement.

`OwnerDeckShuffleTests` covers exact physical identity against another Clea copy,
same-owner and encounter controls, foreign control, multiple owners, seeded order,
empty selections, mixed valid/invalid owners in both orders, strict lowering,
query purity including full RNG state and pending work, subsequent execution,
and actual optional defeat acceptance/decline for both same and foreign owners.
The direct action fixtures are synthetic capability probes, not printed Clea actions.

Evidence is outside the repository at `C:/Users/mggar/Source/owner-shuffle-*.log`:

- `baseline.log`: one passing legacy destination control and one expected lowering failure.
- `final-focused.log` and `restored-tests.log`: 29 passing tests from the owner,
  deck-bottom and Wrecking selected-deck classes. The attempted `ResolutionStatusTests`
  filter matched no class; the correctly named class is verified separately below.
- `mutant.log`: replacing owner routing with resolving-player routing compiled and
  failed three distinguishing tests (17 passed).
- `restored-build.log`: forced nonincremental restored build, zero warnings/errors.

Normal commit hooks and the final corrected class/architecture checks are recorded
in `owner-shuffle-hooks.log`, `owner-shuffle-final-tests.log` and
`owner-shuffle-architecture.log`. The corrected complete-class selection passes
39 tests; the architecture project passes 129 tests. Broader integration, native/UI and cross-platform
certification remain the integrated delivery's responsibility.

## Independent review of `cf09e73417e1eb289ada51594a0b7211a81fe41f`

The review compares the implementation with `dd689e71`, the printed 09013 face,
and the current vendored ownership, interrupt, shuffle and elimination rules.
Verdict: **blocked on eliminated-owner routing**. The owner bounds preflight
accepts an existing eliminated seat. After actual `Elimination.Eliminate` of
seat 1, a surviving seat 0 can resolve a synthetic owner-deck shuffle on its
foreign-owned ally. The operation emits a movement into, and repopulates, seat
1's removed player deck. `rr:ownership-and-control.7.2` instead requires a
departing foreign-controlled card to be "removed from the game if that player
is no longer in the game". This finding concerns the new destination and does
not propose rewriting defeat cleanup or legacy movement operations.

`OwnerDeckShuffleReviewTests` preserves that counterexample with a matching
active-owner control, physical movement identity and destination assertions,
and RNG observations. It also independently verifies an empty owner's deck
receives exactly the selected card without random draws or player-deck reset,
and that an ambiguous singular deck lookup after an owner-deck shuffle raises
before the source's exhaustion cost. The latter preserves the documented opaque
area-projection boundary; the new destination does not silently offer that
unsupported composition. The original classes cover foreign control, exact
physical identity/incarnation, optional defeat accept/decline, grouped seeded
ordering, both mixed-invalid-owner orders and query purity. Their checks remain
retained; the previous `yourDeck` and encounter projections are unchanged.

Review logs and TRX files are outside the repository at
`C:/Users/mggar/Source/owner-review-*.log` and
`C:/Users/mggar/Source/owner-review-results/`. These are engine capability probes,
not public Clea admission, a played Doctor Strange game, native interaction
evidence or cross-platform certification. No production engine, ability book or
shared fixture is changed by this review. A repair and independent recheck of
the eliminated-owner case are required before accepting this prerequisite.

Final review selection: 58 tests across the complete owner-shuffle, review,
deck-bottom movement, selected-deck projection and both resolution-status classes:
57 passed and the eliminated-owner case failed as described above.
The architecture project passed all 129 tests. Logs: owner-review-final.log and
owner-review-architecture.log outside the repository. Normal commit-hook
evidence is retained in owner-review-hooks.log. No repair has been applied.

## Eliminated-owner repair from review commit `023bc113`

The repair checks participation for each owner group after validating every owner.
An eliminated seat routes to the same shared `RemovedArea` used by elimination;
the existing movement event names the actual destination and exact physical card.
The owner-deck area-mutation query includes this possible removal destination.
Legacy `yourDeck` and encounter execution and projection keep their controls.
This changes no generic defeat cleanup, production ability row or product admission.

`OwnerDeckEliminationTests` adds three-seat synthetic boards with actual elimination
of owner 0 or owner 1, both before and after the active owner in deterministic group
order. Multiple foreign-controlled cards exercise retained selector order. Tests
compare complete state and RNG with an unprobed control, prove repeated offers
preserve pending work, and preserve the removed area's existing contents and order.
The independent review's active-owner and empty-owner-deck controls remain intact.

Evidence is outside the repository at `C:/Users/mggar/Source/owner-elimination-*.log`
and `C:/Users/mggar/Source/owner-elimination-results/`:

- `baseline.log`: reviewed eliminated-owner destination fails; three controls pass.
- `focused.log`: all 70 tests in the complete owner-shuffle, elimination, deck-bottom,
  selected-deck and resolution-status class selection pass.
- `routing-mutant.log`: routing eliminated owners into their deck compiles and fails
  three distinguishing tests; four controls pass.
- `rng-mutant.log`: shuffling the removed area compiles and fails three tests;
  four controls pass. This also detects reordering existing removed cards.
- `restored-build.log` and `restored-tests.log`: nonincremental restored build has
  zero warnings/errors; 72 tests pass, including rule-citation validation.

Architecture and normal commit-hook results are retained in `architecture.log` and
`hooks.log`. Independent recheck and integrated platform, native/UI and
product-admission evidence remain separate delivery obligations.

## Independent closeout recheck of `80ddfbbae7fc1af6c28ea279c0b054f8381059bd`

Verdict: **accepted for this engine prerequisite**. The repair was compared with
review commit `023bc11374120e213f7b4dc0cf347d8d0f3e9f6a`, and the original
feature `cf09e73417e1eb289ada51594a0b7211a81fe41f` with `dd689e71`.
Printed face 09013 and current `rr:ownership-and-control.7.2` require owner
routing and removal when that owner is no longer in the game. The reproduced
eliminated-owner counterexample now passes without repopulating that owner's deck
or consuming shuffle draws. Active and mixed-owner controls retain physical
movement identity, selector order, deterministic owner order and seeded RNG state.
The existing compiled routing and removed-area shuffle mutants each fail three
distinguishing tests; their restored nonincremental source verification is retained
above. No further production repair is required by this recheck.

Additional independent tests preserve the invalid-owner preflight with an actual
eliminated owner present: neither scenario ownership nor a nonexistent seat can
partially remove valid cards or advance RNG. Both the direct and nested shuffle
reject a subsequent ambiguous singular player-deck lookup before exhaustion,
preserving card state, RNG and outstanding work. A matching nested shuffle with
an independent encounter-deck lookup executes, pays exhaustion and emits both
physical movements. These are synthetic engine probes, not printed Clea actions.

Evidence is outside the repository at `C:/Users/mggar/Source/owner-recheck-*`:

- `final.log` and `results/final.trx`: 73 passing tests across the complete owner
  shuffle, elimination, deck-bottom, selected-deck projection, resolution-status
  and rule-citation classes. The three `WreckingSelectedDeckTests` cases omitted
  from that filter pass separately in `selected-deck.log` and
  `results/selected-deck.trx`, for 76 total passing cases at the reviewed source.
- `architecture.log`: all 129 architecture tests pass.
- `hooks.log`: normal commit-hook evidence for the reviewer tests and this verdict.

The earlier `focused.log` records an initial test assertion failure: it expected
the departed source to remain exhausted. The final control observes the paid
exhaustion event, since departure resets that state. This changed no production
behavior. The recheck changes only reviewer tests and this canonical record.
It supplies no public Clea/Doctor Strange admission, native/UI review or second
platform certification; those remain integrated delivery obligations.

