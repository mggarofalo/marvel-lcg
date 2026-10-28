# Independent conditional resource contract review

Verdict: **reject candidate `197954289c00034f938342e1e1ce05d3a6dcd9e5` pending wire repair and protocol compatibility decision.** This reviews the synthetic contract only; it does not activate a Power card or resolve MARVEL-878.

## Findings

1. `ConditionalResourceBonus.ComponentMask` is not required on the wire. Removing `component_mask` from an otherwise valid response is accepted as zero by `EngineJson.ReadResponse`; client completeness validation also checks only that bonus `Generates` is non-null. The compiled negative control in `ConditionalResourceWireReviewTests` fails with “No exception was thrown”. Require the field and reject malformed bonus values at the appropriate boundary, with valid and invalid controls. Review valid icon letters and masks against the represented components as part of that repair.
2. The response shape adds `conditional_bonus` to the strict protocol, whose current version is 42. Old peers disallow unknown members. As with the protocol 38 precedent in `docs/presentation-layer.md`, make and document an explicit compatibility decision: advance the protocol boundary to 43, reject 42 peers before gameplay, and exercise the affected embedded and socket paths. Unchanged unconditional JSON alone does not establish compatibility. This does not by itself establish a replay version change; assess MARVEL-870 against genuine predecessor archives separately.

## Independent evidence

All observations below used Windows Release and candidate source, with logs under `Source/evidence/conditional-review-*` outside the worktree.

- `ConditionalResourceCaptureReviewTests`: 1 passed. A synthetic resource generator genuinely suspends at a defeat interrupt before a conditional hand source commits. Mutating the original quote list and allocation during suspension leaves the accepted payment intact; the final receipt is `YGG` and the source is discarded. A compiled mutant that aliases `Price.Sources` fails this control after resumption with only `YG` generated. Restoring the original and forcing a non-incremental build yields 1 passing control again.
- `ConditionalResourceComposerReviewTests`: 1 passed. A bonus assigned only to a nonqualifying component is rejected by both composer and engine; a qualifying allocation activates it and is accepted by both. Progress reports base output before qualification and bonus output afterward.
- `ConditionalResourceWireReviewTests`: 1 failed as intended on missing `component_mask`; valid serialization was checked before removing the field. This red test is retained as a repair control, not a passing certification.

The candidate author's earlier allocation and unconditional-shape evidence remains separate. The suspended control proves capture through the shared payment entry point, not every possible caller. Actual Power activation, native conditional display and final compatible replay evidence remain integration requirements before publication. The renderer's automatic excess count is guarded against selected conditional sources; it is not a finding in this review.

## Repair closure review

**Accepted for the synthetic internal contract at `aaedbb07ca59a49666d0f9ac0140c3f7a23ac768`.** The two findings above are closed: required JSON members reject omission; the Rules-owned validity check rejects empty or invalid bonus icons and zero or out-of-range component masks at protocol and direct-client boundaries. Protocol 43 is documented, and protocol 42 requests are rejected before game creation on both embedded and socket paths. Unconditional source JSON retains its previous shape. This is not a replay compatibility verdict.

Independent Windows Release reruns with `-m:1` and `MSBUILDDISABLENODEREUSE=1` passed: Server wire and transport classes 15/15, including the unchanged missing-mask control, added null and negative masks, unconditional shape, valid conditional round trip, and old-version rejection on both transports; suspended capture 1/1; composer parity 1/1; direct-client conditional validation 5/5. Logs are under `Source/evidence/conditional-closure-*`. The repair author's compiled mask-guard and protocol-version mutants and restored non-incremental build are recorded in `conditional-resource-wire-repair.md`.

Actual Power activation, native conditional interaction and the final MARVEL-870 replay assessment remain required before reachable publication. This synthetic contract does not resolve MARVEL-878 or authorize a replay version change by itself.
