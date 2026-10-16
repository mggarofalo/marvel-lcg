using Marvel.Rules.Setup;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class SpiderManInterruptTests : SpiderManCardTestBase
{
    [Rule("rr:prevent")]
    [Rule("rr:prevent.1.2")]
    [Rule("rr:damage.step.3")]
    [Fact]
    public void BackflipPreventsTheImminentAttackDamage()
    {
        // "If an effect prevents all damage dealt to a character, that
        // character is not considered to have taken damage." Seven remains
        // the dealt amount while the taken amount becomes zero.
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var backflip = world.CreateCard(AuthoredCards.Backflip, world.Seats[0].Hand);
        var hero = world.Seats[0].IdentityCard;
        var occurrence = Occurrence.ForAttack(
            1,
            [Steps.DamageWouldBeDealt],
            world,
            Cards,
            world.TheCardIn(DeckType.VillainArea)!.ObjectId,
            hero.ObjectId,
            player: 0);
        var ability = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            pending => pending.Card == backflip.ObjectId);

        runner.ResolveWindow(world, occurrence, ability, new Decision(ability.Card, [], []));

        DamageReplacement replacement = DamagePlacement.Replace(world,
            new DamageRequest(hero, hero, 7, Steps.DamageWouldBeDealt, "Damage"), []);
        Assert.True(replacement.IsComplete);
        Assert.Equal(7, replacement.ResolvedAmount);
        Assert.Equal(0, runner.WouldTake(world, hero, hero, 7, []));
        Assert.Equal(DeckType.DiscardPile, backflip.Area.Type);
        Assert.Equal(Steps.EventPlayed, world.Agenda.Current!.Value.What);
        Assert.Equal(backflip.ObjectId, world.Agenda.Occurrence!.Subject);
        Assert.True(world.Agenda.Occurrence.Is(Steps.CardPlayed));
        Assert.False(world.Agenda.Occurrence.Is(Steps.DamageWouldBeDealt));
    }

    [Rule("rr:cancel.4")]
    [Fact]
    public void EnhancedSpiderSenseCancelsOnlyTheTreacheriesWhenRevealedEffect()
    {
        var world = Deal(hero: true);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var sense = world.CreateCard(AuthoredCards.EnhancedSpiderSense, world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        var treachery = world.CreateCard(
            AuthoredCards.ImTough, world.AreaOf(DeckType.RevealingArea));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var occurrence = new Occurrence(
            1, [Steps.CardRevealed], Subject: treachery.ObjectId, Player: 0);
        var ability = Assert.Single(
            runner.Waiting(world, occurrence, WindowKind.Interrupt),
            pending => pending.Card == sense.ObjectId);

        Assert.Equal(
            "Cancel the revealed treachery's When Revealed effects",
            runner.Describe(world, ability).Description);

        runner.ResolveWindow(world, occurrence, ability, new Decision(ability.Card, [], [payment.ObjectId]));
        var canceled = Assert.Single(runner.ExecuteReveal(world, treachery, 0).Events
            .OfType<Marvel.Rules.Events.WhenRevealedCanceled>());
        Assert.Equal(treachery.ObjectId, canceled.Card);
        Assert.Equal(sense.ObjectId, canceled.Source);

        Assert.False(Statuses.Has(world, villain, Statuses.Tough));
        Assert.Equal(DeckType.RevealingArea, treachery.Area.Type);
        Assert.Equal(DeckType.DiscardPile, sense.Area.Type);
    }

    [Rule("rr:when-revealed-abilities.3")]
    [Theory]
    [InlineData(DeckType.VillainArea)]
    [InlineData(DeckType.MainSchemesArea)]
    public void VillainAndMainSchemeWhenRevealedAbilitiesCannotBeCanceled(
        DeckType area)
    {
        var world = Deal(hero: true);
        var card = world.TheCardIn(area)!;
        var runner = new Marvel.Cards.Run.AbilityRunner(
            Marvel.Cards.Dsl.AbilityCatalog.Parse(
                $$"""
                { "cards": [ { "card": "{{card.FaceId}}", "abilities": [ {
                    "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed", "subject": "this" },
                    "effect": { "draw": { "player": "you", "count": 1 } }
                } ] } ] }
                """));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: "cancelWhenRevealed",
            Affects: card.ObjectId,
            Lasts: new Duration(Uses: 1)));
        int held = world.Seats[0].Hand.Cards.Count;

        runner.ExecuteReveal(world, card, 0);

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Contains(
            world.Effects.Active(), effect => effect.Affects == card.ObjectId);
    }

}
