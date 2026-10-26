# Melee native sequential-target proof

The prepared cross-pack component scene uses Spider-Man, Rhino I, one Hydra
Mercenary, and actual full-book Melee 05030. It does not admit Ms. Marvel through
the public catalog or claim a played opening-to-victory transcript. The six-card
prepared hand keeps payment labels unique before any test input.

`contract-melee-sequential-100-reduced` is registered in representative and
exhaustive native profiles, which CI runs on Windows and Linux. The shared native
driver pays exactly three resources (Energy plus Swinging Web Kick), explicitly
chooses the sole Guard, and separately selects and confirms Rhino. Host assertions
pin three resolves, Guard health 3 to 0 and encounter discard, a fresh second
offer excluding the first physical enemy, no premature Rhino damage, final Rhino
damage 3, Melee discard, and the next PlayerAction prompt.

Windows development evidence uses Godot 4.7.1 .NET, Debug client and Release
managed helper, 1920x1080, 100%, reduced motion. The passing manifest headless run
is `%TEMP%/melee-native-final-headless/results.jsonl`. A direct rendered run passed
with `MARVEL_NATIVE_RENDERED=true`, `MARVEL_NATIVE_CONTRACT=melee-sequential`,
`GODOT_BIN` set to the installed console executable, and
`MARVEL_UI_EVIDENCE_DIRECTORY=%TEMP%/melee-native-final-rendered`, using
`dotnet test tests/Marvel.Godot.Tests/Marvel.Godot.Tests.csproj -c Release --no-build --filter FullyQualifiedName~NativeContractJourneyTests`.
The captured `melee-second-choice.png` was inspected: Melee, Spider-Man as the
answering hero, Choose another enemy, the printed 3 damage before modifiers and
Rhino's sole explicit choice remain readable together. Existing continuation
controls provide the interaction; no materially new UI interaction is introduced.

Ambient `MARVEL_NATIVE_RENDERED` is deliberately cleared by the manifest process
isolation. The directory named `melee-native-rendered-01` therefore contains a
second headless run, not pixel evidence. The direct rendered run above is the
pixel evidence. Linux execution and hosted persistence remain outside this local
proof; registration is not a claim of a passing remote CI run.
