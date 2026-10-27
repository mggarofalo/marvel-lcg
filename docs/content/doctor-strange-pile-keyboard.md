# Other piles keyboard repair

MARVEL-842 repairs the overflow pile opener's native keyboard behavior. The
Godot renderer explicitly sets `FocusMode.All`, matching the existing region
inspection menus. Focus belongs to Godot; this changes no engine, View, card
ability, setup or protocol behavior.

The independent review of `ed03a479` with the public native evidence supplement
found that the default MenuButton focus mode was accessibility-only (3). Eighty
actual Tab presses visited 25 controls without reaching Other piles. Dismissing
the pile inspector returned to a card action instead of its opener and emitted
Godot's focus warning. Baseline evidence is external at
`C:/Users/mggar/Source/evidence/strange-public-native/independent-review/keyboard`.

`invocation_pile_keyboard_review.gd` extends that review probe through the complete
keyboard inspection round trip. It uses the public Doctor Strange/Rhino Standard
setup with the printed starter, seed 2 and recommended Bomb Scare. It keeps the
hand once, performs the public supplement's initial pointer inspection, asserts
Escape focus restoration, then leaves the opener with Tab and traverses back
through actual viewport keyboard input. Enter opens Other piles; arrow keys and
Enter select the unique Invocation deck; Escape dismisses the native inspector
and restores the same opener. The draft, public pile and revision 1 remain
unchanged. The run log contains exactly one resolve, the initial mulligan.

Windows Godot `4.7.1.stable.mono.official.a13da4feb`, 1920×1080, 100% scale,
one player and reduced motion passed. The rendered run reached the opener after
25 Tab presses, reported focus mode 2, showed the one authorized Seven Rings
face (`1 / 1`), and restored the opener. Inspected captures show the complete
Invocation face and the amber focus ring around Other piles after dismissal.
The table capture also retains a passive hand-card preview; it does not obscure
the opener. The native popup has its own inspector PNG.

Commands (Debug client and Release test host rebuilt; Release server dependency
copied to the Debug runtime before the first rendered run):

```powershell
dotnet build src/Marvel.Godot/Marvel.Godot.csproj -c Debug
dotnet build tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj -c Release
Get-ChildItem tests/Marvel.Godot.Tests/bin/Release/net8.0 -Filter Marvel.Server*.dll |
  Copy-Item -Destination src/Marvel.Godot/.godot/mono/temp/bin/Debug -Force
pwsh tools/godot-acceptance.ps1 -NoBuild -Rendered `
  -Script res://smoke/automation/invocation_pile_keyboard_review.gd `
  -ResultsDirectory <fresh-external-directory>
pwsh tools/godot-acceptance.ps1 -NoBuild -ResultsDirectory <fresh-external-directory>
dotnet test tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj -c Release --no-build `
  --filter 'FullyQualifiedName~BoardInteractionFocusTests|FullyQualifiedName~OverflowSourceInspectionTests|FullyQualifiedName~CardInspectionProtectionTests|FullyQualifiedName~CardInspectionDraftTests'
```

All ten selected managed contracts pass. The shared native driver contracts
pass; their deliberately rejected gestures print expected failure diagnostics.
Removing the explicit focus mode and rebuilding is the negative control: the
same keyboard probe rejects Escape's failure to restore the opener. The
independent baseline separately proves the unreachable Tab route.

Evidence is under `C:/Users/mggar/Source/evidence/strange-pile-keyboard/`:
`rendered-2/` holds the first passing round trip, `driver-contract/` the driver
suite, `managed/` the TRX, and `mutant/` the rejected rebuilt negative control.
The initial `final-rendered/` run rejected a probe parse error and is not passing
evidence. `restored-final/` holds the post-mutation passing rebuild and run;
`final-split/` repeats the rendered proof after decomposing the probe into
focused traversal, inspection and dismissal helpers for the complexity gate.
All standalone runners bound and harvest their processes. This is narrow
regression evidence; it does not claim Linux, other scales, package verification,
whole-game coverage or owner product acceptance. Root integration owns Plane
and independent review closure.
