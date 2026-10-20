using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HitPointAssignmentAdmissionTests
{
    [Fact]
    public void AssignmentMakesFollowingLethalStageEntryAnUnknownReadBeforeCost()
    {
        // Synthetic labelled attack: assigning the villain's dial can make the next hit promote a stage.
        var runner = ActionAbilitySupportFixtures.Runner(AuthoredCards.AuntMay, "Action", """
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[
              {"setHitPoints":{"card":{"query":"villain"},"amount":1}},
              {"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}},
              {"dealAttackDamage":{"cards":{"query":"villain"},"amount":{"remainingHealth":{"query":"villain"}}}}
            ]}}}
            """, cost: """{"exhaust":"this"}""", includeAuthored: true);
        Card? source = null;
        Card? villain = null;
        var error = Assert.Throws<RulesNotImplementedException>(() => ActionAbilitySupportFixtures6.Playing(world =>
        {
            villain = world.TheCardIn(DeckType.VillainArea);
            source = world.CreateCard(AuthoredCards.AuntMay, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, hero: true, abilities: runner));
        Assert.Contains("unprojected", error.Message);
        Assert.True(source!.Ready);
        Assert.Equal("01094", villain!.FaceId);
        Assert.Equal(0, villain.Damage);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(9, false)]
    public void DependentDiscardRequiresAnActualDialChange(long damage, bool offered)
    {
        var runner = ActionAbilitySupportFixtures.Runner(AuthoredCards.AuntMay, "Action", """
            {"then":{"effect":{"setHitPoints":{"card":"you","amount":1}},"then":{"discard":"this"}}}
            """);
        Card? source = null;
        var (game, world) = ActionAbilitySupportFixtures6.Playing(board =>
        {
            board.Seats[0].IdentityCard.TakeDamage(damage);
            source = board.CreateCard(AuthoredCards.AuntMay, board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, abilities: runner);
        var options = game.Pending!.Affordances.Where(item => item.AnchorId == source!.ObjectId && item.Verb == Game.ActionVerb);
        if (!offered)
        {
            Assert.Empty(options);
            Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
            Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
            return;
        }
        game.Resolve(Decision.Take(Assert.Single(options).Id));
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
    }

    [Theory]
    [InlineData("{\"eachPlayer\":{\"effect\":BODY}}")]
    [InlineData("{\"forEach\":{\"count\":2,\"effect\":BODY}}")]
    [InlineData("{\"eachTime\":{\"effect\":BODY,\"when\":{\"titleInPlay\":\"Spider-Man\"},\"then\":{\"draw\":{\"player\":\"you\",\"count\":1}}}}")]
    public void RepeatedAssignmentRefusesBeforeExhaustionOrHealthChanges(string wrapper)
    {
        // Synthetic future shape: repeated damage transfers do not represent an absolute dial assignment.
        string effect = wrapper.Replace("BODY", """{"setHitPoints":{"card":"you","amount":1}}""", StringComparison.Ordinal);
        var runner = ActionAbilitySupportFixtures.Runner(AuthoredCards.AuntMay, "Action", effect,
            cost: """{"exhaust":"this"}""");
        Card? source = null;
        Card? identity = null;
        var error = Assert.Throws<RulesNotImplementedException>(() => ActionAbilitySupportFixtures6.Playing(world =>
        {
            identity = world.Seats[0].IdentityCard;
            identity.TakeDamage(2);
            source = world.CreateCard(AuthoredCards.AuntMay, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, abilities: runner));
        Assert.Contains("inside repeated frames", error.Message);
        Assert.True(source!.Ready);
        Assert.Equal(2, identity!.Damage);
    }

    [Fact]
    public void ImpossibleAssignmentRefusesBeforeExhaustion()
    {
        var runner = ActionAbilitySupportFixtures.Runner(AuthoredCards.AuntMay, "Action",
            """{"setHitPoints":{"card":"you","amount":100}}""", cost: """{"exhaust":"this"}""");
        Card? source = null;
        var error = Assert.Throws<RulesNotImplementedException>(() => ActionAbilitySupportFixtures6.Playing(world =>
        {
            source = world.CreateCard(AuthoredCards.AuntMay, world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        }, abilities: runner));
        Assert.Contains("within the modified maximum", error.Message);
        Assert.True(source!.Ready);
    }
}
