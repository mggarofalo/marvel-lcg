# Doctor Strange search packet — MARVEL-810

Base `2da740d9`, 2026-10-26. The commit containing this record authors **09008 Sanctum Sanctorum and 09012 Brother Voodoo** only. The ability book is 661 rows, including 16/40 Doctor Strange faces. No public admission, setup, UI or engine changes. Mystical Studies 09006 remains unauthored behind **MARVEL-812**.

## Authority and interpretation

Printed authority is the actual generated `datasets/cards/cards.json` rows 09008 and 09012, not a synthetic replacement. Sanctum's Alter-Ego Action exhausts its source, shuffles a Spell from the controller's discard into that controller's player deck, and independently draws one. Brother's entry Response searches only the top five physical cards for Events, adds the chosen physical copy, and shuffles the whole player deck even with no match. `rr:search.1–.3`, `rr:target.6`, `rr:ability.13` and `rr:initiating-abilities.step.5` govern selection, shuffle, form and cost. No matching local MarvelCDB FAQ or HoH card-specific Sanctum/Brother ruling was found. The existing Invocation insert and ruling `2b8ccf2ab5f6391e` keep “your deck” as the player deck; the latter is pre-1.5 and consistent with the current separate-deck contract.

RR 1.8 `rr:target.2.3` says drawing has a valid target with a nonempty deck, and `.3` says any part affecting a target suffices. Thus Sanctum with no Spell can still exhaust/draw from a nonempty player deck; no Spell plus empty player deck has no effect and no offer. It is not a dependent “then” draw. A prepared empty player deck beside nonempty discard is not a stable reachable board: `rr:player-deck.4` requires the reset. The ordering test uses reachable nonempty deck/discard state and observes the Spell movement before Draw.

## Behavioral evidence

`DoctorStrangeSearchCardTests` and its nonpublic fixture execute the actual rows. They prove exact duplicate physical choice and non-first controller, a foreign-owned Sanctum under seat 1's control, own-discard Spell selection excluding foreign/non-Spell/Invocation cards, invalid answer preservation of digest/RNG/pending work, top-five bound before Event filtering (a sixth Event excluded), fewer-than-five and no-match shuffle, source departure after accepted Brother response, and no Invocation deck/discard movement. Sanctum costs are paid before a suspended selection; refreshing it during suspension distinguishes a continuation that incorrectly pays exhaustion again. Event order distinguishes shuffle-before-draw. Fixtures use actual printed catalog cards with directly prepared engine boards; they are not public gameplay/admission evidence. No synthetic production-row replacement is used by these two-card tests.

Whole relevant classes/data/citation command:

```powershell
dotnet test tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --filter 'FullyQualifiedName~DoctorStrange|FullyQualifiedName~BoundedSearch|FullyQualifiedName~ThorSearchCard|FullyQualifiedName~BlackWidowIdentitySearch|FullyQualifiedName~AbilityData|FullyQualifiedName~AbilityLowering|FullyQualifiedName~RuleCitationTests|FullyQualifiedName~AbilityAdmission|FullyQualifiedName~AbilityBook'
```

Final result: **130 passed, 0 failed, 0 skipped**. `dotnet build tests/Marvel.Content.Tests/Marvel.Content.Tests.csproj -c Release --no-incremental` passed with zero warnings/errors after mutant restoration. Architecture suite: **129 passed**. Final relevant run follows correction of the two existing global 659-row inventory assertions and the 14-face count. Existing D1 segment identity/order remains exact; the new packet inventory asserts exact total 661. No duplicate-detection gate is weakened.

Mutation evidence: remove both Brother `top: 5` bounds, leaving valid lowered production data: **2 failed, 1 passed**, including an unexpected sixth Event choice. Move Sanctum's draw before its shuffle selection: **1 failed**, the movement-order assertion. Both were executed against compiled tests and lowered by the real runner, then restored from exact bytes. The restored project was rebuilt nonincrementally. Full logs are outside the repo under `C:/Users/mggar/Source/strange-search-*.log`; final relevant results supersede earlier inventory-failure runs.

A raw JSON-decoder comparison against base `2da740d9` proved all **659 predecessor object strings byte-identical in UTF-8**, and the total is 661. The append adds no starting counters. Resource reprint content, public admission, and Invocation mechanisms are unchanged. Broader Content/whole-game/platform certification belongs to integration, not this packet.

## Independent two-card review

Reviewed candidate `5e07f6420aa223f0372d96316d218440ac3e1c73` against
`2da740d9` on 2026-10-26, including both actual authored rows, the complete
search test class and its fixtures, changed inventory assertions, generated
printed rows, RR 1.8 search/target/ownership/form/cost clauses, the Invocation
insert and local FAQ/HoH corpus. The local FAQ has no card-specific entry for
either card; the historical “your deck” ruling is consistent with the current
separate-deck insert. The new review tests use the actual production book.

**Verdict: accept this bounded two-card content packet.** No correctness
finding remains in the reviewed Sanctum/Brother behavior. Sanctum's controller
supplies the form, discard, deck and drawn card even when another seat owns the
support; its draw is independent and follows the Spell shuffle. Brother searches
physical top-five cards before Event filtering, accepts entry without playing,
and leaves the search optional. Existing exact-copy, foreign-area, short-deck,
no-match, departure and cost-continuation controls retain their assertions.

`DoctorStrangeSearchReviewTests` adds four independent cases. Sanctum offer and
description queries have a baseline captured before the first query, including
digest, full RNG state/consumption and agenda work, followed by comparison with
an unprobed world's completed decisions for both Spell/no-Spell branches.
Its resolution explicitly preserves Invocation deck/discard order and membership
and the other player's hand. Brother's accepted/declined put-into-play cases
distinguish an entry response from a played-only response and prove declining
does not move cards or shuffle.

The whole relevant-class command above passes **134/134**, zero skipped,
including the four independent cases. Log:
`C:/Users/mggar/Source/strange-search-independent-relevant.log`.
The full Architecture project passes **129/129**, zero skipped; log:
`C:/Users/mggar/Source/strange-search-independent-architecture.log`.
An independent raw JSON-decoder comparison again confirms all 659 predecessor
object strings unchanged, exactly 661 rows, and only 09008/09012 appended.
The implementation's two compiled/lowered mutant logs and restored
non-incremental build log were inspected; their failures distinguish the sixth
Event and shuffle-before-draw errors as reported above. Those mutant runs were
performed by the implementer, not rerun by this reviewer.

This verdict does not admit Doctor Strange publicly or accept the remaining
24 faces, Mystical Studies, a complete starter, UI behavior, save/replay or
cross-platform gameplay. MARVEL-812's exact printed-set prerequisite is separate
from this two-card acceptance.

## MARVEL-812: exact printed-set prerequisite

Generated card metadata already carries `set: "doctor_strange"` for 09006 and signature cards. `ICardFacts.EncounterSet(faceId)` exists, and `src/Marvel.Content/CardCatalog.cs:102` returns `Find(faceId).Set`; despite its historical name it exposes this general printed set. Reuse that port. Current `cardsIn` filtering admits area(s), kind, trait, title, top, aspectOnly, cardClass, but **no printed-set filter**. Hero class is not exact printed Doctor Strange membership, and enumerating the signature list in the authored row is not the shared mechanism.

The following expected-behavior probe was temporarily compiled and run. Its failure is preserved here rather than introducing a known red test into the two-card packet. Result: **1 failed**, `AbilityException: '09006' ability 0 at 'effect/chooseCard/from/cardsIn/set': unknown argument 'set'`. Parsing alone is insufficient: constructing the runner exercises lowering.

```csharp
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Xunit;
namespace Marvel.Content.Tests.Play;
public sealed class MysticalStudiesSetFilterPrerequisiteTests
{
    [Fact]
    public void ExactPrintedSetSearchCanLower()
    {
        var book = AbilityCatalog.Parse("""
        {"cards":[{"card":"09006","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"alter-ego"},
          "effect":{"chooseCard":{"from":{"cardsIn":{"areas":["yourDeck","yourDiscardPile"],"set":"doctor_strange"}},
            "effect":{"addToHand":"chosen"}}}}]}]}
        """);
        var runner = new AbilityRunner(book);
        Assert.NotNull(runner);
    }
}
```

After the shared selector receives independent implementation/review, author Studies and prove own deck/discard exact physical choices, non-first controller, printed-set exclusions, no-match shuffle, Invocation exclusion and alter-ego restriction. This record claims none of that Studies coverage as implemented.
