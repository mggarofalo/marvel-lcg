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

## Native smoke

The native smoke loads the real scene, selects Spider-Man and Rhino, enters seed
`1`, opens the table and activates only visible buttons until the UI reports the
seeded villain win. It exercises both submit and pass paths across seven
authoritative responses, checks that the event log is populated, and prints
`LOCAL_GAME_SMOKE_OK` on success.

macOS and Linux:

```bash
GODOT_BIN="/path/to/Godot" bash tools/godot-smoke.sh
```

Windows PowerShell:

```powershell
tools/godot-smoke.ps1 -GodotBin "C:\path\to\Godot_v4.7.1-stable_mono_win64_console.exe"
```

The smoke uses `--headless`; it does not use movie capture. CI downloads the
official Godot 4.7.1 .NET archives, verifies their SHA-256 digests and runs this
path on Windows and Linux. The macOS command is verified with the 4.7 .NET
editor and exercises the same checked-in script.

The companion managed test drives the same seed through `LocalGameClient` and
`DecisionComposer`. It makes the deterministic gameplay path debuggable without
turning the native smoke into a second rules implementation.
