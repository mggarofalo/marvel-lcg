using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MomentOfTriumphActorTests
{
    [Theory]
    [InlineData("identity", 0, true)]
    [InlineData("identity", 1, true)]
    [InlineData("other", 0, false)]
    [InlineData("ally", 0, false)]
    [Rule("rr:you-your.15")]
    [Rule("rr:overkill.3")]
    public void ResponseRequiresThisIdentitysAttack(string actor, int controller, bool expected)
    {
        // Ally abilities "are not considered to be performed by that player's identity".
        // An excess-counting ability uses the "same value of excess damage" as overkill.
        var controlBook = AbilityCatalog.Parse("""
            {"cards":[{"card":"01003","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"Response","target":"enemy","form":"hero"},
              "effect":{"placeCounters":{"card":"you","counter":"control","count":1}}
            }]}]}
            """);
        var runner = new AbilityRunner(controlBook with
        {
            Abilities = [.. controlBook.Abilities, .. AuthoredCards.Book.On("12030")],
            Authored = controlBook.Authored.Append("12030").ToHashSet(StringComparer.Ordinal),
        });
        var world = ThorActualFixture.Board("06001a", 2);
        world.Abilities = runner;
        var hero = world.Seats[controller].IdentityCard;
        hero.TakeDamage(4);
        var moment = world.CreateCard("12030", world.Seats[controller].Hand);
        var control = world.CreateCard("01003", world.Seats[controller].Hand);
        var enemy = world.CreateCard("01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(controller)));
        enemy.TakeDamage(DamagePlacement.Health(world, world.Facts, enemy) - 1);
        if (actor == "ally")
        {
            var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(controller), cardOwner: controller));
            world.Effects.Register(new(EffectSource.LastingEffect, "attack", 1, Affects: ally.ObjectId));
            AllyBasicPowers.AllyPower(world, world.Facts, ally, enemy, BasicPowers.AttackVerb, []);
        }
        else BasicPowerInitiation.BasicAttack(world, world.Facts, actor == "other" ? 1 - controller : controller, enemy, []);
        bool sawControl = false;
        bool sawMoment = false;
        for (int step = 0; step < 20; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            sawControl |= HasOrdinaryControl(prompt, control.ObjectId);
            var response = prompt.Affordances.SingleOrDefault(option => option.AnchorId == moment.ObjectId);
            if (response is not null)
            {
                Assert.Equal(controller, prompt.Player);
                Assert.Equal([enemy.ObjectId], response.ContextCardIds);
                Assert.Contains("up to 1", response.Description);
                sawMoment = true;
            }
            Sequence.Answer(world, world.Facts, runner, prompt,
                response is null ? Decision.Decline : Decision.Take(response.Id), []);
        }
        Assert.True(sawControl);
        Assert.Equal(expected, sawMoment);
        Assert.Equal(DeckType.EncounterDiscardPile, enemy.Area.Type);
        Assert.Equal(expected ? DeckType.DiscardPile : DeckType.HandsArea, moment.Area.Type);
        Assert.Equal(expected ? 3 : 4, hero.Damage);
    }

    private static bool HasOrdinaryControl(Prompt prompt, int card)
    {
        var offer = prompt.Affordances.SingleOrDefault(option => option.AnchorId == card);
        if (offer is null) return false;
        Assert.DoesNotContain("Heal up to", offer.Description ?? "");
        return true;
    }
}
