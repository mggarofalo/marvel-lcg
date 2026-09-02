# Godot client

The desktop client is an editor-run Godot 4.7 .NET project. Exported application
bundles are not part of the current local-play slice.

## Prerequisites

- the .NET SDK selected by [`global.json`](../global.json);
- the .NET 8 runtime hosted by the client; and
- a Godot 4.7 .NET/Mono editor. The standard non-.NET editor cannot load the
  C# project.

The project and its managed dependencies build from the repository root:

```bash
dotnet build src/Marvel.Godot/Marvel.Godot.csproj
```

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

The first screen should offer the committed Core assignments. Spider-Man,
standard Rhino, the recommended modular set and any unsigned 32-bit seed open a
local game. All subsequent decisions are made in the right-hand decision rail;
no console or debug action is part of play.

The embedded engine is the default. To use an already-running standalone engine
on a trusted private network, set one explicit TCP endpoint before launch:

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

## Native smoke

The native smoke loads the real scene at each supported UI scale, selects
Spider-Man and Rhino, enters seed `1`, opens the table and activates only
visible buttons until the UI reports the seeded villain win. It exercises both
submit and pass paths across seven authoritative responses, checks that the
event log is populated, and prints `LOCAL_GAME_SMOKE_OK` on success. The full
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

The smoke uses `--headless`; it does not use movie capture. The visual QA tools
additionally check and retain rendered viewport images at
setup, open-table/prompt, player-phase, villain-phase and terminal checkpoints.
The open-table checkpoint includes the dense horizontal rails and both visible
player cards and concealed deck cards. They run both motion preferences at the
representative 1280×720 standard-scale profile:

```bash
GODOT_BIN="/path/to/Godot" bash tools/godot-visual-qa.sh
```

```powershell
tools/godot-visual-qa.ps1 -GodotBin "C:\path\to\Godot.exe"
```

Set `MARVEL_SMOKE_CAPTURE_DIR` to retain the PNGs at a chosen absolute path;
otherwise the tools create a temporary directory and print it. No capture is
written into the repository.

CI downloads the
official Godot 4.7.1 .NET archives, verifies their SHA-256 digests and runs this
path on Windows and Linux. The macOS command is verified with the 4.7 .NET
editor and exercises the same checked-in script.

The companion managed test drives the same seed through `LocalGameClient` and
`DecisionComposer`. It makes the deterministic gameplay path debuggable without
turning the native smoke into a second rules implementation.

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
adds an expanded three-pixel ring, legal targets carry a diamond marker and
left rail, selections carry a checkmark and heavier rail, and unavailable
actions say `UNAVAILABLE` as well as using a disabled treatment. Standard body
text is at least 16 logical pixels, captions are at least 14, and interactive
targets have a 44-pixel floor. Managed tests pin the declared contrast and
metric contracts; the native smoke resolves the actual Godot styles.

The standard scale is the default. For desktop accessibility checks or local
use, set `MARVEL_UI_SCALE` to `large` or `extra-large` before launch. These
select the other tested, monotonic type and spacing scales; gameplay and its
deterministic state are unaffected.

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
