# Physical Toll prerequisite independent review

Reviewed source: `9f6119b8d04c4cd0de42a8dff1c056ec321a48d5`, on
`review/physical-toll-event-cost`. MARVEL-831. Verdict: accept this bounded
engine prerequisite. No functional defect was reproduced. This review changes
tests and evidence only; actual 09027 ability-book authoring, complete Doctor
Strange admission and public session/save acceptance remain separate work.

Authority review used generated `datasets/cards/cards.json` 09027, current
RR 1.8 `rr:obligation.1`, `.4`, `rr:initiating-abilities.step.4` through `.7`
and `rr:event.2`. The optional form change and Stephen-only exhaust/remove
branch use existing engine instructions. The second branch applies three extra
resources to the next event actually played, preserves that obligation until
completion, and applies even when event effects are canceled. The pre-1.5
`ruling:277ab401b1269ec7` distinguishes resolving an Invocation ability from
playing that card; the current printed Spell Mastery program and event rules
remain consistent with that distinction. Master of the Mystic Arts is itself
an event and therefore consumes the increase even though its child procedure
resolves an Invocation Special. No conflicting current authority was identified.

Source review traced the increase into shared card pricing before discounts
and affordability checks, its exact applied effect into payment completion,
and its cleanup through common event discard. Reservation precedes the paid
event initiation window and attack status cancellation. It consumes the
increase once, while retaining a distinct receipt until the event completes.
Completion is bookkeeping for the printed sentence, not an invented Forced
Response. The source incarnation prevents an old effect or receipt from
attaching to a reentered obligation. No AbilityRunner-owned mutable table,
new agenda operation, RNG change or digest-format change is introduced.

`PhysicalTollIndependentReviewTests` adds five cases:

- A real nested played event has its ordinary zero cost while the enclosing
  event retains its surcharge receipt; both complete and the obligation leaves.
- Both ordinary completion and forced initiation cancellation discard the
  obligation before the event's CardPlayed response. Paid hand cards remain
  discarded; draw counts distinguish canceled effects from resolved effects.
- Independently reconstructed nonpublic boards retain the same future event
  cost, completion events, RNG, agenda and card digest after JSON round trips
  of either the persistent increase or the reserved receipt. The records alone
  preserve the identities needed for their behavior, without live delegates.

The last pair proves effect-data reconstruction, not production journal replay
or a public save route. The card digest intentionally does not contain the
continuous-effect list. Comparing it alone cannot certify future legality;
these tests separately compare effect records and actual future pricing and
completion. Production save verification still requires the fully admitted
content and accepted-decision replay path.

Validation on Windows, all shell calls using PTYs, serialized builds/tests:

- Initial compiled independent class: 5 passed.
- Compiled mutant: omit `NextEventCost.Reserve` in `AbilityEventPayment`, with
  `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release
  --no-incremental`, then test the independent class with `--no-build`.
  Four cases failed, one passed. The failures distinguish a missing retained
  receipt and obligations incorrectly remaining in their play area. The direct
  reserved-record round trip correctly remains an unaffected control.
- Restore the exact source, rebuild the Content test project with
  `--no-incremental`, then run affected whole classes with `--no-build`: 93
  passed. Filter: `FullyQualifiedName~PhysicalToll|FullyQualifiedName~EventPlayCancellationTests|FullyQualifiedName~EventPlayInitiationTests|FullyQualifiedName~NestedEventModifierTests|FullyQualifiedName~CoreLastingEffectCardsTests|FullyQualifiedName~DoctorStrangeInvocationCardTests|FullyQualifiedName~SpellMastery|FullyQualifiedName~MasterOfMysticArts|FullyQualifiedName~ObligationTests|FullyQualifiedName~CoreObligationChoiceMeaningTests|FullyQualifiedName~RuleCitationTests`.
- `dotnet test tests/Marvel.Rules.Tests/Marvel.Rules.Tests.csproj -c Release
  --filter 'FullyQualifiedName~CardPlayAnOmittedCostTests|FullyQualifiedName~RuleCitationTests'`:
  18 passed (16 actual cost cases and 2 citation checks). The source evidence
  incorrectly claimed its original `CardPlayTests` filter covered this class;
  that claim is corrected in the prerequisite record.

Logs: `C:/Users/mggar/Source/physical-toll-review-initial.log`,
`physical-toll-review-mutant-build.log`, `physical-toll-review-mutant.log`,
`physical-toll-review-restored-build.log`,
`physical-toll-review-restored-content.log`, `physical-toll-review-rules.log`.
Normal pre-commit hooks must pass on this review commit. Broad Content,
solution, platform, native and public-session acceptance are not claimed.

## Combined integration

Source `9f6119b8` and independent review `6cb8396e` are combined with Invocation
meaning source `fc061f45` and independent review `91d725db` on the current
675-row candidate. All 1,060 selected Content cases pass across cost/payment,
Astral, Invocation, Spell Mastery, Physical Toll and citations; the complete
View assembly passes 405 cases. Logs:
`Source/evidence/strange-toll-invocation-integrated-{content,view}.log`.
The actual 09027 row, public journal continuation and complete pack certification
remain separate. Native Seven Rings/Vapors parent-action context is still open.