# Independent review of paid-event status cancellation transcripts

Reviewed revision: `4b7d42cf92d5ebcd88caa22431f67110b5d00325`, based on
`54b9055a`. Verdict: accept this bounded test/spec repair. No correctness or
assertion-strength finding remains in the reviewed changes. No additional test
was needed, and this review changes no engine, ability, dataset or transcript.

The vendored RR 1.8 text supports the expected decision sequence:
`rr:labeled-ability.6` cancels the entire labeled ability except costs;
`rr:status-cards.2` gives the status priority over conflicting triggered
abilities; `rr:cancel.2` prevents effect initiation while retaining payment;
`rr:cancel.3` retains event play and discard. Initiating-abilities steps 5–7
place payment before attempted effect initiation. Stun/confuse clause 5 retains
costs, and labeled-ability clauses 6.1–6.2 explain no attack/thwart occurrence
and removal of the canceling statuses. The effect recipient in these printed
cards is not an arrow cost. The vendored Melee FAQ additionally states that
stun cancels its one attack with two separate targets in full.

The removed target answers are replaced by direct no-prompt observations,
without staging a target or auto-answering another effect choice. Payment,
event discard, status removal, unchanged damage/threat and Clear the Area's
absent conditional draw remain observed. The staged attack retains its zero
attack-end-response counter and idle-agenda assertion. The Core scenario still
uses the canonical dealt scene and observes both payments, removed stun,
unchanged Rhino damage and the event on top of discard. No digest expectation,
catalog disposition, authority fingerprint or production implementation changes.

Ordinary controls remain distinguishing: Clear the Area's helper requires the
scheme offer; Swinging Web Kick observes its target prompt before damage;
actual Melee's unstunned theory branch answers its enemy choice. Additional
existing control classes prove For Justice! and Swinging Web Kick's deferred
recipient route, semantic target descriptions, and successful staged attack-end
responses with nonzero counters. Adding one deck card to the synthetic For
Justice! fixture isolates cancellation from empty-deck recycling and strengthens
the exact payment-discard observation.

Fresh Windows Release evidence from the isolated review checkout:

- 165 passed, zero failed/skipped: all five changed test classes, the complete
  `Marvel.Content.Tests.Behavior` namespace including the Core corpus, and
  `RuleCitationTests`.
- 16 passed, zero failed/skipped: complete `StagedAttackReceiptTests`,
  `EventRecipientCommitmentTests` and `CoreTargetEffectDescriptionTests`.
- `Marvel.Behavior.Index check` and `Marvel.Cards.Extract check` exited zero;
  source/dataset diff against `54b9055a` and working-tree status were empty
  before this review document was added. `git diff --check` passed.

Logs are external files under `C:/Users/mggar/Source/evidence/`:
`status-priority-review-focused.log`, `status-priority-review-controls.log`,
`status-priority-review-index.log` and `status-priority-review-cards.log`.
The focused command uses `dotnet test
tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter` with
the class/namespace names above; the additional control run uses `--no-build`
against that same fresh build.

The implementation packet's external `status-priority-closure-mutant.log` and
`status-priority-closure-restored.log` were inspected. Disabling the early
`AbilityEventStatusPriority.Cancel` call failed the three initial cancellation
tests and the Core no-opportunity assertion. Its twelve reported failures
include consumers of the same cached corpus failure; they are not twelve
independent mutation kills. This review did not repeat or extend that mutation.

This verdict covers the changed regression expectations and their retained
controls. It does not certify the complete content suite, Linux, public pack
admission, native interaction or product acceptance. The separate consumed
continuation failure is outside this repair and remains owned by its dedicated
fix packet.
