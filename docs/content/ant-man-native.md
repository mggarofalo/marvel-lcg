# Ant-Man native evidence

The public journey in
`src/Marvel.Godot/smoke/automation/ant_man_public_acceptance.gd` uses the
public Ant-Man starter, Rhino Standard and seed 1. No gameplay fixture is
injected. All setup selections, seed entry, menu gestures and decisions use
viewport input. The witness reads only the authorized response shown by the
client.

The journey checks the six-card Scott Lang opening, his physical identity and
face, separate Tiny and Giant destination controls, dismissal without a
commitment, one Giant form commitment, and the optional Giant response against
the physical Rhino. It checks the response's visible answering seat, decline,
target and one-damage explanation, then the revision and health result. Selecting
the optional response commits revision 3 and opens a target decision; choosing
Rhino commits revision 4 and changes his health from 14 to 13.

On the working tree based on `08ff9b1280971025fd1171c812c3dc3947b13f0d`, Windows
Godot 4.7.1 at 1920×1080, 100%, one player and reduced motion passed:

```powershell
$env:MSBUILDDISABLENODEREUSE='1'
dotnet build src/Marvel.Godot/Marvel.Godot.csproj -c Debug --nologo -m:1
pwsh tools/godot-acceptance.ps1 -Script res://smoke/automation/ant_man_public_acceptance.gd -NoBuild -ResultsDirectory C:/Users/mggar/Source/evidence/ant-man-native-h12
pwsh tools/godot-acceptance.ps1 -Script res://smoke/automation/ant_man_public_acceptance.gd -NoBuild -Rendered -ResultsDirectory C:/Users/mggar/Source/evidence/ant-man-native-r3
```

The rendered setup, opening, destinations, response, target and result images
were inspected in the `ant-man-native-r3/artifacts` directory. The choices are
separate and legible, and the response shows the player, optional pass, one
damage and Rhino's predicted health. The destination menu visibly exposed the
internal face IDs `12001a` and `12001c` in engine-supplied descriptions. Engine
copy fix `3d2d463d` removes those IDs while retaining the destination labels and
the voluntary-change explanation.

An independent retest of that fix, merged with the native packet and its new
no-ID assertion, passed after rebuilding Debug with the command above:

```powershell
pwsh tools/godot-acceptance.ps1 -Script res://smoke/automation/ant_man_public_acceptance.gd -NoBuild -ResultsDirectory C:/Users/mggar/Source/evidence/ant-man-copy-h1
pwsh tools/godot-acceptance.ps1 -Script res://smoke/automation/ant_man_public_acceptance.gd -NoBuild -Rendered -ResultsDirectory C:/Users/mggar/Source/evidence/ant-man-copy-r1
```

The rendered destination, response, target and result images in
`ant-man-copy-r1/artifacts` were inspected. Both destinations name the form and
say the voluntary change is used this round, without internal IDs; the response
and result remain visible. This focused pass does not establish product readiness
or other platforms and scales.

The existing generic native payment cases do not establish Team-Building
Exercise's scoped quote and optional overpayment. A separate public seeded
journey or explicitly labelled prepared component is still required for that
boundary; it is not claimed by this script. Broader managed, replay, platform
and package evidence is tracked separately. Routine content acceptance does
not require an owner review; a materially new interaction or unresolved product
question requires focused review with inspected media.

