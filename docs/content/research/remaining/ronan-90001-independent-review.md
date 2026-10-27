# Independent review of Ronan 90001

MARVEL-567. Reviewed `07434f5ad2786c44078619065265b2b41aeaf29e`
against `abdca907ee1f5fc8e228a3e5ce147da61ceea4b9` on 2026-10-27.
Canonical implementation scope and earlier evidence remain in
[the authored-face record](ronan-90001-authored.md).

Verdict: no blocking finding in this bounded face. The diff appends one
production row without changing the preceding 718 rows or engine, setup and
admission code. The actual row implements the joined 90001 text: cannot be
stunned, printed Toughness through entry, the game-subject forced interrupt
before villain-phase step-one threat, and boost entry engaged with the
activation's resolving player. Current RR 1.8 governs the reviewed behavior.

The raw Ronan section in `datasets/rulings/pages/official-ffg-rulings.html`
and its indexed records confirm first-player ties, no same-seat re-engagement
and the absolute stun restriction during Vapors replacement. The latter is
`ruling:17ea4e93b0ab8a25`, scoped pre-1.5; its answer remains consistent with
current `rr:cannot` and `rr:status-cards.1`. These historical answers do not
replace current rules. Printed authority remains `datasets/cards/cards.json`;
the [original source pin](ronan-source-pin.md) supplies provenance.

The implementation's actual-row tests distinguish tied heroes from alter-egos
and allies, retain the current engagement among legal ties, and cover no heroes,
a unique current minimum and stale selected identities. Transfers preserve
damage, readiness, counters, statuses, attachments and incarnation without
fresh Toughness. The complete phase test observes movement before threat and
the new engagement's activation in both first-player orders. Complete boost
attack and scheme activations place Ronan with non-first seat 1, grant one
Toughness, finish without termination and retain him through boost cleanup.
The existing Vulture/Thor/Hawkeye engagement-hook controls are explicitly
synthetic; they establish the shared transfer lifecycle rather than a played
Ronan set.

The independent `RonanStatusReplacementReviewTests` adds three cases using
production Ronan and production Vapors on nonpublic boards:

- Both ordinary Spell Mastery and Master of the Mystic Arts select Ronan's
  exact Tough status. Only Confused is offered, the original status remains
  until commitment, an invalid answer preserves the complete fixture snapshot,
  and resolution replaces that exact status without moving another minion's
  Tough. The Invocation continuation completes through either route.
- When Ronan is already Confused and Tough, neither status has a legal
  replacement destination. His statuses remain untouched while an otherwise
  matching mercenary's Tough can become Stunned. This supplies a valid control
  for the exclusion.

Independent evidence is in `C:/Users/mggar/Source/evidence/`:

- `ronan-actual-review-focused.log`: 21 passed, zero failed/skipped across the
  complete actual-card, phase, selected-engagement, independent-replacement and
  citation classes.
- `ronan-actual-review-status-mutant.log`: the compiling/lowered wrong-status
  row (`cannot_be_confused` replacing `cannot_be_stunned`) fails all three new
  cases. Both Vapors routes offer Stunned instead of Confused; the duplicate
  fixture incorrectly offers Ronan's Tough. These are behavioral failures.
- `ronan-actual-review-restored-build.log`: exact original book bytes restored,
  verified with `git diff --exit-code`, then a forced non-incremental Content
  Release build passed with zero warnings/errors.
- `ronan-actual-review-restored-focused.log`: 102 passed, zero failed/skipped
  using the original exact `ronan-final-filter.txt`, which also includes the
  three new Ronan cases. Command: Content `dotnet test -c Release --no-build
  --no-restore --tl:off --filter <that filter>` after the forced rebuild.
- `ronan-actual-review-architecture.log`: all 129 Architecture tests passed
  using its project with `dotnet test -c Release --tl:off`.

An initial independent fixture incorrectly expected Confused to remain selectable
on a Ronan already holding Tough and Confused; current duplicate-status limits
exclude both statuses. That assertion was corrected before the recorded final
passing runs. No production repair was required.

This review accepts only 90001. It does not repeat full Content/Rules/Session,
claim the implementation's earlier 926-case partial run green, or establish
public setup/save/restart/native acceptance. Actual 90002–90005 remain
unauthored, and the incomplete modular set remains publicly excluded.
