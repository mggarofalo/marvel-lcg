# Doctor Strange delivery checkpoint

Canonical parent: MARVEL-565. The [40-face handoff](research/remaining/doctor-strange-implementation-handoff.md)
defines complete scope. This branch contains nonpublic prerequisites, not admitted
Doctor Strange content.

Reviewed lifecycle source 3a20ae07 and review dd5b2f4a reuse player-owned additional
deck/discard zones with exact physical binding, seeded shuffle, post-mulligan top
reveal and penalty-free immediate recycling. Independent Rules 1,156 and View 365
checks pass, including collision, departure/re-entry and four-scope visibility.
No new digest vocabulary or ordinary Core setup allocation is introduced.

Reviewed authority source fb3303ed supplies the three current, hash-pinned
Master/Invocation-initiation relationships and scoped historical supersessions.
Independent focused relationship checks, full Rules 1,160, offline ruling/FAQ
parity and behavior catalog checks pass. Historical source bytes are unchanged.

Six reprints are integrated as `2660ed01` (source `fb77aa99`): Power of
Protection, Med Team, Energy, Genius, Strength and Avengers Mansion. Independent
review verified all 645 predecessor raw rows unchanged, exact printed/Core
program parity and 97 content plus four public-exclusion checks. Three authored
program mutants were killed, restored nonincremental verification and normal
hooks passed. That packet established 651 rows and six of the 40 Strange faces.
It does not admit the product. See
[reprint evidence](doctor-strange-reprints-evidence.md).

Cloak of Levitation, Eye of Agamotto and Sorcerer Supreme are integrated from
`60e4a9f7`, together with the separate generic printed-trait prerequisite fix
`e34f921a`. The combined independent review passes 119 focused checks with no
exclusions, including the unchanged non-Mystic rejection and a non-first
non-Strange identity gaining Mystic, playing Supreme and retaining its upgrade
after losing the trait. Four lowered card-program mutants and the compiled
parser-removal mutant distinguish the intended behavior. The book now has
654 rows, covering nine of the 40 faces. See
[constant/resource evidence](doctor-strange-constants-evidence.md) and
[printed prerequisite evidence](printed-have-trait-prerequisite.md).

Spell Mastery source `46352908` and independent review `a2fb6989` are integrated.
The nonpublic real-card Winds fixture proves exact top/owner/incarnation and
captured cost, hero exhaustion even at zero cost, explicit Special resolution,
discard/reveal/recycle and separation from the player-deck penalty. Independent
checks pass 16 Spell Mastery cases, 27 adjacent cases including those 16,
20 existing Special-sequence cases and 17 Invocation lifecycle cases. Unsupported
Specials explicitly raise after committed costs; this does not invent their
effects. Source mutation and full-regression scope are recorded in the
[Spell Mastery evidence](doctor-strange-spell-mastery.md).

Natural Talent and Wong top-discard source `bc5e1b2f` and review `7ed7ec4a`
are integrated as `cd05bfd9` and `eb6b17a9`. The typed top query rejects
unbound lookalikes and ambiguous decks without consuming randomness. Actual Wong
under another player's control heals its controller's identity while referring
to the game's Invocation deck; review covers that distinction and phase limits.

Master source `b3b566af` and review `fc93062f` are integrated with the two
subsequent admission repairs: exact bound self-discard (`d242e47a`, reviewed in
`b801bb88`) and direct empty scheme choice (`bf51629a`, reviewed in `71c7a798`).
The latter independently passes 996 affected Content cases and 19 lifecycle/
authority cases. It preserves whole-ability admission and dependent completion;
it does not supply Vapors status replacement or generic empty-choice behavior.
Combined integration at `05cb51c2` passes all 122 selected Invocation, Master,
Spell Mastery, Covert Ops, Black Panther and next-Special tests. Evidence:
`Source/evidence/strange-integrated-master-top-discard.log`.

MARVEL-808 adds actual identity, Wong, Master and Winds rows against
these reviewed mechanics. MARVEL-809 implements the separate Vapors status
replacement prerequisite. MARVEL-803's four response events await independent
acceptance of MARVEL-804's full-healing-cost repair; the unchanged previously
supported mixed damage/healing regression is a required gate, not an unsupported
boundary waiver. Remaining production rows, other Invocations, whole-pack
admission, save/UI contracts and complete pack proof remain open. The integrated
book now has 659 rows and 14 of the 40 Strange faces.

Combined integration at `e9584f11` passes 89 focused Content checks covering
Spell Mastery, actual Strange rows, both printed-prerequisite spellings,
controlled-character restrictions and ability data. After adding the independent
Supreme test, all nine constant-card checks pass. Logs:
`Source/evidence/strange-combined-d1-constants-content.log` and
`strange-combined-supreme-review.log`. These are focused integration results,
not whole-pack certification.

D1 production source `ede4159f` and independent review `febb7986` are integrated
as `d7acab49` and `60a1239a`. All 654 predecessor raw JSON objects are unchanged;
the five added faces are 09001a/b, 09002, 09005 and 09036. Independent evidence
passes 164 affected Content cases, 23 lifecycle/ruling/citation cases, the host
stale-revision control, offline card-data check and normal hooks. Five authored
mutants distinguish the costs, limit, return and draw behavior. A sixth review
mutant removes the printed hero-form restriction and kills its targeted control.

The form-rejection fixture now explicitly tests the same Spell Mastery program
across both faces; the production alter-ego correctly has a different action.
A separate legal form-change control rejects the earlier answer without state
mutation, then executes the fresh action. The direct-world-mutation ordinal alias
is documented rather than claimed fixed. See
[D1 production evidence](doctor-strange-d1-production-rows.md). The integrated
`src`, `tests` and `datasets` trees at `60a1239a` exactly match the reviewed
`febb7986` trees; intervening lead commits only update delivery documentation.