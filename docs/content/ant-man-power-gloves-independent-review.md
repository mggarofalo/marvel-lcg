# Independent Power Gloves actual-row review (MARVEL-569 / MARVEL-872)

Reviewed source `44e46e05567cb9ad0476e76f3ba72c960b98d6ef` against `b10f3ec533207725b48bf47b75daa0518110e563`. Verdict: accepted for the actual Power Gloves row and its focused engine behavior, with no blocking finding. This does not accept the complete Ant-Man pack or public gameplay.

## Authority and review

[Printed 12017](../../datasets/cards/cards.json) requires an Avenger ally, max one per ally, and an optional response after that attached ally attacks or thwarts to deal one damage to an enemy. The FAQ inventory lists 12017 without a card-specific answer; a search of the vendored rulings found no Power Gloves ruling. [Attach to](../../datasets/rules-reference/entries/attach-to.md), [max](../../datasets/rules-reference/entries/max-maximum.md), [response](../../datasets/rules-reference/entries/response.md), [ownership and control](../../datasets/rules-reference/entries/ownership-and-control.md), and [consequential damage](../../datasets/rules-reference/entries/consequential-damage.md) govern the restrictions, control and timing. Trigger and query names are engine choices.

The row expresses both completion triggers against the attached performer and selects an enemy for one damage. Paid cross-player play tests the Avenger restriction and same-host duplicate exclusion with eligible controls; the response tests distinguish owner 0 from controller 1, accept and decline, and place lethal consequential damage after the response. The actual row is also used with a synthetic labeled ally thwart to verify exact performer binding, other-ally exclusion, blanked and departed sources, confusion and aborted targets. Host departure and reentry during a nested response use an explicitly synthetic response body. Enemy tests offer both players' minions and exclude allies and identities. The separately accepted [ally completion mechanism review](ally-thwart-completion-independent-review.md) supplies labeled attack, nested and incarnation evidence. No additional test was needed for the reviewed row.

The exact raw book comparison retains the 732 predecessor object bytes and LF, appends one row, and has 733 unique IDs. The reviewed book SHA-256 is `ba342d8fa6f02d1a4cba2ef6b56cfb3fa4b9a80208dcc10abee4f826a7553a65`.

## Independent verification

A fresh `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental -m:1` passed with zero warnings and errors. The subsequent `dotnet test` with `--no-build -m:1` passed 78 cases filtered by `PowerGlovesActualTests`, `AllyThwartCompletionTests`, `AllyPowerIndependentReviewTests`, `BasicPowerResponseTests`, `CardPowerContinuationTests`, `AllyControllerPowerTests`, `AttackTriggerBindingTests`, `BasicUseAlly`, `AbilityDataTests`, and `RuleCitationTests` within that project. Logs: `C:/Users/mggar/Source/power-gloves-independent-build.log` and `C:/Users/mggar/Source/power-gloves-independent-focused.log`. This selection is not the source packet's 111-case filter.

The source packet's compiled actual-row one-to-two damage mutant failed three of six actual-row cases on damage assertions; its restored nonincremental build and 111-case focused run passed. Those are source-worker observations in `C:/Users/mggar/Source/ant-man-power-gloves-mutant.log` and `ant-man-power-gloves-restored-focused.log`, not a new independent mutation experiment.

Public admission, full pack, native interaction, broad certification and replay compatibility remain outside this review. Replay boundary work remains MARVEL-870.
