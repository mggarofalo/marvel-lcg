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

On macOS, point at the executable inside the downloaded `.app` bundle:

```bash
GODOT_BIN="/Applications/Godot_mono.app/Contents/MacOS/Godot"
"$GODOT_BIN" --path src/Marvel.Godot
```

On Windows PowerShell, point at the .NET editor executable extracted from the
official archive:

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
can be replayed. All subsequent decisions are made in the right-hand decision
rail; no console or debug action is part of play.

The project opens at 1920x1080 by default. Its compact layout remains supported
down to 1040x680, but the large desktop canvas is the intended play profile: it
keeps setup controls and the decision rail visible while leaving the table room
for complete card names, printed text, traits, statistics and live values.

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

The rail is a desktop workbench rather than a summary card. Its Action tab owns
the available height and shows several affordances at once; the complete
diagnostic chronology remains in the adjacent History tab. At wide desktop
sizes the rail grows to 680–720 logical pixels. Board areas use fixed shelves so
multiple areas wrap into each lane instead of stretching one area across the
whole remaining table. Occupied villain, scheme, identity and other live-play
areas begin open; draw piles, discard piles and empty engine areas sit behind a
single More areas disclosure in each scenario or player lane. A prompt target
opens either disclosure automatically when needed. A player's visible hand is
pinned below the table scroll so it does not disappear while inspecting another
area, and its wider cards keep complete Core Set titles readable at the desktop
profile.

The decision header asks a player-facing question and names its visible card
source. Wire label, question-kind and timing metadata remain selectable in the
History tab. After an action is selected, its summary stays above an independently
scrolling target and payment editor, while validation, overpayment consequences
and the final commitment stay fixed below it. A newly resolved action appears as
a compact narrative result above the editor. It can be collapsed or dismissed,
clears when the next action is opened, and expires after eight seconds; the full
chronology remains in History.

Compact is the default interface scale. The toolbar slider switches among the
eleven supported scales immediately, including card geometry and the prompt rail,
and the adjacent motion toggle controls event animation. Table and hand cards
expose a concise summary; clicking a readable card opens the full card inspector
beside that card. The inspector remains pinned and scrollable until the same card
or the surrounding interface is clicked. Character
health is one current/maximum value rather than separate hit-point and damage
values.
Settled synchronization uses the toolbar indicator rather than reserving a
large event-cue box, and the History tab gives its log a readable minimum height.
Completed history units appear there as one player action rather than as their
individual card movements. A card play names its payment cards and resource
abilities in the same sentence; genuine discard results remain subordinate
narrative lines. An action with dependent decisions does not enter History or
replace Last Resolved until it completes. The shareable interaction report uses
the same action history and omits the raw animation-event causes.
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
Web-Shooter's payment generators, changes to hero form and
activates only visible buttons until the UI reports the seeded loss. It exercises
submit, target-selection and pass paths, verifies the enemy-attack interrupt
context, checks that history and the primary result remain populated, and prints
`LOCAL_GAME_SMOKE_OK` on success. The full
matrix runs with event motion enabled; a representative desktop profile also
completes the same game with motion disabled.

macOS and Linux:

```bash
GODOT_BIN="/path/to/Godot" bash tools/godot-smoke.sh
```

Windows PowerShell:

```powershell
tools/godot-smoke.ps1 -GodotBin "C:\path\to\Godot_v4.7.1-stable_mono_win64_console.exe"
```

The hosted multiplayer smoke starts a real restricted server, loads two
independent `Main.tscn` instances, and drives the visible Start, Copy
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

The smoke uses `--headless`; it does not use movie capture. The visual QA tools
additionally check and retain rendered viewport images at
setup, open-table/prompt, player-phase, villain-phase and terminal checkpoints.
The open-table checkpoint includes the dense horizontal rails and both visible
player cards and concealed deck cards. They run both motion preferences at the
default 1920x1080 profile and the compact 1280x720 regression profile:

```bash
GODOT_BIN="/path/to/Godot" bash tools/godot-visual-qa.sh
```

```powershell
tools/godot-visual-qa.ps1 -GodotBin "C:\path\to\Godot.exe"
```

Set `MARVEL_SMOKE_CAPTURE_DIR` to retain the PNGs at a chosen absolute path;
otherwise the tools create a temporary directory and print it. No capture is
written into the repository.

CI downloads the official Godot 4.7.1 .NET archives, verifies their SHA-256
digests and runs both native paths on Windows and Linux. macOS uses the same
checked-in scripts as a local release check because the CI matrix has no macOS
runner.

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

The client can place local illustrations inside the procedural card face. Art
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
