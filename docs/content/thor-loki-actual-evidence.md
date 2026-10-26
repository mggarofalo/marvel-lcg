# Actual Loki authoring

Actual `06028` is appended to the complete ability book, bringing it to 611
faces and all 35 Thor faces. Public Thor admission remains closed pending its
engine integration, starter and complete public proof.

Generated card text specifies a mandatory would-defeat interrupt, exact top
encounter discard, and healing all damage only when that discarded card is a
treachery. The authored physical discard receipt survives a deck reset. Current
RR 1.8 overkill requires actual defeat; the older Loki answer does not override
that changed rule. See FFG's [1.8 explanation](https://www.fantasyflightgames.com/mission-updates/).

Five actual-book cases cover treachery/non-treachery with ordinary and last-card
encounter decks, mandatory resolution, surviving incarnation, no unrelated prior
discard determining the branch, and no overkill or defeat response when Loki
heals. The initial actual/data/citation selection passes 40 checks. A compiled
row mutant testing minion instead of treachery fails all five actual-card cases.
The original book is restored byte-for-byte before a forced nonincremental build
and rerun; all 40 checks pass again. External logs are
`C:/Users/mggar/Source/evidence/thor-loki-actual-*.log`.

The earlier external candidate's behavior note and overkill assertion used
superseded authority. This authoring corrects that expectation and leaves the
engine unchanged. Separate independent RR 1.8 probes cover actual defeat and
Relentless Assault/Hall of Heroes credit. The independent actual-row review is
recorded below; Hammer Throw recovery-preflight remains a separate obligation.

## Independent actual-row review

Reviewed authoring `ad1ec15a` and authority packet `33da5af3` on 2026-10-26 in
`C:/Users/mggar/Source/marvel-thor-loki-actual-review`. The generated `06028`
text, vendored FAQ's mandatory interrupt erratum, current `rr:overkill.1`, damage
steps 6/8 and `rr:encounter-deck.1` agree with the authored row. FFG's linked
RR 1.8 explanation explicitly supersedes the earlier spill-before-healing
interpretation. No production authoring or engine defect was found in this
bounded review.

One evidence defect was corrected: `LokiRr18Fixture` replaced the authored Loki
row with a test-local row, insulating the earlier authority probes from actual
book defects. The fixture is removed; the probes now use `AuthoredCards.Runner`.
Eight cases cross treachery/non-treachery with ordinary/last-card decks in both
completed direct overkill attacks and actual Relentless Assault paid by Strength.
The tests require actual defeat before spill, Hall of Heroes' positive glory
response only on defeat, physical payment disposal, completed pending work and
no accidental game termination. Direct attack events require exactly one top
discard, reset before healing/departure, and departure before villain damage.
An older opposite-kind discard cannot select the branch, including after reset.

Baseline focused Loki/ability-data/citation selection passes 48 checks. Mutating
the real `06028` row's condition from treachery to minion builds successfully
and fails all eight expanded cases with reversed zero/two excess or zero/one
glory expectations. This proves the probes now hold the authored row rather than
a replacement. The ability book is restored byte-for-byte; a nonincremental
Release rebuild succeeds with zero warnings/errors and the same 48-check
selection passes again.
Logs are outside the repository under
`C:/Users/mggar/Source/evidence/thor-loki-review-{baseline,row-mutant,restored-build,restored}.log`.

Commands: `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj
-c Release --no-incremental`, followed by `dotnet test
tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build
--filter "FullyQualifiedName~Loki|FullyQualifiedName~AbilityData|FullyQualifiedName~RuleCitationTests"`.
The mutant uses the same project and configuration, filtering only
`FullyQualifiedName~LokiRr18OverkillTests`.

Normal pre-commit review rejected the initial expanded test at CCN 23. Cohesive
board-result, single-discard and resolution-order helpers retain every assertion;
the unchanged CCN 10 policy passes. After this decomposition, all 48 selected
checks pass again (`thor-loki-review-closeout.log`).

Verdict: bounded approval of actual Loki authoring and its RR 1.8 behavior.
This review does not establish public pack admission, setup, native interaction,
Hammer Throw recovery preflight or full Stealth Strike execution.
