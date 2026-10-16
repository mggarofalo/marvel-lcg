using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class NestedAttackActivityTests
{
    [Fact]
    public void CounterPunchOwnsItsDamageAndTheOuterAttackEndsAfterIt()
    {
        // Engine contract choice: nested attacks keep separate event ownership through response windows.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, players: 1);
        var seat = world.CreateSeat("p0");
        var hero = seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        var enemy = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var counterPunch = world.CreateCard("01077", seat.Hand);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Activation = new(enemy.ObjectId, 0, true, 7);
        world.Attack = new(enemy.ObjectId, 0, hero.ObjectId, Defender: hero.ObjectId, BasicDefense: true);
        world.Agenda.Add(new PhaseStep(new AttackCompletionOperation(), Round: 1, Number: 2, Subject: enemy.ObjectId, Seat: 0) { ActivationId = 7 });
        List<GameEvent> events = [];
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, cards, runner, events));
        var offered = Assert.Single(prompt.Affordances, option => option.AnchorId == counterPunch.ObjectId);
        var outer = Assert.IsType<ActiveAttack>(ActiveAttack.From(world)).Identity;
        Sequence.Answer(world, cards, runner, prompt, Decision.Take(offered.Id), events);
        events.AddRange(Agendas.Finish(world, cards, runner));
        var nested = Assert.Single(events.OfType<AttackBoundary>(), boundary => boundary.Started && !boundary.Identity.EnemyActivation);
        var damage = Assert.Single(events.OfType<FieldSet>(), entry => entry.Card == enemy.ObjectId && entry.Field == "health");
        Assert.Equal(nested.Identity, damage.AttackIdentity);
        int nestedEnd = events.FindIndex(entry => entry is AttackBoundary { Started: false } boundary && boundary.Identity == nested.Identity);
        int outerEnd = events.FindIndex(entry => entry is AttackBoundary { Started: false } boundary && boundary.Identity == outer);
        Assert.True(nestedEnd >= 0 && outerEnd > nestedEnd, $"outer={outer} nestedEnd={nestedEnd} outerEnd={outerEnd}\n" + string.Join("\n", events.OfType<AttackBoundary>()));
        foreach (var resumed in events.OfType<AttackBoundary>().Where(boundary => boundary.Identity == outer && boundary.Resumed))
            Assert.True(events.IndexOf(resumed) > nestedEnd);
    }
}
