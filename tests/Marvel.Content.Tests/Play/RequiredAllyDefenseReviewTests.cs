using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class RequiredAllyDefenseReviewTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:ally.5")]
    [Fact]
    public void AnAttributedOwnAllyDefenseSatisfiesTheRequirement()
    {
        // "Attacks, thwarts, defenses ... that resolve from allies ... are not
        // considered to be performed by that player's identity." This synthetic
        // ability is performed by the required ally, not the prohibited hero.
        var (world, ally) = Board();
        var performer = LabeledAbilities.Performer(world, Cards, 0, ally);

        Attack.BeginDefenseAbility(world, 0, performer);

        Assert.Equal(ally.ObjectId, world.Attack!.Defender);
        Assert.Equal(ally.ObjectId, world.Attack.Target);
        Assert.False(world.Attack.BasicDefense);
    }

    [Rule("rr:defend-defense.3.3")]
    [Fact]
    public void ACardCanDeclareAnExhaustedOwnAllyWhileAnotherReadyAllyMakesDefenseRequired()
    {
        // "A card ability that allows an ally to be declared as a defender
        // without exhausting can be used on an exhausted ally." A different
        // ready ally keeps the attack's ally restriction active throughout.
        var (world, ready) = Board();
        var exhausted = world.CreateCard("01041",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        exhausted.Exhaust();
        var foreign = world.CreateCard("01067",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));

        Assert.True(Attack.CanDeclareByAbility(world, Cards, exhausted));
        Assert.False(Attack.CanDeclareByAbility(world, Cards, foreign));
        Attack.DeclareByAbility(world, Cards, exhausted);

        Assert.Equal(exhausted.ObjectId, world.Attack!.Defender);
        Assert.False(exhausted.Ready);
        Assert.True(ready.Ready);
        Assert.True(foreign.Ready);
    }

    [Rule("rr:support.3")]
    [Fact]
    public void ASupportDefenseLeavesTheRequiredAllyAvailableForDeclaration()
    {
        // "Defenses ... that resolve from supports ... are not considered to be
        // performed by that player's identity." A support does not establish a
        // character defender, so the attacked player can still fulfill 07051.
        var (world, ally) = Board();
        var support = world.CreateCard("01091",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var performer = LabeledAbilities.Performer(world, Cards, 0, support);

        Attack.BeginDefenseAbility(world, 0, performer);

        Assert.False(world.Attack!.IsDefended);
        Assert.True(Attack.CanDeclareByAbility(world, Cards, ally));
        Assert.False(Attack.CanDeclareByAbility(world, Cards, world.Seats[0].IdentityCard));
    }

    [Theory]
    [InlineData(true, "01011")]
    [InlineData(false, "01011")]
    [InlineData(true, "01091")]
    [InlineData(false, "01091")]
    public void OffersAndExecutionHonorAttributionForBothLabelForms(bool envelope, string face)
    {
        // Synthetic ally/support defenses test the admission boundary as well
        // as execution. Exhaustion is a committed cost, not a basic defense.
        var (world, ally) = Board();
        var source = face == "01011" ? ally : world.CreateCard(face,
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var drawn = world.CreateCard("01087", world.Seats[0].Deck);
        string labels = envelope ? "\"labels\":[\"defense\"]," : "";
        string body = "{\"draw\":{\"player\":\"you\",\"count\":1}}";
        string effect = envelope ? body : "{\"defense\":{\"effect\":" + body + "}}";
        var runner = new AbilityRunner(AbilityCatalog.Parse($$"""
            {"cards":[{"card":"{{face}}","abilities":[{
              "trigger":{"event":"WhenBoostCardGiven","timing":"Interrupt","subject":"game"},
              {{labels}} "cost":{"exhaust":"this"}, "effect":{{effect}}
            }]}]}
            """));
        world.Abilities = runner;
        var occurrence = new Occurrence(2, ["WhenBoostCardGiven"], Player: 0);

        var pending = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt),
            ability => ability.Card == source.ObjectId);
        runner.ResolveWindow(world, occurrence, pending, Decision.Take(pending.Card));

        Assert.False(source.Ready);
        Assert.Equal(DeckType.HandsArea, drawn.Area.Type);
        Assert.Equal(face == "01011" ? ally.ObjectId : -1, world.Attack!.Defender);
    }

    private static (World World, Card Ally) Board()
    {
        // Synthetic engine contract: the generic opcode is installed directly;
        // this does not admit Ramming Speed as executable expansion content.
        var world = new World(Cards, players: 2);
        for (int player = 0; player < 2; player++)
        {
            var seat = world.CreateSeat($"p{player}");
            seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        }
        var enemy = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var ally = world.CreateCard("01011",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.Attack = new EnemyAttack(enemy.ObjectId, 0, world.Seats[0].IdentityCard.ObjectId);
        world.Activation = new EnemyActivation(enemy.ObjectId, 0, Attacking: true, Id: 1);
        AttackAllyDefenseRequirement.Install(world, enemy);
        return (world, ally);
    }
}
