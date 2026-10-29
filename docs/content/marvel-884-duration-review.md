# MARVEL-884 independent duration review

Candidate: `5a64ab1dfa5883ca3a0f721a30a46293601451bf`, based on `05e52f511c33ec509210bc94945aa0ef840b583a`. This is a prerequisite review, not admission of card 12025 or its pack. The candidate uses synthetic abilities and research boards.

## Authority and interpretation

`datasets/cards/cards.json` identifies 12025 as Care for Cassie: after choosing and discarding a hand card, the player cannot change form until their next turn ends, and the obligation is discarded. RR 1.8 `rr:cannot` makes the prohibition absolute; `rr:lasting-effects.1` makes its duration independent of the source's continued presence, and `.5` expires it at the specified point before end responses. `rr:leaves-play.1` treats a returning card as a new copy. `rr:player-elimination.step.5` removes the eliminated player's area and game elements. Searches of the vendored FAQ and rulings found no card-specific answer for 12025, Care for Cassie or the precise beginning boundary.

The candidate treats the beginning occurrence as unresolved during its interrupts and resolved before its responses. RR `rr:interrupt.3` places ordinary interrupts after initiation but before resolution, and `rr:response` places responses after occurrence. Applying those timing distinctions to whether that turn is already the player's current turn is an inference; no explicit ruling resolving that wording was found. Under that interpretation an effect created in a beginning interrupt expires at that turn's end; one created in its response expires at the following turn's end. This is not a claim of explicit card-specific authority.

## Independent examination

The duration is attached to a seat and each registration tracks its own beginning. It applies immediately via existing form-change permissions; an observed beginning arms in the occurrence body and an unobserved beginning arms synchronously. The end body selects ordinary and armed next-turn effects together for existing settlement. The authored DSL restricts this duration to unit prohibition on the resolving identity. Source and target departure semantics use existing lasting-effect machinery. The active eliminated player's game path calls `FinishTurn` after pending work; eliminating the identity also removes its direct effect. No baseline or exception file changed in the candidate diff.

The candidate includes tests for own and other seats, villain-phase creation, source departure, beginning and ending windows, unobserved beginning, rejected stale input with RNG and pending-work comparisons, simultaneous health expiry and eliminated-identity cleanup. These are candidate-authored evidence, not independent test results. The independent tests in `IndependentNextTurnDurationTests` challenge separate registrations made on opposite sides of a beginning and source departure/reentry without renewal.

Known limits: no actual obligation row, public admission or complete pack journey is claimed. The candidate's simultaneous-health test uses synthetic modifiers; the authored duration permits only form prohibition. Ally-limit settlement and a full active-player elimination interaction have not been independently exercised here. The cited turn-beginning interpretation remains an authority uncertainty, not an established card-specific ruling.

## Independent verification

On the candidate plus the independent test file, `dotnet test tests/Marvel.Rules.Tests/Marvel.Rules.Tests.csproj -c Release --filter FullyQualifiedName~IndependentNextTurnDurationTests` passed 2/2. A compiled mutant initializing every registration as already started failed the opposite-sides test at the assertion that the later effect remains; the reentry control passed. After restoring the candidate, a `--no-incremental` Release build and the same `--no-build` test passed 2/2. Logs are `C:/Users/mggar/Source/cassie-independent-duration-test.log`, `cassie-independent-mutant.log`, `cassie-independent-restored-build.log`, and `cassie-independent-restored-test.log`. These checks do not replace the candidate-author's broader reported runs.

Review result: no production defect identified in the bounded prerequisite, subject to the explicit timing inference and evidence limits above.
