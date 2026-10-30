# Care for Cassie independent content review

Reviewed source `11ec5c48`, relative to `93879985`, on 2026-10-28. Scope is
actual obligation `12025`, its inventory changes and prepared-board fixtures.
The duration engine has its separate [MARVEL-884 review](marvel-884-duration-review.md).

Verdict: accept this bounded actual-card increment with the citation corrections
and independent choice control in this review. No production defect or ability-row
change was needed. This does not accept the incomplete Ant-Man pack or a public,
native, complete-game or replay journey.

## Authority and examination

The generated `datasets/cards/cards.json` row says to give the card to Scott Lang,
permits an alter-ego flip, then offers exhaust Scott Lang to remove it, or a chosen
hand discard followed by form prohibition through the next own turn and obligation
discard. The authored row follows that order and uses the existing GiveTo routing.
RR 1.8 `rr:obligation.4` assigns both the card and decision to the associated player;
`rr:form-change-form.4` excludes exhausting alter-ego Scott while in hero form.
`rr:cost-arrow-icon` requires the text before the arrow to be fully paid/resolved,
and `rr:exhausted.1` prohibits exhausting an already exhausted card.

The source citations needed correction: `rr:cost.3` concerns resource payment,
not the exhaust arrow; `rr:ability.7.1` makes the initial "may" flip optional,
not a general instruction to resolve as much as possible. The empty-hand branch
is interpreted from the whole printed option: it still has valid identity and
obligation targets (`rr:choose-option.1`, `rr:target.2`), its sentences resolve in
order (`rr:ability.4`), and neither a cost arrow nor "then" makes the prohibition
and cleanup dependent on discarding. This is an interpretation of the current
Rules Reference and printed text, not an explicit card-specific ruling. The
vendored FAQ contains no `12025` entry; searches of the vendored rulings for its
code/title found none. No online acquisition or dataset refresh was needed.

The source's six actual-card cases exercise non-first Scott, both hero forms,
optional flip/removal, foreign-hand rejection with digest/RNG/pending-work
preservation, empty hand, exhausted alter-ego, source departure and real own-turn
expiry. The reveal fixture schedules the encounter reveal procedure rather than
calling the ability body directly. Its worlds are prepared research boards with
distinct identities; they bypass public setup/admission intentionally.

The independent control flips Giant form to ready alter-ego, verifies both final
options remain offered, then chooses the second physical hand card for discard.
It verifies the first card remains, Scott stays ready in alter-ego, the obligation
is discarded and only Scott is prohibited from changing form. Thus the optional
flip does not commit the exhaust branch, and the player's particular discard
selection matters.

All 754 previous ability objects compare equal independently against the base.
Their first 764372 raw bytes are unchanged, SHA-256
`9d979626bc6d6f87626890bfb305c4b39b17fa9eb544be96151211f67322f28f`.
The sole appended row is `12025`: 755 rows and 32 authored Ant-Man faces. The pack
remains incomplete and publicly excluded.

## Independent verification

Commands ran in this review worktree with `MSBUILDDISABLENODEREUSE=1`, Release,
`-m:1`, Windows PTY, and normal hooks enabled. Evidence is external under
`C:/Users/mggar/Source/evidence/`.

- `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release -m:1 --filter 'FullyQualifiedName~CareForCassie|FullyQualifiedName~NextPlayerTurnProhibitionTests|FullyQualifiedName~AntManReprintTests|FullyQualifiedName~AbilityData'`: 56 passed (`cassie-review-focused.log`).
- Temporary compiled mutant in `AbilitySelectedHandDiscard.Commit` validated the input but discarded the first hand card instead of the selected card. `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release -m:1 --filter 'FullyQualifiedName~CareForCassie'`: the independent control failed at retained-object identity; all six source actual-card controls passed (`cassie-review-first-card-mutant.log`). This demonstrates a plausible wrong implementation the source controls alone missed.
- The original engine file was restored byte-for-byte. `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release -m:1 --no-incremental` passed (`cassie-review-restored-build.log`). The first command repeated with `--no-build` passed all 56 (`cassie-review-restored-tests.log`). No engine change remains.
- Independent semantic/raw-prefix comparison is captured in `cassie-review-book-prefix.log`.
- The normal commit hooks' result is captured in `cassie-review-hooks.log`; no hook bypass is authorized or used.

An initial independent-test compile hit CA1861 on a constant array argument, and
an early fixture assertion ran before `Sequence.Work` applied the queued flip.
Both test-only defects were corrected before the passing run and mutation trial.
Source-author reports of 86 focused checks and 12 catalog/admission cases remain
separate evidence; this review does not claim to have independently rerun them.

Broader duration boundary semantics, public admission, UI explanation/interaction,
complete games and replay publication are outside this review. The preexisting
turn-beginning timing inference remains as documented in the duration review.
