# Ms. Marvel ally/condition packet evidence

2026-10-26. Base `cdc2c2f9`. Four actual research faces: 05002 Red Dagger,
05023 Endurance, 05032 Morale Boost, 05033 Down Time. The ability book has 562
faces; 20 of 34 Ms. Marvel faces are authored. Public admission is unchanged.
All preceding ability-book bytes are preserved, with new UTF-8 rows appended.
Prepared engine games establish card behavior, not public setup, whole-pack
play, save/restart or native acceptance.

Authority is joined `datasets/cards/cards.json`. The Red Dagger rulings
10293662ffefd5b2 and 2368dda1610740b3 distinguish controller payment from owner
hand return. Ruling c511a191fe8cc74d confirms the interrupt returns the card
before a later defeat response could retrieve it. The vendored MarvelCDB FAQ
and Hall of Heroes corpus were searched for all seven requested card names.

Actual full-book tests:

- `MsMarvelRedDaggerTests`: consequential defeat, different-type resources,
  same-type rejection, two wild resources, paying controller seat 1 versus owner
  seat 0, exact ally returned with damage cleared, decline without payment.
- `MsMarvelConditionUpgradeTests`: ordinary paid cross-player play, health and
  alter-ego recovery increments, printed maximum one per player, second player
  still eligible, rejected duplicate payment atomicity, health in both forms,
  and bonus removal on upgrade departure.
- `MsMarvelAdditionalEventTests`: all three Morale Boost fields on either hero,
  untouched other player, persistence through phase end and expiry at round end.

Focused actual-row, ability-data and lowering profile passed 167 tests before
adding the duplicate-payment atomicity assertions; final command/result is
recorded in the handoff. Logs live outside the repository under
`C:/Users/mggar/Source/ms-events-*.log`.

Four valid data mutants each parsed/lowered and failed the intended assertions:
Red Dagger spendDistinct -> spendAny (4 failed), Endurance +3 -> +2 (2 failed),
Morale Boost EndOfRound -> EndOfPhase (2 failed), Down Time +2 -> +1 (2 failed).
The restored original dataset passed the same profile (167/167). Mutations
changed runtime JSON only, not compiled sources; original bytes were restored.
Normal commit hooks remain enabled.

## Omitted faces and next engine work

05030 Melee remains absent. `AbilityCardSelection`/`AbilityCardQuery` currently
has no relation excluding a previous enemy while retaining the character across
villain-stage promotion. Required semantics: one attack with two sequential
damage instances, choose another enemy after the first resolves, re-evaluate
Guard after defeating it, and exclude the next stage of the same villain.
Authorities: ruling:9aa6221cd0950fbb (same villain stages are not different
enemies), ruling:e0333bc6316bd7f0 (defeat Guard first, then target villain),
ruling:f32c53356db718a6 (one attack; attack responses after full resolution),
ruling:a43333f9f9d3634a (Embiggen modifies each instance).
A focused engine prerequisite should distinguish A then B from A then A;
one-enemy partial resolution; Guard-first defeat versus surviving Guard;
villain promotion exclusion; one attack-end response after both instances;
and Embiggen applied to both damage instances. Do not replace this with two
independent attack wrappers or simultaneous preselection.

05015 Tackle and 05031 Concussive Blow also remain absent. Exact failing
candidate rows: `C:/Users/mggar/Source/ms-events-status-gap-rows.json`; candidate
tests: `C:/Users/mggar/Source/ms-events-status-gap-tests.cs`.
`ms-events-test2.log` and `ms-events-test3.log` preserve reproduction evidence.
Each card gives its printed status and then deals three damage when the settled
payment includes physical. Fresh-status controls passed with physical-declared
wild (damage 3) and mental-declared wild (damage 0), while both already-statused
physical controls had no action offer. A faithful root payment conditional with
separate complete target/attack branches reproduced the same two failures.
The original candidate nested its payment conditional after status inside the
attack body. No engine source is changed by this packet.
The next engine packet must preserve no-effect rejection when status already
exists and no damage-paying resource can be selected, while offering the same
card when a physical payment makes damage effective. Include settled wild
allocation, stale/invalid payment atomicity, status immunity and no-damage
controls. Investigation belongs in engine action/target admission and payment
branch preflight; `AbilityEffectStructure.PaidResourceQueries` and
`AbilityExpressionEvaluation` are relevant resource-condition helpers. The
precise rejecting branch has not yet been diagnosed.
