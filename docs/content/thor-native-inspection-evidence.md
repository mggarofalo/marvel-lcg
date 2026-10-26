# Thor native inspection evidence

The measured source is `e5c76701fdbc2903cfb2ee6a608ea400a5cdd408`, based on
`96b14f94` plus the Thor admission candidate and its mutation-evidence commits
(`c823a4b1`, `86a6a097`, cherry-picked as `3683a83b`, `a5f0f3d8`).
This is automated component evidence, not complete Thor product acceptance.

## Fixture and observations

`HeimdallNativeScene` calls the production `DatasetGameFactory.Create` for Thor.
Only after legal public setup does its labelled fixture preparation put the actual
full-book Heimdall into play and stack three existing physical encounter cards.
It answers opening-hand fixture prompts to expose the pending entry response.
Those preparations are distinct from the native gestures; this is not a played
Heimdall payment or complete opening-to-entry transcript.

The saved GDScript uses the shared viewport input driver for the response, Look,
discard selection, ordered target selection and each explicit commitment. The
authoritative socket assertions require exactly four resolve requests: accept
the response, begin looking, discard the middle physical card, return the other
two in reverse order. Selection does not commit early. The final deck is hidden
again, its exact top-first physical order matches the answer, and play resumes.

The managed two-seat fixture test checks public Thor discovery, three distinct
physical candidates, concealment before Look, identities visible only to the
acting seat during the inspection, two remaining faces after discard, and
concealment after ordered return. Its rule citation has the normal citation gates.

## Commands and results

Run from `C:/Users/mggar/Source/marvel-thor-native` on Windows with Godot
`4.7.1.stable.mono.official.a13da4feb`:

```powershell
dotnet build src/Marvel.Godot/Marvel.Godot.csproj -c Debug --nologo
dotnet test tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj -c Release --filter 'FullyQualifiedName~HeimdallNativeSceneTests|FullyQualifiedName~RuleCitationTests' --nologo
pwsh -NoProfile -File tools/native-cases.ps1 -GodotBin C:/Tools/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64_console.exe -Case 'contract-heimdall-order-*' -NoBuild -ResultsDirectory C:/Users/mggar/Source/thor-native-final
```

Debug and Release builds passed without warnings. The focused managed run passed
three tests. Normal commit hooks passed 66 architecture tests, complexity and
file-length checks. The coherent final four-case native run passed:

| Case | Seconds |
|---|---:|
| `contract-heimdall-order-100-reduced` | 12.908 |
| `contract-heimdall-order-150-reduced` | 9.109 |
| `contract-heimdall-order-100-rendered` | 14.564 |
| `contract-heimdall-order-150-rendered` | 10.985 |

Every case uses 1920 by 1080, one player and reduced motion. Rendered cases set
`MARVEL_NATIVE_RENDERED` explicitly in the manifest; inherited `MARVEL_*` values
are intentionally removed by the process runner. An earlier directory named
`thor-native-rendered-final` contains headless runs and is not pixel evidence.

Machine-local logs and artifacts remain outside the repository:

- `C:/Users/mggar/Source/thor-native-final/results.jsonl` and each case's `.log`.
- `C:/Users/mggar/Source/thor-native-fixture-test.log`.
- `C:/Users/mggar/Source/thor-native-debug-build.log`.
- `C:/Users/mggar/Source/thor-native-commit-final.log`.

## Inspected captures and concrete remaining finding

All six PNGs were opened and inspected: `heimdall-entry.png`,
`heimdall-private-three.png`, and `heimdall-selected-order.png` beneath both
`C:/Users/mggar/Source/thor-native-final/artifacts/contract-heimdall-order-100-rendered/`
and the corresponding `contract-heimdall-order-150-rendered/` directory.
The response identifies Heimdall and its cause. Both private-look captures show
False Alarm, Shadow of the Past and Crowd Control with the discard explanation.
Both ordering captures show Crowd Control then False Alarm as the chosen order.

At **150%**, `heimdall-selected-order.png` exposes only the top strip of the
Return in selected order footer at the bottom of the viewport; its label is
clipped below the 1080-pixel boundary. At 100% the complete footer is visible.
The input driver can click the exposed hit area, so the automated success is not
a sufficient rendering assertion. This is a concrete agent-diagnosable layout
regression to fix and guard with a fully-visible commitment assertion before
accepting the scale profile. It is not a request for routine owner approval.
The ordering gallery also presents the Heimdall source face above text choices;
the capture records that actual presentation without claiming a new interaction
design was accepted.

## Uncovered scope

Lightning Strike's actual printed one-resource play cost plus variable energy
payment and causal recipients has no new native journey in this bounded packet.
The existing variable-payment case covers Energy Channel and the declaration
case covers For Justice; neither establishes Lightning Strike's combined cost.
Heimdall play/payment, cancellation, changing a selected order, multiplayer native
rendering, exported packages, Linux execution and a full native profile were not
run here. The privacy check is managed two-seat evidence, separate from the
single-seat native capture. No product Rules, Cards, ability book or setup data
was edited by this packet, and no PR was opened or published.
