# Yellowjacket's Plan independent review

Reviewed `76758101adf2d4b40a108f80b0646f9fb0e1db35` against base
`75314782` for the bounded `12029` row. No production defect found in this
review. Printed text in `datasets/cards/cards.json` specifies discarding until an
Ant-Man nemesis-set card is discarded and revealing that card. The authored set
selector and reveal result match that text; Rules Reference 1.8
`rr:encounter-deck.1` and `.2` prescribe immediate reset and stopping at the
original deck boundary, while `rr:reveal.step.4` requires the full reveal
procedure. The earlier independent prerequisite review covers the shared engine
mechanism and active-deck routing.

The source tests distinguish a same-trait wrong-set minion, both physical Size
Increase copies, revealing seat, attachment host, reset and outer cleanup. The
source author's valid wrong-set row mutant failed the six actual-card cases;
that mutation was not repeated in this review. The independent cancellation
test schedules the actual Plan reveal, cancels its When Revealed ability and
checks the original physical deck order, treachery cleanup and drained agenda.
Windows Release `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj
-c Release --filter FullyQualifiedName~YellowjacketPlan` exercises this control
and the six source cases: seven passed, none failed or skipped. The log is
`Source/evidence/yellowjacket-plan-independent-tests.log`.

An independent byte and parsed-object comparison to the base found all 753 prior
objects unchanged and the original 763820-byte prefix unchanged, with SHA-256
`d4a1cd9455440203ac9e95f84e11e10499fba775730d5a9d7e0b9621ce1be7ca`.
The source artifact's JSON records the author's check; this comparison was
performed separately.

The vendored Ant-Man encounter inventory has one Plan, so the prior synthetic
recursive-copy test is not an actual legal two-copy nemesis-set scenario. Tech
Theft `12026` remains unauthored; actual Plan to Tech Theft reveal and blanking
must be verified after authoring before pack admission. Public admission,
complete pack, native interaction, replay and cross-platform verification are
outside this review.
