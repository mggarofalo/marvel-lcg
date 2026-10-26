# Doctor Strange next four actual rows

Packet base: `8506121cfe1ba28ecb23340f13d6c933dd4a229c`.
Canonical parent: MARVEL-565; search child MARVEL-810 and Invocation child
MARVEL-813. This packet appends actual 09006, 09010, 09032 and 09034:
671 rows and 26/40 Strange faces. All 667 predecessor raw JSON objects and their
textual prefix remain byte-identical. No new starting counters, engine code,
public admission, setup, UI, protocol or save compatibility change is included.

Printed authority is the exact generated `datasets/cards/cards.json` text.
The four codes are queried in the vendored MarvelCDB FAQ with no card-specific
entries. Current RR 1.8 search.1/.3, ability.13, ownership-and-control.2.1,
forced.1, choose-game-element.3.1, status-cards.1 and target govern the relevant
choices. The reviewed current-compatible Invocation rulings
`8883bc6b0af1c9b8`, `8c9c9d30f4c01189` and `b07d53e12625ab23`
retain independent discard and Master discard/disclosure/recycle/return order.
The printed-set, Crimson independent-discard and Seven Rings prerequisite
records supply the reviewed mechanisms; this packet changes no such mechanism.

Studies uses exact `cardsIn.set: doctor_strange` over its resolving player's
ordinary deck and discard. Actual default-program tests cover each physical
search area, another Hero set, same pack/different set, foreign seat and typed
Invocation exclusion, alter-ego-only admission, paid miss shuffle, and rejected
answers. Altered generic selector probes remain synthetic. The other-Hero
control uses Captain Marvel so an incorrect Spider-Man guard cannot pass just
because an unrelated Spider-Man card happens to satisfy it.

Magical Enhancements uses the existing play-under-control identity attachment
contract. Actual-card tests pay the printed cost and select either seat's hero,
assert unchanged printed owner and recipient control, all three +1 modifiers,
no alter-ego stat leakage, restored bonuses on returning to hero, survival of
player-phase end, and mandatory single round-end discard in either form. The
scheduled phase transition exercises the real Forced Interrupt window.

Crimson's default prerequisite fixtures now retain its actual production
program; only the labeled synthetic damage prohibition or alternative effect is
replaced. Existing tests exercise selected minion, seven ordinary damage, Tough,
Stun, Guard, protected/no-effective-enemy independent discard, rejected stale
answers, both callers, non-first seat, last-card recycle and exact Master return.
Seven Rings' card-shaped tests likewise use the actual row, covering explicit
zero, one, two and three distinct recipients, enemies, foreign identity, invalid
complete groups, status restrictions, physical lifecycle and both callers.
Generic alternate status probes remain synthetic on another face.

All affected inventory totals/lists and bounded historical slices recognize the
new rows, including older Black Widow classes. Negative inventory controls name
still-unauthored 09003. Explicit empty synthetic 09032 replacements preserve the
unsupported-Special tests without relaxing duplicate rejection.

Verification evidence is outside the repository at
`C:/Users/mggar/Source/next-four-*.log`. The preliminary restored actual-book run
passes 250 focused cases. Five actual-program mutants fail for the intended
behavior: wrong Studies search guard (2 failures), +2 rather than +1 thwart
(3), optional rather than forced round-end discard (3), six rather than seven
Crimson damage (5), and four-recipient Seven Rings maximum (1). The first guard
mutant survived the original mixed-set fixture; changing its unrelated Hero
control to Captain Marvel distinguished it. All mutants are restored byte-for-byte.
Final nonincremental restoration build, whole affected classes, citation checks,
offline printed-card regeneration and normal hooks are recorded below after
completion. Broader whole-game/platform and complete-pack acceptance remain open.

Final restored evidence on this packet's working tree (2026-10-26): forced
nonincremental Release build succeeds with zero warnings/errors; all 250 cases
pass with zero failed/skipped in the complete affected class filters above;
`Marvel.Cards.Extract -- check` confirms both generated files match. Final raw
prefix verification confirms all 667 predecessor objects and 671 unique rows.
The test filter covers DoctorStrange*, CrimsonIndependent*, SevenRings*,
PrintedSetSearch*, StatusRecipientContract*, SpellMastery*, MasterOfMysticArts*,
AbilityData*, BlackWidowPreventionInventoryTests, BlackWidowPreparationCardTests,
BlackWidowFinalResponseCardTests, BlackWidowBurnNoticeTests and RuleCitationTests.
The normal pre-commit hook result is retained in `next-four-commit.log`.

Closeout: the first commit attempt passed 66 architecture cases but the
complexity hook identified the synthetic unsupported-Special branch in
`SpellMasteryFixture.Runner` as CCN 11. A cohesive helper now owns that explicit
negative control; the Runner retains its original complexity. All 250 affected
cases pass again after this extraction (`next-four-closeout-tests.log`). The
normal commit hooks are rerun without exceptions or bypasses.

## Independent actual-row review (2026-10-26)

Reviewed `e4081bcb40a390b8fc456c2447cc2c8299b6af42` against
`8506121cfe1ba28ecb23340f13d6c933dd4a229c` on branch
`review/strange-next-four`. Verdict: approved for these four nonpublic ability
rows; no correctness blocker found. This review changes only independent tests
and this evidence record. Complete-pack admission, public sessions, protocol,
native behavior and product acceptance are outside the verdict.

Independent JSON parsing confirms 667 predecessor raw objects are byte-identical
and the only additions, in order, are 09006, 09010, 09032 and 09034: 671 rows and
26 Strange faces. The printed generated card text, current RR search, form,
forced, status, target and ownership clauses, and the three current-compatible
Invocation rulings above support the programs. In particular, Crimson uses
ordinary damage and an independent self-discard; Seven Rings accepts zero through
three distinct characters, including enemies. Master retains exact physical
discard, disclosure, recycle and return order.

The fixture migration preserves the original behavioral assertions. Default
Studies and Crimson fixtures now use the actual book; altered selectors and
damage prohibitions remain explicit synthetic controls. Seven Rings card-shaped
tests use the actual row, while generic status alternatives use another face.
The unsupported Crimson negative control explicitly replaces its now-authored
row with an empty synthetic row. Inventory updates keep exact face lists and
bounded historical slices, with still-unauthored 09003 as the negative control.

`DoctorStrangeNextFourReviewTests` adds six independent passing cases:

- Two physical Magical Enhancements copies, both owned by seat 1 but controlled
  by different heroes, each give exactly the printed three bonuses. At round end
  the first player chooses either order; each copy is discarded exactly once in
  that order to seat 1, and both heroes lose their bonuses.
- Seven Rings gives Tough to an ally controlled by another seat and an engaged
  minion together, preserving their existing damage and leaving unselected heroes
  and villain untouched. Both callers and last/nonlast Invocation decks resolve
  the physical discard and finish their continuations.

The five implementation mutant logs were inspected: wrong search guard, +2 THW,
optional cleanup, six Crimson damage and a four-recipient maximum each fail the
intended behavioral assertion. The implementation's nonincremental restored
build and 250-case restored runs are retained above. No additional production
mutation was needed for these conventional rows and no mutant is present in the
review worktree.

Review verification: all 256 cases pass with zero failed or skipped in the whole
affected class filter documented above, including the six new cases. Offline
`Marvel.Cards.Extract -- check` confirms both generated files match, and
`git diff --check` passes. Logs are outside the repository:
`C:/Users/mggar/Source/next-four-review-new.log`,
`next-four-review-combined.log`, `next-four-review-cards-check.log` and
`next-four-review-commit.log` (normal commit hooks). These are focused engine
and content checks, not a complete Content suite or whole-game acceptance run.
