# Black Widow identity and search batch

Nonpublic content packet for MARVEL-564. Base `850597c2`; the first 620 ability rows are preserved. This packet appends **three** faces, bringing the ability book to 623 rows. It does not admit Black Widow setup or the full product.

| Face | Result | Focused evidence |
|---|---|---|
| 08001b Natasha Romanoff | Authored | Own played Preparation draws one; other seat, non-Preparation, put-into-play and hero-side play excluded. Form changes retain use; both phase boundaries restore it. |
| 08002 Winter Soldier | Authored using reviewed B4 | Zero/two/five controlled Preparations cost four/two/zero. Other seat, discard, Spy ally and non-Preparation support excluded. Queries preserve state/RNG/pending work; payment reaches ally entry. |
| 08005 Safe House #29 | **Unauthored: missing cost-side selection** | Compiled candidate exhaust cost plus filtered discard choice effect exhausts before selection. See red probe below. |
| 08011 Agent Coulson | Authored | Non-first controller chooses an exact own deck/discard Preparation. Search identities remain absent from the initial response. Discard/no-match paths shuffle. Accepted response survives source departure; foreign answer preserves pending search/state/RNG and accepts a valid retry. |

None of the three authored faces starts counters; the focused inventory asserts this explicitly. Existing counter inventory is unchanged. Generated printed authority is `datasets/cards/cards.json`. Local FAQ lists all four IDs as queried and provides no entries for them. Local Hall of Heroes text was checked; same-title later Winter Soldier material does not modify these printed faces. Current RR 1.8 `rr:limit`, `rr:form-change-form.2`, `rr:play-put-into-play.3`, and `rr:initiating-abilities` govern the tested boundaries.

`BlackWidowIdentitySearchFixture` adds explicitly test-local empty 08001a and 08017 rows to isolate the actual new rows. Their unimplemented text is not claimed as coverage; these are bounded nonpublic engine tests, not full-pack gameplay. The Preparation fixture respects the printed one-copy limit by discarding its previous copy before each subsequent play.

## Safe House prerequisite

The minimal probe is preserved outside the repository at `C:/Users/mggar/Source/bw-safe-house-red-probe.cs`, with compiled failure in `C:/Users/mggar/Source/bw-identity-first.log`. It constructs an alter-ego support action using `exhaust: this` followed by `chooseCard` from own Preparation discard. After accepting with no selected card, `source.Ready` is false; the required pre-arrow choice has not happened. `AbilityCostLowering` has no card-choice cost, and support `AbilityRecipientCommitment` only binds identity recipients. Recasting the printed choice as a post-arrow effect would silently change the card.

The next engine packet should add a cost-side exact filtered own-discard selection with atomic exhaust/selection commitment: non-first own card, no eligible card, missing/invalid/foreign/stale answers, source readiness and post-arrow timing. It needs separate implementation and independent review. No approximated Safe House row is included here.

## Validation

External logs use prefix `C:/Users/mggar/Source/bw-identity-`. `mutant-build.log` compiles a Natasha draw-two actual-row mutant; `mutant.log` kills it with expected one versus actual two. Restoration uses `dotnet build ... -c Release --no-incremental` before repeating the focused tests (`restored-build.log`, `restored.log`). Filter: `BlackWidowIdentitySearchTests|AbilityData|EveryCitedRuleExists`.

Ability JSON SHA-256: `0deac5f3340c97d93f1a49e07805eaf4353de21dee3376e10d9456970e24484c`. Public setup/admission and other Widow rows are unchanged. Broader combined Black Widow Content verification belongs to the lead after B1 integration; this packet does not repeat that suite or claim native/product acceptance.
