# Independent Hammer Throw terminal stage review

Review candidate: `df8f8fab` plus public-game source
`39923a290bc9bc808a08be567cfaa86dbf7adf8c`, initial stage fix `15b4c9b5`,
and terminal extension `09cd27280b9c4ed2e9dca1eaa79e603676874ca2`.
The corresponding isolated review-tree source tip is `c55e254d`.

Verdict: approve this bounded engine change. No production correctness finding
was identified in the changed projection/dependency/suspension paths. This is an
engine review, not whole-pack or product acceptance.

Printed joined 06005 requires exhaustion, eight attack damage with overkill and
the Mjolnir return. Rules Reference 1.8 `rr:overkill.1` requires actual initial
minion defeat before excess damage; `rr:villain-defeat` reveals the next stage
without carrying excess damage onto it. Printed 04077 distinguishes an existing
Super Absorbing Power from searching for it. `rr:hit-points.2.3` owns the health
loss when a constant ends. No earlier simultaneous-overkill ruling is used.

The terminal bypass applies to a unique owned, currently in-play titled upgrade
return and does not construct a stage result. The live return reselects after
stage work and owns constant-ending health settlement. The existing source-gone
test therefore correctly permits an empty live return. The stable-read proof
remains necessary where a following instruction consumes projected state.

Added distinguishing evidence:

- Actual 01101 minion defeat, five excess damage promoting Absorbing Man I to
  II, existing Super Absorbing Power, one facedown encounter card per player,
  unchanged existing environment, zero damage on the new stage and exact paid
  Mjolnir returned to its owner. Querying before commitment preserves digest,
  RNG and outstanding work. This is a prepared two-player component state.
- Synthetic Aerial-dependent Helmet constant, with 12 and 16 damage on Thor,
  proves the post-stage return settles both survival and owner elimination;
  the other player survives. Printed Helmet is unconditional; this synthetic
  condition deliberately isolates the dependency.
- Synthetic nested sequence and conditional return followed by an outer damage
  read both refuse before target-answer mutation when the stage can discard
  Mjolnir. Digest, RNG and outstanding work remain unchanged.

Windows Release checks on this source plus the review tests:

- Five added cases pass. External log: `C:/Users/mggar/Source/hammer-review-focused.log`.
- Compiled mutation passes `false` for terminal admission at the CanFinishLive
  call. Both health-loss cases fail with the intended unprojected When Revealed
  exception; the three other controls pass. External log:
  `C:/Users/mggar/Source/hammer-review-mutant.log`.
- Original production source restored byte-for-byte. A nonincremental build
  succeeds with zero warnings/errors, then all 53 HammerThrow-filter cases pass.
  Logs: `C:/Users/mggar/Source/hammer-review-restored-build.log` and
  `C:/Users/mggar/Source/hammer-review-restored.log`.
- The ten unchanged production-factory Thor games, including original Rhino
  seeds 8 and 1, complete with exact decision replay. External log:
  `C:/Users/mggar/Source/hammer-review-public.log`. These cover the existing
  selected scenarios/seeds, not every stage in the component matrix.

Untested here: full managed/integration lanes, Linux and native UI. The stage
matrix remains component evidence and does not prove a complete public game
against every scenario or every expert stage. Public-game completion does not
itself establish player victory.
