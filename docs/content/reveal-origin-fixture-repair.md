# Explicit origin for prepared encounter revelations

MARVEL-823 follows actual signature-event source `4131304f` and independent
review `fec49e75`. The reviewed card qualifications distinguish an encounter-deck
revelation (including a dealt card) from a direct discard-pile revelation. An
unknown origin must remain unknown; the production qualifier is unchanged.

The three Core transcript reveal helpers prepare the exact physical card in the
answering player's dealt-encounter queue before scheduling revelation. Each now
supplies an `EncounterRevealOrigin` naming that card and queue with `Dealt: true`,
matching `EncounterRevealProcedure.RevealNextEncounterCard`. The transcript text,
expected optional windows and all behavioral assertions remain unchanged.

`RedSkullFixture.Reveal` models a direct encounter-deck revelation. It creates the
requested card in that deck, captures its exact source, and moves it to the
revealing area before scheduling the same operation. This preserves its prepared
reveal-stage contract while allowing actual Enhanced Spider-Sense in the Media
Coverage tests to inspect truthful provenance. It does not label an arbitrary
direct discard-pile reveal as dealt.

The initial broad Content run with the transcript repair passed 7,152 cases and
failed only the two Media Coverage choices using that older fixture. After the
fixture correction, the complete transcript and Media Coverage selection passed
71 cases. The restored full Content lane passes all 7,154 cases with no skips
(`signature-events-full-content-restored.log`, 6m19s). Independent closure remains
separate. Logs are outside the repository under `Source/evidence/`,
with prefix `signature-events-`. No production engine/card interpretation, public
admission or historical session archive is changed by this repair.

Independent closure accepts this repair and the bounded signature-event packet;
see [the review closure](doctor-strange-final-signature-events.md#independent-review-closure-after-reveal-fixture-repair).
On `f3aa192c` plus the unchanged independent review tests (`d955c1d9`), the
reviewer's restored focused selection passes 233/233, zero skips. A compiled
null-origin mutant of only the three transcript helpers reproduces the original
46 failures; byte-exact restoration and a nonincremental rebuild restore the
green corpus. Missing, wrong-card and direct-discard origin negatives remain
green. Evidence uses prefix `signature-events-closure` under `Source/evidence/`.
