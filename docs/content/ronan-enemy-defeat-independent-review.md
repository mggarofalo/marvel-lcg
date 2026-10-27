# Independent enemy attack defeat review

Verdict: accept the engine behavior at `561e39040bbfe18b36bd39f7f71234e392b49589`; integration requires an explicit replay compatibility boundary for actual Rapid Response timing.
This review does not author or admit Ronan content and does not approve 90004.

The governing authority is vendored RR 1.8 `rr:damage.step.7`, `.step.8`,
`.step.9`, `rr:triggering-condition.1` and `.2`, and
`rr:attack-enemy-activation.step.6`: defeat interrupts precede departure;
damage and defeat from one attack share the completed attack's response window;
optional responses to those conditions can occur in either order.
Printed 90002 is read from `datasets/cards/cards.json`. Its test-local body and
synthetic observers use the complete production ability book without admitting
the scenario. The existing composition record documents the absent card-specific
FAQ and the narrower production-content boundary.

`EnemyAttackDefeatIndependentReviewTests` adds four independent executable cases:

- Direct and indirect damage both allow the after-attack optional response before
  the plain after-defeat response. Each remains available until used, then cannot
  repeat; forced threat has already resolved, and the attack has completed.
- A nonattack defeat caused inside the attacked ally's defeat interrupt retains
  its own earlier optional response while the enemy attack remains active. Its
  defeat fact stays out of the final attack receipt. The same observer can later
  answer the attacked ally's distinct defeat, producing exactly two uses across
  two occurrences.

The first diagnostic run passed 21/22. The new nested test initially stopped on
the required forced-order prompt before Judge resolved; its helper now resolves
that mandatory choice before checking the optional tier. No production change
was needed. The corrected focused run passed 22/22.

A compiled mutation made `Occurrence.Also(Defeated, deferResponse)` always expose
the defeat condition immediately. All three new cases failed: optional responses
appeared while the enemy attack was still active. Restored sources were rebuilt
with `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release
--no-incremental` (zero warnings/errors), then all 52 selected cases passed.
That final selection includes the four Ronan/defeat classes, rule citation checks,
attack replacement continuations, attack trigger binding, staged attack safety,
and actual Rapid Response composition. It covers ordinary attack, indirect,
overkill, surviving-game identity elimination, prevention/replacement, Boost and
retaliation exclusions, nested nonattack attribution, recipient reentry and
historical defeat/controller binding.

Source review confirms that direct and overkill damage register exact occurrence,
object and incarnation before defeat settlement; `Defeat.Record` captures defeat
facts before identity elimination; only matching causes enter the attack receipt.
Indirect settlement registers only positive-placement recipients and tests actual
defeat. Shared occurrence frames survive early activation completion. The exact
incarnation comparison was inspected; this packet does not claim an independently
killed incarnation-only mutant. Existing adjacent reentry/trigger-binding cases
passed in the final selection.

Logs outside the repository:

- `C:/Users/mggar/Source/evidence/ronan-enemy-defeat-independent-review.log`
- `C:/Users/mggar/Source/evidence/ronan-enemy-defeat-independent-mutant.log`
- `C:/Users/mggar/Source/evidence/ronan-enemy-defeat-independent-restored-build.log`
- `C:/Users/mggar/Source/evidence/ronan-enemy-defeat-independent-restored.log`

These are focused Windows engine results. The source packet's broader results
remain separate evidence; this review does not claim a new full-suite or Linux run.

The new attack receipt and occurrence fields are reconstructed from setup and
accepted decisions, not serialized session JSON (`JsonIgnore`). They change no
schema, RNG or digest format. `docs/release-policy.md` requires a replay-contract
decision when the same setup and decision trace may resolve differently, even
for a correctness repair; passing current Content/Session tests does not establish
historical equivalence. Actual authored 08031 provides a concrete affected card, independently of the
synthetic observer and provisional 90002 body. With genuine predecessor
`8c08d196` production sources, its ordinary lethal-defending-ally attack offers
Rapid Response while `World.Attack` remains active. With candidate sources, the
offer follows `AttackCompleted`; accepting still returns the exact ally as a new
incarnation with one damage. The predecessor fails the distinguishing completed-
attack assertion. A non-incremental restored-source build succeeded before final
verification. The initial current probe incorrectly expected seat zero; actual
Rapid Response belongs to seat one, so that fixture expectation was corrected
without changing the timing assertion. The final selection passed 53/53 after
this addition; the earlier 52-case result predates this concrete replay probe.
The final fourth case exercises that actual 08031 behavior.

This is a concrete semantic trace change requiring an explicit replay-contract
decision under the release policy. The coordinator owns that production boundary
and historical source-admission work. The earlier Core/RoRS-only row scan did not
cover pack 08 and cannot establish equivalence for the full current product.
Passing existing direct-damage Rapid Response and Session tests does not discharge
this compatibility requirement. Predecessor evidence is in
`C:/Users/mggar/Source/evidence/ronan-enemy-defeat-rapid-predecessor.log`; final
53-case results replace the earlier restored log listed above.
