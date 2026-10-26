# Black Widow identity/search independent review

Reviewed source `7749e123766d074346414a60e891ff02ee71dbd9` against base
`850597c2`. **Approve the bounded nonpublic three-face packet**, with the test
repairs and independent probes in this review commit. No production finding.
This does not admit Black Widow, Safe House #29, or other unauthored faces.

Printed authority: generated `datasets/cards/cards.json`, faces 08001b, 08002,
08005 and 08011. FAQ snapshot harvested 2026-08-22 explicitly queried all four
and has no matching entries. The Hall of Heroes snapshot harvested 2026-08-31
has no applicable Natasha/Mission Prep/Coulson/Safe House clarification; the
Winter Soldier references concern the later identity. This is review against
the repository's pinned authority, not a new online ruling acquisition.

The exact ability diff appends only Natasha Romanoff, Winter Soldier and Agent
Coulson. Parsed JSON comparison preserves all first 620 rows and yields 623;
SHA-256 remains `0deac5f3340c97d93f1a49e07805eaf4353de21dee3376e10d9456970e24484c`.
Their text correctly expresses own played Preparations with once-per-phase use,
live controlled Preparation pricing, and exact own deck/discard search followed
by shuffle. None declares starting counters. Existing engine mechanisms retain
form use, controller context and accepted source continuations.

Two evidence findings are repaired here. The original Winter pricing theory
used multiple simultaneous Counterintelligence copies despite its printed max
one per player; it now uses distinct Preparation faces. Natasha's entry-only
negative initialized entry state without scheduling an entry window; it now
also processes an explicit isolated `CardEntersPlay` lifecycle, distinguishing
entry from play. Neither repair changes production code or the ability book.

Three independent actual-row tests distinguish declined Mission Prep from a
used response and verify the correct draw recipient; reprice Winter Soldier
after trait loss/restoration, control change and departure; and put Coulson into
play through `CardPlay.PutAllyIntoPlay`, excluding hand, foreign discard and
non-Preparation deck cards from his search while still shuffling. Source tests
also cover both phase boundaries, zero cost payment/entry, both search origins,
no match, source departure, and invalid answer preservation/retry.

Final focused command: `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter "FullyQualifiedName~BlackWidowIdentitySearch|FullyQualifiedName~AbilityData|FullyQualifiedName~EveryCitedRuleExists"`.
Result: **47 passed, zero failed**, log `C:/Users/mggar/Source/bw-identity-independent-review.log`.
Inspected source mutation logs: compiled actual-row Natasha draw-two mutant
fails expected-one/actual-two; restored nonincremental build and 44 original
focused tests pass. `git diff --check` passes. No full Content, public admission,
native journey, or product acceptance claim is made by this review.

Safe House's pre-arrow discard selection remains a separately reviewed engine
prerequisite. Its deliberately absent row preserves fail-closed behavior.
