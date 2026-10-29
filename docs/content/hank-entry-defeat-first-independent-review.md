# Hank entry ordering independent review

Reviewed source `338cf0fb24ab328d843d61ec0c0eade38e51fe57` against Rules Reference 1.8 hit points, ally limit and interrupts, and the owner adjudication recorded in [Hank Pym entry ordering](hank-entry-defeat-first.md). The rules require zero-health defeat and immediate enforcement of the ally limit before entry abilities; the ordering between those obligations is the owner's project choice. No source defect was found in the reviewed scope.

The independent synthetic control puts an ally owned by one player under another player's control above their ally limit. A would-be-defeated interrupt rescues it; the controller receives the mandatory limit choice and discards the entering ally to its owner's discard pile. The existing allies remain, the departed ally's entry response does not fire, and the agenda completes.

The independent test class passed three cases. A compiled mutant skipping the post-rescue limit check failed the new control: it offered `WhenCardEntersPlay` instead of `ChooseAllyForLimit`. After exact source restoration, a non-incremental Release build succeeded with no warnings or errors and all 26 focused entry and Hank tests passed. Logs are in `C:/Users/mggar/Source/evidence/hank-defeat-first-review-*.log`.
