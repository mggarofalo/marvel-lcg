# Doctor Strange Invocation interaction contract

MARVEL817 packet based on `650d6c57`, branch
`feat/invocation-interaction-contract`. This is prepared, nonpublic engine
contract evidence. Doctor Strange is not admitted to the public deck picker;
this record establishes neither native usability nor owner acceptance.

The fixture uses the production ability book and all five physical Invocations,
with the current top arranged before exposure. Strange is seat 1, after seat 0's
turn. A separately labelled synthetic replacement removes a Special's discard
instruction solely to test that descriptions do not assume that behavior.

## Concrete contract and repair

| Player question | Existing owner and observed contract | Packet result |
| --- | --- | --- |
| Where is the Invocation deck? | `WorldDescriptor.InvocationDecks` binds owner, deck and separate discard; `BoardPresentation` names both piles. | Preserved; no card-name or area-order inference. |
| Which Invocation will resolve? | The arrow's exact one-card `TargetRequest` contains only the owned public top. | The effect and cost descriptions also name that public top. |
| What must I pay? | `CostOptions` carries the engine's payable amount and generators. Spell Mastery includes identity exhaustion; Master has `PlaysCard` and its event price combined with the Special price. | `CostDescription` names the Special's separate fixed amount using the same engine amount function as payment. Its effect text is no longer embedded in the cost field. |
| Does this exhaust Strange? | Spell Mastery's typed exhaustion cost and visible source state supply the answer. | Preserved; Master does not acquire that cost. |
| What happens to this Invocation? | Ordinary Spell Mastery previously described only resolving the captured Special. Master had no effect description for its dependent return. | The effect field describes resolving the named top and uncertainty. An unconditional final typed `DiscardInvocation` instruction supplies independent discard/public-top refresh text. Master separately describes returning the exact physical card faceup after the Special completes. |
| Who answers? | The actual prompt and both sources name seat 1; restricted seat 0 receives no prompt. | Verified in projection. |
| How does Seven Rings select recipients? | One required prompt, one complete subset, minimum zero, maximum three, no repetitions; each candidate has a Tough consequence. | Preserved through View, including the explicit empty-selection explanation and enemy eligibility. |
| How does Vapors replace a status? | One physical-status prompt followed by a separate legal replacement-kind prompt, both answered by seat 1. | Preserved through View; the old kind is absent from the replacement choices. |

The descriptions interpret typed checked instructions, not printed prose. They
do not calculate legality or forecast target-specific outcomes. The independent
discard statement is emitted only for one Special whose unconditional final
sequence instruction is `DiscardInvocation`. Other shapes retain the generic
resolution description. The exact-return description requires the typed
`Dependent(ResolveInvocation, ReturnInvocation, OnFull: true)` shape. This uses
existing protocol 42 fields and changes no wire, replay, save, digest or RNG
identity. No View, Decisions, Protocol or Godot implementation change is needed
to retain these fields.

## Evidence

Windows Release commands, with normal hooks retained:

```powershell
dotnet test tests/Marvel.View.Tests/Marvel.View.Tests.csproj -c Release
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~SpellMasteryTests|FullyQualifiedName~SpellMasteryPaymentTests|FullyQualifiedName~MasterOfMysticArtsTests|FullyQualifiedName~MasterOfMysticArtsReviewTests|FullyQualifiedName~SevenRingsRecipientTests|FullyQualifiedName~SevenRingsIndependentReviewTests|FullyQualifiedName~VaporsStatusReplacementTests|FullyQualifiedName~DoctorStrangeInvocationCardTests'
```

The full View assembly passes 401 tests. The eight affected whole content
classes pass 80 tests. The ten new contract cases cover all five actual public
tops, the synthetic no-discard control, and both initiation paths through Seven
Rings and Vapors. Projection preserves the state digest, serialized RNG state
and current prompt. Existing content classes establish execution and lifecycle;
the new tests establish authorized explanatory contracts, not new rules.

Artifacts are outside the repository at
`C:/Users/mggar/Source/marvel-evidence/invocation-interaction/`: `view.trx`,
`view.log`, `content-focused.trx`, `content-focused.log`, the bounded mutation
script and its individual logs. Mutations challenge exact-return polarity,
the separate Special amount and assumed self-discard. The restored source is
rebuilt non-incrementally before repeating the contract class.

## Independent engine-contract review

Reviewed source revision `fc061f45` in branch `review/invocation-interaction`.
The bounded verdict is **accept for the prepared, nonpublic engine explanatory
contract**. The printed Doctor Strange and Master costs and the authored typed
Special instructions agree with the offered descriptions. Return semantics follow
the actual successful Special procedure and ruling `ruling:b07d53e12625ab23`:
discard/exposure/recycling precede return of the same physical card. This is not
a promise that later choices or unsupported interrupted execution cannot fail.
The cost calculation reads the resolving owner's current public top; publication
captures strings and target identities rather than deferring a hidden lookup.

Four independent boundary cases add restricted seat 0/seat 1 concealed-order
noninterference, detached published prompt/board behavior after the offered top
departs, and a labelled synthetic failure-only return control. Removing the
`OnFull: true` condition from the description matcher compiled and failed exactly
the failure-only control (13 passed, 1 failed). Restoring the source followed by
a non-incremental build passed the full View assembly: **405 passed**. The initial
eight affected whole content classes also passed **80 tests** on the restored
source. The initial
hidden-order probe needed to restore the top's faceup state after its synthetic
movement; the initial two failures were fixture findings, retained in the log.

Review artifacts are outside the repository at
`C:/Users/mggar/Source/marvel-evidence/invocation-contract-review/`:
`review-initial.log`, `review-baseline.trx`, `review-mutant.log`,
`review-mutant.trx`, `review-restored-build.log`, and
`review-view-restored.trx`, and `review-content-restored.trx`. No engine implementation, View, Decisions, Godot,
wire version, or public admission change belongs to this review.

## Remaining native interaction packet

`DecisionCopy.ActionSummary`, `AbilityLabel` and `AbilityDetail` already consume
the offered cost/effect fields; `CardPaymentWorkspaceHeader` retains them while
paying. Source inspection establishes a rendering route, not pixel evidence.
There was no Godot launch or rendered screenshot in this packet.

The downstream packet needs a clearly labelled prepared engine fixture through
the existing native fixture/driver seam. It must exercise seat 1's ordinary
Spell Mastery and Master against the same top, show separate Special payment
and exhaustion/event commitment, then retain the originating action through
nested prompts. In particular, the Seven Rings prompt currently describes
"later instructions still resolve" without repeating the parent's exact return
consequence; Vapors' replacement-kind prompt identifies the Invocation but does
not itself restate the parent commitment. Whether the surrounding UI preserves
that explanation needs native verification; these tests do not establish it.

Run headless input checks for staging/unstaging a complete Seven Rings subset,
explicit zero confirmation, changing a Vapors status/kind, cancellation before
payment, and rejecting stale/repeated answers. Confirm paid costs cannot be
cancelled by closing a later prompt and that inspecting seat 0 does not transfer
answer authority. Inspect rendered screenshots at payment, each nested choice
and the final separate discard/top/return result before making visual claims.

If this introduces materially new interaction, the lead should create the
specific Plane review item with the exact build and accessible screenshots or
video. Its focused question is whether an experienced player can identify the
Invocation, separate costs, current answering seat, remaining choice and next
commitment throughout both paths, including explicit zero recipients. Agent
interaction and visibility regressions should be fixed before that review.
