# Ronin and Assess the Situation independent review

Verdict: accept source `e8cc8db85e53df0a301705579b7e76a7bca899a6` for
exactly Ronin `12013` and Assess the Situation `12033`, relative to baseline
`a45dff783576c0ff3976f82fe2ff3c9619ccda10`. No blocking finding or additional
engine or dataset repair was identified. This review adds evidence only.

Printed authority in `datasets/cards/cards.json` gives Ronin +1 THW and +1 ATK
while an upgrade is attached, and gives Assess an unrestricted zero-cost Action
granting its player +1 hand size until the end of the phase. The authored rows
match those instructions. Ronin's existential test grants the bonus once;
Inspired's separate contribution is not a second Ronin bonus. RR 1.8
`rr:upgrade.3` and `rr:attachment` distinguish attached upgrades from encounter
Attachments; `rr:ownership-and-control.2.1` and `.6` preserve the hosted upgrade's
controller when another player owns it or takes control of its host.

Assess's lasting effect remains after the event is discarded
(`rr:lasting-effects.1`) and after changing form. End-player draw occurs before
phase-duration effects expire (`rr:end-of-player-phase.step.2` and `.step.4`).
The actual row correctly uses EndOfPhase. Both acquisition records report no
MarvelCDB FAQ entry. The older Iron Man answer `ruling:4bf1b4ba524db1d6` is
confirmed by the 1.7–1.8 answer `ruling:39c26b6b8d77edfd`: his total hand-size
maximum is 7, including Assess. The selector and maximum prerequisites have
independent closure in [the Ronin selector review](ronin-upgrade-selector-independent-review.md)
and [the Iron Man maximum review](iron-man-hand-size-maximum-review.md).

Independent byte and JSON inspection confirms 729 baseline rows and 731 current
rows, with exactly `12013` and `12033` appended, no duplicate card ids, and all
original parsed rows unchanged. The original 738108-byte prefix is identical to
the baseline and hashes to SHA-256
`bb077c7f2fed44384b1248abe23df80fde3fadf607442d7040773968384a526b`.
The book retains LF line endings. The expected count, explicit authored-face
inventory and Ant-Man inventory each admit only the two added faces.

Review considered plausible wrong implementations: counting upgrades instead of
existence; counting an encounter Attachment or another host's upgrade; retaining
the bonus after departure or source blanking; losing it when the upgrade is
blanked; granting Assess to every player or only the first seat; restricting it
to hero form; expiring at turn end or round end; and bypassing Iron Man's cap.
The existing actual-card and prerequisite checks distinguish these cases. In
particular, `AssessOnAnotherHeroIsNotSubjectToIronMansMaximum` now uses the actual
row on seat 1 and checks both seats, so an additional first-seat control would
duplicate existing evidence. No test was added merely to increase the count.

Independent verification on the reviewed source:

- `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental`:
  succeeded with zero warnings and errors.
- `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~.Cards.|FullyQualifiedName~Ronin|FullyQualifiedName~AssessTheSituationActualCardTests|FullyQualifiedName~IronManHandSizeMaximumTests|FullyQualifiedName~AntMan|FullyQualifiedName~RuleCitationTests'`:
  all 217 cases passed, none skipped.
- Source mutation logs were inspected: the compiled +2 Ronin mutation fails both
  actual-card tests at the expected stat; the compiled EndOfRound Assess mutation
  fails all three actual cases at phase expiration. These mutations were not
  rerun by this independent review. The independent nonincremental build and
  focused run establish the restored source separately.

Independent logs are outside the repository under
`C:/Users/mggar/Source/evidence/ant-ronin-assess-independent-`, with suffixes
`build.log`, `focused.log` and `hooks.log`. Normal commit hooks remain enabled;
the worktree-local Husky helper was installed before committing.

This is acceptance of two nonpublic actual-card rows, not complete Ant-Man
coverage or public product admission. No full-game or native UI acceptance was
performed. Generic projected hand-size evidence remains explicitly synthetic;
this review establishes no played-card projected hand-size consumer.
