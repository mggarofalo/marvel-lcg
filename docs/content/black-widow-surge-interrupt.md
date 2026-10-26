# Black Widow B7: actual surge-resolution interrupt

MARVEL-772 (`b70e924b-f9d4-4536-ac82-27ec0d3e6229`), based on reviewed integrated
`f4105b82`. This packet supplies engine capability and test-local Espionage
`08033`; it does not add production ability rows, setup, public admission or a
version change. Independent review and wave integration remain separate.

The printed card discards itself as an optional interrupt cost and draws two.
Its trigger is actual surge resolution, after reveal cancellation and any
first-player ordering of simultaneous When Revealed abilities. The new typed
`SurgeKeywordOperation` owns the narrower `WhenSurgeWouldResolve` occurrence.
Printed surge uses the existing ordered reveal continuation. Gained surge uses
that same operation and the existing procedure suspension mechanism, preserving
child completion before the originating ability continues. Direct low-level
helpers without an agenda retain immediate execution; those helpers do not
establish full encounter timing evidence.

Authority: generated `datasets/cards/cards.json` face `08033`; current RR 1.8
`rr:surge.1` says "Deal yourself 1 facedown encounter card" and `rr:surge.2`
requires original-card resolution and its responses to complete before the extra
card is revealed. The current definition governs over historical immediate-reveal
wording in rulings `466074a868d96ed2` and `3b62c62b7cbcf49e`. Ruling
`84569f1a5fcccb6f` says canceled surge leaves Espionage nothing to interrupt; that
cancellation conclusion remains compatible with the current definition. The
vendored MarvelCDB FAQ has no direct Espionage ruling entry. B5 already derives
the controlled-SPY play restriction from printed facts; B1 supplies completed
Preparation receipts.

`EspionageSurgeTests` has 11 cases covering the printed discard/draw, Synth-Suit
receipt after the draw but before the surge deal, three separately optional
copies, both controller/resolving-player ownership directions and non-first
seat, optional decline, encounter-deck reset, both orders of a synthetic printed
When Revealed ability and surge, canceled reveal effects, repeated query and
invalid/stale answer purity with full RNG state and pending frames, gained surge,
and complete villain-phase treachery cleanup plus the extra card's reveal.
The printed Espionage row is test-local; the ordering and cancel controls are
explicitly synthetic and do not claim Spycraft implementation.

Evidence retained outside the repository under `C:/Users/mggar/Source/`:

- `bw-surge-mutant.log`: compiled mutant maps the surge operation to no triggering
  conditions. The printed characteristic test fails because its interrupt prompt
  is absent (1 failed), distinguishing a silent immediate deal.
- `bw-surge-restored-build.log`: restored sources rebuilt with `--no-incremental`.
- `bw-surge-restored.log`: 86 passing focused content tests, including all 11
  Espionage cases, keyword stacking, Harvest receipts, printed Preparation
  resolution, controlled-character restrictions, boost cancellation, Core
  interrupt context, rule citations and Absorbing Man playerless setup.
- `bw-surge-final-regressions.log`: the same 86 focused tests pass after separating
  encounter stage dispatch from reveal decisions, immediate outcome recording
  from procedure continuation, and fixture catalog composition from board setup.
- `bw-surge-architecture.log`: 22 focused source-layout/debt checks before those
  cohesive extractions; the normal hooks recheck the final source layout.
- `bw-surge-hooks.log`: normal commit hooks on the final candidate.
- `bw-surge-hooks-first.log`: initial normal hook run passed 66 architecture tests
  and rejected complexity in three methods; the separations above address those
  responsibilities without changing baselines.

An initial mutation attempt failed compilation because newline translation
corrupted its XML documentation. That attempt is not mutation evidence; the
byte-preserving compiled mutation above replaces it. Initial fixture failures
(unknown card face, a malformed synthetic cancellation operand, and expecting
minion placement for Sonic Boom) were corrected before the final restored run.
No full managed suite, native UI journey, replay/transport journey or public
Black Widow journey is claimed by this bounded packet.

## Independent B7 review

Reviewed source `2bc94fef14b686b5bef73d4ba7b9297de76dc335` against
`f4105b82` in an isolated worktree. Raw `08033`, RR 1.8 `rr:surge.1` and
`rr:surge.2`, the cancellation ruling, and the current vendored FAQ were checked.
The typed child correctly isolates actual keyword resolution from initial reveal;
printed ordering, cancellation, seat ownership, separately optional copies,
encounter reset, full RNG/pending query purity and playerless setup have focused
source coverage. The stable keyword ordinals are unchanged. No production card,
setup, admission or version changes are part of this review.

Independent `SurgeReplayReviewTests` adds four durable cases using the existing
`IDurableGameFactory` seam: printed/gained surge crossed with accepting/declining
the completed Preparation's Synth-Suit response. The test-local book and synthetic
initial full `RevealEncounterCards` queue do not establish public Black Widow
admission. Each case round-trips strict save JSON and restarts a fresh `EngineHost`
both before the surge deal and at the Preparation completion receipt. It compares
raw replay events, digest, all MT19937 state words/index/consumption, exact durable
prompt, pending step coordinates/stage, phase and round. It verifies the extra
card remains facedown through the receipt, both encounter cards finish their
appropriate lifecycle, and a following gained-surge instruction cannot run before
the child completes. The source passed all four cases; log
`C:/Users/mggar/Source/bw-surge-review-tests.log`.

**Compatibility finding:** `EncounterRevealStages` hardcodes `"surge"` for the
new deal, including gained surge. The predecessor interpreter passes the originating
ability trigger. Independent `SurgeHistoryReviewTests` exercises actual admitted
Core `01104` with the production book through the complete encounter queue. It
passes on genuine predecessor `f4105b82` and fails on reviewed source because the
raw deal event changes from `"WhenCardRevealed"` to `"surge"`. Existing replay-v26
histories can therefore diverge even with no Espionage present. This is a source
provenance defect requiring correction before integration, rather than grounds to
rewrite historical fixtures or unilaterally change versions. Evidence:
`bw-surge-review-history-base.log` and `bw-surge-review-history-current.log` under
`C:/Users/mggar/Source/`. The lead owns remediation and the final source-revision
verification.

Initial independent fixture failures (missing `Marvel.Content` import, unauthored
reveal data, and a synthetic ability accidentally attached to Quickstrike Vulture)
were corrected; they are not production findings. An exploratory metadata tuple
expectation was also corrected against the predecessor: ordinary When Revealed
results have no forced-ability association. No metadata regression is claimed.
No full suite, native UI, public admission or complete historical-save matrix is
claimed by this bounded review.

### Corrected-source verdict

The lead's four-file correction `5d6c446822a0361a97e9e0b545104540ea80f921`
was applied unchanged as review-tree commit
`806005dcc80b6032600c03112f924c9af4610b75`, on independent test commit `208d99a0`.
It captures the originating gained-surge trigger and preserves the printed
keyword's `surge` spelling. No metadata or version changes were needed.

On that exact corrected tree, 49 focused content checks pass: the independent
predecessor-sensitive history regression, all 11 Espionage cases, keyword stacking,
reveal resolution status, printed Preparation completion and independent
Preparation continuations. All four durable JSON/fresh-host cases also pass.
Logs: `C:/Users/mggar/Source/bw-surge-review-final-content.log` and
`C:/Users/mggar/Source/bw-surge-review-final-durable.log`. The source's earlier
86-case sweep supplies the separately recorded playerless setup evidence; this
49-case filter does not claim an additional playerless setup run.

The reported compatibility defect is resolved. This bounded B7 engine-capability
review has no remaining diagnosed correctness finding. It covers timing,
provenance and durable continuations, without expanding public admission or
claiming a complete historical-save matrix, full suite or native acceptance.
Independent test commit hooks passed 66 architecture checks, complexity and file
length. Final review-note hooks are recorded in `bw-surge-review-final-hooks.log`.
