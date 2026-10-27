# Independent review: one basic attack's overkill — MARVEL-845

Verdict: accept the isolated engine prerequisite at `08619549a5a275b3f561866599e52a914b488213`, with the adversarial tests in this review commit. No production correction is required. This is engine prerequisite acceptance; Hulk ability authoring, public admission, complete pack journeys, Linux CI and integration remain separate work.

Reviewed the exact feature diff against its parent, root/scoped AGENTS, pack implementation/proof guidance, printed `10003`, RR 1.8 overkill/damage/interrupt clauses and the existing scoped-keyword, payment and continuation paths. Printed Hulk Smash gives +10 ATK for that attack and conditional overkill; the keyword DSL spelling is an engine choice. No printed card rows or admission boundaries change here. Historical overkill answers do not supersede RR 1.8's actual-defeat condition.

`PhaseStep` creates the `BasicUse` for the original basic attack. Its grant is separate from modified character fields and is read into the damage request before damage execution. `AttackDamagePolicy` snapshots it once; the existing resolution retains that policy through replacement and suspended defeat. Nested card attacks have a different occurrence without the outer receipt. Cancellation and later basic attacks cannot reuse it. The integer and matching-ally instruction shapes keep their existing paths. Keyword-only lowering rejects other triggers/timing and runtime admission checks the attack field and current incarnation. The resource condition reads the settled payment receipt. Query and stale-payment controls retain state/RNG/agenda observations and do not grant effects during preview.

The source packet's missing redirect proof is now covered by `BasicAttackOverkillReviewTests`. These are explicitly synthetic programs, not new printed card interpretations:

- A synthetic attachment reflects the boosted attack from its minion host to the hero. A replacement choice suspends that redirected damage; choosing prevention leaves both unharmed, while choosing draw applies all 12 to the hero. The minion remains undefeated and the villain takes zero in both cases. A health grant keeps the hero alive so the completed continuation, rather than game termination, determines the result. The otherwise identical attack without reflection defeats the minion and spills nine.
- A lethal 12 damage first opens a would-defeat choice. Healing the minion suppresses defeat and spill; choosing draw permits defeat and nine spill. Both drain the actual continuation and assert no pending agenda or game-ending escape.

Both controls use the same physical payment and occurrence grant as the source packet. The reflected fixture deliberately extends the existing generic reflection mechanism to a minion host; it does not claim that printed Photographic Reflexes can attach to a minion.

Verification is Windows Release, serialized in `C:/Users/mggar/Source/marvel-hulk-basic-attack-review`. Final filter: `FullyQualifiedName~BasicAttackOverkill|FullyQualifiedName~LokiRr18OverkillTests|FullyQualifiedName~PhotographicReflexesTests|FullyQualifiedName~RuleCitationTests` against `tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj`. All 42 cases pass, including five new review cases. The restored sources were rebuilt with `--no-incremental` before the final `--no-build` test. Logs are outside the repository:

- `C:/Users/mggar/Source/hulk-p2-review-restored-build.log`
- `C:/Users/mggar/Source/hulk-p2-review-restored.log`
- `C:/Users/mggar/Source/hulk-p2-review-mutant-reflection.log`: replacing the settled replacement amount with the original request amount compiles and kills both reflected controls; the unreflected control passes.
- `C:/Users/mggar/Source/hulk-p2-review-mutant-defeat.log`: removing the actual-defeat guard compiles and kills the healing control; the actual-defeat control passes.

Both temporary mutants were restored byte-for-byte; this commit contains only reviewer tests and this document. Initial fixture construction errors (health API name, heal selector spelling and defeat subject binding) were corrected before the green runs and are not engine findings. Normal pre-commit architecture, complexity and file-length hooks remain enabled. The lead owns Plane reconciliation and the frozen-base integration dependency.
