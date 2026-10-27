# Ronan remaining-face checkpoint

MARVEL-567. Candidate based on ca149e0d7c0079142af8049806fad371d301a3e5.
Two production rows, 90003 and 90005, are appended to the exact 719-row prefix.
The book contains 721 rows. Printed authority is joined cards.json; quantities
remain 1/1/1/2/2 under the pinned seven-page PDF in ronan-source-pin.md.
No setup, engine or admission changes. Public exclusion remains.

The current new tests prove actual 90003 ordinary and boost entry on non-first
identity, either-form attachment, exact attack bonus lifetime, other defeat
control and Ronan defeat discard; actual 90005 exact physical discard-to-facedown
selection, multiplayer ownership, and overkill lifetime through completed attacks.
The final reduced candidate passed the exact combined focused filter: 84 passed,
zero failed, zero skipped. This includes the six new test cases (five methods,
one two-case theory), actual Ronan coverage, updated face inventories and
selected ability-data/rule-citation checks. Independent review remains pending.

90002 remains unauthored. Its candidate and complete actual tests are preserved
outside the repository at C:/Users/mggar/Source/evidence/ronan-90002-candidate.json
and ronan-remaining-candidate-tests.cs. Judge entry from reveal and boost passes,
but the cross-seat ally defender defeat test completes the attack and observes
zero main threat instead of printed two. The surviving-ally control passes.
The current attempt uses WhenDamageDealt + WhenCardDefeated with enemy actor and
DamageFromAttack; earlier WhenAttackEnds + defeat also missed the response.
This is a composition/receipt investigation, not an established missing engine
capability. Do not integrate that provisional row. Evidence is
ronan-remaining-focused.log; earlier integrated ability inventory run is explicitly
red (85 passed, three failed), not final green evidence.

90004 remains unauthored. Existing EnemyActivation retains damage, threat and
CompletedEnemyAttack (activation id, defender, damage), but no exact defeat list.
afterActivation exposes no defeat result; wasDefeated reads the current occurrence.
delayUntil admits only discard and damage-triggered stun. A concrete executable
90004 defeat probe, authority adjudication, independent review and its own
mutations/verification remain required. No engine gap is yet claimed.

## Two-face verification checkpoint

Verified against the dirty candidate based on the revision above; no further
production changes followed the restored run. The ability book SHA-256 is
`9411a9fd446fb0c50d0957d9f214bb0a67a51ffb0d41e658dc4501662334bee8`.
The first 719 rows were compared both structurally and as an exact byte prefix
with HEAD; the full book is UTF-8 with LF endings. Printed 90003/90005 text was
reviewed from joined cards.json; neither has a specific vendored FAQ entry.

Evidence is under `C:/Users/mggar/Source/evidence/`:

- `ronan-two-face-complexity.log`: early normal complexity gate, exit 0.
- `ronan-two-face-mutant.log`: actual 90003 attack bonus changed from 1 to 2;
  compiled focused test failed at the intended assertion (expected 3 damage,
  actual 4). Mutant process completed before exact-byte restoration.
- `ronan-two-face-restored-build.log`: restored content test project built with
  `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release
  --no-incremental`, exit 0.
- `ronan-two-face-restored-focused.log`: `dotnet test
  tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build
  --filter $ronanFilter`, 84 passed, zero failed/skipped. `$ronanFilter` is the
  trimmed raw content of `ronan-remaining-filter.txt`; its exact file SHA-256 is
  `54eee686733527d5d0dfb104cf48b967e940fd6be80a8746161714d96b9c0f17`. The earlier reduced run in
  `ronan-remaining-reduced-focused.log` also passed 84, but the restored run is
  the final focused evidence.
- `ronan-two-face-architecture.log`: `dotnet test
  tests/Marvel.Architecture.Tests/Marvel.Architecture.Tests.csproj -c Release`,
  129 passed, zero failed/skipped.

Normal pre-commit architecture-policy, complexity and file-length hooks are
required for the closure commit. This checkpoint claims only two production
rows and focused/architecture evidence; it does not claim a whole-suite run,
independent review, complete modular-set implementation or public admission.
