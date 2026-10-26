# Ms. Marvel Aamir/resource packet evidence

2026-10-26. Base: `a3862b323b825e2f65d4678b1aeaec78f82771fe`.
Prepared nonpublic card-execution scenes; this packet does not establish public
setup, whole-pack play, restart or native client acceptance.

Seven actual rows: `05006`, `05016`, `05019`, `05020`, `05021`, `05022`, `05024`.
The book has 558 distinct faces, with 16 of 34 Ms. Marvel faces authored.
Rows append as UTF-8 text; the preceding ability-book bytes remain identical.
Exact authored membership and starting-counter inventory expectations are updated.

Authority: joined `datasets/cards/cards.json`, including printed resource icons,
forms, counters, costs and selectors. Deep research:
`docs/content/research/ms-marvel-deep-research.md`. The vendored MarvelCDB FAQ
queries include these faces but have no entries. Hall of Heroes card-name search
has no specific clarification for these faces. Resource-generation ruling
`ruling:966be0c679fc50e7` concerns the generic generation/payment distinction;
it does not change these printed operations. No authority snapshot changed.

Actual-card evidence:

- `MsMarvelAamirCardTests`: selected discard physical identity, ordered bottom
  placement before draw with empty/one/multiple-card decks, normal exhaustion
  reset, untouched discard, foreign selection rejection preserving pending work,
  RNG and state, empty-discard unavailability, form/exhaust cost and second-seat
  discard/deck/hand/ownership.
- `MsMarvelResourceReprintTests`: each actual double-icon resource pays for actual
  Reflexes; Protection wild doubling enables three-cost Med Team with one extra
  resource but rejects same-cost Mockingbird, with unchanged state. Hero-set
  Big Hands receives a single wild. Mansion exhausts and draws only for the
  selected self/other-player recipient.
- `MsMarvelReflexesCardTests`: actual play places three energy counters; Hero
  Resource generates energy, exhausts, spends one counter, and discards on last
  use while completing payment. Alter-ego, exhausted, empty and foreign-control
  generators are unavailable.

Release focused command:
`dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~MsMarvelAamirCardTests|FullyQualifiedName~MsMarvelReflexesCardTests|FullyQualifiedName~MsMarvelResourceReprintTests|FullyQualifiedName~AbilityData|FullyQualifiedName~AbilityProgramTests'`

61 passed. Final restored run and commit hooks are recorded in the handoff.
Logs: `%LOCALAPPDATA%/Temp/ms-aamir-test.log`, `ms-aamir-restored.log`, and
`ms-aamir-final.log`.

Seven admitted data mutants fail the intended observations: Aamir draws two;
Aamir loses alter-ego restriction; Protection doubles for Aggression; Mansion
draws for controller; Reflexes produces mental, lacks Uses, or lacks Hero form.
Each mutated book lowered and executed before behavioral failures. Logs:
`%LOCALAPPDATA%/Temp/ms-aamir-mutant-<name>.log`. An attempted top-placement
spelling was rejected during lowering and receives no behavioral mutant credit.
Restored ability bytes are exact. No engine prerequisite is outstanding for
these seven faces. Bruno and the remaining Ms. Marvel faces remain separate work.
