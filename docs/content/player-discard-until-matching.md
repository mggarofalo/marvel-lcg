# Player-deck discard-until matching (MARVEL-871)

Base: `56a4632c043a879d4bacacc834e3e38fe99b7b03`. This packet adds player-deck
kind matching with an optional conjunctive printed trait and retains identity-set
matching through the same bounded discard procedure. It changes no production
ability row or public admission. Printed 12015 is exercised only in a test-local
book. The production book remains 731 rows with SHA256
`e42de604ecb1fa087a64f407310b4f5dfda079b2bcee9461663420000ab00f73`.

Authority is `datasets/cards/cards.json`, face 12015: discard until an Avenger
ally, then add that ally. RR 1.8 `rr:player-deck.1` immediately resets an emptied
deck and deals an encounter card; `.3` prohibits further discards from the
replacement deck. `rr:discard.4`, `.4.1`, and `.4.2` preserve ordered, simultaneous
deck discards with responses after all discards. The September 2023 answer
`ruling:638de69fbb0b5aa6` (RR scope 1.5) agrees with current RR 1.8 on that timing
and the shuffle preceding a last-card discard response. No per-card FAQ answer
for 12015 exists in the vendored FAQ corpus.

`CallForAidMatchingTests` distinguishes a non-Avenger ally (Black Cat 01002) and
an Avenger non-ally (Avengers Mansion 01091) from the exact Spider-Woman 01011
copy, with another physical copy in discard. Seat 1 resolves while seat 0 is
first player. Exhaustion covers both last-card match and no match, compares the
replacement deck and seeded RNG with ordinary bounded discarding, and proves
that a matching card already in discard is not newly searched after shuffling.
The completed discard activity precedes physical addition. A separately labeled
synthetic following choice suspends and resumes the effect without repeating its
discard, preserving seat ownership and complete deterministic event streams.
Ambiguous set-plus-kind/trait and trait-only DSL instructions are rejected.

There is no discard-from-deck response-trigger contract in the current engine.
This packet preserves existing ordered discard/add behavior and does not claim
to implement such response windows. An audit of the 731 authored faces found no
printed Response/Interrupt trigger containing discard before its first clause
separator. The corpus's player-deck discard triggers Jackpot! 40043, White Fox
40057, Digging Deep 40060, and Skrull Business 56056 are all unauthored. Their
future admission requires a separate timing capability. The tests prove batch
completion, not a nonexistent response window.

Evidence is outside the repository under `C:/Users/mggar/Source/`:

- `player-discard-until-build.log`, `player-discard-until-focused.log`: a
  non-incremental Release build and 32 passing focused cases.
- `player-discard-until-mutant-trait.log` and `player-discard-until-mutant-kind.log`:
  separately compiled mutations of each conjunction term both fail the exact
  ordered-discard assertion in the characteristic card test.
- `player-discard-until-restored-build.log` and
  `player-discard-until-restored-focused.log`: mandatory restored non-incremental
  build and 32 passing cases. `player-discard-until-final-build.log` and
  `player-discard-until-final-focused.log` cover the final additional query-purity
  assertions against prompt, agenda and RNG state.
- `player-discard-until-trigger-audit.log` and
  `player-discard-until-authored-trigger-audit.log`: corpus candidates with
  authored-membership results and the empty authored-trigger scan.
- `player-discard-until-hooks.log`: normal commit hooks.

Commands use `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj
-c Release --no-incremental`, followed by `dotnet test` on that project with
`--no-build` and filter `FullyQualifiedName~CallForAidMatchingTests|FullyQualifiedName~TeenSpirit|FullyQualifiedName~EncounterDiscardInstructionTests|FullyQualifiedName~NestedRevealCompletionTests`.
The mutation filter selects the characteristic near-miss test alone. No broad
suite, native interaction, production card authoring or whole-pack acceptance is
claimed. Independent engine review follows this packet.

## Independent engine review

Reviewed source `74c1d59aebe084ba7b0a099821d61dcafb4ff960` against the stated
base. Verdict: accepted for this nonpublic engine capability; no production
defect found. Independently read printed 12015 and fixture faces 01002, 01011,
01091, current RR 1.8 player-deck/discard clauses, and the cited September 2023
ruling. The historical ruling agrees with the current clauses governing these
assertions. The vendored FAQ contains no entry for 12015.

Review adds a labeled synthetic kind-only case: Black Cat qualifies without an
Avenger trait, Avengers Mansion is discarded first, and the next ally remains
in the deck. The suspended following choice is queried three times through its
actual continuation; card digest, complete RNG state and consumption, pending
prompt identity and agenda remain unchanged. Resumption matches an unprobed
control's full events, serialized prompt offers, digest, RNG and pending agenda.
These tests supplement the existing seat-1, conjunctive near-miss, exact-copy,
ordered-batch and exhaustion cases without changing production code or data.

External evidence under `C:/Users/mggar/Source/`:

- `player-discard-until-independent-verified-build.log`: non-incremental Release
  build, zero warnings/errors.
- `player-discard-until-independent-verified-focused.log`: all 33 cases pass,
  using the same focused class filter listed above.
- `player-discard-until-independent-focused.log`: preserved first review failure
  in a new cross-game prompt assertion; nested collections compare by reference.
  The assertion now compares serialized observable offers.
- `player-discard-until-independent-final-build.log`: preserved review compile
  failure from an ambiguous serializer method group, corrected with lambdas.
- `player-discard-until-independent-hooks.log`: normal review commit hooks.

No independent mutation run, broad suite, native interaction, discard response
window, production 12015 admission or complete pack acceptance is claimed.
