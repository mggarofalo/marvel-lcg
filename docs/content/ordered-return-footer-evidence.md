# Ordered-return footer at large reading scale

The measured candidate is `e5c76701fdbc2903cfb2ee6a608ea400a5cdd408`
plus the layout and native assertions in the commit containing this record.
The production change belongs to Godot: ordered card galleries use the existing
bounded, focus-following decision body, with the existing separate commitment
bar. Ordinary search galleries retain their complete-card, non-scrolling layout.
No rules, card implementation, ability book or setup data changes are involved.

## Distinguishing evidence

On Windows with Godot `4.7.1.stable.mono.official.a13da4feb`, the base layout
with the new visibility assertion fails the rendered 150% Heimdall case:
`Timed out: fully visible enabled button 'Return in selected order'`.
Only three resolve requests were accepted. The selected-order PNG shows the
footer below the 1080-pixel boundary, with only its top strip exposed.
Artifacts: `C:/Users/mggar/Source/heimdall-footer-before/`.

The shared driver assertion checks all four transformed control corners against
the viewport and every clipping ancestor, and rejects disabled controls.
Its synthetic contracts distinguish a complete control from a partly offscreen
control and a partly clipped control whose center remains inside its ancestor.
The native Heimdall script additionally requires the entire commitment rectangle
inside its modal. Real Tab input reaches the target controls through the body's
focus-following scroll; actual pointer input selects exact physical targets.
The existing authority assertions retain the reverse selected order, unchanged
revision during staging, exactly four resolves, final physical deck order and
concealment after return.

## Reproduction

From `C:/Users/mggar/Source/marvel-heimdall-layout`:

```powershell
dotnet build src/Marvel.Godot/Marvel.Godot.csproj -c Debug --nologo
dotnet build tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj -c Release --nologo
dotnet test tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~LayoutTests|FullyQualifiedName~HeimdallNativeSceneTests'
pwsh -NoProfile -File tools/godot-acceptance.ps1 -NoBuild -ResultsDirectory <fresh-external-directory>
pwsh -NoProfile -File tools/godot-acceptance.ps1 -NoBuild -Rendered -ResultsDirectory <fresh-external-directory>
& tools/native-cases.ps1 -GodotBin C:/Tools/Godot_v4.7.1-stable_mono_win64/Godot_v4.7.1-stable_mono_win64_console.exe -NoBuild -Case @('contract-heimdall-order-*','raid-dialog-*','search-choice-100-motion','search-choice-150-motion','shuri-search-150-motion','contract-card-look-100-motion','contract-wakanda-daggers-first-100-motion','synthetic-grouped-contract-100-motion') -ResultsDirectory <fresh-external-directory>
```

Debug and Release builds pass without warnings; 26 focused managed tests pass.
The final driver contracts pass headless and rendered, with results under
`C:/Users/mggar/Source/heimdall-footer-driver-final-headless/` and
`C:/Users/mggar/Source/heimdall-footer-driver-final-rendered/`.
The combined native results and individual logs are under
`C:/Users/mggar/Source/heimdall-footer-final2/`.
All thirteen selected cases pass on the pre-extraction Debug/Release candidate:
Raid at 80/100/150%, search at 100/150%, Shuri search at 150%, Wakanda
Daggers-first, synthetic grouped selection, Futurist card look, and Heimdall
at 100/150% in both headless and rendered modes. The search and Wakanda cases
also retain their existing multi-scale checkpoints and draft/revision checks.

The final source extracts body installation and its scroll policy into
`DecisionPanelPromptRenderer.InstallBody`, keeping editor construction separate
and satisfying the existing complexity gate without changing its limits.
Fresh Debug/Release builds and the same 26 managed tests pass after extraction.
All six affected native cases pass again: ordinary search at 100/150%, and
Heimdall at 100/150% headless and rendered. These final-source results and
captures are under `C:/Users/mggar/Source/heimdall-footer-closeout/`.
The broader thirteen-case run above remains evidence for the pre-extraction
source, rather than a claim that every case was rerun after extraction.

All six final Heimdall PNGs were opened: `heimdall-entry.png`,
`heimdall-private-three.png`, and `heimdall-selected-order.png` under both
`artifacts/contract-heimdall-order-100-rendered/` and
`artifacts/contract-heimdall-order-150-rendered/` in that directory.
The entry identifies Heimdall and its cause. The private look shows all three
complete encounter faces. Both selected-order captures show the source's causal
explanation, two selected targets, Crowd Control then False Alarm, and the complete
enabled Return in selected order button. At 150%, the modal occupies y=90..990
and the button y=910..976, safely inside the 1080-pixel viewport. The source card
is accessible through the visibly scrolling body; the text was not reduced.

An initial broad scroll change failed the existing ordinary search assertion
`search choices introduced a scrollbar container`. That assertion remains intact;
the final implementation restricts this layout change to ordered galleries.
The initial run under `heimdall-footer-final/` is not final passing evidence.

This is a correction to an existing interaction. It does not establish Thor
play/payment, Lightning Strike typed payment, multiplayer native rendering,
an exported package, Linux execution, the complete native profile or owner
acceptance of materially new interaction design.
