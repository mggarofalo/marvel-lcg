# Call for Aid — MARVEL-569

Base `cd18a9b0f23cdd7541cc7b8a35a99730490b63b2`, including reviewed prerequisite
MARVEL-871 source `74c1d59aebe084ba7b0a099821d61dcafb4ff960` and independent
review `cd18a9b0f23cdd7541cc7b8a35a99730490b63b2`.

This packet appends exactly actual research face `12015` Call for Aid. The book
has 732 rows and nine Ant-Man faces: `12012`, `12013`, `12015`, `12018`, `12019`,
`12021`, `12022`, `12023`, `12033`. The original 731-row raw UTF-8/LF prefix is
739313 bytes with SHA-256
`ca581902158c18a2341ac0c693c4924606294b7f7bf723ec019671a527441c7f`.
The prefix excludes only the closing array/document delimiters; only a comma
and the new row precede those unchanged delimiters. No engine, starting-counter,
setup or public-admission changes are included. Ant-Man remains incomplete and
unadmitted.

Printed authority is `datasets/cards/cards.json`, face `12015`: zero-cost Hero
Action, discard until an Avenger ally, then add that physical ally to your hand.
Current RR 1.8 `rr:player-deck.1` resets an emptied deck immediately and deals
one facedown encounter card; `.3` prevents continuing through the replacement
deck. `rr:discard.4`, `.4.1`, `.4.2` preserve ordered simultaneous discards and
place discard responses after the complete batch. The September 2023 answer
`ruling:638de69fbb0b5aa6` is scoped to RR 1.5 and agrees with these current
clauses, including the last-card shuffle before a discard response. The vendored
MarvelCDB FAQ queried 12015 and contains no per-card entry.

`CallForAidMatchingTests` uses the complete actual authored book by default.
Its near-miss case distinguishes Black Cat `01002` (ally without Avenger),
Avengers Mansion `01091` (Avenger without ally), and the exact Spider-Woman
`01011` ally, with another copy in discard. Seat 1 acts while seat 0 is first
player; the other deck is unchanged. Last-card match and no-match exhaustion
compare replacement-deck order and seeded RNG with ordinary bounded discarding,
prove one encounter-card penalty, and prevent searching the replacement deck.
The completed discard batch precedes physical addition. Actual Hero Action checks
cover owner/form restrictions, zero printed cost, event disposal, recovery and
unchanged hand-size duration. Explicitly synthetic kind-only and following-choice
variants replace this row inside the complete book, preserving duplicate rejection
and the prior resumption/query-purity assertions. They do not claim printed
Call for Aid choices or a kind-only printed matcher.

The existing discard-from-deck response-window limitation remains. The current
authored-trigger audit found no affected authored trigger; future White Fox,
Digging Deep and other such cards require a separate timing capability. Batch
completion assertions do not establish such response windows. No complete-pack,
public-setup, native-interaction or broad-suite acceptance is claimed.

Verification evidence is outside the repository under `C:/Users/mggar/Source/`:

- `ant-call-for-aid-build.log`: initial nonincremental Release build, zero warnings/errors.
- `ant-call-for-aid-focused.log`: preserved initial integration failure, 43 passed
  and one failed. The full authored book correctly exposes Captain Marvel's own
  Action alongside Call for Aid. The synthetic query now asserts exactly one
  action for its source card; all prior purity and resumption assertions remain.
- `ant-call-for-aid-repaired-build.log` and `ant-call-for-aid-repaired-focused.log`:
  nonincremental Release rebuild and all 44 focused cases passing.
- `ant-call-for-aid-mutant-build.log` and `ant-call-for-aid-mutant-focused.log`:
  one actual production-row mutation removes the Avenger trait requirement.
  The mutant builds successfully and the characteristic near-miss case fails
  its ordered-discard assertion: two cards discarded instead of three because
  Black Cat incorrectly qualifies. This exercises the actual authored row,
  not a test-local overlay or engine constant.
- `ant-call-for-aid-restored-build.log` and `ant-call-for-aid-restored-focused.log`:
  restored bytes, mandatory nonincremental build, zero warnings/errors, then
  all 44 focused cases pass with no failures or skips.
- `ant-call-for-aid-integrity.log`: independent byte-prefix/count reconciliation.
- `ant-call-for-aid-hooks.log`: normal commit hooks.

Build command: `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj
-c Release --no-incremental`. Tests use that project with `-c Release --no-build`
and filter `FullyQualifiedName~CallForAidMatchingTests|FullyQualifiedName~TeenSpirit|FullyQualifiedName~EncounterDiscardInstructionTests|FullyQualifiedName~NestedRevealCompletionTests|FullyQualifiedName~AbilityDataTests|FullyQualifiedName~AntManReprintTests|FullyQualifiedName~RuleCitationTests`.
The mutation filter selects only
`CallForAidMatchingTests.ResolvingSeatDiscardsBothNearMissesAndRecoversOnlyTheExactAvengerAlly`.
Independent content review follows this source packet.
