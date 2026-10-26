# Independent Nakia and Suit review

Reviewed revision: `2cbfc75b9da56d0b0e19964c8f78492d6a449d19` against
`10d071dd201573ca2158c3aa0f331bf64ee3f880` in an isolated worktree.
Verdict: approved for the two actual authored rows; no behavioral finding.

Read the raw joined printed 05008 and 05009 rows, vendored FAQ coverage, and
`ruling:5b44b2adcc60d4b7`. The event-ability versus selected-ally cost distinction
matches the existing engine payment-source contract. Neither card has a specific
vendored MarvelCDB FAQ entry. The ability diff adds exactly those two rows;
existing non-ASCII dataset text is unchanged. Inventory is 551 distinct rows,
nine Ms. Marvel faces; Aamir 05006 and Bruno 05007 remain unauthored. The evidence
record's deck-bottom and optional grouped retrieval gaps match the DSL surface.

Independently retained tests add five cases: another player's actual play does
not consume Nakia; printed-zero and reduced-to-zero plays do consume it; leaving
play does not remove the lasting reduction; Suit follows its second player's
hero form independently of the first player's form. These are nonpublic engine
fixtures, not public pack admission or UI acceptance evidence.

Focused command (58 passed, zero failed):

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-restore --filter 'FullyQualifiedName~MsMarvelNakiaSuitIndependentReviewTests|FullyQualifiedName~MsMarvelPersonaTests|FullyQualifiedName~MsMarvelSuitCardTests|FullyQualifiedName~AbilityData|FullyQualifiedName~AbilityProgramTests'
```

Run log: `%TEMP%/nakia-suit-independent-review.log`. The author's 14-case
characteristic run and six actual-row mutant logs were inspected: each mutant
loaded, ran, and failed a behavioral assertion for the changed restriction,
amount or exhaustion; the restored 53 author/data/lowering cases also pass in
the independent run. Author pre-commit log records all normal hooks passing.
Independent review tests are committed with normal hooks enabled. No production
edits, full solution, server, integration or native suites were run here.

Nonblocking formatting observation: `git diff --check` reports a trailing blank
line at `MsMarvelPersonaTests.cs:99` in the reviewed packet.
