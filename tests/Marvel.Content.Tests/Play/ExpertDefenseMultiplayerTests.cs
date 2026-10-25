using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ExpertDefenseMultiplayerTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:defend-defense.4.7")]
    [Rule("rr:defend-defense.5")]
    public void OnlyTheDefendingHeroCanUseItsOwnersEvent(bool ally, bool foreignEvent)
    {
        // "the defending player becomes the new target of that attack."
        // With an ally defending, "the player's identity does not become the defender."
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, players: 2);
        var first = world.CreateSeat("Peter Parker");
        first.IdentityCard = world.CreateCard("01001a", first.Hero);
        var second = world.CreateSeat("Carol Danvers");
        second.IdentityCard = world.CreateCard("01010a", second.Hero);
        foreach (var seat in world.Seats) world.CreateCard("01088", seat.Deck);
        var defender = ally ? world.CreateCard("01076",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1)) : second.IdentityCard;
        var expert = world.CreateCard("03033", world.Seats[foreignEvent ? 0 : 1].Hand);
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            "attack", Amount: 4, Affects: villain.ObjectId));
        for (int index = 0; index < 3; index++) world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1, Subject: villain.ObjectId, Seat: 0));
        int played = Defend(world, defender, expert, allowed: !ally && !foreignEvent);
        Assert.Equal(!ally && !foreignEvent ? 1 : 0, played);
        Assert.Equal(0, first.IdentityCard.Damage);
        Assert.Equal(ally ? 0 : foreignEvent ? 6 : 3, second.IdentityCard.Damage);
        Assert.Equal(played == 1 ? DeckType.DiscardPile : DeckType.HandsArea, expert.Area.Type);
        Assert.Equal(1, StateFields.Modified(world, second.IdentityCard, "defense", cards, 2));
    }
    private static int Defend(World world, Card defender, Card expert, bool allowed)
    {
        int played = 0;
        bool selectedDefender = false;
        for (int decisions = 0; ; decisions++)
        {
            var prompt = Sequence.Work(world, world.Facts, world.Abilities, []);
            if (prompt is null) break;
            Assert.True(decisions < 20, prompt.Label);
            var answer = Decision.Decline;
            if (prompt.Asking == Question.Defender && prompt.Affordances.Any(offer => offer.Id == defender.ObjectId))
            {
                answer = Decision.Take(defender.ObjectId);
                selectedDefender = true;
            }
            var offer = prompt.Affordances.FirstOrDefault(option => option.AnchorId == expert.ObjectId);
            if (offer is not null)
            {
                Assert.True(allowed);
                Assert.Equal(1, prompt.Player);
                answer = Decision.Take(offer.Id);
                played++;
            }
            Sequence.Answer(world, world.Facts, world.Abilities, prompt, answer, []);
        }
        Assert.True(selectedDefender);
        return played;
    }

}
