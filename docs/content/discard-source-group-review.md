# Discard source grouping review

Independent narrow review of `5571b62bc5861fbdae957abb6abd94b92fd42f63`,
in an isolated worktree on Windows, 2026-10-26. Verdict: no blocking findings
within the source-heading correction and accompanying assertion updates.

`CompleteChoiceGroups` uses the authorized visible board source location. Hand
play remains in Hand, discard play appears in Discard pile, and an unknown
source falls back to Current decision. Non-turn choices retain their existing
group. This is presentation categorization; legality and payment remain owned
by the engine. The renderer indexes the added heading consistently.

Reviewed the registered native Lockjaw script and the existing headless result
at `C:/Users/mggar/Source/lockjaw-source-group-headless/results.jsonl`.
The script uses viewport clicks, stages payment without a revision change and
asserts one explicit commitment moves the same discarded card into allies.
The added heading assertion is paired with visual inspection: all three PNGs
in `C:/Users/mggar/Source/lockjaw-source-group-rendered-100/` were inspected.
At 1920x1080, 100 percent scale and reduced motion, Play Lockjaw is visibly
under Discard pile, separate from hand plays. Payment identifies Spider-Man,
Lockjaw, Pay 4, Energy and Genius, four resources selected and the explicit
Pay and play Lockjaw commitment. The result shows Lockjaw at 4/4, three cards
remaining in hand and the movement result. Native execution was reused because
the reviewed production and native-script sources match that evidence.

The two integration-test adjustments preserve distinguishing observations.
`EngineBuildIdentityTests` still asserts the complete literal display string;
protocol 39 matches `EngineProtocol.Version`. `ActiveAttackTests` now asserts
the faceup occurrence is reached, retains attacker and enclosing victim checks,
and separately verifies the boost occurrence targets its subject. The boosting
zone check applies at interrupt timing; the existing boost procedure can discard
the card before response timing. Other attack occurrences still require their
target to equal the live attack target. No production rules changed here.

Independent Release reruns in the isolated tree passed: DecisionOrganizationTests
11/11, ActiveAttackTests 7/7 and EngineBuildIdentityTests 2/2. Commands used
`dotnet test <project> -c Release --filter FullyQualifiedName~<class> --nologo`.
Logs are `C:/Users/mggar/Source/discard-review-{godot,rules,server}.log`.
The working tree stayed clean after these tests.

This verdict covers the corrected heading and reviewed integration assertions.
It does not claim whole-product comprehension acceptance, public Ms. Marvel
admission, other scale profiles, Linux or packaged-client verification.
