# Packaged predecessor campaign verification

This probe resumes the pinned, genuinely played Standard RoRS campaign from
runtime `91098022`, through the selected desktop artifact's own manager and
table. It does not create a replacement campaign. The original fixture has
13 attempts, two completed scenarios, and Taskmaster at revision 5.

Use a fresh directory for each hosting mode. Preserve the downloaded package,
its published SHA-256, the CI run and exact source commit. Extract the portable
ZIP without changing its payload. The runner verifies every payload hash
against the manifest inside that ZIP, including the selected executable.

```powershell
tools/campaign-upgrade-native.ps1 -Journey Prepare -Root C:/Temp/upgrade-embedded
$arguments = @{
    Root = 'C:/Temp/upgrade-embedded'
    ClientArtifact = 'C:/Temp/artifacts/MarvelChampions-VERSION-windows-x64-portable-unsigned.zip'
    ClientArtifactSha256 = 'PUBLISHED_64_HEX_SHA256'
    ArtifactCommit = 'EXACT_40_HEX_SOURCE_COMMIT'
    ClientExecutable = 'C:/Temp/unpacked/MarvelChampions.exe'
    ReplayContract = 'engine-replay-v20'
    ExternalInteraction = $true
}
tools/campaign-upgrade-native.ps1 @arguments -Journey inspect
tools/campaign-upgrade-native.ps1 @arguments -Journey continue
tools/campaign-upgrade-native.ps1 @arguments -Journey restart
```

Each invocation starts a new desktop process and waits for external UI input.
The release template does not support `--script`; the runner never substitutes
an editor process. Use an external native UI driver or the computer-use skill
to open Campaigns, inspect the saved records and select Return to current game.
Retain screenshots and the observed controls. Close the app normally after
each journey. `inspect` must show the exact original prompt. After normal
client shutdown, the runner verifies the entire saved aggregate and original
backup, allowing only explicitly named compatibility fields to differ.
During `continue`, press the actual End turn control once and observe the next prompt.
Its persisted aggregate must match the original predecessor's independently
recorded continued archive, including every event, RNG count, digest and
authority. During `restart`, resume that exact continued prompt without answering it.
Logs and artifact identities remain in the isolated directory; the external
driver must retain its screenshots and action observations there too. The
runner's persistence checks alone do not certify those UI observations.

For remote hosting, pass `-Endpoint tcp://127.0.0.1:PORT` to `Prepare`. The
generated authority root contains `.campaigns`, matching the standalone server
layout. Start the exact selected server artifact with that authority root,
then run `inspect`. Stop and restart that server before `continue`, and again
before `restart`. Retain its executable/image identity, invocation, logs and
process lifecycle evidence alongside the client evidence. The client runner
does not start or certify the remote server. Container mounts must grant the
image's configured user access to the isolated root; never mount a player's
real save directory.

This is a verification tool, not acceptance by itself. All three journeys and
the authority restarts must pass on the final artifacts. Evidence must
distinguish Windows portable desktop packaging from Linux native development
client execution: the current release policy has no Linux desktop package.
A Linux remote campaign claim additionally requires an identified packaged
server image, not a source-built host relabeled as that image. CI's generic
container smoke does not preserve its image for this separate journey.

The fixture is Standard campaign/Standard scenario, with zero delay counters.
Expert campaign evidence is tracked separately in MARVEL-642. Neither
MARVEL-630 nor MARVEL-634 is accepted solely by these Standard checks. Native
journeys are not yet certified until their exact artifact evidence is recorded
in the work item.

## Recorded artifact run

CI run `39483718882`, artifact `12294006860`, produced the Windows portable ZIP
with SHA-256 `c661ac82d9ef0bc2faf355d33b769f5bd4888c9f2b33560fad8fb78203865178`.
Its embedded commit is `d5f86da7641da3fef2488c24609df51df02a4341` (the tested
PR merge commit), product `0.1.0-preview.1`, replay v20 and protocol 35.
That commit, PR head `9097da07`, and merged main `e4c7d7f8` have the same
tree `6fb228f3293ecccddd12ac8ffb2938aac31116ea`.

Three normal exported-app processes exercised embedded hosting through external
Windows accessibility and pointer input. The manager displayed Taskmaster,
Spider-Man, Basic Thwart Upgrade, retained Energy Shield, and consumed Adrenal
Stims. The first process resumed the original player-turn prompt; the second
pressed End turn once; the third resumed the saved end-phase discard prompt.
All three exited normally. Full aggregate comparison passed before continuation
and after both later processes, including exact original backup bytes.

This run is **not clean native acceptance**. Every campaign resume logged
`_sub_window_grab_focus: Condition "index == -1" is true` from
`CampaignController.OpenTable` while hiding the manager. Pointer input reproduced
the same failure as accessibility activation. MARVEL-644 tracks the fix.
The preceding external-script attempt timed out because release export templates
do not support that option; it is not counted as a journey.

## Modal dismissal regression (MARVEL-644)

Godot 4.7.1's embedded `Window` dismissal removes the subwindow before it
clears the parent's exclusive-child association. With accessibility enabled,
the parent focus callback tries to focus that removed child. A standalone
exclusive-window reproduction emits the same diagnostic as campaign resume;
no campaign or gameplay code is needed.
The ordering is visible in upstream
[`Window::set_visible` and its focus callback](https://github.com/godotengine/godot/blob/4.7.1-stable/scene/main/window.cpp)
and [`Viewport::_sub_window_remove`](https://github.com/godotengine/godot/blob/4.7.1-stable/scene/main/viewport.cpp).

`ModalWindowFocus` releases exclusivity during the hidden window's focus-exit
callback and restores the setting at visibility notification. A visible modal
retains its focus boundary, including when a nested dialog owns focus. The
binding covers the owned campaign, deck, published-search and management
dialog constructors, including dialogs that dismiss internally.

The `modal-focus-100-motion` native case checks nested focus, repeated reopen,
dialog cancellation, disposal and nonexclusive windows. Its parent-focus
assertion also runs headlessly; removing the release compiles and fails that
assertion. A rendered Windows run with `--accessibility always` additionally
checks the actual Godot error path. These focused controls do not replace
the packaged campaign journey or Windows/Linux acceptance evidence.

### Windows candidate evidence

The clean local candidate `2bb00463f176162791d30a2d5dd38a72fbd6db15` produced
portable ZIP SHA-256
`772a7de3418bdfaee1c17ee5a246cbd223dc2636ee22b5f591dcf798ca0f30ff`.
This is a locally built artifact, not the downloaded CI artifact above.
Three externally operated exported processes passed resume, End turn, and
restart, with clean native logs and exact full campaign comparisons against
both predecessor controls. The original migration backup remained unchanged.
Screenshots, process logs and manifest/executable identities are retained under
`marvel644-exported-38fb1d6b95e84883a424104cf8e41819` in the recorded evidence root.
Plane MARVEL-630 and MARVEL-644 link the evidence and outstanding requirements.

### Manual Linux evidence workflow

`campaign-upgrade-evidence.yml` builds and retains an exact server image,
including its image ID, revision label, archive checksum and version banner.
Its Linux client is explicitly a source-built development runtime. The workflow
does not claim a Linux desktop release package or replace the external Windows
exported-app observations.

Both hosting modes use three fresh client processes. The remote mode stops
the configured server image after every journey, copies its stopped authority
archive for full comparison, then starts the same container for the next
journey. Only the isolated fixture mount changes ownership to the configured
container user. Client evidence records its own source and executable identity
separately from the server artifact. The workflow retains screenshots, original
proofs, stopped-server snapshots, logs, and container identities.

The development driver uses the existing fixed SubViewport pointer harness.
It does not establish OS accessibility behavior. A forced-accessibility
SubViewport experiment produced an AccessKit diagnostic and is not counted as
clean evidence; the exported Windows root-window journey above passes with
accessibility enabled. A root-window variant of the development pointer driver
failed its hit-area assertion and was not retained as the runner.
