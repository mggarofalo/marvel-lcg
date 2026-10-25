using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class OtherVillainAttackRestrictionTests
{
    private sealed record Continuation : CardPowerContinuation;

    private const string Book = """
        {"cards":[{"card":"07035","abilities":[{
          "trigger":{"timing":"Constant"},
          "effect":{"grant":{"card":"attachedTo",
            "keyword":"other_villains_cannot_be_attacked","amount":1}}
        }]}]}
        """;

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AttachmentRestrictsEveryPlayerToItsHostAndLeavesMinionsLegal(int player)
    {
        // Printed 07035: "Players cannot attack other villains." Synthetic
        // constant-only program isolates this sentence from the card's response.
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var host = groups[2].Villain;
        world.CreateCard("07035", world.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId));
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(Book));
        var minion = world.CreateCard("01102", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;

        var legal = BasicPowers.Attackable(world, world.Facts, player);

        Assert.Equal(new[] { host.ObjectId, minion.ObjectId }.Order(), legal.Select(card => card.ObjectId).Order());
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.True(world.DamageAbilities.CanTakeDamage(world, groups[0].Villain, world.Seats[0].IdentityCard));
    }

    [Theory]
    [InlineData("hero")]
    [InlineData("ally")]
    [InlineData("labelled")]
    [InlineData("aggregate")]
    public void ProhibitedAttackIsRejectedBeforeExhaustionOrScheduling(string mode)
    {
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var host = groups[2].Villain;
        var other = groups[0].Villain;
        world.CreateCard("07035", world.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId));
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(Book));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var events = new List<Marvel.Rules.Events.GameEvent>();
        Assert.Throws<RulesNotImplementedException>(() =>
        {
            if (mode == "hero") BasicPowers.BasicAttack(world, world.Facts, 0, other, events);
            else if (mode == "ally") BasicPowers.AllyPower(world, world.Facts, ally, other, BasicPowers.AttackVerb, events);
            else BasicPowerInitiation.CardAttack(world, world.Facts,
                new CardPowerInvocation(0, world.Seats[0].IdentityCard,
                    mode == "aggregate" ? host : other, 1, "synthetic attack")
                {
                    Targets = mode == "aggregate" ? [host, other] : default,
                    Continuation = mode == "aggregate" ? new Continuation() : null,
                }, events);
        });
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Empty(events);
        Assert.True(ally.Ready);
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.Null(world.CharacterAttack);
        Assert.Null(world.Agenda.Current);
    }

    [Fact]
    public void DiscardAndReentryFollowTheCurrentHostAndGuardStillWins()
    {
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var host = groups[2].Villain;
        var attachment = world.CreateCard("07035", world.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId));
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(Book));
        Assert.Equal(host, Assert.Single(BasicPowers.Attackable(world, world.Facts, 0)));
        // Synthetic zone transition: the fixture's encounter groups contain no
        // members, so ordinary encounter-discard routing is outside this probe.
        World.MoveToTop(attachment, groups[2].Discard);
        Assert.Equal(4, BasicPowers.Attackable(world, world.Facts, 0).Count);
        var replacement = groups[1].Villain;
        World.MoveToTop(attachment, world.AreaOf(DeckType.UpgradesArea, replacement.Area.PlayArea, replacement.ObjectId));
        Assert.Equal(replacement, Assert.Single(BasicPowers.Attackable(world, world.Facts, 0)));
        var guard = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.Equal(guard, Assert.Single(BasicPowers.Attackable(world, world.Facts, 0)));
        Assert.Equal(replacement, Assert.Single(BasicPowers.Attackable(world, world.Facts, 1),
            card => CardKinds.IsVillain(EffectiveCards.Kind(card, world.Facts))));
    }

    [Theory]
    [InlineData("Piledriver", true)]
    [InlineData("Wrecker", false)]
    public void LabelledActionAdmissionRejectsOtherVillainsBeforeCosts(string title, bool offered)
    {
        var (world, groups, _) = WreckingProjectionFixture.Create();
        var host = groups[2].Villain;
        world.CreateCard("07035", world.AreaOf(DeckType.UpgradesArea, host.Area.PlayArea, host.ObjectId));
        string action = """
            ,{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "labels":["attack"],
              "cost":{"exhaust":"this"},
              "effect":{"attack":{"target":{"titled":"TITLE"},
                "effect":{"dealAttackDamage":{"cards":{"titled":"TITLE"},"amount":1}}}}
            }]}
            """.Replace("TITLE", title, StringComparison.Ordinal);
        var runner = new AbilityRunner(AbilityCatalog.Parse(Book[..^2] + action + "]}"));
        world.Abilities = runner;
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;
        var source = world.Cards.Single(card => card.FaceId == "01006");

        Assert.Equal(offered, runner.Actions(world, 0).Any(option => option.Card == source.ObjectId));
        Assert.True(source.Ready);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
    }
}
