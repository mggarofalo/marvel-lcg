using System.Text.Json;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class InvocationActivationContinuationTests
{
    [Theory]
    [Rule("rr:stun-stunned.1")]
    [InlineData(false)]
    [InlineData(true)]
    public void AcceptedContextSurvivesAChoiceAndAnActivationBeforeTheNextChoice(bool master)
    {
        // Synthetic Special: two explicit choices bracket a scheduled enemy activation.
        const string special = """
            {"seq":[
              {"chooseCard":{"from":{"query":"identities"},"effect":{"seq":[
                {"enemyAttacks":{"enemies":{"query":"villain"}}},
                {"chooseCard":{"from":{"query":"identities"},"effect":{"draw":{"player":"you","count":1}}}}
              ]}}}
            ]}
            """;
        AbilityRunner runner = null!;
        var probe = InvocationInteractionFixture.Create("09034", captureRunner: value => runner = value,
            syntheticSpecial: special);
        var control = InvocationInteractionFixture.Create("09034", syntheticSpecial: special);
        foreach (var scene in new[] { probe, control })
        {
            int source = master ? scene.Master.ObjectId : scene.World.Seats[1].IdentityCard.ObjectId;
            var action = Assert.Single(scene.Game.Pending!.Affordances,
                item => item.AnchorId == source && item.Verb == Game.ActionVerb);
            scene.Game.Resolve(InvocationInteractionFixture.Pay(scene.World, action, scene.Deck.PublicTop!));
            Assert.Contains("Already paid: 1 resource", scene.Game.Pending!.Description);
            // "When this character would attack, remove each stunned status card from it instead."
            Statuses.Give(scene.World, scene.World.TheCardIn(DeckType.VillainArea)!, Statuses.Stunned);
            scene.Game.Resolve(Decision.Take(scene.World.Seats[0].IdentityCard.ObjectId));
            Assert.False(Statuses.Has(scene.World, scene.World.TheCardIn(DeckType.VillainArea)!, Statuses.Stunned));
        }

        var current = probe.World.Agenda.Current!.Value;
        string digest = probe.World.Digest().Canonical();
        string random = JsonSerializer.Serialize(probe.World.Random.Generator.GetState());
        var pending = probe.World.Agenda.Outstanding.ToArray();
        var lineage = EnclosingAbilityContinuations.For(probe.World.Agenda, current).ToArray();
        for (int count = 0; count < 3; count++)
        {
            var prompt = runner.Choosing(probe.World, current)!;
            Assert.Contains(master ? "Master of the Mystic Arts" : "Spell Mastery", prompt.Description);
            Assert.Contains("Already paid: 1 resource", prompt.Description);
            Assert.Equal(probe.Deck.PublicTop!.ObjectId, current.Subject);
            Assert.Equal(current, probe.World.Agenda.Current);
            Assert.Equal(lineage, EnclosingAbilityContinuations.For(probe.World.Agenda, current));
        }
        Assert.Equal(digest, probe.World.Digest().Canonical());
        Assert.Equal(random, JsonSerializer.Serialize(probe.World.Random.Generator.GetState()));
        Assert.Equal(pending, probe.World.Agenda.Outstanding);
        foreach (var scene in new[] { probe, control })
        {
            var invocation = scene.Deck.PublicTop!;
            var drawn = scene.World.Seats[1].Deck.Cards[^1];
            int held = scene.World.Seats[1].Hand.Cards.Count;
            int otherHeld = scene.World.Seats[0].Hand.Cards.Count;
            scene.Game.Resolve(Decision.Take(scene.World.Seats[1].IdentityCard.ObjectId));
            Assert.Equal(held + 1, scene.World.Seats[1].Hand.Cards.Count);
            Assert.Contains(drawn, scene.World.Seats[1].Hand.Cards);
            Assert.Equal(otherHeld, scene.World.Seats[0].Hand.Cards.Count);
            Assert.True(scene.Game.IsRootPrompt);
            Assert.Same(invocation, scene.Deck.PublicTop);
        }
        Assert.Equal(control.World.Digest().Canonical(), probe.World.Digest().Canonical());
        Assert.Equal(JsonSerializer.Serialize(control.Game.Pending), JsonSerializer.Serialize(probe.Game.Pending));
        Assert.Equal(JsonSerializer.Serialize(control.World.Random.Generator.GetState()),
            JsonSerializer.Serialize(probe.World.Random.Generator.GetState()));
        Assert.Throws<InvalidOperationException>(() => runner.Choosing(probe.World, current));
    }
}
