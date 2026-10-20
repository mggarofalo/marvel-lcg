using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Printed-card resolution contracts in arranged worlds, not pack admission evidence.</summary>
public sealed class WreckingEnergyProjectilesTests : CoreCardTestBase
{
    [Theory]
    [InlineData("01001a")]
    [InlineData("01001b")]
    public void RevealDamagesOnlyResolvingPlayersCharactersInEitherForm(string identity)
    {
        // card:07027: "Deal 1 damage to each friendly character you control."
        var world = Hero(identity, 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var ally = Ally(world, 0);
        var otherAlly = Ally(world, 1);
        var villain = world.CreateCard("07017", world.AreaOf(DeckType.VillainArea));
        var source = world.CreateCard("07027", world.AreaOf(DeckType.EncounterDiscardPile));

        runner.ExecuteReveal(world, source, 0);
        Sequence.Finish(world, Cards, runner, []);

        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, ally.Damage);
        Assert.Equal(0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, otherAlly.Damage);
        Assert.Equal(0, villain.Damage);
    }

    [Rule("rr:defend-defense.3")]
    [Rule("rr:defend-defense.6")]
    [Theory]
    [InlineData("hero")]
    [InlineData("ally")]
    [InlineData("other-player")]
    [InlineData("undefended")]
    [InlineData("finished")]
    [InlineData("departed")]
    public void BoostUsesOnlyCurrentLiveDefender(string mode)
    {
        // card:07027: "Deal 1 damage to the defending character."
        // rr:defend-defense.3: "Damage from the attack is dealt to that ally."
        // rr:defend-defense.6: "If no character is used to defend ... that attack
        // is considered undefended."
        var world = Hero("01001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var hero = world.Seats[0].IdentityCard;
        var ally = Ally(world, 0);
        var otherAlly = Ally(world, 1);
        var villain = world.CreateCard("07017", world.AreaOf(DeckType.VillainArea));
        var source = world.CreateCard("07027", world.AreaOf(DeckType.BoostingArea));
        Card? defender = mode switch
        {
            "hero" => hero,
            "other-player" => otherAlly,
            "undefended" => null,
            _ => ally,
        };
        var attack = new EnemyAttack(villain.ObjectId, 0,
            defender?.ObjectId ?? hero.ObjectId, defender?.ObjectId ?? -1);
        SetAttack(world, attack, ally, mode);
        bool hasDefender = mode is "hero" or "ally" or "other-player";
        var context = new AbilityQueryContext(world, source, new Occurrence(0, []), 0,
            source.Incarnation, null, null, null, []);
        Assert.Same(hasDefender ? defender : null,
            AbilityCardQueries.Named(AbilityCardBinding.DefendingCharacter, context));

        runner.ExecuteBoost(world, source, 0);
        Sequence.Finish(world, Cards, runner, []);

        foreach (var character in new[] { hero, ally, otherAlly, world.Seats[1].IdentityCard, villain })
            Assert.Equal(hasDefender && character == defender ? 1 : 0, character.Damage);
    }

    private static Card Ally(World world, int player) =>
        world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(player), cardOwner: player));

    private static void SetAttack(World world, EnemyAttack attack, Card ally, string mode)
    {
        if (mode == "finished") world.FinishedAttack = attack;
        else world.Attack = attack;
        if (mode == "departed")
            World.MoveToTop(ally, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)));
    }
}
