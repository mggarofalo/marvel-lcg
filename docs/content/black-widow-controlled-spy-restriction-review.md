# Independent B5 review (MARVEL-770)

Reviewed implementation `9557fc3900087809b9f3871a258f02bf3cb2b6a1` against
base `279e8398`, using the canonical `08018` Spycraft and `08033` Espionage
printed text, `rr:character`, and `rr:ownership-and-control.8`. No production
correctness finding remains in this bounded review.

The printed prerequisite requires a character currently controlled by the acting
player and currently in play. The predicate uses effective kind, authoritative
control, and live traits. Identity forms, transferred allies, granted/lost traits,
noncharacter supports, scenario-controlled encounter characters, and out-of-play
cards are distinguished. The existing identity-trait and form checks remain in
the shared legality path before payment. Review of that path found no new
mutation or RNG access in the restriction query.

The supplied stale-play test snapshots card state, full RNG state/consumption,
registered continuous effects, agenda stage/busy state and outstanding frames
before querying. It preserves the pending prompt and compares subsequent play
with an unprobed control. Its direct `CardPlay.Play` rejection is engine evidence;
it does not claim a new public session or UI journey.

Added nine independent cases in `ControlledCharacterPlayRestrictionReviewTests`:

- The complete canonical catalog exposes this restriction on exactly `08018`
  and `08033`; Cerebro's incidental PSIONIC character reference is excluded.
- Six synthetic parser cases distinguish a complete restriction sentence from
  action text, support restrictions, incidental references and identity traits.
- Both restricted cards follow actual `TakeControl` and `ReturnToOwnerControl`
  operations across two acting seats while physical ownership remains fixed.
  Same-cost unrestricted cards remain eligible on both seats.

Focused Release verification passed 33 cases, comprising supplied restriction,
existing identity-trait, rule-citation and independent review tests. A compiled
mutant replacing the acting-seat comparison with constant seat zero failed both
transfer cases at the intended eligibility assertion. Restored sources were
rebuilt with `--no-incremental`; all 33 cases passed again.

One initial review fixture omitted explicit player ownership and therefore
exercised the scenario-player-card ownership exception on transfer. The fixture
now sets `cardOwner: 0`; this was not a production defect.

Logs are external under `C:/Users/mggar/AppData/Local/Temp/`:
`bw-spy-review-focused.log`, `bw-spy-review-mutant.log`,
`bw-spy-review-restore-build.log`, and `bw-spy-review-restored.log`.
These are focused Windows checks. Full combined managed/native/platform
verification and protocol integration remain lead-owned. This packet changes
no production ability data, setup, admission or protocol version.
