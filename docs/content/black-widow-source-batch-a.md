# Black Widow source batch A â€” MARVEL-764

Nonpublic authored-content packet based on `63937f6d`, for canonical pack
MARVEL-564. Printed authority is `datasets/cards/cards.json`; existing same-title
rows supply composition patterns only. This packet changes no engine mechanism,
setup recipe, public discovery or product admission. Black Widow remains incomplete.

## Exact inventory

The ability book grows from 611 to 620 physical faces. Only these nine are appended;
the original 611-row prefix and ordering are preserved byte-for-byte. Explicit
face inventory adds only these IDs; starting-counter inventory adds 08016 only.

| Face | Actual-row evidence |
|---|---|
| 08014 The Power of Justice | Two wild for Justice card payment, one for unrelated card/ability cost; mixed-printing maximum two. |
| 08015 Interrogation Room | Identity basic attack and nonattack event defeat credit; foreign identity and ally attack excluded; exhaust and remove exactly one threat. |
| 08016 Surveillance Team | Three snoop counters from actual play; empty/exhausted source cannot pay; final-counter discard retains committed one-threat effect. |
| 08019 Nick Fury | Actual paid entry offers and resolves all three printed choices; round-end discard follows only the retained incarnation. |
| 08020 Energy | Actual payment generates YY; explicit empty row; same-title maximum one across printings. |
| 08021 Genius | Actual payment generates BB; explicit empty row; same-title maximum one across printings. |
| 08022 Strength | Actual payment generates RR; explicit empty row; same-title maximum one across printings. |
| 08027 Killer for Hire | Full reveal yields printed three plus one per player; acceleration contributes one during threat step; numeric boost three. |
| 08028 Hydra Mercenary | Full reveal enters engaged; printed HYDRA and Guard; basic/labeled attacks exclude protected villain; another seat and nonattack damage remain eligible; numeric boost one, explicit empty row. |

## Authority and limits

Printed traits, keywords, limits, stats, icons and resources remain catalog facts.
Guard and numeric boosts require no invented abilities. RR 1.8 `rr:you-your.6`,
`.12` and `.15` govern defeat responsibility: an ally's attack is not its
controller's identity attack; a played event is the identity's action. These
expectations follow the current rule rather than a permissive same-title comment.
RR `rr:uses-x-type` discards a depleted Surveillance Team; `rr:leaves-play.1`
makes returned Nick Fury a new copy. The reentry probe isolates an old delayed
effect through physical departure/reentry; it does not claim a Rapid Response
composition journey. No new shared capability was needed for these nine faces.
08029 and the other 24 Black Widow faces remain unauthored.

## Verification

Focused content command: `dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj
-c Release --filter "FullyQualifiedName~BlackWidow|FullyQualifiedName~AbilityData|FullyQualifiedName~RuleCitationTests"`.
58 tests pass. The packet contains 22 Black Widow cases; the remainder are data and
citation gates. Four test classes cover the actual canonical rows rather than
test-local interpretations. The prepared zero-escalation main scheme explicitly
starts at zero so Killer for Hire's live acceleration contributes exactly one.

A compiled 08016 row mutant changed threat removal from one to two. Both final-
counter and multi-counter cases failed on the resulting threat. The original row
was restored, the project rebuilt with `--no-incremental`, and the same 58 tests
passed. The final fixture-baseline refinement passes the same 58 checks in
`C:/Users/mggar/Source/widow-a-final.log`. External artifacts are `C:/Users/mggar/Source/widow-a-{focused,mutant,
restored-build,restored}.log`; initial fixture-diagnosis logs are retained separately.
Normal pre-commit architecture, complexity and file-length hooks run at commit.

Independent review and integrated broad verification belong to the delivery lead.
This packet does not establish public Black Widow readiness or whole-pack acceptance.

## Independent review of e7151e96

Approved for this nine-face nonpublic packet. The reviewer separately read the
generated printed faces, the vendored reprint records, RR 1.8 defeat responsibility,
Uses, Guard, delayed-effect and incarnation clauses, and the applicable Nick Fury
and Power of Justice rulings. All nine faces and the seven original player-card
printings were queried in the vendored MarvelCDB FAQ and have no entries. The
historical Nick Fury blanking answer supports retention of the created effect;
RR 1.8 delayed-effect and leaves-play definitions govern current timing and copy
identity. This is not a claim to have tested Beguiled or overpayment.

Independent JSON decoding compared both the semantic objects and exact raw row
text: the original 611-row prefix is unchanged, and the appended nine IDs and
their order match this inventory. Raw quantities are 2 each for 08014–08016,
1 each for 08019–08022 and 08027, and 2 for 08028. Player reprints map respectively
to 01062, 01063, 01064, 01084, 01088, 01089 and 01090. Printed traits, resource
symbols, limits, Uses, Guard, acceleration and numeric boosts match the authored
interpretation; the encounter faces have distinct printed authority.

No behavioral defect was found. The 08015 authoring note incorrectly described
all controlled characters despite its identity-only implementation and tests;
the note now states the current rule. Independent coverage adds all four defeat
sources with the support controlled by seat 1, and checks the answering seat for
both controllers. This distinguishes hardcoded first-player resolution from the
support controller's actual entitlement.

The source's 58 focused checks passed independently. The reviewed packet passes
62 checks, including 26 Black Widow cases, using the same filter above. Logs are
`C:/Users/mggar/Source/widow-a-review-{inventory,focused,final}.log`.
The intermediate `widow-a-review-extended.log` records one added-fixture failure:
the seat-1 event initially retained seat 0 in its explicit PendingAbility; its
resolver now matches the event controller. Source Surveillance mutant and restored
build/test logs were inspected and confirm the two intended assertion failures
and restored 58-test pass. Integration and broader verification remain separate.
