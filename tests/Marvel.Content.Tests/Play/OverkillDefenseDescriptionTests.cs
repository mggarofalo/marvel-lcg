using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class OverkillDefenseDescriptionTests
{
    [Theory]
    [InlineData(true, 0, "Spider-Man")]
    [InlineData(false, 0, "Spider-Man")]
    [InlineData(true, 1, "Carol Danvers")]
    [InlineData(false, 1, "Carol Danvers")]
    [Rule("rr:overkill.1")]
    public void DefenseWarnsAboutActiveOverkillAndNamesTheAllysController(
        bool charged, int controller, string recipient)
    {
        // "If this attack defeats an ally, deal the damage beyond its hit points
        // to the identity of that ally's controller." Charge grants this keyword
        // before Spider-Sense and defender selection; unrevealed boosts stay unknown.
        var (_, world) = Playing(_ => { }, hero: true,
            heroes: controller == 0 ? ["spider_man"] : ["spider_man", "captain_marvel"]);
        var runner = AuthoredCards.Runner();
        Card ally = world.Cards.First(card => card.FaceId == "01059");
        World.MoveToTop(ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(controller)));
        if (charged)
            Reveal.Resolve(world, world.Facts,
                world.Cards.First(card => card.FaceId == AuthoredCards.Charge), 0, []);
        Card rhino = world.TheCardIn(DeckType.VillainArea)!;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), Round: 1, Number: 2, Subject: rhino.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        Prompt sense = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(charged, sense.Description!.Contains("Overkill:", StringComparison.Ordinal));
        Sequence.Answer(world, world.Facts, runner, sense, Decision.Decline, events);
        Prompt defense = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(Question.Defender, defense.Asking);
        Assert.Equal(controller, defense.Player);
        Assert.Equal(charged, defense.Description!.Contains("Overkill:", StringComparison.Ordinal));
        string explanation = Assert.Single(defense.Affordances, option => option.Id == ally.ObjectId).Description!;
        Assert.Equal(charged, explanation.Contains("Overkill:", StringComparison.Ordinal));
        if (charged) Assert.Contains($"excess damage hits {recipient}", explanation);
        Assert.Contains("Boosts and later effects are unresolved", explanation);
        Assert.DoesNotContain(events, entry => entry is BoostResolved);
    }
}
