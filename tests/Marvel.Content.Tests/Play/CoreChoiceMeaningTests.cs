using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CoreChoiceMeaningTests : ChoosingCardsTestBase
{
    [Rule("rr:you-your.2")]
    [Fact]
    public void HydraOptionsNameTheResolvingIdentityAndActualMainSchemeWithoutPromisingPlacement()
    {
        // "the player resolving that damage applies it to the hit point dial
        // of their identity." Printed Core 01110 instructs
        // taking 2 damage or placing 1 threat on the main scheme.
        World world = Deal("spider_man", "she_hulk");
        var (bomber, _) = Reveal(world, AuthoredCards.HydraBomber, player: 1);
        Prompt prompt = AuthoredCards.Runner().Choosing(world, bomber, 1, 1)!;
        Affordance damage = prompt.Affordances.Single(option => option.Label == "dealDamage");
        Affordance threat = prompt.Affordances.Single(option => option.Label == "placeThreat");
        Assert.Equal([0, 1], prompt.Affordances.Select(option => option.Id));
        Assert.Equal("Take 2 damage: Jennifer Walters", damage.DisplayLabel);
        Assert.Equal(damage.DisplayLabel, damage.CommitLabel);
        Assert.Contains("15", damage.Description);
        Assert.Contains("13", damage.Description);
        Assert.DoesNotContain("Peter Parker", damage.Description);
        Assert.Contains("Place 1 threat on The Break-In!", threat.DisplayLabel);
        Assert.Contains("0/14", threat.Description);
        Assert.Contains("prevention can change", threat.Description);
        Assert.Null(prompt.Description);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AChosenPlayerDrawExplainsEachRecipientAndDrawsOnlyForThatPlayer(bool mansion)
    {
        // Printed Core Carol Danvers and Avengers Mansion both choose a
        // player to draw one card. This isolated canonical state is not native
        // gameplay evidence; no future deck face belongs in the explanation.
        World world = Deal("spider_man", "captain_marvel");
        AbilityRunner runner = AuthoredCards.Runner();
        world.Abilities = runner;
        Card source = mansion ? world.CreateCard("01091",
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1))
            : world.Seats[1].IdentityCard;
        var action = Assert.Single(runner.Actions(world, 1), candidate => candidate.Card == source.ObjectId);
        runner.Act(world, action, [], []);
        PhaseStep waiting = Assert.Single(world.Agenda.Outstanding);
        Prompt prompt = runner.Choosing(world, source, 1, waiting.Index, waiting.Tier)!;
        Assert.Contains("choose a player to draw 1 card", prompt.DisplayQuestion);
        Affordance chosen = Assert.Single(prompt.Affordances,
            option => option.AnchorId == world.Seats[0].IdentityCard.ObjectId);
        Assert.Equal("Spider-Man draws 1 card", chosen.CommitLabel);
        Assert.Contains("Spider-Man draws 1 card", chosen.Description);
        int first = world.Seats[0].Hand.Cards.Count;
        int second = world.Seats[1].Hand.Cards.Count;
        runner.Chose(world, source, 1, waiting.Index, Decision.Take(chosen.Id), waiting.Tier);
        Assert.Equal(first + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(second, world.Seats[1].Hand.Cards.Count);
    }

    [Fact]
    public void AnIdentityChoiceWithAnotherEffectDoesNotClaimADraw()
    {
        // Synthetic typed relation isolates the non-draw identity choice.
        World world = Deal("spider_man", "captain_marvel");
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [{ "card": "01110", "abilities": [{
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed", "subject": "this" },
              "effect": { "chooseCard": { "from": { "query": "identities" },
                "effect": { "giveStatus": { "card": "chosen", "status": "tough" } } } }
            }]}]}
            """));
        Card source = world.CreateCard("01110", world.AreaOf(DeckType.RevealingArea));
        runner.WhenRevealed(world, source, 0);
        PhaseStep waiting = Assert.Single(world.Agenda.Outstanding);
        Prompt prompt = runner.Choosing(world, source, 0, waiting.Index, waiting.Tier)!;
        Assert.All(prompt.Affordances, option => Assert.DoesNotContain("draws", option.Description));
    }
}
