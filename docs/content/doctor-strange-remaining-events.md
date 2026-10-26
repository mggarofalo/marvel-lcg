# Doctor Strange remaining events: prerequisite checkpoint

MARVEL-823, parent MARVEL-565. Examined against `74bef97d` on
`feat/strange-remaining-events`. No production rows are added by this packet.
The authored book remains 672 rows, including 27 of the 40 Doctor Strange faces.
Astral Projection (09003), Magic Blast (09004), and Protective Ward (09007) remain
unauthored. This is prerequisite evidence, not complete pack admission or acceptance.

## Authority

Printed text is the generated `datasets/cards/cards.json`. Astral Projection
has a choose-scheme arrow cost, followed by removal of three threat **and** private
encounter-top inspection, then additional threat removal from the chosen scheme.
`rr:cost.1`, `rr:initiating-abilities.step.5` and `.step.6` require that choice
before effects attempt to initiate. Confusion cancels the effects, not costs.
The inspection prerequisite's synthetic View tests already cover the private
look receipt, numeric boosts versus stars, ordering, partial removal and departed
schemes; they do not implement the printed choose-scheme cost.

Magic Blast deals five damage **and** discards the player's top deck card;
printed physical, energy and mental resources cause stun, a separate two damage,
and confuse respectively, while wild applies all three. The current RR 1.8
`rr:and` opening requires simultaneous effects, with independent resolution under
`.1`. Replacing that structure with a sequential sentence is not an accepted
workaround. Catalog resource encodings are `R` physical, `Y` energy, `B` mental,
and `G` wild; 01090, 01088 and 01089 print doubles of their respective types.

Local rulings `59bea1238c870653`, `e4ea1bf855e41aac`, and
`d1f9f33e3ae4ba2f` respectively establish resource-type presence rather than icon
multiplication, separate five/two damage instances, and player-deck reset before
Magic Blast itself is discarded. These rulings are tagged pre-1.5. Their stated
resource, separate-instance and event-cleanup facts agree with the current
printed card and RR 1.8 printed/event definitions; no superseded keyword ruling
is used. The Norman Osborn example concerns the same physical enemy changing
face, which must be distinguished from a defeated enemy or stage replacement.

Protective Ward requires a treachery revealed **from the encounter deck**,
allows another player's revelation, and cancels all effects, including keywords.
`rr:cancel.4` retains revelation and encounter discard. A generic treachery-reveal
condition alone omits the printed origin restriction.

## Distinguishing probes

Preserved reproducible source and logs are under
`C:/Users/mggar/AppData/Local/Temp/strange-remaining-events-823/`:

| Probe | Observation |
|---|---|
| `DoctorStrangeRemainingEventGapTests.AstralRequiresSchemeCommitmentBeforePaidEventInitiation` | A confused hero supplies printed payment and no selected scheme to a candidate using the existing effect-choice route. Expected prepayment refusal; actual execution returns without an exception. The label cancels the effects without paying a choose-scheme cost. |
| `DoctorStrangeMagicBlastTests.ExactDiscardReceiptAppliesEachPrintedResourceTypeOnce` | Four resource cases fail at `Actions`, before any payment, with `'09004' suspends inside a labelled power, which is not implemented`. |
| `DoctorStrangeMagicBlastTests.DefeatedEnemyIsNotReplacedForWildBranches` | The same admission guard prevents reaching the exact-target departure check. |
| `DoctorStrangeMagicBlastTests.FinalDeckCardResetsBeforeMagicBlastIsDiscardedAndReceiptSurvives` | The same guard prevents reaching reset/receipt/event-cleanup checks. |
| `DoctorStrangeWardOriginProbeTests.FullRevealOffersWardOnlyForEncounterDeckOrigin` | The real reveal procedure offers the candidate Ward for both encounter-deck and encounter-discard origins. The deck control passes; the discard-origin assertion fails, expected false, actual true. |

The Astral candidate is explicitly a probe of the available effect-choice route,
not a proposed faithful row: it lacks the unexpressible arrow choice. A candidate
with a nested thwart power also reaches the unsupported-power guard. Adding
top-level labels without a corresponding saveable power is refused for an
uncanceled ability. None of these substitutions supplies a choose-scheme cost.

The Magic Blast candidate is `candidate-09004.json`, with one attack containing
the printed simultaneous damage/discard body and the later independent resource
branches. `AbilityPowerSuspension.SuspendsPowerEffect` explicitly rejects a
multi-effect `and` inside that power. Removing the power wrapper and retaining
only an attack label instead fails with “attack label without exactly one
saveable attack power.” No sequential replacement was retained.

The Ward probe uses a synthetic 01092 Action to reveal the exact catalogued
01190 Shadow of the Past from the selected area through the ordinary
`AbilityEncounterReveal` and scheduled reveal path. Both origins have the same
revealing-area card and occurrence by interrupt time. The passing deck-origin
control additionally pays for seat 1's Ward during seat 0's revelation, cancels
the reveal, discards treachery/event/payment, suppresses the resulting Surge,
and drains the agenda without ending the game. This verifies the cancellation
composition while exposing the missing origin restriction.
`AbilityEncounterReveal.Prepare` captures the former area only for movement
events; `AbilityRevealRequest` retains only card/player, and the condition DSL
has no reveal-origin predicate. This requires an owning engine receipt and
condition, not a renderer or event-history inference.

`strange-remaining-final-probes.log` records a compiled 10-case run: eight
expected distinguishing failures and two passes (candidate inventory and the
complete deck-origin Ward control). It is deliberately not green implementation
evidence. The archived tracked patch and three test sources reproduce the
candidate; red probes are excluded from committed test discovery. The Astral
engine worker separately preserved its original probe before removal.

## Preservation and next work

During the temporary Magic Blast append, all first 672 JSON objects were compared
as raw object text against `git show HEAD:datasets/abilities/abilities.json` and
matched exactly. The final book was restored byte for byte against that same
revision; SHA-256 is
`74a57273b75b9acbb8e9f7f14eabc339f15b0c003e02a0d329e0defe1eb36f20`.
Inventory, fixtures, duplicate-admission checks and unknown-card controls are
unchanged. No public admission, engine, AbilityRunner or Agenda edit is retained.

Implement and independently review the scheme-choice arrow cost, supported
simultaneous damage/discard attack continuation, and exact reveal-origin receipt.
Then resume actual rows and their authority/behavior review. The remaining Magic
Blast assertions have not executed past admission; they need further validation,
including same-physical face change versus defeat/promotion, resource receipt after
reset, both legal initial orders, and actual-row mutants. Ward still needs its
full origin/form/payment matrix against the completed primitive. Astral still
needs actual-row inspection/partial/defeated-scheme and confusion evidence.

No actual-row mutant is claimed because no row is admitted. Broad Content and
whole-game lanes were not started for this prerequisite-only packet.

After restoring the original book and excluding the red candidates,
`dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release
--filter 'FullyQualifiedName~DoctorStrange|FullyQualifiedName~AbilityData'`
passes all 135 cases. `dotnet run --project tools/Marvel.Cards.Extract -- check`
also passes offline. Logs are `strange-remaining-restored.log` and
`strange-remaining-offline.log` in the local Temp directory. These verify the
restored baseline, not implementation of the three blocked cards.
