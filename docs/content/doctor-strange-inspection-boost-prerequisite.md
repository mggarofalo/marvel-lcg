# Private encounter inspection numeric boost prerequisite

MARVEL818 (`c8e84411-0d95-484b-9323-2d4bed5c08c2`), based on
`78efa9e0`. This packet adds an engine prerequisite, without authoring 09003,
changing product admission, or claiming native/client acceptance.

## Authority and interpretation

`datasets/cards/cards.json`, 09003 Astral Projection, instructs the player to
choose a scheme, remove 3 threat and look at the encounter top, then remove one
additional threat from the chosen scheme per numeric boost icon. The printed
card does not use the rules keyword "then". Current RR 1.8 `rr:and.1` makes
the initial removal and inspection independent: resolve as much of each as
possible. A partial initial removal does not suppress inspection. An already
defeated scheme is not replaced with another scheme for the bonus.

Current `rr:boost-boost-icon.1` says a star is not a boost icon.
`rr:look-looked-at` restricts looking to the resolving player;
`rr:look-looked-at.1` and `.1.1` retain deck membership and order. The local
Doctor Strange research handoff identifies the same numeric, private, retained
scheme requirements. The local FAQ corpus contains no 09003-specific FAQ entry
beyond its acquisition inventory. This packet introduces no alternate ruling.

## Demonstrated gap and implementation

The first compiled synthetic composition failed during lowering: inspection
accepted only `treacheriesLookedAt`. The new `boostIconsLookedAt` result uses
the existing numeric `Boost` catalog attribute, excluding stars and boost text.
The existing acknowledgement records it in the current ability continuation;
neither admission nor action offers read the hidden printed value. Existing
prompt-scoped visibility and captured chosen-card provenance remain the owners
of disclosure and target retention. No card-specific switch is added.

A second probe, a selected scheme followed by a sequential removal/inspection,
failed in `AbilityAdmissionAreaDependencies` with "Unknown compiled effect in
area-dependency analysis". Inspection now joins effects without a singular area
lookup dependency: it reads the current deck at its own prompt, without
predicting hidden future cards or numeric values during admission. The pending
decision admits no intervening game action.

## Behavioral evidence

`EncounterInspectionBoostTests` has 12 cases. Its synthetic identity action uses
an exhaustion commitment and the existing choice/continuation mechanisms; it is
not a production Astral Projection row. Its representative effect combines
`and(removeThreat(3), inspectEncounterTop(1))`, followed by bonus removal from
the captured chosen scheme. The first player explicitly orders simultaneous
effects; seat 1 receives the private inspection.

Cases cover real catalog numeric boosts 0/1/2, star-only and star-plus-one,
partial initial threat 0/1/2, a defeated side scheme without retargeting, an
empty encounter deck with a nonempty discard pile and no refill, exact retained
deck order and facedown state, authorizing-seat visibility and revocation,
invalid and stale acknowledgements, committed exhaustion, repeated query purity
including RNG and pending agenda, identical public offers for different hidden
boost values, and the same seeded subsequent RNG stream as an unqueried control.
A separate direct sequential composition distinguishes the area-dependency fix.

Final restored verification:

- `dotnet build tests/Marvel.View.Tests/Marvel.View.Tests.csproj -c Release --no-incremental`:
  passed, zero warnings/errors.
- `dotnet test tests/Marvel.View.Tests/Marvel.View.Tests.csproj -c Release --no-build`
  filtered to whole classes `EncounterInspectionBoostTests`,
  `EncounterInspectionTests`, `OrderedInspectionAdversarialTests`,
  `OrderedEncounterInspectionTests`, `JessicaDrewInspectionTests`,
  `WreckingEncounterInspectionTests`, `RuleCitationTests`: 43 passed.

External logs are under `C:/Users/mggar/Source/astral-inspection-` with suffixes
`probe.log`, `mutant-star.log`, `mutant-area.log`, `final-build.log`, and
`final-regression.log`. The initial probe is expected red. No full Content suite
or native lane is claimed for this bounded prerequisite packet.

## Mutation evidence

Both mutations compiled and were restored before a non-incremental build and
the final green run:

- Counting each printed star as one additional numeric boost failed the two
  star cases with excess threat removal.
- Omitting inspection's no-area-dependency classification failed the direct
  sequential composition with the demonstrated preflight exception. An earlier
  simultaneous-only mutant survived; adding the direct sequential case closed
  that coverage gap rather than weakening a correctness gate.

The production row, its printed arrow target/payment behavior, complete-book
admission and client/native journey remain separate work. This packet does not
change ability data, `AbilityRunner.cs`, `Agenda.cs`, rules data, RNG or digest
formats, and adds no movement, shuffle, reveal, refill, or boost-ability execution.

## Independent prerequisite review

Reviewed source `50404772ea885315fe5fc47a78d3da612f3bab90` against
`78efa9e0` in the isolated `review/astral-inspection-boost` worktree. The review
read printed 09003, current RR 1.8 look/and/boost clauses, the local FAQ and
Doctor Strange research handoff, lowering, admission classification and the
existing acknowledgement path. No blocking defect was found in this bounded
engine prerequisite. This is independent engine review, not production-row,
payment, complete-book admission, client or native acceptance.

The existing offer is a required acknowledgement for the resolving seat, with
the inspected physical card IDs and source context. While it is pending, no
game action may change the encounter top; answering records the numeric result
and resumes the existing continuation. Thus rereading that top on acknowledgement
is adequate for the offered decision. There is no separate intervening choice
that would require retaining an independently frozen card group. Numeric hidden
values do not change the inspection's public offers, and the private faces are
revoked on completion. The selected scheme continues through the existing
chosen-card provenance rather than being selected again after defeat.

`EncounterInspectionBoostReviewTests` adds seven independent cases:

- A three-card inspection over empty, one-, two- and four-card decks proves
  the result sums only the requested top prefix, caps at available cards, and
  survives a subsequent inspection recorded under `treacheriesLookedAt`.
  The four-card control distinguishes inspecting the whole deck, counting only
  the first card, and counting stars. Empty inspection occurs after an effective
  removal, without claiming an empty inspection alone is a legal action.
- Two numeric inspections using the same result name prove the later value
  replaces the earlier value instead of accumulating it.
- The first player's explicit inspection-first simultaneous-effect order leaves
  threat untouched until acknowledgement and then completes both removals from
  the originally selected scheme for the non-first resolving seat.
- Entire restricted observer projections are identical while zero- and two-boost
  cards are inspected. Repeated projections preserve the pre-query digest, RNG,
  pending agenda and events; completion matches an unqueried control, including
  the subsequent seeded RNG stream. The acting seat's temporary exposure ends.

Verification on the reviewed source plus these tests:

`dotnet test tests/Marvel.View.Tests/Marvel.View.Tests.csproj -c Release` filtered
to whole classes `EncounterInspectionBoostReviewTests`,
`EncounterInspectionBoostTests`, `EncounterInspectionTests`,
`OrderedInspectionAdversarialTests`, `OrderedEncounterInspectionTests`,
`JessicaDrewInspectionTests`, `WreckingEncounterInspectionTests` and
`RuleCitationTests`: **50 passed**, zero failures/skips. The external log is
`C:/Users/mggar/Source/astral-inspection-independent-review.log`.
The review adds only tests and this evidence; it does not modify engine code,
the ability book or shared fixtures. No full Content or native run was performed.
