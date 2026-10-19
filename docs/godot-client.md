# Godot client

The desktop client is a Godot 4.7 .NET project. It runs from the editor during
development and exports as a macOS application ZIP or Windows x64 MSIX. The
exported client carries the same embedded engine as editor play and can select
the same socket transport for a separately hosted table.

## Prerequisites

- the .NET SDK selected by [`global.json`](../global.json);
- the .NET 8 runtime hosted by the client; and
- a Godot 4.7 .NET/Mono editor. The standard non-.NET editor cannot load the
  C# project.

The project and its managed dependencies build from the repository root:

```bash
dotnet build src/Marvel.Godot/Marvel.Godot.csproj
```

For local play on Windows, run `pwsh ./tools/run-game.ps1` from the repository
root. It discovers the installed .NET Godot editor and builds before launching.
Use `-NoBuild` only when the current source has already been built.

Setup includes a [Core deck browser, builder and local library](deck-construction.md)
with MarvelCDB JSON and published URL import. Open **Deck library · browse, build,
import**, then capture a valid deck for a seat before starting the game.

For a short current-decision check, start Iron Man against Rhino on Standard
with seed `7`. Keep the opening hand, end the turn and finish the player phase.
Business Problems should remain visible through its form and consequence
choices without opening Complete Choices. Choosing to remain in the current
form exposes the second decision. This is a reproducible gameplay check, not
product acceptance: the owner affirms the tableau and decision area after
trying the working build.

### Table interactions

Minions and allies use continuous collections; side schemes expose landscape
faces and named headers in a scrollable stack. Upgrades and supports share
compact named source strips, including controlled upgrades attached to another
character. Hover or keyboard focus previews a complete card; pinning retains
its detail and owns focus. Inspection fits native controls in 10% steps down
to 50%, without scrollbars or partial card faces. If no complete face fits
beside protected context, an explicit pin notice opens a closeable reading
overlay. Related faces and modifier sources are directly visible choices.

Drag the title band of an upgrade or support into an empty grid slot or swap it
with an occupied slot. The destination outline previews the placement, including
duplicate copies. Escape or dropping outside the grid restores the order.
Dragging preserves the point grabbed on the card, including exhausted characters.
A contact shadow separates the lifted face; cancellation settles back in 220–280 ms
without scaling its text. A fresh grab cancels the old return. Slot gaps and
engine-offered target edges have an 8-pixel acquisition tolerance and 12-pixel
release tolerance; this never adds targets or commits a decision. Reduced motion
restores the final pose immediately. Mulligan and hand reflow use 120 ms cubic-out
movement while retaining the same explicit commitment boundary.
Native ivory/navy cursors distinguish inspection, an available grab, a held card,
an offered drop and an unavailable destination. Original vector artwork is
rasterized at the selected physical cursor size (32–48 pixels), with a stable
grab hotspot across drag states. Cursor changes add no frame-driven pointer lag.
For keyboard use, focus its title grip, press Space, move with arrows, then Enter to
place or Escape to cancel. Ordering follows physical card identities locally
through board updates and resets with the session. Column count depends on the
available area, so adding cards preserves existing slot coordinates at the same
window size. Empty slots remain available for arranging cards.
Compact headers use distinct support/upgrade and ready/exhausted symbols, with
countable counter pips and an explicit numeric treatment for large counts.

Drag an opening-hand card to the discard to stage it physically, or drag it back
to keep it. Space is the keyboard equivalent. Redraw commits the staged set.
Kept cards close the gaps while redraw cards are staged. Reordering the kept
hand preserves the staged cards' local return slots. Hovering a nonempty
discard previews its visible top card.
Mulligan instructions remain in the visible decision area and card accessibility
descriptions; they do not create tooltips over the hand.
Dragging within the hand previews a local insertion order without changing game
state. Playing a hand card opens the current engine-offered composition;
authorized attachment targets can be outside the player area.

History is a vertical account with expandable details. Complete Choices is next
to the current decision. Temporary causal references have a reserved location
and do not create another physical action target. Status cards use shape and
letter badges: octagonal T for tough, jagged S for stunned, ring C for confused;
defense uses a separate shield.

Accepted activity uses native effect glyphs, source/target impulses and local
quantity or status marks on visible affected objects. A zoomable chronology in
the table seam shares occurrences with History's expandable account. Ordinary
actions group costs and consequences beneath their intention; phase-wide ready,
draw and discard batches retain their semantic boundaries.

With motion enabled, consequence playback reserves the prior table slots
and blocks table actions while exact receipt quantities, readiness and departures
land at their causal beat. Authorized draw faces arrive together into the final
hand slots with a short overlapping deal. Meaningful consequences have individual
650–900 ms reading beats; ready/draw groups use one 800 ms beat. Consecutive plain
elimination removals and a terminal movement suffix each share a counted cleanup
beat while retaining every receipt in History. Meaningful consequences between
cleanup runs keep their own beats. Card text never scales for impact: edge
emphasis, clipped connectors and local quantity marks carry the animation. The
accepted snapshot reconciles the
table before the next decision. During playback, the decision entry names the
accepted next prompt. Opening it settles the exact accepted revision and opens
its existing choices without selecting or submitting an answer. Prior-board
gestures remain gated; no old click is forwarded to a replacement control.
Health keeps its typed maximum; health-only receipts omit damage quantities until
an exact damage fact or the accepted snapshot supplies them. Inspection uses the
same temporary facts and retains modal dismissal. Cleanup grouping retains the
complete ordered chronology.
These temporary card presentations do not create intermediate worlds or answer prompts.
Reduced motion presents the accepted snapshot immediately. Sync and Undo cancel
superseded playback. Starting a new draft clears transient object emphasis.
The active attack has a separate source-to-target cue from initiation through
interrupts, defense, boosts, damage and the final resolution window. Drafting,
inspection and reduced motion preserve it; the accepted defender becomes its
target. The cue clears when the attack's final consequences finish presenting,
or immediately when a synchronized snapshot has no active attack.

Interrupts and responses use a contextual modal with engine-offered effect and
recipient choices. Optional windows include Pass; choosing a complete effect
commits it directly. Forced ability notifications require acknowledgement, with
required decisions kept in the same surface. Complex costs and selections still
use the prompt-bound composer; the client never answers a future prompt itself.

The MARVEL-492 SVG direction is approved. Acceptance of this implementation
remains the owner's affirmation of the working build.

### Card rendering

The Compatibility renderer uses native framebuffer pixels with canvas stretch
disabled. Resizing changes the responsive layout, rather than stretching a
1920×1080 logical canvas. The UI scale slider changes integer font and control
metrics; it does not resample the finished interface. This keeps the same text
rasterization at 1080p, 1440p, 4K and fractional window widths. Fitted collection
cards allocate their actual face dimensions
and integer font sizes instead of shrinking a prebuilt control. Theme roots enable
scale-aware font oversampling for ancestor/DPI transforms. Pinned inspection
reveals at its final size, without scaling already-shaped text during animation.

Canonical SVG symbols use `DpiTexture`: their vector source is rasterized at the
requested icon size and rerasterized for canvas oversampling. Resource rows are
composed as vector geometry before rasterization. Card illustrations remain
filtered image textures with mipmaps; they are not fonts or vector symbols.
Transparent edge colors are extended to avoid dark fringes.
B1 polygons have a one-pixel transparent coverage fringe, including the compact
source rails, rotated outer frames and interaction outlines. Relationship
connectors use explicit coverage on both sides of each segment. This smooths their diagonals without a full-screen blur or a change
of renderer.

The rendered redesign gate samples pixels along an isolated production card
edge, a rotated production frame, and a production connector. It requires intermediate coverage between the ink and paper, rather than
trusting a renderer setting. Removing the fringe fails this check. Native card
layout and interaction checks run separately; the pixel check requires a real
rendering device (Xvfb on Linux), not the headless dummy renderer.

These choices follow Godot's [multiple-resolution guidance](https://docs.godotengine.org/en/stable/tutorials/rendering/multiple_resolutions.html),
[DPI texture contract](https://docs.godotengine.org/en/stable/classes/class_dpitexture.html),
and [font guidance](https://docs.godotengine.org/en/stable/tutorials/ui/gui_using_fonts.html).
Native-size hinted raster fonts suit the small table copy; MSDF loses hinting and
can be less clear at small sizes. Geometry antialiasing is a separate concern:
[2D MSAA is unavailable in Compatibility](https://docs.godotengine.org/en/stable/tutorials/2d/2d_antialiasing.html),
so the renderer retains explicit edge coverage for rotated card frames and lines.

### Visual Studio Code

The checked-in VS Code launch configuration builds the managed project, starts
the Godot host at `src/Marvel.Godot`, and attaches the C# debugger. Install the
recommended Microsoft C# extension, open the repository root in VS Code, select
**Play Marvel Champions**, and press F5.

The macOS configuration uses the standard .NET editor location at
`/Applications/Godot_mono.app`. On Windows, set `GODOT4` to the full path of the
main Godot 4.7 .NET executable before starting VS Code. The Godot project stays
a library: Godot is the executable that loads it, so `dotnet run` is not part of
the development launch path.

## Desktop artifacts

The committed export presets deliberately contain no signing identity or
credential. They export the application first; the build scripts then place the
three runtime datasets and `release-manifest.json` beside the executable where
the exported client resolves them. The setup toolbar displays the compiled
product, replay-contract, protocol and save-schema identities. Its tooltip gives
the complete source commit.

An ad-hoc-signed macOS developer input is built from a clean checkout with the
official Godot 4.7.1 .NET editor and installed .NET export templates:

```bash
commit=$(git rev-parse HEAD)
mkdir -p artifacts
bash tools/build-macos-desktop.sh \
  --godot "/Applications/Godot_mono.app/Contents/MacOS/Godot" \
  --version "0.1.0-dev.0" \
  --commit "$commit" \
  --output artifacts/macos
```

Windows PowerShell uses the Windows .NET editor and Windows SDK. A developer
package uses a commit-scoped unsigned identity, not the preview/stable package
family:

```powershell
$commit = (git rev-parse HEAD).Trim()
New-Item -ItemType Directory -Path artifacts -Force | Out-Null
tools/build-windows-desktop.ps1 `
  -GodotBin "C:\Tools\Godot\Godot_v4.7.1-stable_mono_win64_console.exe" `
  -Version "0.1.0-dev.0" `
  -Commit $commit `
  -Output artifacts\windows
```

By default the scripts use Windows SDK 10.0.26100.0 from its machine-wide
installation. A non-administrator may instead restore the official
`Microsoft.Windows.SDK.BuildTools` package and pass its
`bin\10.0.26100.0\x64` directory as `-WindowsSdkBin` to both the build and
signing scripts. The override requires that exact version and architecture
path, containing the pinned `makeappx.exe` and `signtool.exe` inputs.

Both scripts reject dirty source, an output that already exists, a source
commit mismatch, malformed identity, missing templates, incomplete datasets or
a malformed package. The Windows script unpacks the resulting MSIX and checks
every declared payload hash. The macOS script normalizes the unsigned ZIP to the
source commit timestamp, so rebuilding the same clean input does not acquire a
clock identity. Both staging scripts replace the developer SDK roll-forward
policy with `tools/release-global.json`; automation installs those exact .NET
8 and .NET 10 SDK versions alongside the pinned Godot editor and templates.
Windows packaging and signing also use the exact Windows SDK 10.0.26100.0
`x64` tools; the runner's newest installed SDK is not a release input.

Preview and stable delivery runs through
`.github/workflows/release-desktop.yml` from an exact protected `v*` tag. The
workflow runs ordinary CI for that tag and uses pinned Godot, .NET and Windows
SDK versions. It requires no Apple membership, commercial certificate, paid
timestamp or stored desktop signing secret.

The release page publishes these community artifacts with SHA-256 files:

- macOS receives the reproducible timestamp-free ad-hoc application ZIP.
- Windows receives the reproducible unsigned MSIX input and unsigned portable
  ZIP.
- Windows also receives a self-signed MSIX, its public certificate and a
  provenance record linking it to the unsigned input.
- Linux receives the immutable server digest and keyless Sigstore bundle.

The Windows job creates a non-exportable private key in its temporary
CurrentUser certificate store. It signs without a timestamp and publishes only
the public certificate. It verifies the package while that certificate is in a
temporary CurrentUser Trusted People store. SignTool must report the exact
expected untrusted-root verdict; any other signature failure stops the job. The
job then removes that trust entry and deletes the private certificate with its
backing key.

The workflow refuses to replace an existing GitHub release. A preview tag
creates a prerelease; a stable tag creates a stable release. The release notes
lead with the desktop trust limitations. Windows installation and trust testing
on a clean machine remains MARVEL-358 because macOS cannot supply that verdict.

### Open the macOS community build

The macOS app is not notarized or Developer ID signed. Verify its SHA-256 file
before extracting it:

```bash
shasum -a 256 -c MarvelChampions-VERSION-macos-adhoc.zip.sha256
```

After extraction, open the app once. macOS will warn that it cannot verify the
developer. If you trust the checked artifact, Control-click the app in Finder,
choose Open, then confirm Open. On macOS versions that direct you to System
Settings, use Privacy and Security to approve that exact blocked app. Do not
disable Gatekeeper for the whole system.

### Install the Windows community MSIX

The MSIX certificate is self-signed and has no trusted timestamp. It proves
package integrity only after you choose to trust the attached public
certificate. It does not prove a publicly verified publisher identity.

Use an elevated PowerShell window on a disposable test system:

```powershell
$package = 'MarvelChampions-VERSION-windows-x64-community.msix'
$certificate = 'MarvelChampions-VERSION-windows-x64-community.cer'
Get-FileHash $package -Algorithm SHA256
Get-FileHash $certificate -Algorithm SHA256
certutil.exe -dump $certificate
$installedCertificate = Import-Certificate `
  -FilePath $certificate `
  -CertStoreLocation 'Cert:\LocalMachine\TrustedPeople'
Add-AppxPackage -Path $package
```

Compare both hashes with their attached `.sha256` files before importing the
certificate. Confirm that the certificate subject is `CN=Marvel Champions
Community`. Remove both the package and the exact certificate when finished:

```powershell
Get-AppxPackage -Name 'mggarofalo.MarvelChampions' | Remove-AppxPackage
Remove-Item "Cert:\LocalMachine\TrustedPeople\$($installedCertificate.Thumbprint)"
```

On a disposable clean Windows account, the repository script performs those
hash, identity, installation, packaged-entry launch and cleanup checks. Run it
from an elevated PowerShell window; `-Interactive` leaves the launched client
available until you finish a bounded game smoke and press Enter:

```powershell
tools/windows-community-install-smoke.ps1 `
  -Package '.\MarvelChampions-VERSION-windows-x64-community.msix' `
  -Certificate '.\MarvelChampions-VERSION-windows-x64-community.cer' `
  -Interactive
```

The script refuses a preinstalled package or pretrusted release certificate so
its cleanup cannot remove state it did not create. Success removes the package,
its application data and the exact public certificate, then prints
`WINDOWS_COMMUNITY_INSTALL_SMOKE_OK`.

The unsigned portable ZIP avoids changing the certificate store. Verify its
hash, extract it to a user-owned directory, and run `MarvelChampions.exe`. It is
unsigned, so Windows may still show reputation warnings.

## Launch

On Windows, the normal development launch builds the managed project, locates
the installed Godot 4.7 .NET editor, and runs the game:

```powershell
pwsh ./tools/run-game.ps1
```

The launcher checks `GODOT_BIN`, applications on `PATH`, and the documented
`C:\Tools\Godot_v4.7.1-stable_mono_win64` installation. A custom installation
can be supplied for one invocation without exporting an environment variable:

```powershell
pwsh ./tools/run-game.ps1 -GodotBin "D:\Tools\Godot\Godot_v4.7.1-stable_mono_win64.exe"
```

Pass `-NoBuild` to reuse the current managed build or `-Editor` to open the
project in the Godot editor instead of running the game. The normal launch
stays attached to the terminal so runtime diagnostics remain visible; close the
game window or press Ctrl+C there to stop it.

On macOS, point at the executable inside the downloaded `.app` bundle:

```bash
GODOT_BIN="/Applications/Godot_mono.app/Contents/MacOS/Godot"
"$GODOT_BIN" --path src/Marvel.Godot
```

The equivalent manual Windows PowerShell launch is:

```powershell
$GodotBin = "C:\Tools\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64.exe"
& $GodotBin --path src/Marvel.Godot
```

The first screen has separate Start and Join flows. Start offers the committed
Core assignments, an explicit game label and one or 2 distinct heroes in seat
order. Spider-Man, standard Rhino, the recommended selection, no modular set,
or one or more authored modular sets and an optional unsigned 32-bit seed open
a local game. Leaving the seed blank
chooses one before setup and displays it throughout play, so the resulting deal
can be replayed. During play, source actions and offered targets are available
on the table. One dock shows the current situation, editable draft, named
commitment and latest result. Complete choices opens the full prompt editor
using the same draft; payment uses a temporary workspace beside the table.

The supported desktop viewport is 1920x1080 physical pixels or larger.
Higher resolutions provide more space; use the UI scale slider for larger text.
OS display scaling and the application slider are separate. Keep OS high-DPI
support enabled; external screenshot scaling and display compositor filtering
are outside the framebuffer guarantee.

The desktop layout keeps setup controls
and the current commitment visible while keeping local card values readable.
Card inspection supplies full printed text and traits. Smaller viewports are
not a product or release acceptance boundary.

Leave the endpoint blank to start against the embedded engine. Enter a trusted
private-network endpoint to host through a standalone engine. A 2-hero game on
a restricted server provides one copy action for the second seat's invitation.
The client never displays or logs that bearer secret. It removes the in-memory
copy after placing the secret on the system clipboard.

Changing the endpoint invalidates the displayed setup catalog. Choose Reload
setup options to read the assignments from the new service before starting.

The embedded engine is the default. To prefill an already-running standalone
engine on a trusted private network, set one explicit TCP endpoint before
launch:

```bash
MARVEL_ENGINE_ENDPOINT=tcp://127.0.0.1:41923 "$GODOT_BIN" --path src/Marvel.Godot
```

```powershell
$env:MARVEL_ENGINE_ENDPOINT = "tcp://127.0.0.1:41923"
& $GodotBin --path src/Marvel.Godot
```

The value must be an absolute `tcp://host:port` endpoint with no credentials,
path, query or fragment. Invalid configuration is reported as
`invalid_endpoint`; a valid but unreachable service is reported as
`transport_unavailable`. The socket protocol and its bearer capabilities are
plaintext, so this option is for development and trusted private networks, not
for exposure to the Internet.

Join requires the server endpoint, the host's game label and a one-time seat
invitation. The invitation field is masked and cleared before the attach request
is sent. A used, expired or unknown invitation is not retried; ask the host for
a new one. While another seat owns the current decision, the joined client
renders the table as an in-progress waiting view.

The compact sync control in the top-right toolbar remains available at prompts,
while waiting for another player and after the game ends. It reads one authoritative
snapshot without repeating a decision or replaying event history. If a decision
frame was never sent, the client preserves the draft and says retry is safe. If
the frame was sent but its response was lost, the table stays locked until a
sync succeeds. An expired or closed capability returns the client to Join and
must be replaced with a new invitation.

At the supported desktop profile, source and target controls sit beside their
authorized table objects. Complete choices exposes all engine-offered alternatives
and the same prompt-bound draft in a bounded sheet; History expands independently.
Payment identifies known cards, resource abilities and costs before commitment.
Its temporary workspace leaves the hand and public table visible. Each source
has separate Inspect and Discard/Use controls; inspecting its printed face,
opening history or looking through a pile preserves the unpaid choices.
Closing an inspected payment source restores its own control, including when
another card has the same title. Cancel card play abandons the unpaid play;
clicking elsewhere on the table does not cancel it. Table action and drag
mutations are unavailable while this composition is open.
A mandatory later target remains a separate engine decision. The primary
commitment names the intended action.

The latest accepted response remains readable while local choices and inspection
continue. A new accepted response, explicit Dismiss result, authorized undo or
session reset replaces or clears it. The full chronology remains in History.

Compact is the default interface scale. The toolbar slider switches among the
eleven supported scales immediately, including card geometry and the prompt rail,
and the adjacent motion toggle controls event animation. Table and hand cards
expose a concise summary; clicking or keyboard-activating a readable card opens
the full card inspector beside that card. The modal moves focus to its explicit
Close control, closes with Escape or the surrounding backdrop, restores focus to
the source card, and keeps long rules text independently scrollable. Character
health is one current/maximum value rather than separate hit-point and damage
values. Printed resources use only their canonical icon row everywhere,
including the full inspector, matching the printed card rather than adding
resource names or tooltips.
Hosted status cards do not become separate table objects: strong named badges
on their host card show stunned, confused and tough state. Snapshot attachment
and ownership relationships remain implicit; connecting paths are reserved for
the selected prompt's actionable target and resource choices.
Settled synchronization uses the toolbar indicator rather than reserving a
large event-cue box, and the History tab gives its log a readable minimum height.
Completed history units appear there as one player action rather than as their
individual card movements. A card play names its payment cards and resource
abilities in the same sentence; genuine discard results remain subordinate
narrative lines. The current receipt names the latest accepted response, including committed
costs before a later mandatory target. History retains the action chronology. The shareable interaction report uses
schema 2 of the distinct, non-authoritative report contract. It preserves each
response boundary, the visibility-filtered semantic events in their explicit
polymorphic wire form, and the response-scoped narrative selected by the client
for presentation. Synchronization records an explicitly empty narrative instead
of inventing a transition. A failed resolve and its recovered synchronization
remain separate report boundaries. Opened
games record scenario, mode, ordered seats, and resolved modular sets; attached
viewers explicitly report that setup as unavailable. Credentials, invitations,
deployment labels, and concealed engine truth remain absent. Schema 1 reports
can be read, but identify their missing event, narrative, and setup evidence as
diagnostic limitations rather than treating their fabricated empty event arrays
as evidence that nothing happened.
`Undo last action` and the eligible history-row
links submit only cursor boundaries advertised by the server. A draw, search,
reveal, shuffle, random selection, open action or another seat's authority can
therefore disable an earlier boundary without the client reconstructing that
policy. An accepted undo replaces the board and active chronology without
animating inverse events.

For a single resource-cost component, selecting a generator applies its icons
deterministically and leaves excess icons unused. An ordinary wild resource is
still declared on the wire, as the rules require, but the client does not ask
which equivalent declaration to use. The selector remains when an offered card
effect observes which resource type was paid, and payments with simultaneous
components retain explicit destination controls. The prompt carries that
observability; the client does not infer it from card text.

Visible cards carry prompt-authorized `ACTION`, `TARGET`, and `RESOURCE`
controls beside their non-color cues. A hand card may be dragged only to the
prompt owner's live lane to prepare its offered play; a body click still opens
the card inspector and a drop outside that lane changes no draft. When a card
has several offered actions, its attached control opens an anchored chooser
rather than selecting by list order. Keyboard activation of an attached control
and the decision-rail fallback compose the same engine decision. The player
selects targets and payment generators before pressing the fixed, explicit
Commit control; the submission latch accepts that prompt revision once.

An open source chooser owns root input in its own viewport before background
board gestures or passive card previews. Escape dismisses that chooser and
restores its source action focus even while a hover preview is in its dismissal
grace period. Decision dialogs and pinned inspection keep their higher priority;
the chooser's other keys follow normal contained GUI focus handling.

Each authorized snapshot includes a host revision. The client echoes that
revision with its next decision, so a draft made for an earlier prompt is
rejected and synchronized instead of being applied to a later prompt whose
visible action happens to reuse the same object ids.

See the [standalone server guide](server.md) for launch options, restricted-seat
configuration, Docker commands and shutdown behavior.

Operational failures use bounded, distinct presentation states. `SERVICE
UNAVAILABLE` means the endpoint cannot currently be reached; `VERSION MISMATCH`
means the client and server wire versions differ; `SESSION UNAVAILABLE` returns
the player to Join; and `STORAGE FAILURE` requires operator attention. A decision
marked `NOT SENT · RETRY SAFE` may be submitted again. A decision marked
`MUTATION NOT REPEATED` remains locked until the client reads the authoritative
table, including after a server restart. The UI derives these states only from
transport and protocol evidence; it does not infer whether a server restarted.

## Native smoke

The native smoke loads the real scene at each supported UI scale, selects
Spider-Man and Rhino, enters seed `1`, opens the table, mulligans three cards,
proves the consolidated result can collapse and clear without hiding
Web-Shooter's payment generators, drags the exact duplicate Web-Shooter anchor
through target, resource and explicit Commit, changes to hero form and
activates only visible buttons until the UI reports the seeded loss. It exercises
attached action choice, target-selection, resource payment, submit and pass paths, verifies the enemy-attack interrupt
context, checks terminal history access with keyboard and pointer before and after refresh,
checks that history and the primary result remain populated, and prints
`LOCAL_GAME_SMOKE_OK` on success. The full
matrix runs with event motion enabled; a representative desktop profile also
completes the same game with motion disabled.

macOS and Linux:

```bash
GODOT_BIN="/path/to/Godot" bash tools/godot-smoke.sh
# Faster pull-request profile:
GODOT_BIN="/path/to/Godot" bash tools/godot-smoke.sh --representative
```

Windows PowerShell:

```powershell
tools/godot-smoke.ps1 -GodotBin "C:\path\to\Godot_v4.7.1-stable_mono_win64_console.exe"
# Faster pull-request profile:
tools/godot-smoke.ps1 -GodotBin "C:\path\to\Godot_v4.7.1-stable_mono_win64_console.exe" -Representative
```

The focused payment comparison probe uses the same real seeded Core setup and
native pointer/keyboard helpers. It checks unpaid source inspection by exact
card identity, focus restoration, removal of a selected source, history access,
cancellation, recovery visibility and explicit play. Run after building:

```bash
MARVEL_UI_SCALE=150 MARVEL_SMOKE_VIEWPORT=1920x1080 \
  "$GODOT_BIN" --headless --audio-driver Dummy --path src/Marvel.Godot \
  --script res://smoke/payment_comparison_smoke.gd
```

This is bounded interaction evidence. It does not establish the MARVEL-431
Spider-Man/Justice ordinary-turn comprehension journey, dense-table acceptance,
or human enjoyment.

The hosted multiplayer smoke starts a real restricted server, loads two
independent `Main.tscn` instances at the supported 1920×1080 desktop viewport,
and drives the visible Start, Copy
invitation, Join, decision, synchronization and terminal controls. It requires
a working system clipboard, so Linux runs it under Xvfb rather than Godot's
clipboard-free headless display:

```bash
GODOT_BIN="/path/to/Godot" bash tools/godot-hosted-multiplayer-smoke.sh
```

```powershell
tools/godot-hosted-multiplayer-smoke.ps1 `
  -GodotBin "C:\path\to\Godot_v4.7.1-stable_mono_win64_console.exe"
```

The hosted runner also has an invitation-layout profile: pass `--invitation-layout`
to the shell runner or `-InvitationLayout` to PowerShell. It keeps a fresh normal
restricted table’s invitation unused and checks sidebar containment and whole
captions at 100/150 with collapsed/expanded history. Both profiles run on Windows
and Linux.

The `text-resolution` native cases resize the real root window through 1080p,
1440p, 4K and an odd-width window, then open the deck editor and table. The
rendered visual QA runner additionally compares a fixed inherited-font crop
byte-for-byte across those resizes at 50/80/100/150% UI scale. This proves
resolution changes do not resample that text; it does not certify every OS DPI
or make tiny text larger. Inspect retained PNGs at 100% image zoom.

The local matrix uses `--headless`; it does not use movie capture. The visual QA tools
additionally check and retain rendered viewport images at
setup, open-table/prompt, player-phase, villain-phase and terminal checkpoints.
The open-table checkpoint includes the dense horizontal rails and both visible
player cards and concealed deck cards. They run both motion preferences at the
supported 1920x1080 profile:

```bash
GODOT_BIN="/path/to/Godot" bash tools/godot-visual-qa.sh
```

```powershell
tools/godot-visual-qa.ps1 -GodotBin "C:\path\to\Godot.exe"
```

The opt-in redesign gate runs the complete native journey at the desktop
contract only: 1920x1080 at 100% and 150%, for both one- and two-player games.
It retains rendered checkpoints and requires all opening-hand choices and
post-mulligan actions to remain visible, the shell to retain the viewport width
after ending the turn, scrollbar chrome to remain hidden, discard piles to stay
compact, and repeated outside clicks to dismiss the inspector without activating
the control beneath it:

```bash
GODOT_BIN="/path/to/Godot" bash tools/godot-redesign-gate.sh
```

```powershell
tools/godot-redesign-gate.ps1 -GodotBin "C:\path\to\Godot.exe"
```

Set `MARVEL_SMOKE_CAPTURE_DIR` to retain the PNGs at a chosen absolute path;
otherwise the tools create a temporary directory and print it. No capture is
written into the repository.

CI downloads the official Godot 4.7.1 .NET archives and verifies their SHA-256
digests. Pull requests run the representative native game on Windows and Linux;
`main`, manual and protected release runs execute the complete viewport and
scale matrix on both. The hosted and rendered-state paths always run on both
operating systems. macOS uses the same checked-in scripts as a local release
check because the ordinary CI matrix has no macOS runner.

The companion managed test drives the same seed through `LocalGameClient` and
`DecisionComposer`. It makes the deterministic gameplay path debuggable without
turning the native smoke into a second rules implementation. The native smoke
also synchronizes once at an ordinary prompt and once after the terminal result,
proving the board remains operable and its event chronology is unchanged.

A second managed journey runs two independent Godot-module clients against one
persistent restricted socket server. It pins a complete two-hero Rhino game,
seat-specific prompts and hands, concealed player and encounter decks, public
state agreement, invitation replay, wrong-seat and stale decisions, client
reconstruction, and repeated terminal synchronization. The exhaustive private
information checks stay managed; the native smoke owns scene loading and real
control signals on each declared client platform.

## Visual system

`VisualSystem` is the presentation-only contract for color, type, spacing,
control size and interactive states. `ClientTheme` maps those semantic tokens
to one inherited Godot theme. Authored scene controls and controls created by
the board and decision renderers use named theme variations instead of local
colors, font sizes or style boxes.

The direction is a dark tabletop mission dossier: warm paper text, an amber
briefing signal, blue legal-target markers and a red encounter rail. The thick
left rail is the signature cue shared by cards, status notices and selected
targets; it evokes a divided encounter file without copying a printed card
frame or requiring scanned art.

State never depends on hue alone. Hover raises the lower edge, keyboard focus
adds an expanded focus ring, legal targets carry a diamond marker and left
rail, selections carry a checkmark and heavier rail, and unavailable actions
say `UNAVAILABLE` as well as using a disabled treatment. Managed tests pin the
declared contrast and metric contracts; the native smoke resolves the actual
Godot styles.

The toolbar scale control offers every ten-percent step from 50% through 150%
and defaults to 80%. For automated desktop checks or local use,
`MARVEL_UI_SCALE` accepts those percentages, with or without `%`; `compact`,
`standard`, `large`, and `extra-large` remain aliases for 80%, 100%, 120%, and
150%. Gameplay and its deterministic state are unaffected.

## Optional local art pack

The client can place local illustrations inside the procedural card face. Every
full frame reserves the same illustration region when no image is available. Art
never replaces the title, rules text, live values or other visibility-safe
information, and it is never fetched over the network.

By default the client looks in `art-pack` beneath Godot's per-user application
data directory. Set `MARVEL_ART_PACK` to an explicit local directory to use a
different pack. That directory contains a `manifest.json` with this v1 shape:

```json
{
  "version": 1,
  "entries": {
    "01001a": {
      "file": "01001a.png",
      "authorized": true,
      "rights": "Why this local copy is authorized for use."
    }
  }
}
```

Face ids are exact and case-sensitive. Files must be relative paths inside the
pack, no path component may be a symbolic link, and the supported format is
PNG. Manifests are limited to 1 MiB, image files to 20 MiB, total accepted
compressed art to 64 MiB, processed entries to 2048, and PNG dimensions
to 4096 pixels per side before decode. Authorized image bytes are loaded once
when the client opens the pack, so later filesystem changes cannot alter play.
`authorized` must be true and
`rights` must be nonblank; the pack owner is responsible for that assertion.

Missing entries, malformed manifests, path escapes, URLs, unauthorized entries
and invalid images silently use the complete procedural face. Concealed cards
carry no face id, and the renderer never asks the art pack about them. This
repository intentionally bundles no card images; an image may be committed only
with its redistribution rights documented alongside it.
