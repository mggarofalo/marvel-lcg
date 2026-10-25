using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Canonical-book contracts in arranged worlds; no scenario admission claim.</summary>
public sealed class WreckingRemainingTreacheryTests
{
    [Theory]
    [InlineData("01001a")]
    [InlineData("01001b")]
    public void MysticalLinkPlacesTwoOnEverySideSchemeInEitherForm(string identity)
    {
        // 07015: "Place 2 threat on each side scheme."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07015", 0, identity);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var extra = world.CreateCard("01107", world.AreaOf(DeckType.SideSchemesArea));
        runner.ExecuteReveal(world, source, 0);
        Finish(world, runner);
        Assert.All(groups, group => Assert.Equal(2, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        Assert.Equal(2, extra.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void DeadMeatRanksRemainingHealthAcrossPlayersAndThreatRequiresActualDefeat(bool lethal, bool tough)
    {
        // 07016: "Deal 1 damage to the hero or ally with the fewest remaining hit points."
        // "If that character is defeated this way, place 3 threat on Wrecker's side scheme."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07016", 0, players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        world.CreateCard("01088", world.Seats[1].Deck);
        int initialDamage = lethal ? 2 : 1;
        ally.TakeDamage(initialDamage);
        if (tough) Statuses.Give(world, ally, Statuses.Tough);
        runner.ExecuteReveal(world, source, 0);
        Finish(world, runner);
        bool defeated = lethal && !tough;
        Assert.Equal(defeated ? DeckType.DiscardPile : DeckType.AlliesArea, ally.Area.Type);
        if (!defeated) Assert.Equal(initialDamage + (tough ? 0 : 1), ally.Damage);
        Assert.Equal(defeated ? 3 : 0, groups[0].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.All(groups.Skip(1), group => Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        Assert.All(world.Seats, seat => Assert.Equal(0, seat.IdentityCard.Damage));
        Assert.Same(groups[1], world.EncounterTopology!.Active);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public void LightningBlastThreatRequiresAnUndefendedThunderballAttack(bool defend, bool stunned)
    {
        // 07030: "Thunderball attacks you. If this attack is undefended, place 3 threat on Thunderball's side scheme."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07030", 1);
        world.EncounterTopology!.SelectActive(groups[0].Villain, "setup", []);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        if (stunned) Statuses.Give(world, groups[1].Villain, Statuses.Stunned);
        runner.ExecuteReveal(world, source, 0);
        Finish(world, runner, defend);
        Assert.Equal(!defend && !stunned ? 3 : 0, groups[1].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.All(groups.Where(group => group != groups[1]), group =>
            Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        Assert.Same(groups[0], world.EncounterTopology.Active);
        Assert.Equal(stunned ? 2 : 1, groups[0].Deck.Cards.Count);
        Assert.Equal(2, groups[1].Deck.Cards.Count);
    }

    [Fact]
    public void LightningBlastInAlterEgoPlacesThreatOnTheActualMostThreatScheme()
    {
        // 07030: "Place 3 threat on the side scheme with the most threat."
        var (world, groups, source) = WreckingEncounterCardFixture.Create("07030", 1, "01001b");
        groups[3].Scheme.PlaceTokens("k_threat", 4);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        runner.ExecuteReveal(world, source, 0);
        Finish(world, runner);
        Assert.Equal(7, groups[3].Scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.All(groups.Take(3), group => Assert.Equal(0, group.Scheme.Tokens.GetValueOrDefault("k_threat")));
        Assert.All(groups, group => Assert.Equal(2, group.Deck.Cards.Count));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    private static void Finish(World world, AbilityRunner runner, bool defend = false)
    {
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) return;
            Assert.True(step < 15);
            Assert.True(prompt.Cancellable, prompt.Description);
            var answer = defend && prompt.Asking == Question.Defender
                ? Decision.Take(world.Seats[0].IdentityCard.ObjectId) : Decision.Decline;
            Sequence.Answer(world, world.Facts, runner, prompt, answer, []);
        }
    }
}
