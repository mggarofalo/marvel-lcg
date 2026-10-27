# Kree Fanatic public admission candidate

Canonical delivery: MARVEL-567 (`3cff2fe7-2656-4b3d-92d2-4a3130eac2cd`).
Base revision: `f04c0e264ac11d9d12abb3ba40757dd473abb8aa`; includes the Hulk
public candidate. Ronan must not merge before Hulk.

This packet admits only the complete Kree Fanatic modular encounter set.
It supplies no standalone scenario or campaign. The separate Galaxy’s Most Wanted
Ronan scenario remains unsupported.

## Authority and boundary

The original FFG seven-page PDF is pinned in
[the source record](research/remaining/ronan-source-pin.md): SHA-256
`09e3fba63e6483c64ffec1cfb09f1461fa81dfa4bfbba7fcaeb6d46bfab9a3a4`.
Its numbered cards supply 90001 ×1, 90002 ×1, 90003 ×1, 90004 ×2 and 90005 ×2.
The joined catalog supplies printed facts. All five actual ability rows were
reviewed before this packet. Their unchanged 723-row book SHA-256 is
`4b1032c2d6b512708ab58549fe49da010337dad7b045b803af950e179596a3c9`.

Admission requires exact catalog pack membership, all five authored faces, modular
role and the complete seven-copy multiset. Missing catalog/authored faces and
missing, short, extra, same-count substitution, empty, foreign or wrong-role recipes
close the complete set. Independent assertions preserve predecessor playable faces,
heroes, scenario recipes, encounter sets and the Rise of Red Skull campaign.
Recipe reordering remains legal; encounter composition uses quantities rather than
an arbitrary ordering gate.

A setup record is a behavioral authority. Its reviewed adjudication is
`outside-core`; the catalog was regenerated offline with `Marvel.Behavior.Index
write`, never edited independently. Expansion and global source/obligation
inventories include it without claiming executable Core transcript coverage.

## Verification

Final focused results: Content **39/39**, Server **38/38**, Rules authority
**25/25**. The offline behavior check passed. The Server run includes all eighty
scenario/mode/player-count setups. Both compiled mutants failed as intended;
restored production sources received a nonincremental build before passing checks.
The commit identifies the candidate; normal commit hooks remain enabled.

The matrix diagnostic identified Zola with three players still resolving a
`WhenCardRevealed` Element choice after eight legal setup answers. A bounded
32-decision helper completes all eighty legal setups with engine-offered answers;
the final Mulligan and exact set-composition assertions remain required.

Raw local logs are under `C:/Users/mggar/Source/evidence/`:

- `ronan-mutant-quantities.log`: compiled quantity-guard bypass is rejected by
  malformed-recipe checks.
- `ronan-mutant-inventory.log`: compiled complete-inventory bypass is rejected
  for all five missing authored faces.
- `ronan-restored-build.log`: nonincremental restored Content test-project build,
  zero warnings/errors.
- `ronan-public-content-restored.log`: final admission, dataset, modular and
  predecessor recipe checks.
- `ronan-public-server.log`: both public transports discover and open the set;
  all twenty modular-enabled Standard/Expert scenario modes at one through four
  players, with solo custom decks and multiplayer starters; exact encounter
  multiset, deterministic seeded shuffle, default module preservation and unknown
  modular/scenario rejection; existing setup and deck discovery contracts.
- `ronan-public-authority.log`: behavior universe and expansion setup authority
  checks; offline generator check follows them.

Initial missing-catalog probes exposed discovery indexing an absent catalog face;
the final admission path filters absent faces before consulting their pack.
This keeps complete-product rejection usable while preserving the predecessor
products. Earlier failing logs are diagnostic evidence, not the final result.

Complete public games, characteristic action/restart, fresh-host replay, independent
review, native and combined/platform gates remain separate delivery evidence.
No owner approval gate is added for ordinary modular content.
