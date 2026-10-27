# Independent review: Invocation payment viewport sizing

Verdict: accept `dff1e9be87847afc11c5263f03a34e0268721b38` for the focused payment
viewport regression. No blocking findings. Reviewed on branch
`review/invocation-payment-scale`; no production or test assertions changed by
this review. This prepared nonpublic component evidence does not admit Doctor
Strange publicly or substitute for owner acceptance of materially new UI.

The 11-line renderer change belongs in Godot: it reserves layout space from
attached native controls and does not interpret game rules, costs or labels.
Measurement follows affordance, draft, payment and cancellation construction.
Recursive `BaseButton` discovery covers body buttons and their native subclasses,
including nested resource/declaration/target controls. Existing larger workspace
minima are retained. Bodies without buttons retain their existing minimum;
search/ability layouts without this scroll node are unchanged. The bound is one
control's minimum height, not the complete body's height, so long lists continue
to scroll and the commitment remains separate. SpinBox variable editors already
reserve the control-metrics minimum independently. No broader layout policy or
new interaction is introduced.

Independent evidence is outside the repository at
`C:/Users/mggar/Source/marvel-evidence/invocation-native/payment-scale-review/`.
Fresh nonincremental Debug client and Release Godot test-host builds both passed
with zero warnings/errors (`debug.log`, `release.log`). With Godot discovered by
`tools/godot-local.ps1`, `MARVEL_NATIVE_INVOCATION=true`,
`MARVEL_NATIVE_RENDERED=true` and the external evidence root, this command passed:

```powershell
dotnet test tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj -c Release --no-build --nologo --filter 'FullyQualifiedName~InvocationNativeJourneyTests&DisplayName~80' --logger 'trx;LogFileName=focused-native.trx' --results-directory C:/Users/mggar/Source/marvel-evidence/invocation-native/payment-scale-review/rendered
```

Both cases passed: Spell Mastery and Master of the Mystic Arts at 80%, Windows
Godot 4.7.1 Mono, 1920x1080, two seats, reduced motion. Each case records
`UI_ACCEPTANCE_OK` and `INVOCATION_RESOLVE_COUNT 2`; existing assertions retain
physical Energy 41, recipient 0, reversible staging, paid continuation and final
Invocation identity. The native PTY was harvested with exit 0.

The retained base negative control at
`C:/Users/mggar/Source/marvel-evidence/invocation-native/copy-review/base-rendered/80/spell/49024-8852594/failure.json`
was inspected directly. Its `DecisionBodyScroll` is 36 pixels tall and the focused
`Resource41` button is 48 pixels tall, so the existing full-visibility assertion
cannot pass at any scroll position. That same assertion is unchanged between
`c1b5656c` and the reviewed revision and passes in the independent current build.
This existing negative control supplies the distinguishing regression evidence;
no duplicate test or additional source-removal mutant was needed.

All six source `payment-scale/rendered/{80,100,150}/{spell,master}/recipients.png`
captures were inspected, plus both 80% payment captures and the 150% Spell payment
capture. Instructions, paid parent context, current selection, card details and
commitment remain visible. At 150% Spell, the lower decorative frame extends
past the viewport, while the complete commitment button, its label and meaningful
content remain visible. This is the documented existing gallery-frame geometry,
not a claim that this patch repairs it. No newly clipped meaningful content or
oversizing was observed. The source matrix TRX records all six cases passed;
its focused managed record reports 57 passed. Those are reused source evidence,
separate from the two independent native reruns above.

Review scope excludes the complete Godot lane, other OS/export packages and
whole-product comprehension acceptance. The source broad managed run was
incomplete and is not accepted as a passing assembly; its unrelated encounter
fixture correction belongs to integration. No rules, ability book, datasets or
engine behavior changed here.
