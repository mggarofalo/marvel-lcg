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
