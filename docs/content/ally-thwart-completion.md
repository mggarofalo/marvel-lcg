# Ally thwart completion prerequisite (MARVEL-872)

Base: `cd18a9b0f23cdd7541cc7b8a35a99730490b63b2`. This record accompanies the
engine-only prerequisite commit containing it. The tests use a local 12017
interpretation and synthetic Vision Action (thwart); they do not admit Ant-Man
or add a production ability row. The production ability book is unchanged.

## Authority and behavior

Printed authority is [12017 in the card dataset](../../datasets/cards/cards.json):
Power Gloves responds after its attached ally attacks or thwarts, dealing one
damage to an enemy. Its optional response binds the exact attached performer;
the upgrade owner and its controller can be different players.

- [RR 1.8 thwart.2](../../datasets/rules-reference/entries/thwart.md):
  "resolving that ability is considered to thwart the specified scheme."
  `WhenThwartEnds` is the engine-chosen condition for resolved basic and labeled
  thwarts. It does not replace `WhenBasicThwartEnds` or attack completion.
- [RR initiating-abilities.2](../../datasets/rules-reference/entries/initiating-abilities.md):
  a response "may be initiated immediately after that triggering condition becomes true."
  The enclosing ability remains suspended until that exact response window ends.
- [RR consequential-damage.1](../../datasets/rules-reference/entries/consequential-damage.md):
  consequential damage is dealt "after resolving abilities that are triggered by
  the ally attacking or thwarting." [RR ally.3](../../datasets/rules-reference/entries/ally.md)
  says "After an ally is used to attack or thwart, deal consequential damage".
  The existing consequential operation now serves labeled ally powers after
  responses and before the enclosing continuation; basic powers retain their
  own scheduled operation and are not charged twice.
- [RR response.1](../../datasets/rules-reference/entries/response.md):
  "Players can only trigger response abilities on cards they control".
  [RR ownership-and-control.2.1](../../datasets/rules-reference/entries/ownership-and-control.md)
  makes attached upgrades controlled by the host's controller.
- [RR leaves-play.1](../../datasets/rules-reference/entries/leaves-play.md):
  a departing card "is considered to be a new copy of the card."
  Consequential damage captures the performer incarnation before suspension.
  Both existing basic attack and basic thwart failed the new reentry control;
  their initiating schedules now capture the same incarnation.

The local FAQ inventory lists 12017 without a per-card answer; the vendored
rulings search revealed no special Power Gloves timing exception. No external
card text or legacy implementation supplied printed authority.

## Evidence

All logs are outside the worktree under `C:/Users/mggar/Source/`, prefixed
`ally-thwart-completion-`.

The final restored command was a non-incremental Release build of
`tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj`, followed by `dotnet test`
with `--no-build`. The filter included the complete classes
`AllyThwartCompletionTests`, `BasicPowerResponseTests`, `CardPowerContinuationTests`,
`AllyControllerPowerTests`, `AttackTriggerBindingTests`, both `BasicUseAlly` classes
and `RuleCitationTests`. Results: build zero warnings/errors; 65 passed, zero
failed. Logs: `restored-build.log` and `restored-focused.log`.

Three temporary compiled mutants were each restored in `finally`:

| Mutant | Discriminating result | Logs |
|---|---|---|
| Omit labeled thwart's general completion condition | Both accept/decline tests fail at missing response | `mutant-marker-build.log`, `mutant-marker.log` |
| Omit basic ally performer incarnation | Attack and thwart both damage a fresh returned copy; two failures | `mutant-incarnation-build.log`, `mutant-incarnation.log` |
| Move consequential prerequisite with ordinary owned work | Both accept/decline tests observe damage before response; two failures | `mutant-ordering-build.log`, `mutant-ordering.log` |

The new tests cover response accept/decline before a following immediate draw,
exact performer/controller, enemy-choice exclusion of allies, basic attack and
thwart response/consequential ordering, another ally, unavailable/blanked Gloves,
host departure during nested initiated response, departure/reentry identity,
confused basic/labeled cancellation, aborted basic target, and unlabeled threat
removal that does not count as a thwart. Initial compiled runs found ordering
and basic reentry failures; the final restored run is the acceptance evidence.

## Limits and integration

This is mechanism evidence, not complete Power Gloves card acceptance. Attachment
play restrictions, duplicate-host admission, all enemy prohibition patterns,
public gameplay, replay compatibility and product admission remain integration
or production-authoring obligations. No dataset, Call for Aid matcher, area,
main-scheme or Kang content changes are included. The changed labeled-power and
basic reentry behavior must be considered by the pending replay-boundary work.
An independent reviewer must assess this exact prerequisite diff before acceptance.
