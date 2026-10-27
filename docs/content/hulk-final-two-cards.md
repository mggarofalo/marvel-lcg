# Hulk final two authored faces

MARVEL-847 and MARVEL-850; canonical pack MARVEL-566. Corrected base
`dc965aa4` cherry-picks `022c8254098a177be1e91462a70ebe2bf587c7b5` onto
`a07d4305`. This packet appends only Toe to Toe 10015 and Beat Cop 10029,
completing **718 authored rows / 33 of 33 Hulk faces**. Setup, public product
admission and full-pack acceptance remain separate work.

## Authority and interpretation

Printed authority is `datasets/cards/cards.json`, faces 10015 and 10029. Current
vendored RR 1.8 governs. Toe to Toe is a one-resource Hero Action (attack): choose
an enemy, that enemy attacks you, then independently deal five damage to that
same enemy. Its printed text has no dependent Then. RR `rr:labeled-ability.6`
cancels the whole ability except costs for hero Stun; `rr:stun-stunned.6`
replaces an enemy's attack with removal of its status. Vendored MarvelCDB FAQ
10015 and ruling `f182a5fac69b442d` explicitly retain five damage after enemy
Stun. That historical answer agrees with current RR and the printed independent
instructions; it is not used to override a current rule.

Toe uses the accepted labelled activation/fixed damage composition, retaining
the selected physical enemy, its incarnation, the original hero attack and
defeat receipt across defense and boost continuations. Actual Chase Them Down
consumes the hero attack's own lethal receipt. Engine DSL spellings are choices;
card effects and timing derive from printed authority and the cited rules.

Beat Cop has two ordinary Actions in either form. First, exhaust this support
and move one threat from a chosen scheme onto it. RR `rr:move.6` considers the
threat removed from the source; a support destination is not a scheme. Second,
exhaust and discard this support to deal one damage per threat here to one
minion. The existing source binding captures its threat before departure;
`tokensOn: this` reads that captured quantity after live tokens are cleared.
Neither Action is an attack or thwart, and neither consumes hero Stun/Confuse.
The already accepted support-destination prerequisite owns removal permissions,
exact incarnation/controller and source-defeat windows. No engine change is
included here.

## Inventory and byte preservation

The two appended rows have no starting counters. Every one of the corrected
716 predecessor raw objects remains byte-identical, including the Clash query
correction. The preserved prefix is 722354 bytes, SHA-256
`583678a5590b8cbc98a220d0238c243aac7bd9ae4ac0a5e911a043ecc71e1ae4`.
The final file uses LF exclusively. External proof:
`%TEMP%/hulk-final-two-raw-prefix.json`.

All whole-book count holders follow 718 rows; packet slices retain their exact
historical offsets and lengths. Complete Hulk enumeration names all 33 faces.
The actual-book whitelist adds exactly 10015 and 10029; 90001 remains unknown.
Both the new fixture and `BeatCopThreatDestinationTests` use
`AuthoredCards.Book` directly, without duplicate injection or fallback rows.

## Behavioral evidence

`HulkToeToToeCardTests` checks paid ordinary/enemy-Stun/hero-Stun paths, the
original chosen minion after departure/reentry, lethal and nonlethal actual
Chase Them Down controls, and a defended villain activation suspended by actual
Target Acquired. The latter checks zero early follow-up damage, boost ability
cancellation, completed activation ordering and the original attack identity.

`HulkBeatCopCardTests` performs two actual threat moves and then the second
Action in both forms. It observes two captured threat becoming two minion
damage after the discarded support has zero live tokens, untouched other-support
threat, retained Stun/Confuse, no attack/thwart, exhaustion and minion/no-threat
admission boundaries. The retained prerequisite fixture adds both seats,
cross-owner controller use and invalid-answer/query purity.

Earlier proposed-row evidence is separate: nine local-row cases passed in
`%TEMP%/hulk-final-two-probes.log`; the compiled local mutations five-to-four and
captured-threat-to-one failed six cases. A restored nonincremental build and
13-case restored run passed. These results establish composition before
admission; final production-book evidence is recorded below.

## Final production-book verification

All logs are outside the repository under `%TEMP%`:

- `hulk-final-two-actual-focused.log`: all 18 actual-book final-card and retained
  Beat Cop prerequisite cases pass.
- `hulk-final-two-production-restored-tests.log`: all 1064 cases pass across
  Hulk, Beat Cop, ability-data, inventory, Black Widow and Doctor Strange
  families. This includes the edited whole-book inventory assertions and
  unchanged public-admission boundaries. The command uses Release `--no-build`
  after the restored nonincremental build, with detailed console logging.
- `hulk-final-two-beat-count-mutant.log`: replacing the second Action's captured
  threat with constant one compiles/lowers and fails both forms at expected two
  damage versus actual one. The original production-book bytes are restored.
- `hulk-final-two-toe-damage-mutant.log`: changing five damage to four lowers and
  fails the ordinary and enemy-Stun controls at expected five versus actual
  four; the hero-Stun cancellation control passes.
- `hulk-final-two-final-restored-build.log`: forced nonincremental restored
  Release build passes with zero warnings/errors.
- `hulk-final-two-final-restored-focused.log`: all 20 final actual-card,
  inventory, retained prerequisite and rule-citation cases pass after that
  final restoration/build.

`hulk-final-two-raw-prefix.json` independently compares the final file with the
corrected base and the saved pre-mutation final file. Both the raw 716-row prefix
and exact final restoration pass. Final ability-book SHA-256:
`8a8935c1e8256354100ea185834a64ba7768cf77470012f3d673955e473ec189`.
`git diff --check` passes. Normal commit hooks remain enabled; their output is
retained in `hulk-final-two-commit-hooks.log`.

An exploratory production Toe dependent-Then mutation is **excluded** from
behavioral mutation proof: its three cases raise the engine's explicit
unsupported labelled-power suspension exception at admission. It demonstrates
rejection, not a distinguished supported timing alternative.

The earlier broad run in `hulk-final-two-actual-book.log` was cancelled and is
inconclusive. An idle wrapper was initially mistaken for stalled execution;
the test process delegates execution to a child. Later inspection of this exact
worktree found only the current active test tree and no abandoned descendant.
No harness cause or fix is claimed. All final mutation/build/test commands are
serial and their owned processes are harvested. No full managed-suite, native,
cross-platform or public Hulk acceptance claim is made by this packet.

## Independent actual-card review

Reviewed `163b1f66229b5426f0ed39e7937162c075802ac2` against its exact parent
`dc965aa4af7194e6fe74c2fdfca2a7c55d68f529` in an isolated worktree. The
production rows agree with printed 10015/10029, current RR 1.8 and the compatible
Toe to Toe enemy-Stun ruling. No blocking finding remains for these two authored
faces. This review does not admit the Hulk product or establish full-pack play.

`HulkFinalCardReviewTests` adds actual-book distinguishing observations:

- Toe can choose another seat's minion. Its attack is against the resolving
  player and can be defended by the other seat's ally. That ally's defeat never
  qualifies actual Chase Them Down; only the followup's own lethal enemy receipt,
  with the original incarnation and resolving player, permits its response.
- A physically cross-owned Beat Cop can use its second Action through its live
  controller. Rejecting a villain target preserves the paid continuation,
  digest, RNG words, agenda and events, and a valid minion retry deals its captured
  two threat as two damage. An unrelated token kind does not add damage.

Independent evidence is outside the repository under `%TEMP%`:

- `hulk-final-independent-inventory.json`: independently compared all 716 raw
  predecessor objects; every object is byte-identical. Final 718 rows, 33 Hulk
  faces and the final book SHA-256 recorded above agree.
- `hulk-final-independent-affected.log`: 969 Release cases pass across complete
  Hulk, Beat Cop, ability-data, Black Widow, Doctor Strange, labelled activation,
  threat movement/support, attack aftermath and citation families selected by
  their fully qualified names. This includes the edited inventory classes.
- `hulk-final-independent-token-mutant.log`: changing the compiled threat-number
  reader to sum every token kind compiles and fails the new Beat Cop control.
  Counting its nine unrelated tokens makes the damage lethal instead of two;
  departure clears live damage to zero, which fails the expected-two assertion.
- `hulk-final-independent-restored-build.log`: restored exact source bytes and
  forced a nonincremental Release build before rerunning tests.
- `hulk-final-independent-restored-focused.log`: all 23 actual-card, inventory,
  retained Beat Cop prerequisite, independent review and citation cases pass.

An exploratory direct reentry of the paid Beat Cop while its mandatory target
prompt was already open raised the explicit no-legal-option exception. There
was no card effect or executable interruption window causing that intervention;
it is excluded from reachable card evidence and from the acceptance verdict.
The earlier exploratory log `hulk-final-independent-new-tests.log` retains this
inconclusive probe. No production engine or ability-book change is retained.
