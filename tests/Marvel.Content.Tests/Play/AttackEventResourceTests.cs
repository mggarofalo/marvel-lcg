using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic generator definitions exercise Martial Prowess's printed Attack trait restriction.</summary>
public sealed class AttackEventResourceTests
{
    [Theory]
    [InlineData("10014", true)]
    [InlineData("10002", true)]
    [InlineData("10003", false)]
    [InlineData("10004", false)]
    [InlineData("01063", false)]
    [InlineData("01001a", false)]
    public void ActualCostCardRequiresBothEventKindAndAttackTrait(string face, bool allowed)
    {
        var fixture = RestrictedResourceFixture.Create("attackEvent", actionFace: face);
        var (world, runner, generator, target) = fixture;
        Assert.Equal(allowed, CardPayment.Generators(world, world.Facts, world.Seats[0], runner, target)
            .Any(source => source.Effect == generator.ObjectId));
        Assert.Equal(allowed, CardPayment.Generators(world, world.Facts, world.Seats[0], runner,
            abilitySource: target).Any(source => source.Effect == generator.ObjectId));
        string before = world.Digest().Canonical();
        if (!allowed)
        {
            var events = new List<Marvel.Rules.Events.GameEvent>();
            Assert.Throws<RulesNotImplementedException>(() => AbilityResourcePayment.Spend(world, runner,
                new(target, 0, [generator.ObjectId], 1, ""), events));
            Assert.Empty(events);
            Assert.Equal(before, world.Digest().Canonical());
            Assert.True(generator.Ready);
        }
        else
        {
            var payment = AbilityResourcePayment.Spend(world, runner,
                new(target, 0, [generator.ObjectId], 1, ""), []);
            Assert.True(payment.IsComplete);
            Assert.False(generator.Ready);
        }
    }

    [Fact]
    public void ProgrammaticPriceRestrictionCannotBypassAdmission()
    {
        var book = AbilityCatalog.Parse("""
            {"cards":[{"card":"10002","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "effect":{"draw":{"player":"you","count":1}}}]}]}
            """);
        var invalid = book.Abilities[0] with { PrintedCostResource = 'G' };
        Assert.Throws<AbilityException>(() => AbilityLowering.Book(book with { Abilities = [invalid] }));
        invalid = book.Abilities[0] with
        {
            PrintedCostResource = 'R',
            Trigger = book.Abilities[0].Trigger with { Timing = Marvel.Rules.Timing.AbilityType.Resource },
        };
        Assert.Throws<AbilityException>(() => AbilityLowering.Book(book with { Abilities = [invalid] }));
    }

    [Theory]
    [InlineData("Action", "\"R\"")]
    [InlineData("Resource", "\"R\"")]
    [InlineData("Action", "\"G\"")]
    [InlineData("Action", "null")]
    [InlineData("Action", "1")]
    public void PrintedPriceRestrictionAdmissionIsStrict(string timing, string resource)
    {
        string json = $$$$"""
            {"cards":[{"card":"10002","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"{{{{timing}}}}","subject":"game"},
              "printedCostResource":{{{{resource}}}},"effect":{"generate":"R"}}]}]}
            """;
        if (timing == "Action" && resource == "\"R\"")
            Assert.Equal('R', Assert.Single(AbilityLowering.Book(AbilityCatalog.Parse(json)).Abilities).PrintedCostResource);
        else
            Assert.Throws<AbilityException>(() => AbilityLowering.Book(AbilityCatalog.Parse(json)));
    }
}
