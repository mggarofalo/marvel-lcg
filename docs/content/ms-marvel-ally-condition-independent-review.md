# Independent ally and condition review

Reviewed candidate `f789c1f5` against base `cdc2c2f9` on 2026-10-26 in
`C:/Users/mggar/Source/marvel-ms-conditions-review`.

Verdict: Endurance, Down Time and Morale Boost have no identified finding in
this review. Red Dagger requires an engine correction before acceptance.
No engine source, ability-book row or public admission boundary is changed by
this review.

## Authority and inventory

Read the generated printed rows in `datasets/cards/cards.json`, the Rules
Reference entries for ownership/control, wild resources, maximum hit points,
healing and recovery, and the vendored Red Dagger rulings
10293662ffefd5b2, 2368dda1610740b3 and c511a191fe8cc74d. The FAQ and ruling
corpora were searched for all four names. The packet uses controller payment
and physical-owner hand return, a defeat interrupt, two distinct resources,
any-player upgrade control, maximum one per player, the printed +3 HP and +2
REC, and all three +1 combat stats until round end.

Independently compared the actual JSON bytes with the base: the first 556055
bytes are identical. Exactly four faces are added: 05002, 05023, 05032 and
05033. There are 562 unique rows, including 20 Ms. Marvel rows. The three
intentionally omitted event faces remain outside this review.

## Finding: invalid Red Dagger payment consumes the defeat opportunity

`MsMarvelConditionIndependentTests.RedDaggerRejectsEqualWildDeclarationsAndAcceptsDifferentDeclarationsOnRetry`
uses the unmodified full ability book and actual printed single-wild resource
cards. Red Dagger is physically owned by seat 0 and controlled by seat 1.
Consequential damage opens its defeat interrupt for seat 1. Submitting two wild
icons declared energy/energy is rejected and preserves the card digest, RNG
state and outstanding agenda records. Submitting the same offered action with
the legal energy/mental allocation then throws:

```text
the selected defeat ability was not offered
```

The cause is `DefeatChoiceResolution.Choose`: it calls `occurrence.Trigger`
(and potentially the containing occurrence's `Trigger`) before `ResolveWindow`
validates the submitted resource allocation. `DefeatWindowEligibility.Pending`
subsequently filters the marked ability. The first rejected answer therefore
changes hidden occurrence eligibility even though no cards move. This violates
the requirement that rejected answers preserve the decision and allow retry.
The regression is deliberately retained without skipping or weakening its
expected successful retry. The correction belongs in the engine; changing
Red Dagger's printed resource constraint would not correct it.

## Independent checks and evidence

Five new executable cases use the actual full book:

- Red Dagger explicit invalid wild allocation followed by valid retry (fails as
  described above).
- Endurance changes maximum and remaining health without healing existing
  damage, follows the recipient across forms, leaves the owner unchanged, and
  returns to its physical owner's discard on departure (passes).
- Down Time follows hero/alter-ego transitions and adds two to an actual basic
  recovery, then ceases to modify REC on departure (passes).
- Morale Boost requires hero form and only offers heroes as recipients, with
  an otherwise eligible hero-form control (two cases, both pass).

Focused command:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~MsMarvel|FullyQualifiedName~AbilityData|FullyQualifiedName~AbilityLowering' --nologo
```

Result: 177 passed, 1 failed, 178 total. The Release build succeeded before this
run. Log: `C:/Users/mggar/Source/ms-condition-independent-ms-profile.log`.
A smaller initial profile passed 79 and failed the same one regression;
`C:/Users/mggar/Source/ms-condition-independent-test.log` records its build and
failure. The author's 167 passing checks and four valid row-mutant logs are
separate earlier evidence; they do not cover the failed retry.

No full suite, Server, native interaction, public setup, save/restart or complete
pack acceptance is claimed. Normal pre-commit hooks remain enabled.
