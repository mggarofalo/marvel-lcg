# Doctor Strange delivery checkpoint

Canonical parent: MARVEL-565. The [40-face handoff](research/remaining/doctor-strange-implementation-handoff.md)
defines complete scope. This branch contains nonpublic prerequisites and authored
rows; Doctor Strange is not publicly admitted.

Black Widow PR #453 is merged at `ffb26f66`; this branch rebased cleanly onto
that accepted baseline. The reviewed 38-commit Strange stack now ends at
`22d67804` (pre-rebase head `4d95d05c`). Earlier hashes below name the revisions
actually tested and remain evidence references. No source packets were dropped.

Latest integration: 672 authored rows, 27 of the 40 Strange faces. Exact printed-set
search source `4261d57f` and review `f7e59a6c` are integrated at `36628a0b`; its
combined search checks pass 40 cases. Four response-event rows from `4953b480`
and independent event review `de7310c9` now accompany the complete healing-cost
chain through source `4d193dd6` and independent acceptance `0cf90669`.
All 661 predecessor raw ability objects remain byte-identical.

Combined Content verification includes whole cost/payment/healing classes,
Doctor Strange, Spell Mastery, Master, Invocation, Black Widow, ability-data,
citation, prevention and Green Goblin controls: 1,557 passed and two stale
packet-inventory assertions failed. Those assertions now recognize the authored
identity and bound the earlier two-row search packet; all 20 tests in their two
classes pass after correction. No behavioral assertions were removed. Logs:
`Source/evidence/strange-integrated-healing-content.log` and
`Source/evidence/strange-integrated-healing-inventory-restored.log`.
The merged sequence assessor receives costs already expanded by
`AbilityPaymentRules.Payable`, preserving Invocation pricing while flattening
nested costs into the complete payment context. Images/Vapors and Crimson
are integrated with their independent reviews. Public admission remains
closed until the complete pack and its integration evidence are ready.

Images/Vapors production source `06fcca2a` and independent review `706fa8bf`
are integrated, preserving all 665 predecessor raw objects. Crimson's prerequisite
source `b0eb9178` and review `4e192432` are integrated; production-row integration
is recorded below. Combined content/Invocation/Black Widow checks pass 479 cases with no
failures or skips (`Source/evidence/strange-invocation-crimson-integration.log`).
Seven Rings prerequisite source `8733cc9b` and independent review `5fe8b7ad` are
also integrated. Its final combined selection/Invocation/Crimson checks pass 127
cases (`Source/evidence/strange-seven-rings-integration.log`). The existing fixture
keeps one optional-last-card overload; DSL documentation preserves exact-set,
independent enemy choice and complete status-subset contracts together.
Seven Rings production-row integration follows below; downstream native/contract
proof remains open.

Hosted Invocation source `76866793` and independent review `68dd71c6` are
integrated. The printed Open the Dark Dimension return is a When Defeated
instruction; ordinary non-defeat departure instead uses separate-discard cleanup.
Source full View (370) and Rules (1,173) pass; source full Content has 6,751 passes
and four inherited inventory-count failures already reconciled in this branch.
Independent review passes 37 Content, 35 View and 17 Rules checks, including
discard preservation, multiple-host isolation and all-five-hosted recovery.
The integrated Content selection passes 146 cases, with no failures/skips:
`Source/evidence/strange-hosted-integration-content.log`. See
[hosted prerequisite evidence](doctor-strange-hosted-invocation-prerequisite.md).
The production 09029 row and public save/replay/native proofs remain open.

Four production rows from `e4081bcb`, independently reviewed at `cadc8a2a`, add
Mystical Studies, Magical Enhancements, Crimson Bands and Seven Rings. All 667
predecessor raw objects are unchanged. Source evidence includes five distinguishing
actual-program mutants, restored nonincremental build, 250 affected cases and the
offline generated-card check. Independent review adds six controls for simultaneous
two-copy Enhancements cleanup and Seven Rings ally/minion recipients; 256 cases
and normal hooks pass. Integration with hosted lifecycle, existing Strange and
Black Widow content passes 526 cases, none failed/skipped:
`Source/evidence/strange-next-four-integration.log`. See
[four-row evidence](doctor-strange-next-four-rows.md).

Typed Invocation pile source `f3542f7a` and independent review `dcea603c` are
integrated. Protocol 42 publishes only owner/deck/discard area IDs and validates
their exact relationships. Typed piles receive explicit labels; ordinary additional
areas are not inferred to be Invocations. The complete integrated View lane passes
372 cases; affected transport, contract and old-peer checks pass 56 cases. Logs:
`Source/evidence/strange-area-integration-view.log` and
`Source/evidence/strange-area-integration-server.log`. See
[area contract evidence](doctor-strange-area-contract.md).
MARVEL-817 remains open for choice/cost/source explanations and native proof;
this partial acceptance is not complete UI or product acceptance.

Astral inspection source `50404772` and independent review `cb3f8744` are
integrated. The private inspection receipt counts printed boost icons, excluding
stars, while preserving the selected scheme and hidden-card visibility. The
complete integrated View lane passes 391 cases and the affected Content selection
passes 41 cases. Logs: `Source/evidence/strange-astral-integration-view.log` and
`Source/evidence/strange-astral-integration.log`. See
[inspection evidence](doctor-strange-inspection-boost-prerequisite.md).
The actual 09003 row remains to be authored.

Unflappable 09020 source `f5d4ed51` and independent review `b90ea489` are
integrated, preserving all 671 predecessor raw objects. The integrated Content
selection passes 502 cases across Doctor Strange, Black Widow, Invocation,
Counterattack and ability-data classes, with no failures/skips. Log:
`Source/evidence/strange-unflappable-integration.log`. See
[support/ally evidence](doctor-strange-support-ally-rows.md). The duplicated
`Take(4)` in the earlier four-row inventory is simplified without changing its
bounded face set or assertions.

Owner-deck shuffle source `cf09e734`, independent finding `023bc113`, repair
`80ddfbba` and independent acceptance `c303d99d` are integrated. Cards owned by
an eliminated player leave play into RemovedArea without consuming shuffle RNG;
active owners retain deterministic deck grouping. Status-host source `d7dcfe92`
and independent reviews `5cd1f848` / `0bbe6106` are also integrated, including
exact paid-prompt purity and local nested binding evidence. The shared player
binding analyzer retains both the typed shuffle destination and bound status-host
case. Combined affected Content/citation checks pass 255 cases, with no failures
or skips (`Source/evidence/strange-owner-status-integration.log`).
The earlier Clea probe now exercises physical-owner routing; the unsupported
spelling control remains genuinely unknown. Night Nurse's legacy spelling test
is named as an unknown-input control, not a claim that status choice is absent.
See [owner-shuffle evidence](doctor-strange-owner-shuffle-prerequisite.md) and
[status-host evidence](doctor-strange-status-host-prerequisite.md).

Exact-host upgrade pricing source `e56ef1ff` and independent review `5631ac14`
are integrated. Existing protocol-42 CostOption.Target carries target-specific
prices; ordinary attachment prompt shapes remain unchanged. Source full Content
6,956 and Rules 1,173 pass; independent focused review passes 72 cases and kills
two additional compiled mutants. Combined Content checks pass 225 cases, full
View passes 391 and full Decisions passes 50. Logs:
`Source/evidence/strange-upgrade-host-{integration,view,decisions}.log`.
See [upgrade-host evidence](doctor-strange-upgrade-host-cost-prerequisite.md).

MARVEL-819 can now author Clea, Night Nurse and ally Iron Man. MARVEL-823's
faithful signature-event probes demonstrate three additional prerequisites;
none of its three rows was retained or admitted. Evidence `6e042c3d` and
[the event gap record](doctor-strange-remaining-events.md) preserve the exact red
cases and positive controls. MARVEL-826 supplies required scheme cost commitment,
827 supplies simultaneous labelled-power continuation shared by Magic Blast and
Astral Projection, and 828 retains authoritative reveal origin for Protective
Ward. Separate implementation and review precede those actual rows.
MARVEL-824 covers Iron Fist/Desperate Defense; MARVEL-825 covers the five
obligation/nemesis faces. Public admission remains closed while thirteen playable
faces are unauthored.

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

MARVEL-808's five actual D1 faces and MARVEL-810's Sanctum/Brother rows are
reviewed and integrated. The book now has 661 rows and 16 of the 40 Strange faces.
MARVEL-809's status-replacement prerequisite is also reviewed and integrated.
Next, MARVEL-813 authors Crimson Bands, Images and Vapors. Mystical Studies waits
on MARVEL-812's exact printed-set search predicate. MARVEL-803's four events wait
on MARVEL-804's healing-cost acceptance, including the retained counter-dependent
invalid-offer control. Remaining faces, setup/admission, save/UI contracts and
complete pack proof remain open.

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
Vapors prerequisite `7e2b9c14` and independent review `e1577aa7` are integrated
as `68abb2f1` and `52033c88`. Independent controls cover all six status replacement
directions, exact physical hosts and events, and empty/unsupported resolution
boundaries. Source and review evidence is under `research/remaining/vapors-status-*`.
No production Invocation row or public contract certification is implied.

Sanctum/Brother source `5e07f642` and review `72c0af60` are integrated as
`22a787d1` and `1edebb22`. All 659 predecessor raw objects are unchanged.
Independent review passes 134 relevant cases, 129 architecture cases and normal
hooks, adding query-purity/unprobed continuation and optional entry-response
controls. See [search evidence](research/remaining/doctor-strange-search-evidence.md).

Combined integration at `1edebb22` passes all 181 selected D1, search, status,
Invocation, Master, Spell Mastery, ability-data and citation cases. Log:
`Source/evidence/strange-integrated-d1-search-vapors.log`. These are focused
integration checks, not complete-pack certification.
