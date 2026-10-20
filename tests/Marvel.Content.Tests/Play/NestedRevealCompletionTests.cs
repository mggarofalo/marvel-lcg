using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class NestedRevealCompletionTests : CoreCardTestBase
{
    [Rule("rr:reveal.step.4")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LaterTextWaitsForRevealEvenWhenTheChildTextIsCanceled(bool canceled)
    {
        // Synthetic composition: a reveal followed by tough.
        // rr:reveal.step.4: "this same resolution procedure applies" to card-text reveals.
        var world = Hero("01001a");
        var source = world.CreateCard("01190", world.AreaOf(DeckType.RevealingArea));
        var deck = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("01121", deck);
        var child = world.CreateCard("01188", deck);
        var runner = Runner("{\"revealTop\":1}");
        if (canceled)
            world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
                Kind: "cancelWhenRevealed", Affects: child.ObjectId, Lasts: new Duration(Uses: 1)));

        runner.ExecuteReveal(world, source, 0);

        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(DeckType.RevealingArea, child.Area.Type);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(canceled ? 0 : 1, world.Seats[0].IdentityCard.Damage);
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(DeckType.EncounterDiscardPile, child.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void RevealInstructionShapesAdvertisePossibleChildSuspensionWithoutInspectingDecks()
    {
        var (world, _, scope) = WreckingProjectionFixture.Create();
        AbilityEffect[] effects = [
            new AbilityEffect.CardAction(AbilityCardInstruction.Reveal, new AbilityCardSelection.Bound(AbilityCardBinding.Chosen)),
            new AbilityEffect.Fixed(AbilityFixedInstruction.RevealTop),
            new AbilityEffect.RevealAuxiliaryTop(AuxiliaryDeckKind.SideSchemes),
            new AbilityEffect.Search("01188", [AbilitySearchArea.EncounterDeck]),
            new AbilityEffect.DiscardUntil(CardKind.Treachery, null, AbilityDiscardUntilResult.Reveal)
        ];
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
        Assert.All(effects, effect => Assert.True(AbilityPowerSuspension.SuspendsPowerEffect(effect, scope)));
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
    }

    [Fact]
    public void UnsupportedDependentRevealRefusesBeforeMutation()
    {
        var world = Hero("01001a");
        var source = world.CreateCard("01190", world.AreaOf(DeckType.RevealingArea));
        var runner = Runner("{\"reveal\":{\"query\":\"yourAsideMinion\"}}", dependent: true);
        var before = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteReveal(world, source, 0));
        Assert.Equal(before, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed));
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static AbilityRunner Runner(string reveal, bool dependent = false) => new(AbilityCatalog.Parse("""
    {"cards":[
      {"card":"01190","abilities":[{
        "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
        "effect":EFFECT
      }]},
      {"card":"01188","abilities":[{
        "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
        "effect":{"dealDamage":{"cards":"you","amount":1}}
      }]}
    ]}
    """.Replace("EFFECT", dependent
        ? "{\"then\":{\"effect\":" + reveal + ",\"then\":{\"giveStatus\":{\"card\":\"you\",\"status\":\"tough\"}}}}"
        : "{\"seq\":[" + reveal + ",{\"giveStatus\":{\"card\":\"you\",\"status\":\"tough\"}}]}",
        StringComparison.Ordinal)));
}
