# Target Acquired boost-ability cancellation

Nonpublic B6 mechanism packet for MARVEL-771, based on `2adfa430`. Production ability
book, setup, public admission and contract versions are unchanged.

Authority: generated `datasets/cards/cards.json` face `08024` prints Hero Response,
discard Target Acquired, cancel that card's boost ability. Vendored official ruling
`a66ca21bca34a71f` explicitly preserves that response before the boost ability resolves.
RR 1.8 `rr:boost-boost-icon.1` distinguishes stars from numeric icons; `rr:response.4`
closes declined windows. The ruling is cited in comments, not a Rule attribute,
because this packet does not add a rules-graph relationship.

`TargetAcquiredBoostAbilityTests` uses the complete printed nonpublic 08024 row and
isolated synthetic boost effects. It proves Response timing, actual cancellation of
only the first of two stars, unchanged numeric icons, attack/scheme and different
controller/activation seats, discarded-source Preparation completion and incarnation,
invalid/stale rejection, hero-form/no-star/decline controls and query/RNG/pending-work
purity compared with an unprobed control. A stale activation and a new in-play card
incarnation each invalidate the exact cancellation receipt.

Evidence logs live outside the repository in `%LOCALAPPDATA%/Temp/b6-*.log`:

- `b6-first.log`: printed-card probe fails for unsupported cancelBoostAbility.
- `b6-mutant.log`: initial mutant survived the idempotent Stunned observation; the
  probe was strengthened with counted damage to distinguish both star executions.
- `b6-mutant-counted.log`: compiled canceled-star-executes mutant killed by both
  attack/scheme cases (damage expected 5/1, actual 6/2); source restored in finally.
- `b6-restored-build.log`: nonincremental restored Release build passed.
- `b6-restored.log`: 249 focused content boost/Preparation tests passed.
- `b6-rules.log`: 58 Rules boost/active-attack tests passed.

The final architecture and normal-hook results are reported with the commit handoff.
Independent review, integrated managed lanes, replay/session and native acceptance
remain lead-owned integration evidence. This packet does not establish pack readiness.
