# Thor obligation, Frost Giant and Teamwork actual-card evidence

The historical Loki overkill finding below is superseded by the
[RR 1.8 adjudication](overkill-rr18-adjudication.md): actual defeat is required
before spill. The three authored faces and their independent review remain valid.

Candidate based on `415ca1f2`. Adds authored 06026 Odin’s Anger, 06029 Frost Giant and 06032 Teamwork without product admission. The book has 606 total faces, including 30 Thor faces before the separately reviewed Valkyrie/Heimdall packet. The narrow StepConditions inventory update registers the already implemented basic-use condition; the actual ability-data gate exposes its absence.

Authority: generated `datasets/cards/cards.json` faces 06026/06029/06032, Rules Reference clauses cited in tests, and the repository’s existing bounded basic-use and delayed-effect mechanisms. Printed facts checked include Teamwork’s zero cost and energy resource, Frost Giant’s toughness, and distinct unique ally fixtures. Boost conditions are represented in the effect’s `if` because direct Boost execution does not inspect a triggered ability’s top-level `when`.

Actual-card tests cover non-first-seat Odinson ownership, optional form change, exhaustion/removal, Mjolnir in hand/play/absent, stun and obligation discard; Frost Giant entry toughness, villain attack versus scheme, fully prevented damage, later unrelated damage, boost damage to another ally and a boosted minion attack; Teamwork matching modified ATK/THW, physical ally selection, no consequential damage, one-use scope, illegal/stale costs and basic-only eligibility. Tests use the complete authored runner; research board construction is not public product admission.

Focused command: `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter "FullyQualifiedName~ThorTeamworkCardTests|FullyQualifiedName~ThorFrostGiantCardTests|FullyQualifiedName~ThorOdinsAngerCardTests|FullyQualifiedName~AbilityData|FullyQualifiedName~RuleCitationTests"`.

External evidence under `C:/Users/mggar/Source/evidence/`: `thor-nemesis-final-baseline.log` (56 pass), `thor-nemesis-mutants.json` and `thor-nemesis-mutant-*.log` (six data mutants), `thor-nemesis-restored-build.log` (nonincremental rebuild), `thor-nemesis-restored-tests.log` (restored focused run), and `thor-nemesis-commit.log` (normal hook). Mutation decisions: wrong Odin status, wrong named hammer, discard instead of removal, Frost scheme instead of attack, Frost minion instead of villain, Teamwork attack-only instead of attack-or-thwart. Final result and exact commit are supplied in the integration handoff; earlier logs ending before final correction are not acceptance evidence.

06028 Loki is withheld. Its actual authored row and tests are preserved externally as `thor-loki-actual-row.json` and `ThorLokiCardTests.cs`, with `thor-loki-actual-red.log`: four ordinary heal/discard/reset cases pass, but overkill expects two damage to the villain and receives zero when Loki heals. Vendored rulings `8fe224a8f1e012b2` and `c55100b5139ab3a5` require spill before the revival check and prohibit defeat credit after healing. `AttackDamageResolution.BeginSpill` currently requires completed defeat; a separate prerequisite must fix and independently review that behavior before Loki authoring is admitted.

## Independent review of 1e066a41592340e4c2f74dcb31b275f5ff5c2f87

Reviewed the actual 06026/06029/06032 rows against generated printed text, local FAQ queried/entry records, Teamwork rulings `ruling:5e2188b043634dba` and `ruling:c9eb4e023e3e57d5`, the triggering/cost/continuation implementation, and the original actual-card tests. No source defect was demonstrated in this bounded review. The original six mutation logs contain four behavioral failures and two lowering-guard rejections; those guard rejections are not proof of executed card behavior.

Added four adversarial cases in `ThorNemesisIndependentReviewTests`: Odin's removal branch is absent in hero form or with exhausted Odinson, a foreign hand's Mjolnir is preserved, and stun/discard still resolve; Frost Giant follows the surviving defending ally rather than the identity, respects Tough, completes the attack and expires before later unrelated damage. Hulk's printed 5 HP permits the 3-damage attack to complete without a defeated-recipient fixture ambiguity.

Review verification: focused actual-card tests plus ability-data and citation checks pass 60/60. A compiled mutant in `DelayedEffects.Stun` substituting the first identity for the condition's damaged subject fails the new ally-recipient assertion (1 failed, 1 passed); this is a behavioral kill. The source was restored, a forced nonincremental Release build passed with zero warnings/errors, and the same 60 checks passed again. External evidence: `C:/Users/mggar/Source/evidence/thor-nemesis-review-baseline.log`, `thor-nemesis-review-mutant-recipient.log`, `thor-nemesis-review-restored-build.log`, `thor-nemesis-review-restored-tests.log`, and `thor-nemesis-review-commit.log`.

This review does not admit Thor, author Loki, verify native interaction or claim whole-pack/public-session readiness. The reviewed packet still adds three faces to the 27-face Thor base (30 Thor / 606 total before the separate two-ally packet).
