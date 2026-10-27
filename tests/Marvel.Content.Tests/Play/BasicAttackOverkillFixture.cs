using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Content.Tests.Play;

internal static class BasicAttackOverkillFixture
{
    // Synthetic prerequisite definitions; no Hulk ability-book row or public admission.
    internal const string Conditional = """
        {"if":{"test":{"not":{"or":[{"paidWithResource":"B"},{"paidWithResource":"Y"},{"paidWithResource":"G"}]}},
          "then":{"boostBasicUse":"overkill"}}}
        """;

    internal static AbilityRunner Runner(bool suspend = false, string? after = null, string extra = "") =>
        new(AbilityCatalog.Parse($$"""
        {"cards":[{"card":"10003","abilities":[{
          "trigger":{"event":"WhenHeroBasicAttackInitiated","timing":"Interrupt","actor":"you","form":"hero","player":"you"},
          "effect":{"seq":[{"boostBasicUse":10},
            {{(suspend ? "{\"choose\":{\"options\":[{\"draw\":{\"player\":\"you\",\"count\":1}},{\"draw\":{\"player\":\"you\",\"count\":2}}]}}," : "")}}
            {{Conditional}}{{(after is null ? "" : "," + after)}}]}
        }]}{{extra}}]}
        """));

    internal static (World World, Card Source, Card Physical, Card Other, Card Minion) Board(AbilityRunner runner, bool mixed = false)
    {
        var (_, world) = Playing(_ => { }, hero: true, abilities: runner);
        var hand = world.Seats[0].Hand;
        var source = world.CreateCard("10003", hand);
        var physical = world.CreateCard("01090", hand);
        var other = world.CreateCard(mixed ? "01004" : "01003", hand);
        var minion = Minion(world);
        return (world, source, physical, other, minion);
    }

    internal static Card Minion(World world) => world.CreateCard("01101",
        world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));

    internal static void Pay(World world, AbilityRunner runner, Card source, Card physical, Card other)
    {
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var offer = prompt.Affordances.Single(item => item.AnchorId == source.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt,
            new(offer.Id, [], [physical.ObjectId, other.ObjectId], Allocations:
                [new(physical.ObjectId, 0, "RR"), new(other.ObjectId, 0, other.FaceId == "01004" ? "B" : "R")]), []);
    }
}
